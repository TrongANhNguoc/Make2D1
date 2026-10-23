using TMPro;
using UnityEngine;

// HUD trong ải: điểm, combo, ví Fruit Coin, đồng hồ ải, tên biome/ải. Ô nào không gán thì bỏ qua.
public class GameHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI walletText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI stageText;
    [Tooltip("Yêu cầu quả của Fruit Challenge: loại quả còn thiếu, thời gian vòng, tiến độ vòng")]
    [SerializeField] private TextMeshProUGUI challengeText;

    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScore;
            ScoreManager.Instance.OnComboChanged += UpdateCombo;
            UpdateScore(ScoreManager.Instance.TotalScore);
            UpdateCombo(ScoreManager.Instance.ComboJumps, ScoreManager.Instance.ComboMultiplier);
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnBalanceChanged += UpdateWallet;
            UpdateWallet(CurrencyManager.Instance.Balance);
        }

        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnStageChanged += UpdateStage;
            UpdateStage(RunManager.Instance.CurrentBiome, RunManager.Instance.CurrentLevel);
        }
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
            ScoreManager.Instance.OnComboChanged -= UpdateCombo;
        }

        if (CurrencyManager.Instance != null) CurrencyManager.Instance.OnBalanceChanged -= UpdateWallet;
        if (RunManager.Instance != null) RunManager.Instance.OnStageChanged -= UpdateStage;
    }

    private void Update()
    {
        UpdateChallenge();

        if (timerText == null || RunManager.Instance == null) return;

        if (RunManager.Instance.HasTimeLimit)
        {
            timerText.text = "Time: " + Mathf.CeilToInt(RunManager.Instance.StageTimeRemaining) + "s";
        }
        else
        {
            timerText.text = "";
        }
    }

    private void UpdateChallenge()
    {
        if (challengeText == null) return;

        FruitChallengeManager challenge = FruitChallengeManager.Instance;
        if (challenge == null || !challenge.IsActive)
        {
            challengeText.text = "";
            return;
        }

        string text = "Vòng " + (challenge.RoundsCompleted + 1) + "/" + challenge.RoundsToClear
                      + "  " + challenge.RoundTimeRemaining.ToString("0.0") + "s\n";
        foreach (var need in challenge.Needs)
        {
            if (need.Value > 0) text += need.Key + " x" + need.Value + "  ";
        }
        challengeText.text = text;
    }

    private void UpdateScore(int total)
    {
        if (scoreText != null) scoreText.text = "Score: " + total;
    }

    private void UpdateCombo(int jumps, float multiplier)
    {
        if (comboText != null) comboText.text = multiplier > 1f ? "Combo x" + multiplier.ToString("0.0") : "";
    }

    private void UpdateWallet(int balance)
    {
        if (walletText != null) walletText.text = "Coins: " + balance;
    }

    private void UpdateStage(BiomeData biome, LevelConfig level)
    {
        if (stageText == null || biome == null || level == null) return;
        stageText.text = biome.biomeName + " - Ải " + level.stageIndex;
    }
}
