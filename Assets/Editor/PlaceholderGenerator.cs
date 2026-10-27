using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlaceholderGenerator
{
    private const string Root = "Assets/Art/Placeholders";
    private const string SpriteDir = Root + "/Sprites";
    private const string AnimDir = Root + "/Animators";
    private const string PrefabDir = Root + "/Prefabs";
    private const string LogFile = "Assets/Art/PLACEHOLDERS.md";

    private const int PPU = 100;

    private static bool overwrite;
    private static readonly List<string> logRows = new List<string>();
    private static readonly Dictionary<string, AnimatorController> controllerCache = new Dictionary<string, AnimatorController>();

    [MenuItem("Make2D1/Placeholders/Generate P0 (giữ asset đã có)")]
    public static void GenerateKeep()
    {
        Generate(false);
    }

    [MenuItem("Make2D1/Placeholders/Regenerate P0 (GHI ĐÈ)")]
    public static void GenerateOverwrite()
    {
        if (!EditorUtility.DisplayDialog("Ghi đè placeholder",
                "Sẽ tạo lại toàn bộ sprite/animator/prefab placeholder P0, mất mọi art đã swap trong các prefab này. Tiếp tục?",
                "Ghi đè", "Hủy")) return;
        Generate(true);
    }

    private static void Generate(bool overwriteAll)
    {
        overwrite = overwriteAll;
        logRows.Clear();

        EnsureFolder(SpriteDir);
        EnsureFolder(AnimDir);
        EnsureFolder(PrefabDir);

        controllerCache.Clear();
        if (overwrite && File.Exists(LogFile)) File.Delete(LogFile);

        CreateSprites();
        AssetDatabase.Refresh();

        CreatePlayer();
        CreateEnemy();
        CreateFruits();
        CreateFruitCoin();
        CreateCheckpoint();
        CreateHazard();

        WriteLog();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Placeholder] Hoàn tất. Xem " + LogFile);
    }


    private struct FruitSpec
    {
        public FruitType type;
        public Color color;
        public bool wide;
        public int size;
    }

    private static readonly FruitSpec[] Fruits =
    {
        new FruitSpec { type = FruitType.Apple,  color = new Color(0.90f, 0.15f, 0.15f), size = 80 },
        new FruitSpec { type = FruitType.Banana, color = new Color(1.00f, 0.88f, 0.20f), size = 90, wide = true },
        new FruitSpec { type = FruitType.Cherry, color = new Color(0.60f, 0.05f, 0.20f), size = 56 },
        new FruitSpec { type = FruitType.Grape,  color = new Color(0.55f, 0.25f, 0.75f), size = 76 },
        new FruitSpec { type = FruitType.Orange, color = new Color(1.00f, 0.60f, 0.10f), size = 80 },
    };

    private static void CreateSprites()
    {
        Color outline = new Color(0.08f, 0.08f, 0.10f, 1f);
        Vector2 center = new Vector2(0.5f, 0.5f);

        WriteSprite("PH_Player_P1", 100, 100, Circle(50), new Color(0.25f, 0.55f, 1f), outline, center, Vector4.zero);
        WriteSprite("PH_Player_P2", 100, 100, Circle(50), new Color(1f, 0.55f, 0.20f), outline, center, Vector4.zero);

        WriteSprite("PH_Enemy", 100, 100, RoundRect(100, 100, 14), new Color(0.85f, 0.15f, 0.20f), outline, center, Vector4.zero);

        foreach (FruitSpec f in Fruits)
        {
            int w = f.size;
            int h = f.wide ? f.size / 2 : f.size;
            Func<float, float, float> shape = f.wide ? RoundRect(w, h, h / 2f) : Circle(f.size / 2f);
            WriteSprite("PH_Fruit_" + f.type, w, h, shape, f.color, outline, center, Vector4.zero);
        }

        Color gold = new Color(1f, 0.82f, 0.15f);
        WriteSprite("PH_FruitCoin", 64, 64, Circle(32), gold, outline, center, Vector4.zero);
        WriteSprite("PH_FruitCoin_Icon", 64, 64, Circle(32), gold, outline, center, Vector4.zero);

        Vector2 bottom = new Vector2(0.5f, 0f);
        WriteSprite("PH_Checkpoint_Inactive", 60, 160, RoundRect(60, 160, 10), new Color(0.6f, 0.6f, 0.6f), outline, bottom, Vector4.zero);
        WriteSprite("PH_Checkpoint_Active", 60, 160, RoundRect(60, 160, 10), new Color(0.25f, 0.85f, 0.35f), outline, bottom, Vector4.zero);

        WriteSprite("PH_Hazard", 64, 64, RoundRect(64, 64, 4), new Color(0.95f, 0.1f, 0.1f, 0.5f),
            new Color(0.95f, 0.1f, 0.1f, 1f), center, new Vector4(8, 8, 8, 8));
    }

    private static Func<float, float, float> Circle(float radius)
    {
        return (x, y) => Mathf.Sqrt((x - radius) * (x - radius) + (y - radius) * (y - radius)) - radius;
    }

    private static Func<float, float, float> RoundRect(float w, float h, float r)
    {
        return (x, y) =>
        {
            float qx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - r);
            float qy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - r);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        };
    }

    private static void WriteSprite(string name, int w, int h, Func<float, float, float> sdf,
        Color fill, Color outline, Vector2 pivot, Vector4 border)
    {
        string assetPath = SpriteDir + "/" + name + ".png";
        if (!overwrite && File.Exists(assetPath)) return;

        const float outlineWidth = 4f;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float d = sdf(x + 0.5f, y + 0.5f);
                float edge = Mathf.Clamp01(0.5f - d);
                float t = Mathf.Clamp01(-d - outlineWidth + 0.5f);
                Color c = Color.Lerp(outline, fill, t);
                c.a = Mathf.Lerp(outline.a, fill.a, t) * edge;
                tex.SetPixel(x, y, c);
            }
        }
        File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = PPU;
        ti.filterMode = FilterMode.Bilinear;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;

        TextureImporterSettings settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(settings);
        ti.spritePivot = pivot;
        ti.spriteBorder = border;
        ti.SaveAndReimport();
    }

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "/" + name + ".png");
    }


    private enum Kind
    {
        Loop, 
        Return,
        Hold  
    }

    private struct StateSpec
    {
        public string name;
        public Kind kind;
        public float sx, sy;

        public StateSpec(string name, Kind kind, float sx, float sy)
        {
            this.name = name;
            this.kind = kind;
            this.sx = sx;
            this.sy = sy;
        }
    }

    private static readonly StateSpec[] PlayerStates =
    {
        new StateSpec("Idle", Kind.Loop, 1.05f, 0.95f),
        new StateSpec("Aim", Kind.Hold, 1.15f, 0.85f),
        new StateSpec("Shoot", Kind.Return, 0.8f, 1.25f),
        new StateSpec("Fly", Kind.Loop, 0.92f, 1.1f),
        new StateSpec("Land", Kind.Return, 1.25f, 0.75f),
        new StateSpec("Pulled", Kind.Hold, 1.2f, 0.9f),
        new StateSpec("Fall", Kind.Loop, 0.9f, 1.15f),
        new StateSpec("Rescued", Kind.Return, 1.2f, 1.2f),
        new StateSpec("Collect", Kind.Return, 1.15f, 1.15f),
        new StateSpec("Win", Kind.Loop, 1.15f, 1.15f),
        new StateSpec("Lose", Kind.Hold, 1.1f, 0.7f),
    };

    private static readonly StateSpec[] EnemyStates =
    {
        new StateSpec("Idle", Kind.Loop, 1.04f, 0.96f),
        new StateSpec("Hit", Kind.Return, 1.2f, 0.8f),
        new StateSpec("Vanish", Kind.Hold, 0.05f, 0.05f),
    };

    private static readonly StateSpec[] FruitStates =
    {
        new StateSpec("Idle", Kind.Loop, 0.25f, 1f),
        new StateSpec("Collect", Kind.Hold, 1.5f, 1.5f),
    };

    private static AnimatorController BuildStateController(string controllerName, StateSpec[] specs, bool withEmotion)
    {
        AnimatorController cached;
        if (controllerCache.TryGetValue(controllerName, out cached)) return cached;

        string dir = AnimDir + "/" + controllerName;
        string path = dir + "/" + controllerName + ".controller";
        if (!overwrite && File.Exists(path)) return Cache(controllerName, AssetDatabase.LoadAssetAtPath<AnimatorController>(path));

        EnsureFolder(dir);
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        foreach (StateSpec s in specs)
            controller.AddParameter(s.name, AnimatorControllerParameterType.Trigger);
        if (withEmotion) controller.AddParameter("Emotion", AnimatorControllerParameterType.Int);

        AnimatorState idle = null;
        Dictionary<string, AnimatorState> states = new Dictionary<string, AnimatorState>();
        foreach (StateSpec s in specs)
        {
            AnimatorState state = sm.AddState(s.name);
            state.motion = MakeScaleClip(dir + "/" + controllerName + "_" + s.name + ".anim", s);
            states[s.name] = state;
            if (s.name == "Idle") idle = state;
        }
        sm.defaultState = idle;

        foreach (StateSpec s in specs)
        {
            if (s.name == "Idle") continue;

            AnimatorStateTransition any = sm.AddAnyStateTransition(states[s.name]);
            any.hasExitTime = false;
            any.duration = 0f;
            any.canTransitionToSelf = false;
            any.AddCondition(AnimatorConditionMode.If, 0, s.name);

            if (s.kind == Kind.Return)
            {
                AnimatorStateTransition back = states[s.name].AddTransition(idle);
                back.hasExitTime = true;
                back.exitTime = 1f;
                back.duration = 0f;
            }
        }

        // Trigger "Idle" để buộc quay về Idle từ bất kỳ trạng thái nào.
        AnimatorStateTransition toIdle = sm.AddAnyStateTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;
        toIdle.canTransitionToSelf = false;
        toIdle.AddCondition(AnimatorConditionMode.If, 0, "Idle");

        EditorUtility.SetDirty(controller);
        return Cache(controllerName, controller);
    }

    private static AnimatorController Cache(string key, AnimatorController controller)
    {
        controllerCache[key] = controller;
        return controller;
    }

    private static AnimationClip MakeScaleClip(string path, StateSpec s)
    {
        AnimationClip clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path) };
        float len = s.kind == Kind.Loop ? 1f : 0.3f;

        AnimationCurve cx, cy;
        switch (s.kind)
        {
            case Kind.Hold:
                cx = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, s.sx), new Keyframe(len, s.sx));
                cy = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, s.sy), new Keyframe(len, s.sy));
                break;
            default:
                cx = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(len * 0.5f, s.sx), new Keyframe(len, 1f));
                cy = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(len * 0.5f, s.sy), new Keyframe(len, 1f));
                break;
        }
        AnimationCurve cz = AnimationCurve.Constant(0f, len, 1f);
        clip.SetCurve("Visual", typeof(Transform), "localScale.x", cx);
        clip.SetCurve("Visual", typeof(Transform), "localScale.y", cy);
        clip.SetCurve("Visual", typeof(Transform), "localScale.z", cz);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = s.kind == Kind.Loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimatorController BuildCheckpointController()
    {
        const string name = "PH_Checkpoint";
        string dir = AnimDir + "/" + name;
        string path = dir + "/" + name + ".controller";
        if (!overwrite && File.Exists(path)) return AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

        EnsureFolder(dir);
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Active", AnimatorControllerParameterType.Bool);
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState inactive = sm.AddState("Inactive");
        AnimatorState active = sm.AddState("Active");
        inactive.motion = MakeSpriteClip(dir + "/" + name + "_Inactive.anim", LoadSprite("PH_Checkpoint_Inactive"));
        active.motion = MakeSpriteClip(dir + "/" + name + "_Active.anim", LoadSprite("PH_Checkpoint_Active"));
        sm.defaultState = inactive;

        AnimatorStateTransition on = inactive.AddTransition(active);
        on.hasExitTime = false;
        on.duration = 0f;
        on.AddCondition(AnimatorConditionMode.If, 0, "Active");
        AnimatorStateTransition off = active.AddTransition(inactive);
        off.hasExitTime = false;
        off.duration = 0f;
        off.AddCondition(AnimatorConditionMode.IfNot, 0, "Active");

        EditorUtility.SetDirty(controller);
        return controller;
    }


    private static AnimationClip MakeSpriteClip(string path, Sprite sprite)
    {
        AnimationClip clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path) };
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("Visual", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding,
            new[] { new ObjectReferenceKeyframe { time = 0f, value = sprite } });

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }



    private static GameObject NewEntity(string name, Sprite sprite, string sortingLayer, int order,
        RuntimeAnimatorController controller)
    {
        GameObject root = new GameObject(name);
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;

        if (controller != null) root.AddComponent<Animator>().runtimeAnimatorController = controller;
        return root;
    }

    private static GameObject NewChild(GameObject root, string name, Vector2 localPos)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        child.transform.localPosition = localPos;
        return child;
    }

    private static GameObject Save(GameObject root, string name, string spriteNote, string artNote)
    {
        string path = PrefabDir + "/" + name + ".prefab";
        GameObject result;
        if (!overwrite && File.Exists(path))
        {
            result = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        else
        {
            result = PrefabUtility.SaveAsPrefabAsset(root, path);
            logRows.Add("| " + name + " | " + spriteNote + " | `" + name + "/Visual` (SpriteRenderer.sprite) | " + artNote + " | Placeholder |");
        }
        UnityEngine.Object.DestroyImmediate(root);
        return result;
    }

    private static void CreatePlayer()
    {
        AnimatorController controller = BuildStateController("PH_Player", PlayerStates, true);

        GameObject p1 = NewEntity("PH_Player_P1", LoadSprite("PH_Player_P1"), "Player", 0, controller);
        p1.tag = "Player";
        Rigidbody2D rb = p1.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        CircleCollider2D col = p1.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        NewChild(p1, "RopeAnchor", Vector2.zero);
        NewChild(p1, "GroundCheck", new Vector2(0f, -0.5f));
        GameObject p1Prefab = Save(p1, "PH_Player_P1", "PH_Player_P1 (tròn xanh)", "100x100px, PPU " + PPU + ", pivot Center; điểm neo dây = `RopeAnchor`");

        // P2 là Prefab Variant của P1: chỉ khác sprite.
        string p2Path = PrefabDir + "/PH_Player_P2.prefab";
        if (overwrite || !File.Exists(p2Path))
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(p1Prefab);
            inst.name = "PH_Player_P2";
            inst.transform.Find("Visual").GetComponent<SpriteRenderer>().sprite = LoadSprite("PH_Player_P2");
            PrefabUtility.SaveAsPrefabAsset(inst, p2Path);
            UnityEngine.Object.DestroyImmediate(inst);
            logRows.Add("| PH_Player_P2 | PH_Player_P2 (tròn cam, Variant của P1) | `PH_Player_P2/Visual` | Khác P1 rõ về màu/viền/phụ kiện | Placeholder |");
        }
    }

    private static void CreateEnemy()
    {
        AnimatorController controller = BuildStateController("PH_Enemy", EnemyStates, false);
        GameObject e = NewEntity("PH_Enemy", LoadSprite("PH_Enemy"), "Default", 1, controller);
        e.tag = "Enemy";
        BoxCollider2D col = e.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        Save(e, "PH_Enemy", "PH_Enemy (vuông đỏ)", "100x100px, PPU " + PPU + ", pivot Center");
    }

    private static void CreateFruits()
    {
        AnimatorController controller = BuildStateController("PH_Fruit", FruitStates, false);
        foreach (FruitSpec f in Fruits)
        {
            string name = "PH_Fruit_" + f.type;
            Sprite sprite = LoadSprite(name);
            GameObject go = NewEntity(name, sprite, "Default", 2, controller);
            go.tag = "Coin";
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            coin c = go.AddComponent<coin>();
            SerializedObject so = new SerializedObject(c);
            so.FindProperty("fruitType").enumValueIndex = (int)f.type;
            so.ApplyModifiedProperties();

            Save(go, name, name, "Dễ phân biệt ở kích thước nhỏ, PPU " + PPU + ", pivot Center; `fruitType` = " + f.type);
        }
    }

    private static void CreateFruitCoin()
    {
        AnimatorController controller = BuildStateController("PH_Fruit", FruitStates, false);
        GameObject go = NewEntity("PH_FruitCoin", LoadSprite("PH_FruitCoin"), "Default", 2, controller);
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;
        Save(go, "PH_FruitCoin", "PH_FruitCoin (tròn vàng); icon HUD: PH_FruitCoin_Icon",
            "64x64px, PPU " + PPU + "; icon HUD tách riêng. Chưa gắn script nhặt (tiền tệ do CurrencyManager)");
    }

    private static void CreateCheckpoint()
    {
        AnimatorController controller = BuildCheckpointController();
        GameObject go = NewEntity("PH_Checkpoint", LoadSprite("PH_Checkpoint_Inactive"), "Default", 0, controller);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.6f, 1.6f);
        col.offset = new Vector2(0f, 0.8f);
        Save(go, "PH_Checkpoint", "PH_Checkpoint_Inactive / PH_Checkpoint_Active",
            "60x160px, PPU " + PPU + ", pivot Bottom; thay sprite trong 2 clip `PH_Checkpoint_Inactive/Active.anim`. Animator Bool `Active`");
    }

    private static void CreateHazard()
    {
        GameObject go = NewEntity("PH_Hazard", LoadSprite("PH_Hazard"), "Default", -1, null);
        SpriteRenderer sr = go.transform.Find("Visual").GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(10f, 2f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(10f, 2f);
        go.AddComponent<KillZone>();
        Save(go, "PH_Hazard", "PH_Hazard (đỏ trong suốt, 9-slice)",
            "64x64px, border 8px, Draw Mode Sliced/Tiled; đổi kích thước bằng SpriteRenderer.size + BoxCollider2D.size");
    }


    private static void WriteLog()
    {
        if (logRows.Count == 0) return;

        StringBuilder sb = new StringBuilder();
        if (!File.Exists(LogFile))
        {
            sb.AppendLine("# Danh sách Placeholder (cập nhật cột Trạng thái khi đã swap art thật)");
            sb.AppendLine();
            sb.AppendLine("Mọi sprite placeholder: trắng/viền tối đã bake màu, KHÔNG tint trong SpriteRenderer → swap sprite là xong.");
            sb.AppendLine("Clip animator placeholder chỉ animate `Visual.localScale`; thay clip bằng animation thật khi có art.");
            sb.AppendLine();
            sb.AppendLine("| Prefab | Sprite placeholder | Vị trí swap | Yêu cầu art | Trạng thái |");
            sb.AppendLine("|---|---|---|---|---|");
        }
        foreach (string row in logRows) sb.AppendLine(row);
        File.AppendAllText(LogFile, sb.ToString(), Encoding.UTF8);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
