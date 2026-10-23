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
    [Tooltip("Tùy chọn: nút 'Về menu' — quay lại màn hình chọn chế độ, lượt mới bắt đầu khi bấm '1 người'")]
    [SerializeField] private Button backToMenuButton;
    [Tooltip("Độ đặc của nền panel khi hiện Victory (1 = che kín hoàn toàn)")]
    [Range(0f, 1f)][SerializeField] private float backgroundAlpha = 0.95f;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgain);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnBackToMenu);

        if (RunManager.Instance != null) RunManager.Instance.OnVictory += Show;
    }

    private void OnDestroy()
    {
        if (RunManager.Instance != null) RunManager.Instance.OnVictory -= Show;
    }

    private void Show(BiomeData finished)
    {
        if (panel != null)
        {
            // Vẽ panel đè lên HUD và làm nền gần đặc để chỉ còn thấy nội dung Victory
            panel.transform.SetAsLastSibling();
            Image background = panel.GetComponent<Image>();
            if (background != null)
            {
                Color c = background.color;
                c.a = backgroundAlpha;
                background.color = c;
            }
            panel.SetActive(true);
        }
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

    private void OnBackToMenu()
    {
        if (panel != null) panel.SetActive(false);

        // Chuẩn bị sẵn run mới (đưa player về điểm xuất phát) rồi dừng ở menu; MainMenuUI tự hiện khi state = Menu
        if (RunManager.Instance != null) RunManager.Instance.PrepareNewRun();
        if (GameManager.Instance != null) GameManager.Instance.GoToMenu();
    }

    private void OnPlayAgain()
    {
        if (panel != null) panel.SetActive(false);
        if (RunManager.Instance != null) RunManager.Instance.StartNewRun();
    }
}
