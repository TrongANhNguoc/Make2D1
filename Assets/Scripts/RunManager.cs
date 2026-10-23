using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    private const string UnlockedBiomeKey = "Biome_UnlockedCount";

    public static RunManager Instance { get; private set; }

    [Header("Biomes")]
    [Tooltip("Danh sách biome theo thứ tự mở khóa. Biome đầu luôn mở; thắng biome mới nhất thì mở biome kế")]
    [SerializeField] private BiomeData[] biomes;

    [Header("Stage Progress")]
    [Tooltip("Tự chuyển ải khi player đạt targetHeight (ngoài cách chui qua Portal)")]
    [SerializeField] private bool autoAdvanceByHeight = false;
    [SerializeField] private float advanceCooldown = 0.5f;

    [Header("Terrain")]
    [Tooltip("Tên object gốc trong scene chứa địa hình mặc định, dùng cho ải không có terrainPrefab (bị ẩn khi ải dùng prefab)")]
    [SerializeField] private string defaultTerrainName = "Grid";

    [Header("Revive")]
    [Tooltip("Số giây cộng thêm cho ải có giới hạn thời gian khi hồi sinh sau lúc hết giờ")]
    [SerializeField] private float reviveExtraSeconds = 15f;

    private Transform playerTransform;
    private Vector3 startPosition;
    private float stageStartX;
    private float stageStartY;
    private StageTerrainLoader terrain;
    private float lastAdvanceTime = float.NegativeInfinity;
    private bool runActive;
    private bool restartPending;
    private bool timedOut;
    private bool rewardGiven;

    public float StageTimeRemaining { get; private set; }
    public bool HasTimeLimit => CurrentLevel != null && CurrentLevel.timeLimit > 0f;
    // Fruit Coins nhận được ở cuối run gần nhất (đã nhân ×2 nếu thắng)
    public int LastRunReward { get; private set; }

    // 0-based: 0 = ải 1 ... 3 = ải 4
    public int CurrentStageIndex { get; private set; }
    public BiomeData CurrentBiome { get; private set; }
    public BiomeData NextBiome { get; private set; }
    public BiomeData JustUnlockedBiome { get; private set; }
    public LevelConfig CurrentLevel => CurrentBiome != null ? CurrentBiome.GetLevel(CurrentStageIndex) : null;
    public float StageStartX => stageStartX;
    public float StageStartY => stageStartY;
    // Terrain của ải có child StartPoint / PortalPoint hay không
    public bool HasStartPoint => terrain != null && terrain.StartPoint != null;
    public bool HasPortalPoint => terrain != null && terrain.PortalPoint != null;
    public Vector3 PortalPointPosition => HasPortalPoint ? terrain.PortalPoint.position : Vector3.zero;
    // Có PortalPoint thì độ cao đích = y của PortalPoint, không thì StageStartY + targetHeight
    public float TargetY
    {
        get
        {
            if (CurrentLevel == null) return stageStartY;
            return HasPortalPoint ? terrain.PortalPoint.position.y : stageStartY + CurrentLevel.targetHeight;
        }
    }
    public int UnlockedBiomeCount { get; private set; } = 1;

    public event Action<BiomeData> OnRunStarted;
    public event Action<BiomeData, LevelConfig> OnStageChanged;
    public event Action<BiomeData> OnVictory;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        terrain = new StageTerrainLoader(defaultTerrainName);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        LoadUnlockedCount();
    }

    private void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleGameStateChanged;
        StartRun();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        Instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        UpdateStageTimer();

        if (!autoAdvanceByHeight || CurrentLevel == null) return;

        Transform player = GetPlayer();
        if (player != null && player.position.y >= TargetY)
        {
            LoadNextStage();
        }
    }

    // Dùng deltaTime có scale: ngắm bắn slow-mo không bị trừ giờ như thời gian thật
    private void UpdateStageTimer()
    {
        if (!runActive || timedOut || !HasTimeLimit) return;

        StageTimeRemaining = Mathf.Max(0f, StageTimeRemaining - Time.deltaTime);
        if (StageTimeRemaining > 0f) return;

        PlayerCollision player = FindObjectOfType<PlayerCollision>();
        // Đang bất tử (vừa hồi sinh) thì Kill bị bỏ qua → chờ hết bất tử mới tính hết giờ, tránh kẹt timedOut
        if (player != null && player.IsInvincible) return;

        timedOut = true;
        if (player != null) player.Kill();
    }

    // Player hồi sinh: nếu chết do hết giờ ải thì mở lại đồng hồ và cộng thêm thời gian
    public void HandleRevive()
    {
        if (!timedOut) return;

        timedOut = false;
        StageTimeRemaining += reviveExtraSeconds;
    }

    // Phá đảo 4 ải: nhận ×2 Fruit Coins toàn trận. Thất bại: giữ 100% số coin đã nhặt
    private void AwardRunCoins(bool victory)
    {
        if (rewardGiven) return;
        rewardGiven = true;

        int coins = ScoreManager.Instance != null ? ScoreManager.Instance.FruitCoins : 0;
        if (victory) coins *= 2;

        LastRunReward = coins;
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.AddCoins(coins);
    }

    public bool IsBiomeUnlocked(int index)
    {
        return index >= 0 && index < UnlockedBiomeCount;
    }

    // Bắt đầu 1 run mới: ải 1 của biome đã chọn (lượt đầu = biome đầu tiên, lượt sau = biome đã random)
    public void StartRun()
    {
        BeginRun(false);
    }

    // Dùng cho nút "Chơi tiếp" sau Victory: run mới + đưa player về điểm xuất phát + chạy lại game
    public void StartNewRun()
    {
        BeginRun(true);
        if (GameManager.Instance != null) GameManager.Instance.StartGame();
    }

    // Chuẩn bị run mới (ải 1, đưa player về điểm xuất phát) nhưng CHƯA chạy game: dùng khi "Về menu" sau Victory,
    // lượt chơi bắt đầu khi người chơi chọn chế độ ở menu (GameManager.StartGame)
    public void PrepareNewRun()
    {
        BeginRun(true);
    }

    private void BeginRun(bool teleportPlayer)
    {
        if (biomes == null || biomes.Length == 0)
        {
            Debug.LogWarning("RunManager: chưa gán danh sách Biome.");
            return;
        }

        CurrentBiome = NextBiome != null ? NextBiome : biomes[0];
        NextBiome = null;
        JustUnlockedBiome = null;
        CurrentStageIndex = 0;
        runActive = true;
        restartPending = false;
        rewardGiven = false;
        LastRunReward = 0;

        ApplyBiome();
        BeginStage(teleportPlayer);

        OnRunStarted?.Invoke(CurrentBiome);
    }

    // Gọi khi đạt độ cao hoặc chui qua Portal
    public void LoadNextStage()
    {
        if (CurrentBiome == null || !runActive) return;
        if (Time.unscaledTime - lastAdvanceTime < advanceCooldown) return;
        lastAdvanceTime = Time.unscaledTime;

        if (CurrentStageIndex + 1 < CurrentBiome.StageCount)
        {
            CurrentStageIndex++;
            BeginStage(true);
        }
        else
        {
            CompleteRun();
        }
    }

    // Room-to-Room: mỗi ải bắt đầu lại từ điểm xuất phát của player
    private void BeginStage(bool teleportPlayer)
    {
        Transform player = GetPlayer();

        // Terrain phải xong trước khi teleport player và trước OnStageChanged (spawner raycast vào ground)
        terrain.Load(CurrentLevel);

        Vector3 start = terrain.StartPoint != null ? terrain.StartPoint.position : startPosition;

        // Có StartPoint thì luôn đưa player tới đó (kể cả lần vào đầu tiên / sau reload scene)
        if (player != null && (teleportPlayer || terrain.StartPoint != null)) TeleportPlayerToStart(player, start);

        stageStartX = start.x;
        stageStartY = (player != null || terrain.StartPoint != null) ? start.y : 0f;

        timedOut = false;
        StageTimeRemaining = HasTimeLimit ? CurrentLevel.timeLimit : 0f;

        if (ScoreManager.Instance != null) ScoreManager.Instance.BeginStage(stageStartY);
        OnStageChanged?.Invoke(CurrentBiome, CurrentLevel);
    }

    private void TeleportPlayerToStart(Transform player, Vector3 target)
    {
        target.z = player.position.z;
        Vector3 delta = target - player.position;
        player.position = target;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = target;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null) controller.ResetJumps();

        // Tránh camera trượt dài từ đỉnh ải cũ về điểm xuất phát
        CinemachineVirtualCamera vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam != null) vcam.OnTargetObjectWarped(player, delta);
    }

    private void CompleteRun()
    {
        Debug.Log("Victory! Hoàn thành biome " + CurrentBiome.biomeName);
        BiomeData finished = CurrentBiome;

        runActive = false;
        AwardRunCoins(true);
        UnlockNextBiome(finished);
        RollNextBiome();

        if (GameManager.Instance != null) GameManager.Instance.Victory();
        OnVictory?.Invoke(finished);
    }

    // Thắng biome mới nhất đang mở thì mở biome kế tiếp trong danh sách
    private void UnlockNextBiome(BiomeData finished)
    {
        int index = Array.IndexOf(biomes, finished);
        if (index < 0 || index + 1 != UnlockedBiomeCount || UnlockedBiomeCount >= biomes.Length) return;

        UnlockedBiomeCount++;
        PlayerPrefs.SetInt(UnlockedBiomeKey, UnlockedBiomeCount);
        PlayerPrefs.Save();
        JustUnlockedBiome = biomes[index + 1];
    }

    // Chọn biome random (trong số đã mở khóa) cho lượt chơi sau, cố tránh lặp lại biome vừa chơi
    private void RollNextBiome()
    {
        if (biomes == null || biomes.Length == 0) return;

        int pool = Mathf.Clamp(UnlockedBiomeCount, 1, biomes.Length);
        BiomeData picked = biomes[UnityEngine.Random.Range(0, pool)];
        if (pool > 1)
        {
            int guard = 0;
            while (picked == CurrentBiome && guard++ < 10)
            {
                picked = biomes[UnityEngine.Random.Range(0, pool)];
            }
        }

        NextBiome = picked;
    }

    private void LoadUnlockedCount()
    {
        int max = biomes != null ? Mathf.Max(1, biomes.Length) : 1;
        UnlockedBiomeCount = Mathf.Clamp(PlayerPrefs.GetInt(UnlockedBiomeKey, 1), 1, max);
    }

    [ContextMenu("Reset Biome Unlocks")]
    private void ResetBiomeUnlocks()
    {
        PlayerPrefs.DeleteKey(UnlockedBiomeKey);
        UnlockedBiomeCount = 1;
    }

    private void ApplyBiome()
    {
        Camera cam = Camera.main;
        if (cam != null && CurrentBiome != null)
        {
            cam.backgroundColor = CurrentBiome.backgroundColor;
        }
    }

    private Transform GetPlayer()
    {
        if (playerTransform == null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                playerTransform = pc.transform;
                startPosition = playerTransform.position;
            }
        }
        return playerTransform;
    }

    // Thua giữa chừng cũng kết thúc run: lượt sau random biome mới, về ải 1 khi reload
    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            AwardRunCoins(false);
            runActive = false;
            restartPending = true;
            RollNextBiome();
        }
        else if (state == GameState.Playing && restartPending)
        {
            StartRun();
        }
    }

    // Scene reload: player mới, camera mới → áp lại biome và điểm bắt đầu ải
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        playerTransform = null;
        if (CurrentBiome == null) return;

        ApplyBiome();
        BeginStage(false);
    }
}
