using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Gắn vào 1 object luôn active (vd. Canvas); `panel` là object con bị ẩn mặc định
public class VictoryUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI nextBiomeText;
    [SerializeField] private Button playAgainButton;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgain);

        if (RunManager.Instance != null) RunManager.Instance.OnVictory += Show;
    }

    private void OnDestroy()
    {
        if (RunManager.Instance != null) RunManager.Instance.OnVictory -= Show;
    }

    private void Show(BiomeData finished)
    {
        if (panel != null) panel.SetActive(true);
        if (titleText != null) titleText.text = "VICTORY - " + finished.biomeName;

        ScoreManager score = ScoreManager.Instance;
        if (scoreText != null && score != null && RunManager.Instance != null)
        {
            // Phá đảo 4 ải: Fruit Coins toàn trận đã được nhân đôi và cộng vào ví
            scoreText.text = "Score: " + score.TotalScore + "\nFruit Coins: +" + RunManager.Instance.LastRunReward + " (x2)";
        }

        if (nextBiomeText != null && RunManager.Instance != null)
        {
            string text = RunManager.Instance.NextBiome != null ? "Biome kế: " + RunManager.Instance.NextBiome.biomeName : "";
            if (RunManager.Instance.JustUnlockedBiome != null)
            {
                text += "\nMở khóa: " + RunManager.Instance.JustUnlockedBiome.biomeName;
            }
            nextBiomeText.text = text;
        }
    }

    private void OnPlayAgain()
    {
        if (panel != null) panel.SetActive(false);
        if (RunManager.Instance != null) RunManager.Instance.StartNewRun();
    }
}
