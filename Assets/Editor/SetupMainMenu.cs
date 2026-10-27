using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupMainMenu
{
    private static readonly Color DisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [MenuItem("Tools/Make2D1/Setup Main Menu")]
    private static void Run()
    {
        SetupGameUI.log.Clear();
        Undo.SetCurrentGroupName("Setup Main Menu");
        int undoGroup = Undo.GetCurrentGroup();

        Canvas canvas = SetupGameUI.GetOrCreateCanvas();
        SetupGameUI.EnsureEventSystem();

        SetStartStateToMenu();

        MainMenuUI menuUI = Object.FindObjectOfType<MainMenuUI>(true);
        GameObject host;
        if (menuUI != null)
        {
            host = menuUI.gameObject;
        }
        else
        {
            host = SetupGameUI.GetOrCreateUI("MainMenuUI", canvas.transform, out bool hostNew);
            if (hostNew) SetupGameUI.Stretch(host.GetComponent<RectTransform>());
            menuUI = Undo.AddComponent<MainMenuUI>(host);
        }

        GameObject panel = SetupGameUI.GetOrCreateUI("MainMenuPanel", canvas.transform, out bool panelNew, typeof(Image));
        if (panelNew)
        {
            SetupGameUI.Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.16f, 1f);
        }

        SetupGameUI.GetOrCreateText("TitleText", panel.transform, "MAKE2D1", 96, TextAlignmentOptions.Center,
            rt => SetupGameUI.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0f, 420f), new Vector2(960f, 160f)), false);

        Button single = SetupDeathUI.GetOrCreateButton("SinglePlayerButton", panel.transform, "1 NGƯỜI CHƠI", new Vector2(0f, 60f));
        Button two = SetupDeathUI.GetOrCreateButton("TwoPlayerButton", panel.transform, "2 NGƯỜI CHƠI", new Vector2(0f, -120f));

        two.interactable = false;
        if (two.targetGraphic != null) two.targetGraphic.color = DisabledColor;

        Transform oldLabel = panel.transform.Find("ComingSoonText");
        if (oldLabel != null)
        {
            Undo.DestroyObjectImmediate(oldLabel.gameObject);
            SetupGameUI.log.Add("Đã xóa ComingSoonText");
        }

        if (panelNew) panel.SetActive(false);

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Reorder Main Menu");
        panel.transform.SetAsLastSibling();

        SerializedObject so = new SerializedObject(menuUI);
        SetupGameUI.Assign(so, "panel", panel);
        SetupGameUI.Assign(so, "singlePlayerButton", single);
        SetupGameUI.Assign(so, "twoPlayerButton", two);
        so.ApplyModifiedProperties();
        SetupGameUI.log.Add("MainMenuUI (" + host.name + "): gán panel, singlePlayerButton, twoPlayerButton");

        if (host == panel || host.transform.IsChildOf(panel.transform))
            Debug.LogWarning("[SetupMainMenu] MainMenuUI đang nằm trên MainMenuPanel (bị ẩn) → hãy chuyển sang object luôn active (vd. Canvas).");

        SetupBackToMenuButton();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupMainMenu] Hoàn tất:\n- " + string.Join("\n- ", SetupGameUI.log) + "\nNhớ lưu scene (Ctrl+S).");
    }

    private static void SetStartStateToMenu()
    {
        GameManager gm = Object.FindObjectOfType<GameManager>(true);
        if (gm == null)
        {
            Debug.LogWarning("[SetupMainMenu] Không tìm thấy GameManager trong scene → chưa đặt được startState = Menu.");
            return;
        }

        SerializedObject so = new SerializedObject(gm);
        SerializedProperty prop = so.FindProperty("startState");
        prop.enumValueIndex = (int)GameState.Menu;
        so.ApplyModifiedProperties();
        SetupGameUI.log.Add("GameManager (" + gm.name + "): startState = Menu");
    }

    private static void SetupBackToMenuButton()
    {
        VictoryUI victory = Object.FindObjectOfType<VictoryUI>(true);
        if (victory == null) return;

        SerializedObject so = new SerializedObject(victory);
        GameObject victoryPanel = so.FindProperty("panel").objectReferenceValue as GameObject;
        if (victoryPanel == null)
        {
            Debug.LogWarning("[SetupMainMenu] VictoryUI chưa có panel → bỏ qua nút 'Về menu' (chạy Setup Game UI trước).");
            return;
        }

        Button back = SetupDeathUI.GetOrCreateButton("BackToMenuButton", victoryPanel.transform, "Về menu", new Vector2(0f, -420f));
        SetupGameUI.Assign(so, "backToMenuButton", back);
        so.ApplyModifiedProperties();
        SetupGameUI.log.Add("VictoryUI (" + victory.name + "): gán backToMenuButton");
    }
}
