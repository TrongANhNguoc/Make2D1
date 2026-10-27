using UnityEngine;

// Phím tắt để test nhanh các tính năng. Gắn vào 1 GameObject bất kỳ trong scene (vd. "GameManager").
// Chỉ chạy trong Editor / Development Build.
public class DebugCheats : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private bool showHelp = true;

    private void Update()
    {
        // Chuyển ải ngay (test Portal / RunManager / Victory khi đang ở ải 4)
        if (Input.GetKeyDown(KeyCode.N) && RunManager.Instance != null) RunManager.Instance.LoadNextStage();

        // Dịch player lên ngay dưới Portal để tự bay vào
        if (Input.GetKeyDown(KeyCode.T) && RunManager.Instance != null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                Vector3 p = pc.transform.position;
                p.y = RunManager.Instance.TargetY - 2f;
                pc.transform.position = p;
            }
        }

        PlayerCollision player = FindObjectOfType<PlayerCollision>();
        if (Input.GetKeyDown(KeyCode.H) && player != null) player.TakeDamage(1);   // mất 1 tim
        if (Input.GetKeyDown(KeyCode.K) && player != null) player.Kill();          // chết -> hiện DeathUI (bị bỏ qua khi đang bất tử)
        if (Input.GetKeyDown(KeyCode.R) && player != null) player.Revive();        // hồi sinh ngay, bỏ qua video (chỉ khi đang chết)

        if (Input.GetKeyDown(KeyCode.C) && CurrencyManager.Instance != null) CurrencyManager.Instance.AddCoins(10);
        if (Input.GetKeyDown(KeyCode.X) && ScoreManager.Instance != null) ScoreManager.Instance.AddPoints(100);

        // Pause / Resume
        if (Input.GetKeyDown(KeyCode.P) && GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentState == GameState.Paused) GameManager.Instance.ResumeGame();
            else GameManager.Instance.PauseGame();
        }

        // Xóa cờ Tutorial đã xong: dừng Play rồi Play lại → "1 người" sẽ vào Tutorial từ đầu
        if (Input.GetKeyDown(KeyCode.F11))
        {
            PlayerPrefs.DeleteKey(RunManager.TutorialDoneKey);
            PlayerPrefs.Save();
            Debug.Log("DebugCheats: đã xóa TutorialDone. Dừng Play rồi Play lại để vào Tutorial từ đầu.");
        }

        // Xóa toàn bộ save (ví + biome đã mở). Dừng Play rồi Play lại để có hiệu lực
        if (Input.GetKeyDown(KeyCode.F12))
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("DebugCheats: đã xóa PlayerPrefs. Dừng Play rồi Play lại.");
        }
    }

    private void OnGUI()
    {
        if (!showHelp) return;

        RunManager run = RunManager.Instance;
        GameManager gm = GameManager.Instance;
        ScoreManager sm = ScoreManager.Instance;
        FruitChallengeManager fc = FruitChallengeManager.Instance;

        string info = "[DEBUG]  N: next stage | T: tới Portal | H: -1 máu | K: chết | R: hồi sinh ngay\n"
                    + "C: +10 coin | X: +100 điểm | P: pause | F11: xóa TutorialDone | F12: xóa save\n";
        if (gm != null) info += "State: " + gm.CurrentState + "  timeScale: " + Time.timeScale.ToString("0.00") + "\n";
        if (run != null && run.IsTutorial) info += "TUTORIAL  (TutorialDone: " + RunManager.IsTutorialDone + ")\n";
        else if (run != null && run.CurrentBiome != null && run.CurrentLevel != null)
        {
            info += "Biome: " + run.CurrentBiome.name + "  Ải " + (run.CurrentStageIndex + 1)
                  + " (" + run.CurrentLevel.stageType + ")  TargetY: " + run.TargetY.ToString("0")
                  + "  Unlocked: " + run.UnlockedBiomeCount + "\n";
            if (run.HasTimeLimit) info += "Time: " + run.StageTimeRemaining.ToString("0.0") + "\n";
        }
        if (sm != null) info += "Score: " + sm.TotalScore + "  Combo: " + sm.ComboJumps + " (x" + sm.ComboMultiplier.ToString("0.0") + ")  FruitCoins run: " + sm.FruitCoins + "\n";
        if (CurrencyManager.Instance != null) info += "Ví: " + CurrencyManager.Instance.Balance + "\n";
        if (fc != null && fc.IsActive)
        {
            info += "Fruit vòng " + (fc.RoundsCompleted + 1) + "/" + fc.RoundsToClear + "  " + fc.RoundTimeRemaining.ToString("0.0") + "s: ";
            foreach (var need in fc.Needs) if (need.Value > 0) info += need.Key + " x" + need.Value + " ";
        }

        GUI.Box(new Rect(10, 10, 560, 140), "");
        GUI.Label(new Rect(18, 14, 550, 135), info);
    }
#endif
}
