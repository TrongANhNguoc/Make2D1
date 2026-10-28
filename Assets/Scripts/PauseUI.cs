using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.CurrentState == GameState.Playing) gm.PauseGame();
        else if (gm.CurrentState == GameState.Paused) gm.ResumeGame();
    }

    private void HandleStateChanged(GameState state)
    {
        if (panel == null) return;

        bool visible = state == GameState.Paused;
        if (visible) panel.transform.SetAsLastSibling();
        panel.SetActive(visible);
    }

    private void OnContinueClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
    }

    private void OnExitClicked()
    {
        if (panel != null) panel.SetActive(false);

        if (RunManager.Instance != null) RunManager.Instance.PrepareNewRun();
        if (GameManager.Instance != null) GameManager.Instance.GoToMenu();
    }
}
