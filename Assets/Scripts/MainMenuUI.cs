using UnityEngine;
using UnityEngine.UI;

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

        if (twoPlayerButton != null)
        {
            twoPlayerButton.interactable = false;
            if (twoPlayerButton.targetGraphic != null) twoPlayerButton.targetGraphic.color = DisabledColor;
        }

        GameManager gm = GameManager.Instance;
        if (gm != null) gm.OnStateChanged += HandleStateChanged;

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

        if (visible) panel.transform.SetAsLastSibling();
        panel.SetActive(visible);
    }

    private void OnSinglePlayerClicked()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SelectMode(GameMode.SinglePlayer);

            RunManager run = RunManager.Instance;
            if (run != null && run.ShouldPlayTutorial) run.StartTutorial();

            gm.StartGame();
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
