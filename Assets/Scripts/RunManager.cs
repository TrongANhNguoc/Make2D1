using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    private const string UnlockedBiomeKey = "Biome_UnlockedCount";
    public const string TutorialDoneKey = "TutorialDone";

    public static RunManager Instance { get; private set; }

    [Header("Biomes")]
    [Tooltip("Danh sách biome theo thứ tự mở khóa. Biome đầu luôn mở; thắng biome mới nhất thì mở biome kế")]
    [SerializeField] private BiomeData[] biomes;

    [Header("Stage Progress")]
    [Tooltip("Tự chuyển ải khi player đạt targetHeight (ngoài cách chui qua Portal)")]
    [SerializeField] private bool autoAdvanceByHeight = false;
    [SerializeField] private float advanceCooldown = 0.5f;

    [Header("Tutorial")]
    [Tooltip("Ải hướng dẫn riêng (KHÔNG nằm trong BiomeData.levels). Trống hoặc chưa có terrainPrefab = bỏ qua Tutorial")]
    [SerializeField] private LevelConfig tutorialLevel;

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
    private bool hasCheckpoint;
    private Vector3 checkpointPosition;

    public float StageTimeRemaining { get; private set; }
    public bool HasTimeLimit => !IsTutorial && CurrentLevel != null && CurrentLevel.timeLimit > 0f;
    public int LastRunReward { get; private set; }

    public int CurrentStageIndex { get; private set; }
    public BiomeData CurrentBiome { get; private set; }
    public BiomeData NextBiome { get; private set; }
    public BiomeData JustUnlockedBiome { get; private set; }
    public LevelConfig CurrentLevel => IsTutorial ? tutorialLevel : (CurrentBiome != null ? CurrentBiome.GetLevel(CurrentStageIndex) : null);
    public bool IsTutorial { get; private set; }
    public bool HasTutorial => tutorialLevel != null && tutorialLevel.terrainPrefab != null;
    public static bool IsTutorialDone => PlayerPrefs.GetInt(TutorialDoneKey, 0) == 1;
    public bool ShouldPlayTutorial => HasTutorial && !IsTutorialDone;
    public float StageStartX => stageStartX;
    public float StageStartY => stageStartY;
    public bool HasStartPoint => terrain != null && terrain.StartPoint != null;
    public bool HasPortalPoint => terrain != null && terrain.PortalPoint != null;
    public Vector3 PortalPointPosition => HasPortalPoint ? terrain.PortalPoint.position : Vector3.zero;
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

    private void UpdateStageTimer()
    {
        if (!runActive || timedOut || !HasTimeLimit) return;

        StageTimeRemaining = Mathf.Max(0f, StageTimeRemaining - Time.deltaTime);
        if (StageTimeRemaining > 0f) return;

        PlayerCollision player = FindObjectOfType<PlayerCollision>();
        if (player != null && player.IsInvincible) return;

        timedOut = true;
        if (player != null) player.Kill();
    }

    public void HandleRevive()
    {
        if (!timedOut) return;

        timedOut = false;
        StageTimeRemaining += reviveExtraSeconds;
    }

    private void AwardRunCoins(bool victory)
    {
        if (rewardGiven || IsTutorial) return;
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

    public void StartRun()
    {
        BeginRun(false);
    }

    public void StartNewRun()
    {
        BeginRun(true);
        if (GameManager.Instance != null) GameManager.Instance.StartGame();
    }

    public void PrepareNewRun()
    {
        BeginRun(true);
    }

    public bool StartTutorial()
    {
        if (!HasTutorial)
        {
            Debug.LogWarning("RunManager: chưa gán tutorialLevel (hoặc LevelConfig chưa có terrainPrefab) → không vào Tutorial.");
            return false;
        }

        IsTutorial = true;
        runActive = false;
        restartPending = false;
        JustUnlockedBiome = null;
        CurrentStageIndex = 0;
        if (CurrentBiome == null && biomes != null && biomes.Length > 0) CurrentBiome = biomes[0];

        ApplyBiome();
        BeginStage(true);
        ResetPlayerState();
        return true;
    }

    public void SkipTutorial()
    {
        if (IsTutorial) CompleteTutorial();
    }

    private void CompleteTutorial()
    {
        PlayerPrefs.SetInt(TutorialDoneKey, 1);
        PlayerPrefs.Save();

        IsTutorial = false;
        BeginRun(true);
        ResetPlayerState();
    }

    private void ResetPlayerState()
    {
        Transform player = GetPlayer();
        PlayerCollision health = player != null ? player.GetComponent<PlayerCollision>() : null;
        if (health != null) health.RestoreFullHealth();
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
    }

    public void SetCheckpoint(Vector3 position)
    {
        hasCheckpoint = true;
        checkpointPosition = position;
    }

    public void RespawnPlayerAtCheckpoint()
    {
        Transform player = GetPlayer();
        if (player == null) return;

        Vector3 target = hasCheckpoint ? checkpointPosition : (terrain.StartPoint != null ? terrain.StartPoint.position : startPosition);
        TeleportPlayerToStart(player, target);
    }

    private void BeginRun(bool teleportPlayer)
    {
        if (biomes == null || biomes.Length == 0)
        {
            Debug.LogWarning("RunManager: chưa gán danh sách Biome.");
            return;
        }

        IsTutorial = false;
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

    public void LoadNextStage()
    {
        if (IsTutorial)
        {
            if (Time.unscaledTime - lastAdvanceTime < advanceCooldown) return;
            lastAdvanceTime = Time.unscaledTime;
            CompleteTutorial();
            return;
        }

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

    private void BeginStage(bool teleportPlayer)
    {
        Transform player = GetPlayer();

        terrain.Load(CurrentLevel);

        Vector3 start = terrain.StartPoint != null ? terrain.StartPoint.position : startPosition;

        if (player != null && (teleportPlayer || terrain.StartPoint != null)) TeleportPlayerToStart(player, start);

        stageStartX = start.x;
        stageStartY = (player != null || terrain.StartPoint != null) ? start.y : 0f;

        hasCheckpoint = false;
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

    private void UnlockNextBiome(BiomeData finished)
    {
        int index = Array.IndexOf(biomes, finished);
        if (index < 0 || index + 1 != UnlockedBiomeCount || UnlockedBiomeCount >= biomes.Length) return;

        UnlockedBiomeCount++;
        PlayerPrefs.SetInt(UnlockedBiomeKey, UnlockedBiomeCount);
        PlayerPrefs.Save();
        JustUnlockedBiome = biomes[index + 1];
    }

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

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        playerTransform = null;
        if (CurrentBiome == null) return;

        ApplyBiome();
        BeginStage(false);
    }
}
