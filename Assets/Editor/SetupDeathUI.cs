using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupDeathUI
{
    [MenuItem("Tools/Make2D1/Setup Death UI")]
    private static void Run()
    {
        SetupGameUI.log.Clear();
        Undo.SetCurrentGroupName("Setup Death UI");
        int undoGroup = Undo.GetCurrentGroup();

        Canvas canvas = SetupGameUI.GetOrCreateCanvas();
        SetupGameUI.EnsureEventSystem();

        DeathUI deathUI = Object.FindObjectOfType<DeathUI>(true);
        GameObject host;
        if (deathUI != null)
        {
            host = deathUI.gameObject;
        }
        else
        {
            host = SetupGameUI.GetOrCreateUI("DeathUI", canvas.transform, out bool hostNew);
            if (hostNew) SetupGameUI.Stretch(host.GetComponent<RectTransform>());
            deathUI = Undo.AddComponent<DeathUI>(host);
        }

        FakeRewardedAd ad = host.GetComponent<FakeRewardedAd>();
        if (ad == null) ad = Undo.AddComponent<FakeRewardedAd>(host);

        GameObject panel = SetupGameUI.GetOrCreateUI("DeathPanel", canvas.transform, out bool panelNew, typeof(Image));
        if (panelNew)
        {
            SetupGameUI.Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        }

        SetupGameUI.GetOrCreateText("TitleText", panel.transform, "BẠN ĐÃ CHẾT", 72, TextAlignmentOptions.Center,
            rt => SetupGameUI.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0f, 350f), new Vector2(960f, 120f)), false);

        Button reviveButton = GetOrCreateButton("ReviveButton", panel.transform, "Xem video để hồi sinh", new Vector2(0f, 0f));
        Button restartButton = GetOrCreateButton("RestartButton", panel.transform, "Chơi lại", new Vector2(0f, -170f));

        if (panelNew) panel.SetActive(false);

        // Panel video giả: nền trắng full màn hình, chữ đếm ngược đen
        GameObject video = SetupGameUI.GetOrCreateUI("VideoPanel", canvas.transform, out bool videoNew, typeof(Image));
        if (videoNew)
        {
            SetupGameUI.Stretch(video.GetComponent<RectTransform>());
            video.GetComponent<Image>().color = Color.white;
        }

        TextMeshProUGUI countdown = SetupGameUI.GetOrCreateText("CountdownText", video.transform, "5", 160, TextAlignmentOptions.Center,
            rt => SetupGameUI.Place(rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 240f)), false);
        if (videoNew) countdown.color = Color.black;

        TextMeshProUGUI hint = SetupGameUI.GetOrCreateText("HintText", video.transform, "Đang xem video (giả lập)...", 44, TextAlignmentOptions.Center,
            rt => SetupGameUI.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(960f, 80f)), false);
        if (videoNew) hint.color = Color.black;

        if (videoNew) video.SetActive(false);

        SerializedObject soDeath = new SerializedObject(deathUI);
        SetupGameUI.Assign(soDeath, "panel", panel);
        SetupGameUI.Assign(soDeath, "reviveButton", reviveButton);
        SetupGameUI.Assign(soDeath, "restartButton", restartButton);
        SetupGameUI.Assign(soDeath, "rewardedAdProvider", ad);
        soDeath.ApplyModifiedProperties();

        SerializedObject soAd = new SerializedObject(ad);
        SetupGameUI.Assign(soAd, "videoPanel", video);
        SetupGameUI.Assign(soAd, "countdownText", countdown);
        soAd.ApplyModifiedProperties();

        SetupGameUI.log.Add("DeathUI (" + host.name + "): gán panel, reviveButton, restartButton, rewardedAdProvider");
        SetupGameUI.log.Add("FakeRewardedAd (" + host.name + "): gán videoPanel, countdownText");

        if (host == panel || host.transform.IsChildOf(panel.transform) || host == video || host.transform.IsChildOf(video.transform))
            Debug.LogWarning("[SetupDeathUI] DeathUI đang nằm trên panel bị ẩn → hãy chuyển sang object luôn active (vd. Canvas).");

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupDeathUI] Hoàn tất:\n- " + string.Join("\n- ", SetupGameUI.log) + "\nNhớ lưu scene (Ctrl+S).");
    }

    internal static Button GetOrCreateButton(string name, Transform parent, string label, Vector2 pos)
    {
        GameObject go = SetupGameUI.GetOrCreateUI(name, parent, out bool created, typeof(Image), typeof(Button));
        if (created)
        {
            SetupGameUI.Place(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), pos, new Vector2(760f, 130f));
            Image img = go.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            go.GetComponent<Button>().targetGraphic = img;
        }

        TextMeshProUGUI text = SetupGameUI.GetOrCreateText("Text (TMP)", go.transform, label, 44, TextAlignmentOptions.Center,
            rt => SetupGameUI.Stretch(rt), false);
        if (created) text.color = Color.black;

        return go.GetComponent<Button>();
    }
}
