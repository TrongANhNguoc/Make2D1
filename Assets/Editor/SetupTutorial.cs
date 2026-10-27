using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu Tools → Make2D1 → Setup Tutorial: tạo asset Stage0_Tutorial (LevelConfig) rồi gán vào RunManager.tutorialLevel,
// dựng TutorialUI (bảng gợi ý + nút "Bỏ qua") trong Canvas, thêm nút "Hướng dẫn" vào MainMenuPanel.
// Chạy lại nhiều lần an toàn: dùng lại asset/object cùng tên, bố cục chỉ áp dụng cho object mới tạo,
// việc gán trường luôn làm lại. Không đụng file .unity ngoài các object trong scene đang mở (nhớ Ctrl+S).
public static class SetupTutorial
{
    private const string LevelAssetPath = "Assets/Level/Stage0_Tutorial.asset";
    private const string TerrainPrefabPath = "Assets/Terrain/Tutorial.prefab";

    [MenuItem("Tools/Make2D1/Setup Tutorial")]
    private static void Run()
    {
        SetupGameUI.log.Clear();
        Undo.SetCurrentGroupName("Setup Tutorial");
        int undoGroup = Undo.GetCurrentGroup();

        LevelConfig level = GetOrCreateLevelAsset();

        Canvas canvas = SetupGameUI.GetOrCreateCanvas();
        SetupGameUI.EnsureEventSystem();

        AssignToRunManager(level);
        SetupTutorialUI(canvas);
        SetupTutorialButton(canvas);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupTutorial] Hoàn tất:\n- " + string.Join("\n- ", SetupGameUI.log) + "\nNhớ lưu scene (Ctrl+S).");
    }

    // ---------------------------------------------------------------- Asset

    private static LevelConfig GetOrCreateLevelAsset()
    {
        LevelConfig level = AssetDatabase.LoadAssetAtPath<LevelConfig>(LevelAssetPath);
        if (level == null)
        {
            level = ScriptableObject.CreateInstance<LevelConfig>();
            AssetDatabase.CreateAsset(level, LevelAssetPath);
            SetupGameUI.log.Add("Tạo asset " + LevelAssetPath);
        }

        SerializedObject so = new SerializedObject(level);
        so.FindProperty("stageIndex").intValue = 1;
        so.FindProperty("targetHeight").floatValue = 60f;       // chỉ dùng khi prefab thiếu PortalPoint
        so.FindProperty("timeLimit").floatValue = 0f;
        so.FindProperty("bossPrefab").objectReferenceValue = null;
        so.FindProperty("fruitChallengeEnabled").boolValue = false;
        so.FindProperty("spawnCoinRate").intValue = 0;
        so.FindProperty("spawnEnemyRate").intValue = 0;
        so.FindProperty("spawnTrapRate").intValue = 0;
        so.FindProperty("fixedTraps").arraySize = 0;

        // Không đè terrainPrefab bạn đã gán tay; chỉ tự điền khi còn trống và Tutorial.prefab đã có
        SerializedProperty terrain = so.FindProperty("terrainPrefab");
        if (terrain.objectReferenceValue == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TerrainPrefabPath);
            if (prefab != null)
            {
                terrain.objectReferenceValue = prefab;
                SetupGameUI.log.Add("Stage0_Tutorial: terrainPrefab = " + TerrainPrefabPath);
            }
            else
            {
                SetupGameUI.log.Add("CHƯA CÓ " + TerrainPrefabPath + " → Tutorial chưa chạy (game vẫn vào ải 1 như cũ). Vẽ prefab rồi chạy lại menu này");
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(level);
        AssetDatabase.SaveAssets();
        SetupGameUI.log.Add("Stage0_Tutorial: timeLimit 0, không Fruit Challenge, spawn rate 0/0/0, không bẫy cố định");
        return level;
    }

    private static void AssignToRunManager(LevelConfig level)
    {
        RunManager run = Object.FindObjectOfType<RunManager>(true);
        if (run == null)
        {
            Debug.LogWarning("[SetupTutorial] Không tìm thấy RunManager trong scene → chưa gán được tutorialLevel.");
            return;
        }

        SerializedObject so = new SerializedObject(run);
        SetupGameUI.Assign(so, "tutorialLevel", level);
        so.ApplyModifiedProperties();
        SetupGameUI.log.Add("RunManager (" + run.name + "): gán tutorialLevel");
    }

    // ---------------------------------------------------------------- UI

    private static void SetupTutorialUI(Canvas canvas)
    {
        // Host luôn active chứa TutorialManager (panel/nút là con của Canvas, không phải con của host)
        TutorialManager manager = Object.FindObjectOfType<TutorialManager>(true);
        GameObject host;
        if (manager != null)
        {
            host = manager.gameObject;
        }
        else
        {
            host = SetupGameUI.GetOrCreateUI("TutorialUI", canvas.transform, out bool hostNew);
            if (hostNew) SetupGameUI.Stretch(host.GetComponent<RectTransform>());
            manager = Undo.AddComponent<TutorialManager>(host);
        }

        // Bảng gợi ý: raycastTarget TẮT để không chặn kéo-thả bắn (PlayerController bỏ qua click trúng UI)
        GameObject panel = SetupGameUI.GetOrCreateUI("TutorialHintPanel", canvas.transform, out bool panelNew, typeof(Image));
        if (panelNew)
        {
            SetupGameUI.Place(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(980f, 260f));
            Image bg = panel.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            bg.raycastTarget = false;
        }

        GameObject iconGo = SetupGameUI.GetOrCreateUI("HintIcon", panel.transform, out bool iconNew, typeof(Image));
        if (iconNew)
        {
            SetupGameUI.Place(iconGo.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(150f, 150f));
            Image icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            iconGo.SetActive(false);
        }

        // Khung minh họa động bên trái (cùng chỗ với HintIcon; TutorialManager chỉ hiện 1 trong 2)
        GameObject demoGo = SetupGameUI.GetOrCreateUI("HintDemo", panel.transform, out bool demoNew, typeof(Image), typeof(TutorialDemoUI));
        if (demoNew)
        {
            SetupGameUI.Place(demoGo.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(15f, 0f), new Vector2(180f, 180f));
            Image demoBg = demoGo.GetComponent<Image>();
            demoBg.color = new Color(1f, 1f, 1f, 0.08f);
            demoBg.raycastTarget = false;
            demoGo.SetActive(false);
        }

        bool textNew = panel.transform.Find("HintText") == null;
        TextMeshProUGUI text = SetupGameUI.GetOrCreateText("HintText", panel.transform, "Gợi ý", 40, TextAlignmentOptions.Center,
            rt =>
            {
                SetupGameUI.Stretch(rt);
                rt.offsetMin = new Vector2(200f, 20f);   // chừa chỗ cho icon bên trái
                rt.offsetMax = new Vector2(-30f, -20f);
            }, true);
        if (textNew)
        {
            text.enableWordWrapping = true;
            text.enableAutoSizing = true;
            text.fontSizeMin = 28f;
            text.fontSizeMax = 40f;
        }

        // Nút Bỏ qua: góc dưới phải, không đè Score/Combo (trái) và Coins (phải trên)
        bool skipNew = canvas.transform.Find("TutorialSkipButton") == null;
        Button skip = SetupDeathUI.GetOrCreateButton("TutorialSkipButton", canvas.transform, "Bỏ qua", Vector2.zero);
        if (skipNew)
        {
            SetupGameUI.Place(skip.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(300f, 100f));
        }

        if (panelNew) panel.SetActive(false);
        if (skipNew) skip.gameObject.SetActive(false);

        SerializedObject so = new SerializedObject(manager);
        SetupGameUI.Assign(so, "hintPanel", panel);
        SetupGameUI.Assign(so, "hintText", text);
        SetupGameUI.Assign(so, "hintIcon", iconGo.GetComponent<Image>());
        SetupGameUI.Assign(so, "demo", demoGo.GetComponent<TutorialDemoUI>());
        SetupGameUI.Assign(so, "skipButton", skip);
        so.ApplyModifiedProperties();
        SetupGameUI.log.Add("TutorialManager (" + host.name + "): gán hintPanel, hintText, hintIcon, demo, skipButton");

        if (host == panel || host.transform.IsChildOf(panel.transform) || host == skip.gameObject)
            Debug.LogWarning("[SetupTutorial] TutorialManager đang nằm trên panel/nút bị ẩn → chuyển sang object luôn active (vd. Canvas).");
    }

    private static void SetupTutorialButton(Canvas canvas)
    {
        MainMenuUI menuUI = Object.FindObjectOfType<MainMenuUI>(true);
        if (menuUI == null)
        {
            Debug.LogWarning("[SetupTutorial] Không tìm thấy MainMenuUI → bỏ qua nút 'Hướng dẫn' (chạy Setup Main Menu trước).");
            return;
        }

        SerializedObject so = new SerializedObject(menuUI);
        GameObject menuPanel = so.FindProperty("panel").objectReferenceValue as GameObject;
        if (menuPanel == null)
        {
            Debug.LogWarning("[SetupTutorial] MainMenuUI chưa có panel → bỏ qua nút 'Hướng dẫn' (chạy Setup Main Menu trước).");
            return;
        }

        // Dưới nút "2 NGƯỜI CHƠI" (y = -120), cùng khoảng cách 180
        Button tutorial = SetupDeathUI.GetOrCreateButton("TutorialButton", menuPanel.transform, "HƯỚNG DẪN", new Vector2(0f, -300f));
        SetupGameUI.Assign(so, "tutorialButton", tutorial);
        so.ApplyModifiedProperties();
        SetupGameUI.log.Add("MainMenuUI (" + menuUI.name + "): gán tutorialButton");
    }
}
