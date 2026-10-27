using System;
using System.Collections.Generic;
using UnityEngine;

public enum FruitType
{
    Apple,
    Banana,
    Cherry,
    Grape,
    Orange
}

// Cơ chế ải Speed Rush: mỗi vòng phải ăn đủ vài loại quả (mỗi loại 1 số lượng) trong thời gian giới hạn,
// xong thì random yêu cầu mới. Đủ số vòng thì qua ải. Hết giờ thì mất máu và sang vòng mới.
// Gắn vào 1 object trong scene. Chỉ hoạt động khi LevelConfig.fruitChallengeEnabled = true.
public class FruitChallengeManager : MonoBehaviour
{
    public static FruitChallengeManager Instance { get; private set; }

    [Header("Reward")]
    [Tooltip("Điểm cơ bản (nhân combo) khi hoàn thành 1 vòng")]
    [SerializeField] private int roundBonusPoints = 50;

    private readonly Dictionary<FruitType, int> needs = new Dictionary<FruitType, int>();
    private LevelConfig level;

    public bool IsActive { get; private set; }
    public float RoundTimeRemaining { get; private set; }
    public int RoundsCompleted { get; private set; }
    public float RoundTime => level != null ? level.fruitRoundTime : 0f;
    public int RoundsToClear => level != null ? level.fruitRoundsToClear : 0;
    public IReadOnlyDictionary<FruitType, int> Needs => needs;

    public event Action OnRoundChanged;   // vòng mới hoặc tiến độ ăn quả thay đổi
    public event Action OnRoundFailed;
    public event Action OnRoundCleared;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnStageChanged += HandleStageChanged;
            HandleStageChanged(RunManager.Instance.CurrentBiome, RunManager.Instance.CurrentLevel);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (RunManager.Instance != null) RunManager.Instance.OnStageChanged -= HandleStageChanged;
    }

    private void Update()
    {
        if (!IsActive) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        // deltaTime có scale: ngắm bắn slow-mo không bị trừ giờ như thời gian thật
        RoundTimeRemaining -= Time.deltaTime;
        if (RoundTimeRemaining <= 0f) FailRound();
    }

    private void HandleStageChanged(BiomeData biome, LevelConfig newLevel)
    {
        level = newLevel;
        needs.Clear();
        RoundsCompleted = 0;
        IsActive = level != null && level.fruitChallengeEnabled
                   && !(RunManager.Instance != null && RunManager.Instance.IsTutorial);

        if (IsActive) StartRound();
        else OnRoundChanged?.Invoke();
    }

    // Gọi từ coin.cs khi Player ăn quả
    public void OnFruitCollected(FruitType type)
    {
        if (!IsActive) return;
        if (!needs.TryGetValue(type, out int remaining) || remaining <= 0) return;

        needs[type] = remaining - 1;

        if (AllCollected())
        {
            ClearRound();
        }
        else
        {
            OnRoundChanged?.Invoke();
        }
    }

    // Spawner dùng để ưu tiên sinh đúng loại quả đang cần
    public bool TryGetNeededType(out FruitType type)
    {
        List<FruitType> wanted = new List<FruitType>();
        foreach (KeyValuePair<FruitType, int> pair in needs)
        {
            if (pair.Value > 0) wanted.Add(pair.Key);
        }

        if (!IsActive || wanted.Count == 0)
        {
            type = default;
            return false;
        }

        type = wanted[UnityEngine.Random.Range(0, wanted.Count)];
        return true;
    }

    private bool AllCollected()
    {
        foreach (int count in needs.Values)
        {
            if (count > 0) return false;
        }
        return true;
    }

    private void ClearRound()
    {
        RoundsCompleted++;
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddPoints(roundBonusPoints);
        OnRoundCleared?.Invoke();

        if (RoundsCompleted >= level.fruitRoundsToClear)
        {
            IsActive = false;
            needs.Clear();
            OnRoundChanged?.Invoke();
            if (RunManager.Instance != null) RunManager.Instance.LoadNextStage();
            return;
        }

        StartRound();
    }

    private void FailRound()
    {
        OnRoundFailed?.Invoke();

        if (level.fruitFailDamage > 0)
        {
            PlayerCollision player = FindObjectOfType<PlayerCollision>();
            if (player != null) player.TakeDamage(level.fruitFailDamage);
        }

        // Người chơi có thể đã chết do sát thương trên
        if (IsActive) StartRound();
    }

    // Hồi sinh: bắt đầu lại vòng hiện tại với đủ thời gian (giữ nguyên số vòng đã xong)
    public void RestartCurrentRound()
    {
        if (IsActive) StartRound();
    }

    // Random N loại quả khác nhau, mỗi loại 1..maxCount quả
    private void StartRound()
    {
        needs.Clear();

        List<FruitType> pool = new List<FruitType>((FruitType[])Enum.GetValues(typeof(FruitType)));
        int typeCount = Mathf.Clamp(level.fruitTypesPerRound, 1, pool.Count);

        for (int i = 0; i < typeCount; i++)
        {
            int pick = UnityEngine.Random.Range(0, pool.Count);
            needs[pool[pick]] = UnityEngine.Random.Range(1, Mathf.Max(1, level.fruitMaxCountPerType) + 1);
            pool.RemoveAt(pick);
        }

        RoundTimeRemaining = level.fruitRoundTime;
        OnRoundChanged?.Invoke();
    }
}
