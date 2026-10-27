using System.Collections;
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

    [Tooltip("Số giây giữ nguyên tên biome/ải khi mới vào màn")]
    [SerializeField] private float stageBannerHold = 1f;
    [Tooltip("Số giây mờ dần sau khi hết thời gian giữ, hết là biến mất hoàn toàn")]
    [SerializeField] private float stageBannerFade = 0.5f;

    private Coroutine stageBannerRoutine;
    private bool stageBannerVisible;

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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += ApplyStateVisibility;
            ApplyStateVisibility(GameManager.Instance.CurrentState);
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
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= ApplyStateVisibility;
        if (RunManager.Instance != null) RunManager.Instance.OnStageChanged -= UpdateStage;
    }

    // Victory: ẩn điểm, combo, ví để chỉ còn bảng Victory; chơi lại thì hiện lại
    private void ApplyStateVisibility(GameState state)
    {
        bool show = state != GameState.Victory;
        if (stageText != null) stageText.gameObject.SetActive(show && stageBannerVisible);
        if (scoreText != null) scoreText.gameObject.SetActive(show);
        if (comboText != null) comboText.gameObject.SetActive(show);
        if (walletText != null) walletText.gameObject.SetActive(show);
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

        string text = "";
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
        bool tutorial = RunManager.Instance != null && RunManager.Instance.IsTutorial;
        if (stageText == null || (!tutorial && (biome == null || level == null))) return;
        stageText.text = tutorial ? "Hướng dẫn" : biome.biomeName + " - Ải " + level.stageIndex;

        if (stageBannerRoutine != null) StopCoroutine(stageBannerRoutine);
        stageBannerRoutine = StartCoroutine(ShowStageBanner());
    }

    private IEnumerator ShowStageBanner()
    {
        SetStageTextAlpha(1f);
        stageBannerVisible = true;
        stageText.gameObject.SetActive(GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Victory);
        yield return new WaitForSeconds(stageBannerHold);

        for (float t = 0f; t < stageBannerFade; t += Time.deltaTime)
        {
            SetStageTextAlpha(1f - t / stageBannerFade);
            yield return null;
        }

        stageBannerVisible = false;
        stageText.gameObject.SetActive(false);
        SetStageTextAlpha(1f);
        stageBannerRoutine = null;
    }

    private void SetStageTextAlpha(float alpha)
    {
        Color c = stageText.color;
        c.a = alpha;
        stageText.color = c;
    }
}
