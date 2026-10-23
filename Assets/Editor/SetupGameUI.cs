using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menu Tools → Make2D1 → Setup Game UI: dựng UI cho VictoryUI / GameHUD / RoundTimerUI trong scene đang mở
// và gán các trường [SerializeField] còn trống. Chạy lại nhiều lần an toàn: object cùng tên được dùng lại,
// bố cục chỉ áp dụng cho object mới tạo (không ghi đè chỉnh sửa tay), còn việc gán trường luôn được làm lại.
public static class SetupGameUI
{
    internal static readonly List<string> log = new List<string>();

    [MenuItem("Tools/Make2D1/Setup Game UI")]
    private static void Run()
    {
        log.Clear();
        Undo.SetCurrentGroupName("Setup Game UI");
        int undoGroup = Undo.GetCurrentGroup();

        Canvas canvas = GetOrCreateCanvas();
        EnsureEventSystem();

        SetupVictoryUI(canvas);
        SetupGameHUD(canvas);
        SetupRoundTimerUI();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupGameUI] Hoàn tất:\n- " + string.Join("\n- ", log) + "\nNhớ lưu scene (Ctrl+S).");
    }

    // ---------------------------------------------------------------- Canvas / EventSystem

    internal static Canvas GetOrCreateCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>(true);
        if (canvas != null)
        {
            log.Add("Dùng Canvas có sẵn: " + canvas.name);
            return canvas.rootCanvas;
        }

        GameObject go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        go.layer = LayerMask.NameToLayer("UI");

        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        log.Add("Tạo mới Canvas (Overlay, Scale With Screen Size 1080x1920)");
        return canvas;
    }

    internal static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>(true) != null) return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        log.Add("Tạo mới EventSystem (StandaloneInputModule - Input legacy)");
    }

    // ---------------------------------------------------------------- VictoryUI

    private static void SetupVictoryUI(Canvas canvas)
    {
        VictoryUI victory = Object.FindObjectOfType<VictoryUI>(true);
        if (victory == null)
        {
            Debug.LogWarning("[SetupGameUI] Không tìm thấy VictoryUI trong scene, bỏ qua.");
            return;
        }

        // Panel full màn hình, ẩn sẵn
        GameObject panel = GetOrCreateUI("VictoryPanel", canvas.transform, out bool panelNew, typeof(Image));
        if (panelNew)
        {
            Stretch(panel.GetComponent<RectTransform>());
            Image bg = panel.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            // Nền chặn click xuống bên dưới khi panel hiện
            bg.raycastTarget = true;
        }

        TextMeshProUGUI title = GetOrCreateText("TitleText", panel.transform, "VICTORY!", 72, TextAlignmentOptions.Center,
            rt => Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0f, 350f), new Vector2(960f, 120f)), false);
        TextMeshProUGUI score = GetOrCreateText("ScoreText", panel.transform, "Score: 0", 48, TextAlignmentOptions.Center,
            rt => Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(960f, 90f)), false);
        TextMeshProUGUI nextBiome = GetOrCreateText("NextBiomeText", panel.transform, "Next biome", 40, TextAlignmentOptions.Center,
            rt => Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(960f, 90f)), false);

        // Nút Chơi tiếp
        GameObject buttonGo = GetOrCreateUI("PlayAgainButton", panel.transform, out bool buttonNew, typeof(Image), typeof(Button));
        if (buttonNew)
        {
            Place(buttonGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -250f), new Vector2(480f, 130f));
            Image img = buttonGo.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            buttonGo.GetComponent<Button>().targetGraphic = img;
        }
        Button button = buttonGo.GetComponent<Button>();

        TextMeshProUGUI buttonLabel = GetOrCreateText("Text (TMP)", buttonGo.transform, "Chơi tiếp", 48, TextAlignmentOptions.Center,
            rt => Stretch(rt), false);
        if (panelNew) buttonLabel.color = Color.black;

        // Panel ẩn sẵn (chỉ khi mới tạo, để không tắt panel mà bạn đang chỉnh)
        if (panelNew) panel.SetActive(false);

        // Object chứa VictoryUI phải luôn active, không được là panel hay con của panel
        if (victory.gameObject == panel || victory.transform.IsChildOf(panel.transform))
            Debug.LogWarning("[SetupGameUI] VictoryUI đang nằm trên VictoryPanel (hoặc con của nó). Panel bị ẩn thì script cũng ngừng chạy → hãy chuyển VictoryUI sang object luôn active (vd. Canvas).");
        else if (!victory.gameObject.activeInHierarchy)
            Debug.LogWarning("[SetupGameUI] Object chứa VictoryUI đang bị tắt: " + victory.name + " → hãy bật lại.");

        SerializedObject so = new SerializedObject(victory);
        Assign(so, "panel", panel);
        Assign(so, "titleText", title);
        Assign(so, "scoreText", score);
        Assign(so, "nextBiomeText", nextBiome);
        Assign(so, "playAgainButton", button);
        so.ApplyModifiedProperties();
        log.Add("VictoryUI (" + victory.name + "): gán panel, titleText, scoreText, nextBiomeText, playAgainButton");
    }

    // ---------------------------------------------------------------- GameHUD

    private static void SetupGameHUD(Canvas canvas)
    {
        GameHUD hud = Object.FindObjectOfType<GameHUD>(true);
        if (hud == null)
        {
            Debug.LogWarning("[SetupGameUI] Không tìm thấy GameHUD trong scene, bỏ qua.");
            return;
        }

        Transform parent = canvas.transform;
        Vector2 topLeft = new Vector2(0f, 1f), topRight = new Vector2(1f, 1f), topCenter = new Vector2(0.5f, 1f);

        // Canvas tham chiếu 1080 rộng: Score/Combo (trái) rộng 300, Stage/Timer (giữa) 400, Wallet (phải) 300 → không chồng nhau
        TextMeshProUGUI score = GetOrCreateText("ScoreText", parent, "Score: 0", 44, TextAlignmentOptions.TopLeft,
            rt => Place(rt, topLeft, new Vector2(30f, -30f), new Vector2(300f, 60f)), true);
        TextMeshProUGUI combo = GetOrCreateText("ComboText", parent, "x1.0", 36, TextAlignmentOptions.TopLeft,
            rt => Place(rt, topLeft, new Vector2(30f, -95f), new Vector2(300f, 50f)), true);
        TextMeshProUGUI wallet = GetOrCreateText("WalletText", parent, "Coins: 0", 40, TextAlignmentOptions.TopRight,
            rt => Place(rt, topRight, new Vector2(-30f, -30f), new Vector2(300f, 60f)), true);
        TextMeshProUGUI stage = GetOrCreateText("StageText", parent, "Biome - Stage 1", 44, TextAlignmentOptions.Top,
            rt => Place(rt, topCenter, new Vector2(0f, -30f), new Vector2(400f, 60f)), true);
        TextMeshProUGUI timer = GetOrCreateText("TimerText", parent, "60", 40, TextAlignmentOptions.Top,
            rt => Place(rt, topCenter, new Vector2(0f, -95f), new Vector2(400f, 50f)), true);
        TextMeshProUGUI challenge = GetOrCreateText("ChallengeText", parent, "Challenge", 36, TextAlignmentOptions.Top,
            rt => Place(rt, topCenter, new Vector2(0f, -160f), new Vector2(900f, 60f)), true);

        SerializedObject so = new SerializedObject(hud);
        Assign(so, "scoreText", score);
        Assign(so, "comboText", combo);
        Assign(so, "walletText", wallet);
        Assign(so, "timerText", timer);
        Assign(so, "stageText", stage);
        Assign(so, "challengeText", challenge);
        so.ApplyModifiedProperties();
        log.Add("GameHUD (" + hud.name + "): gán scoreText, comboText, walletText, timerText, stageText, challengeText");
    }

    // ---------------------------------------------------------------- RoundTimerUI

    private static void SetupRoundTimerUI()
    {
        RoundTimerUI timerUI = Object.FindObjectOfType<RoundTimerUI>(true);
        if (timerUI == null)
        {
            Debug.LogWarning("[SetupGameUI] Không tìm thấy RoundTimerUI trong scene, bỏ qua.");
            return;
        }

        SerializedObject so = new SerializedObject(timerUI);
        GameObject root = so.FindProperty("root").objectReferenceValue as GameObject;
        if (root == null)
        {
            Debug.LogWarning("[SetupGameUI] RoundTimerUI chưa có trường root → bỏ qua (hãy gán root rồi chạy lại).");
            return;
        }
        if (root.GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning("[SetupGameUI] root của RoundTimerUI (" + root.name + ") không phải UI (không có RectTransform) → bỏ qua.");
            return;
        }

        GameObject fillGo = GetOrCreateUI("ClockFill", root.transform, out bool fillNew, typeof(Image));
        Image fill = fillGo.GetComponent<Image>();
        if (fillNew)
        {
            Place(fillGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 160f));
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            fill.color = new Color(0.3f, 0.8f, 0.4f);
            fill.raycastTarget = false;
        }

        TextMeshProUGUI seconds = GetOrCreateText("SecondsText", root.transform, "10", 64, TextAlignmentOptions.Center,
            rt => Place(rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 160f)), true);

        // Đảm bảo số giây vẽ sau (đè lên) hình tròn
        seconds.transform.SetAsLastSibling();

        Assign(so, "clockFill", fill);
        Assign(so, "secondsText", seconds);
        so.ApplyModifiedProperties();
        log.Add("RoundTimerUI (" + timerUI.name + "): gán clockFill, secondsText (con của root '" + root.name + "', giữ nguyên root)");
    }

    // ---------------------------------------------------------------- Helpers

    // Tìm con trực tiếp theo tên (kể cả đang inactive) hoặc tạo mới; bảo đảm có đủ component yêu cầu
    internal static GameObject GetOrCreateUI(string name, Transform parent, out bool created, params System.Type[] components)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            created = false;
            foreach (System.Type t in components)
                if (existing.GetComponent(t) == null) Undo.AddComponent(existing.gameObject, t);
            return existing.gameObject;
        }

        System.Type[] all = new System.Type[components.Length + 1];
        all[0] = typeof(RectTransform);
        components.CopyTo(all, 1);

        GameObject go = new GameObject(name, all);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        created = true;
        log.Add("Tạo mới " + name + " (trong " + parent.name + ")");
        return go;
    }

    internal static TextMeshProUGUI GetOrCreateText(string name, Transform parent, string text, float fontSize,
        TextAlignmentOptions alignment, System.Action<RectTransform> layout, bool outline)
    {
        GameObject go = GetOrCreateUI(name, parent, out bool created, typeof(TextMeshProUGUI));
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();

        if (created)
        {
            layout(go.GetComponent<RectTransform>());
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            if (outline)
            {
                tmp.outlineWidth = 0.25f;
                tmp.outlineColor = new Color32(0, 0, 0, 255);
            }
        }
        return tmp;
    }

    internal static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Anchor = pivot = anchor point (góc/cạnh/tâm), pos tính từ anchor đó
    internal static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    internal static void Assign(SerializedObject so, string field, Object value)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError("[SetupGameUI] " + so.targetObject.GetType().Name + " không có trường '" + field + "'.");
            return;
        }
        prop.objectReferenceValue = value;
    }
}
