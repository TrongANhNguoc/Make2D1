using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;


public static class SetupPauseUI
{
    [MenuItem("Tools/Make2D1/Setup Pause UI")]
    private static void Run()
    {
        SetupGameUI.log.Clear();
        Undo.SetCurrentGroupName("Setup Pause UI");
        int undoGroup = Undo.GetCurrentGroup();

        Canvas canvas = SetupGameUI.GetOrCreateCanvas();
        SetupGameUI.EnsureEventSystem();

        PauseUI pauseUI = Object.FindObjectOfType<PauseUI>(true);
        GameObject host;
        if (pauseUI != null)
        {
            host = pauseUI.gameObject;
        }
        else
        {
            host = SetupGameUI.GetOrCreateUI("PauseUI", canvas.transform, out bool hostNew);
            if (hostNew) SetupGameUI.Stretch(host.GetComponent<RectTransform>());
            pauseUI = Undo.AddComponent<PauseUI>(host);
        }

        GameObject panel = SetupGameUI.GetOrCreateUI("PausePanel", canvas.transform, out bool panelNew, typeof(Image));
        if (panelNew)
        {
            SetupGameUI.Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        }

        SetupGameUI.GetOrCreateText("TitleText", panel.transform, "TẠM DỪNG", 72, TextAlignmentOptions.Center,
            rt => SetupGameUI.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0f, 350f), new Vector2(960f, 120f)), false);

        Button continueButton = SetupDeathUI.GetOrCreateButton("ContinueButton", panel.transform, "Tiếp tục", new Vector2(0f, 0f));
        Button exitButton = SetupDeathUI.GetOrCreateButton("ExitButton", panel.transform, "Thoát", new Vector2(0f, -170f));

        if (panelNew) panel.SetActive(false);

        SerializedObject so = new SerializedObject(pauseUI);
        SetupGameUI.Assign(so, "panel", panel);
        SetupGameUI.Assign(so, "continueButton", continueButton);
        SetupGameUI.Assign(so, "exitButton", exitButton);
        so.ApplyModifiedProperties();

        if (host == panel || host.transform.IsChildOf(panel.transform))
            Debug.LogWarning("[SetupPauseUI] PauseUI đang nằm trên panel bị ẩn → hãy chuyển sang object luôn active (vd. Canvas).");

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupPauseUI] Hoàn tất: gán panel, continueButton, exitButton cho " + host.name + ". Nhớ lưu scene (Ctrl+S).");
    }
}
