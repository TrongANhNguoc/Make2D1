using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Menu Tools → Make2D1 → Create Terrain Prefab From Selection: chọn Grid (hoặc Tilemap con) trong scene →
// lưu thành prefab ở Assets/Terrain/ và tự thêm StartPoint / PortalPoint / CameraBounds nếu thiếu.
// Làm việc trên bản copy nên scene gốc không bị đổi.
public static class CreateTerrainPrefab
{
    private const string Folder = "Assets/Terrain";

    [MenuItem("Tools/Make2D1/Create Terrain Prefab From Selection")]
    private static void Run()
    {
        GameObject selected = Selection.activeGameObject;
        Grid grid = selected != null ? selected.GetComponentInParent<Grid>() : null;
        if (grid == null)
        {
            EditorUtility.DisplayDialog("Create Terrain Prefab", "Hãy chọn một object Grid (hoặc Tilemap con của Grid) trong scene.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Terrain");

        string baseName = grid.gameObject.name == "Grid" ? "Terrain_" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : "Terrain_" + grid.gameObject.name;
        string path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + baseName + ".prefab");

        if (grid.transform.position != Vector3.zero)
            Debug.LogWarning("[CreateTerrainPrefab] Grid không nằm ở (0,0,0). Terrain luôn được Instantiate tại (0,0) nên map sẽ bị lệch so với scene.");

        GameObject copy = Object.Instantiate(grid.gameObject);
        copy.name = Path.GetFileNameWithoutExtension(path);

        try
        {
            Bounds? bounds = CalculateBounds(copy);
            if (bounds == null)
            {
                Debug.LogWarning("[CreateTerrainPrefab] Không tìm thấy tile nào, các điểm mốc sẽ đặt tại gốc tọa độ.");
            }
            Bounds b = bounds ?? new Bounds(Vector3.zero, Vector3.one * 2f);

            EnsureMarker(copy.transform, "StartPoint", new Vector3(b.center.x, b.min.y + 1.5f, 0f));
            EnsureMarker(copy.transform, "PortalPoint", new Vector3(b.center.x, b.max.y - 1.5f, 0f));
            EnsureCameraBounds(copy.transform, b);
            WarnAboutLayers(copy);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(copy, path);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("[CreateTerrainPrefab] Đã tạo " + path + ". Chỉnh StartPoint / PortalPoint / CameraBounds trong prefab rồi kéo vào LevelConfig.terrainPrefab.");
        }
        finally
        {
            Object.DestroyImmediate(copy);
        }
    }

    // Bao toàn bộ tile của mọi Tilemap (tọa độ thế giới)
    private static Bounds? CalculateBounds(GameObject root)
    {
        Bounds? result = null;
        foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>())
        {
            tilemap.CompressBounds();
            if (tilemap.cellBounds.size == Vector3Int.zero) continue;

            Bounds local = tilemap.localBounds;
            Vector3 min = local.min, max = local.max;
            Transform t = tilemap.transform;

            foreach (Vector3 corner in new[]
            {
                new Vector3(min.x, min.y, 0f), new Vector3(max.x, min.y, 0f),
                new Vector3(min.x, max.y, 0f), new Vector3(max.x, max.y, 0f)
            })
            {
                Vector3 world = t.TransformPoint(corner);
                if (result == null) result = new Bounds(world, Vector3.zero);
                else
                {
                    Bounds r = result.Value;
                    r.Encapsulate(world);
                    result = r;
                }
            }
        }
        return result;
    }

    private static void EnsureMarker(Transform root, string markerName, Vector3 position)
    {
        if (FindChild(root, markerName) != null) return;

        GameObject marker = new GameObject(markerName);
        marker.transform.SetParent(root, false);
        marker.transform.position = position;

        // Icon nhãn để dễ thấy trong Scene view
        Texture2D icon = EditorGUIUtility.IconContent(markerName == "StartPoint" ? "sv_label_3" : "sv_label_6").image as Texture2D;
        if (icon != null) EditorGUIUtility.SetIconForObject(marker, icon);
    }

    // Collider biên cho CinemachineConfiner2D: PolygonCollider2D trigger bao quanh map, ở layer Ignore Raycast
    private static void EnsureCameraBounds(Transform root, Bounds b)
    {
        if (FindChild(root, "CameraBounds") != null) return;

        GameObject go = new GameObject("CameraBounds");
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        go.transform.SetParent(root, false);
        go.transform.position = Vector3.zero;

        PolygonCollider2D poly = go.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.points = new[]
        {
            new Vector2(b.min.x, b.min.y), new Vector2(b.min.x, b.max.y),
            new Vector2(b.max.x, b.max.y), new Vector2(b.max.x, b.min.y)
        };
    }

    private static void WarnAboutLayers(GameObject root)
    {
        int ground = LayerMask.NameToLayer("Ground");
        TilemapCollider2D[] colliders = root.GetComponentsInChildren<TilemapCollider2D>();
        if (colliders.Length == 0)
        {
            Debug.LogWarning("[CreateTerrainPrefab] Prefab không có TilemapCollider2D → player sẽ rơi xuyên map.");
            return;
        }

        foreach (TilemapCollider2D c in colliders)
        {
            if (c.gameObject.layer != ground)
                Debug.LogWarning("[CreateTerrainPrefab] Tilemap '" + c.name + "' không ở layer Ground → ground check / spawner sẽ không nhận.");
        }
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t != root && t.name == childName) return t;
        }
        return null;
    }
}
