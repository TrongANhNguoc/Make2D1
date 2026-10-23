using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Height Score")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float heightPointsPerUnit = 10f;

    [Header("Combo")]
    [SerializeField] private int comboStartJumps = 5;
    [SerializeField] private int comboMaxJumps = 10;
    [SerializeField] private float comboStartMultiplier = 1.5f;
    [SerializeField] private float comboMaxMultiplier = 2.0f;

    [Header("Economy")]
    [SerializeField] private int pointsPerFruitCoin = 100;

    private float startY;
    private float maxHeight;
    private int bankedHeightScore; // điểm độ cao của các ải đã qua
    private int actionScore;   // điểm ăn quả / diệt quái đã nhân combo
    private int comboJumps;

    public int ActionScore => actionScore;
    public int HeightScore => bankedHeightScore + Mathf.FloorToInt(maxHeight * heightPointsPerUnit);
    public int TotalScore => actionScore + HeightScore;
    public int ComboJumps => comboJumps;
    public float ComboMultiplier => GetComboMultiplier(comboJumps);
    public int FruitCoins => TotalScore / pointsPerFruitCoin;

    public event Action<int> OnScoreChanged;
    public event Action<int, float> OnComboChanged;

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
        ResetScore();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (playerTransform == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        float height = playerTransform.position.y - startY;
        if (height > maxHeight)
        {
            maxHeight = height;
            OnScoreChanged?.Invoke(TotalScore);
        }
    }

    public void ResetScore()
    {
        actionScore = 0;
        comboJumps = 0;
        maxHeight = 0f;
        bankedHeightScore = 0;
        if (playerTransform != null) startY = playerTransform.position.y;

        OnScoreChanged?.Invoke(TotalScore);
        OnComboChanged?.Invoke(comboJumps, ComboMultiplier);
    }

    // Sang ải mới: chốt điểm độ cao ải cũ rồi đo độ cao lại từ điểm xuất phát mới
    public void BeginStage(float newStartY)
    {
        bankedHeightScore = HeightScore;
        maxHeight = 0f;
        startY = newStartY;
        OnScoreChanged?.Invoke(TotalScore);
    }

    // Gọi khi ăn quả / diệt quái: điểm cơ bản được nhân với hệ số combo hiện tại
    public void AddPoints(int basePoints)
    {
        actionScore += Mathf.RoundToInt(basePoints * ComboMultiplier);
        OnScoreChanged?.Invoke(TotalScore);
    }

    // Gọi mỗi lần bắn/nhảy khi đang ở trên không
    public void RegisterJump()
    {
        comboJumps++;
        OnComboChanged?.Invoke(comboJumps, ComboMultiplier);
    }

    // Gọi khi chạm đất
    public void ResetCombo()
    {
        if (comboJumps == 0) return;
        comboJumps = 0;
        OnComboChanged?.Invoke(comboJumps, ComboMultiplier);
    }

    // <5 lần: x1.0; 5 lần: x1.5; tăng đều đến 10 lần: x2.0; trên 10 vẫn x2.0
    private float GetComboMultiplier(int jumps)
    {
        if (jumps < comboStartJumps) return 1f;
        if (jumps >= comboMaxJumps) return comboMaxMultiplier;

        float t = (float)(jumps - comboStartJumps) / (comboMaxJumps - comboStartJumps);
        return Mathf.Lerp(comboStartMultiplier, comboMaxMultiplier, t);
    }
}
