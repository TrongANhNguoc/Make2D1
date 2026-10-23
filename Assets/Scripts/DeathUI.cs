using UnityEngine;
using UnityEngine.UI;

// Bảng hiện khi Player hết máu (GameState.Dead): "Chơi lại" hoặc "Xem video để hồi sinh" (1 lần mỗi ải).
// Gắn vào 1 object luôn active (vd. Canvas); `panel` là object con bị ẩn mặc định.
public class DeathUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button reviveButton;
    [SerializeField] private Button restartButton;
    [Tooltip("Component implement IRewardedAd (hiện là FakeRewardedAd). Trống = hồi sinh ngay, không xem video")]
    [SerializeField] private MonoBehaviour rewardedAdProvider;

    private PlayerCollision player;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (reviveButton != null) reviveButton.onClick.AddListener(OnReviveClicked);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);

        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.Dead) Show();
        else Hide();
    }

    private void Show()
    {
        player = FindObjectOfType<PlayerCollision>();
        if (panel == null) return;

        panel.transform.SetAsLastSibling();
        panel.SetActive(true);

        if (reviveButton != null)
        {
            // Lần chết thứ 2 trong cùng ải chỉ còn nút Chơi lại
            reviveButton.gameObject.SetActive(player != null && player.CanRevive);
            reviveButton.interactable = true;
        }
    }

    private void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnReviveClicked()
    {
        if (player == null) return;
        if (reviveButton != null) reviveButton.interactable = false;

        IRewardedAd ad = rewardedAdProvider as IRewardedAd;
        if (ad == null)
        {
            Debug.LogWarning("DeathUI: chưa gán rewardedAdProvider (IRewardedAd), hồi sinh ngay.");
            Revive();
            return;
        }

        ad.Show(Revive);
    }

    private void Revive()
    {
        if (player != null) player.Revive();   // GameManager chuyển Playing → HandleStateChanged ẩn panel
    }

    private void OnRestartClicked()
    {
        Hide();
        if (player != null) player.RestartRun();
    }
}
