using System;
using UnityEngine;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver,
    Victory
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("State")]
    [SerializeField] private GameState startState = GameState.Playing;

    private float slowMotionScale = 1f;

    public GameState CurrentState { get; private set; }
    public bool IsPlaying => CurrentState == GameState.Playing;

    // Các script khác đăng ký để phản ứng khi trạng thái đổi (UI, spawner, ...)
    public event Action<GameState> OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetState(startState);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState && Time.timeScale == GetTimeScale(newState)) return;

        CurrentState = newState;
        ApplyTimeScale();

        OnStateChanged?.Invoke(newState);
    }

    // Slow-motion khi ngắm bắn: chỉ có hiệu lực lúc Playing, pause vẫn ưu tiên
    public void SetSlowMotion(float scale)
    {
        slowMotionScale = scale;
        ApplyTimeScale();
    }

    public void StartGame()
    {
        slowMotionScale = 1f;
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
        SetState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing) SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused) SetState(GameState.Playing);
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        SetState(GameState.GameOver);
    }

    public void Victory()
    {
        if (CurrentState == GameState.Victory) return;
        SetState(GameState.Victory);
    }

    private void ApplyTimeScale()
    {
        Time.timeScale = GetTimeScale(CurrentState);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private float GetTimeScale(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
            case GameState.Menu:
            case GameState.Victory:
                return 0f;
            case GameState.Playing:
                return slowMotionScale;
            default:
                return 1f;
        }
    }
}
