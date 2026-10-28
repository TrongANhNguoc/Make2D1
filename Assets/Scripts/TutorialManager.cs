using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI hintText;
    [Tooltip("Icon chuột/phím kèm theo (tùy chọn); ẩn khi bước không có icon")]
    [SerializeField] private Image hintIcon;
    [Tooltip("Hình minh họa động (kéo-thả, Ground Slam, giẫm quái). Bước có icon riêng thì dùng icon thay cho hình này")]
    [SerializeField] private TutorialDemoUI demo;
    [SerializeField] private Button skipButton;

    private readonly List<TutorialStep> activeSteps = new List<TutorialStep>();
    private TutorialStep shown;
    private readonly List<KeyValuePair<TutorialStep, float>> lingering = new List<KeyValuePair<TutorialStep, float>>();
    private bool tutorialActive;
    private PlayerController player;
    private PlayerCollision playerHealth;

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
        player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerCollision>();
            player.OnLaunched += HandleLaunched;
            player.OnLanded += HandleLanded;
            player.OnGroundSlam += HandleGroundSlam;
        }

        if (playerHealth != null)
        {
            playerHealth.OnEnemyStomped += HandleEnemyStomped;
            playerHealth.OnDamaged += HandleDamaged;
        }

        if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);

        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnStageChanged += HandleStageChanged;
            tutorialActive = RunManager.Instance.IsTutorial;
        }

        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleStateChanged;

        Refresh();
    }

    private void OnEnable()
    {
        coin.OnCollected += HandleFruitCollected;
    }

    private void OnDisable()
    {
        coin.OnCollected -= HandleFruitCollected;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (player != null)
        {
            player.OnLaunched -= HandleLaunched;
            player.OnLanded -= HandleLanded;
            player.OnGroundSlam -= HandleGroundSlam;
        }

        if (playerHealth != null)
        {
            playerHealth.OnEnemyStomped -= HandleEnemyStomped;
            playerHealth.OnDamaged -= HandleDamaged;
        }

        if (RunManager.Instance != null) RunManager.Instance.OnStageChanged -= HandleStageChanged;
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        if (lingering.Count == 0) return;

        bool changed = false;
        for (int i = lingering.Count - 1; i >= 0; i--)
        {
            if (Time.unscaledTime < lingering[i].Value) continue;
            lingering.RemoveAt(i);
            changed = true;
        }

        if (changed) Refresh();
    }

    public void EnterStep(TutorialStep step)
    {
        if (!tutorialActive || step.Completed || activeSteps.Contains(step)) return;

        activeSteps.Add(step);
        Refresh();
    }

    public void ExitStep(TutorialStep step)
    {
        if (!activeSteps.Remove(step)) return;
        Refresh();
    }

    private void HandleLaunched(bool fromGround)
    {
        Report(TutorialAction.Launch);
        if (!fromGround) Report(TutorialAction.LaunchInAir);
    }

    private void HandleLanded() { Report(TutorialAction.Land); }
    private void HandleGroundSlam() { Report(TutorialAction.GroundSlam); }
    private void HandleEnemyStomped() { Report(TutorialAction.EnemyStomped); }
    private void HandleDamaged() { Report(TutorialAction.Damaged); }
    private void HandleFruitCollected(FruitType type) { Report(TutorialAction.FruitCollected); }

    private void Report(TutorialAction action)
    {
        if (!tutorialActive || activeSteps.Count == 0) return;

        bool changed = false;
        for (int i = activeSteps.Count - 1; i >= 0; i--)
        {
            if (activeSteps[i].Action != action) continue;

            TutorialStep done = activeSteps[i];
            done.MarkCompleted();
            activeSteps.RemoveAt(i);
            if (done.LingerSeconds > 0f) lingering.Add(new KeyValuePair<TutorialStep, float>(done, Time.unscaledTime + done.LingerSeconds));
            changed = true;
        }

        if (changed) Refresh();
    }

    private void HandleStageChanged(BiomeData biome, LevelConfig level)
    {
        tutorialActive = RunManager.Instance != null && RunManager.Instance.IsTutorial;
        activeSteps.Clear();
        lingering.Clear();
        Refresh();
    }

    private void HandleStateChanged(GameState state)
    {
        Refresh();
    }

    private void OnSkipClicked()
    {
        if (RunManager.Instance != null) RunManager.Instance.SkipTutorial();
    }

    private void Refresh()
    {
        shown = null;
        foreach (TutorialStep step in activeSteps)
        {
            if (shown == null || step.Order < shown.Order) shown = step;
        }

        if (shown == null && lingering.Count > 0) shown = lingering[lingering.Count - 1].Key;

        bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;

        if (skipButton != null) skipButton.gameObject.SetActive(tutorialActive && playing);

        bool showHint = tutorialActive && playing && shown != null;
        if (hintPanel != null) hintPanel.SetActive(showHint);
        if (!showHint) return;

        if (hintText != null) hintText.text = shown.Message;
        if (hintIcon != null)
        {
            hintIcon.sprite = shown.Icon;
            hintIcon.gameObject.SetActive(shown.Icon != null);
        }

        if (demo != null)
        {
            if (shown.Icon != null) demo.Hide();
            else demo.Show(shown.Action);
        }
    }
}
