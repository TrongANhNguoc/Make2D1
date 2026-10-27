using UnityEngine;
using UnityEngine.UI;

// Màn hình chọn chế độ chơi. Hiện khi GameManager ở state Menu (lần đầu vào game, hoặc "Về menu" sau Victory);
// reload scene sau khi chết thì GameManager đã ở Playing nên không hiện lại.
// Gắn vào 1 object luôn active (vd. Canvas); `panel` là object con bị ẩn mặc định.
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Button twoPlayerButton;
    [Tooltip("Chơi lại ải Tutorial bất cứ lúc nào")]
    [SerializeField] private Button tutorialButton;

    private static readonly Color DisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private void Start()
    {
        if (singlePlayerButton != null) singlePlayerButton.onClick.AddListener(OnSinglePlayerClicked);
        if (tutorialButton != null) tutorialButton.onClick.AddListener(OnTutorialClicked);

        // Chế độ 2 người chưa có: khóa nút, đổi xám
        if (twoPlayerButton != null)
        {
            twoPlayerButton.interactable = false;
            if (twoPlayerButton.targetGraphic != null) twoPlayerButton.targetGraphic.color = DisabledColor;
        }

        GameManager gm = GameManager.Instance;
        if (gm != null) gm.OnStateChanged += HandleStateChanged;

        // GameManager.Start có thể chạy sau: state mặc định của nó là Menu nên vẫn hiện, rồi HandleStateChanged sẽ ẩn nếu cần
        SetVisible(gm != null && gm.CurrentState == GameState.Menu);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        SetVisible(state == GameState.Menu);
    }

    private void SetVisible(bool visible)
    {
        if (panel == null) return;

        // Đè lên mọi UI khác (HUD, Victory...)
        if (visible) panel.transform.SetAsLastSibling();
        panel.SetActive(visible);
    }

    private void OnSinglePlayerClicked()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SelectMode(GameMode.SinglePlayer);

            // Lần đầu (chưa có TutorialDone): vào Tutorial thay vì ải 1; Tutorial xong thì RunManager tự bắt đầu run thường
            RunManager run = RunManager.Instance;
            if (run != null && run.ShouldPlayTutorial) run.StartTutorial();

            gm.StartGame();   // state → Playing, HandleStateChanged ẩn panel
        }

        SetVisible(false);
    }

    private void OnTutorialClicked()
    {
        GameManager gm = GameManager.Instance;
        RunManager run = RunManager.Instance;
        if (gm == null || run == null || !run.StartTutorial()) return;

        gm.SelectMode(GameMode.SinglePlayer);
        gm.StartGame();
        SetVisible(false);
    }
}
