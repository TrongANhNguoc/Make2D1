using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Nạp địa hình (Tilemap) riêng cho từng ải. Do RunManager sở hữu và gọi Load() ở đầu BeginStage,
// tức là TRƯỚC khi teleport player và TRƯỚC khi bắn OnStageChanged (ObjectSpawner / Portal / Fruit Challenge).
// Ải không có terrainPrefab thì dùng lại Grid có sẵn trong scene (tương thích ngược).
public class StageTerrainLoader
{
    public const string StartPointName = "StartPoint";
    public const string PortalPointName = "PortalPoint";
    public const string CameraBoundsName = "CameraBounds";

    private readonly string defaultTerrainName;

    private GameObject current;          // terrain đang Instantiate (null nếu đang dùng Grid mặc định)
    private GameObject defaultTerrain;   // Grid có sẵn trong scene
    private CinemachineConfiner2D confiner;
    private Collider2D defaultConfinerShape;

    public Transform StartPoint { get; private set; }
    public Transform PortalPoint { get; private set; }

    public StageTerrainLoader(string defaultTerrainName)
    {
        this.defaultTerrainName = defaultTerrainName;
    }

    public void Load(LevelConfig level)
    {
        Unload();
        FindDefaultTerrain();

        GameObject prefab = level != null ? level.terrainPrefab : null;
        if (prefab != null)
        {
            if (defaultTerrain != null) defaultTerrain.SetActive(false);

            current = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            StartPoint = FindChild(current.transform, StartPointName);
            PortalPoint = FindChild(current.transform, PortalPointName);
            BuildColliders(current);
        }
        else if (defaultTerrain != null)
        {
            defaultTerrain.SetActive(true);
        }

        UpdateConfiner();

        // Collider mới phải có trong physics scene trước khi spawner raycast vào groundLayer
        Physics2D.SyncTransforms();
    }

    private void Unload()
    {
        StartPoint = null;
        PortalPoint = null;
        if (current == null) return;

        // Tắt ngay để collider cũ biến khỏi truy vấn physics; Destroy thật diễn ra cuối frame
        current.SetActive(false);
        Object.Destroy(current);
        current = null;
    }

    // Grid mặc định: root object trong scene tên defaultTerrainName (kể cả đang bị ẩn). Tìm lại sau khi reload scene
    private void FindDefaultTerrain()
    {
        if (defaultTerrain != null) return;

        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == defaultTerrainName)
            {
                defaultTerrain = root;
                return;
            }
        }
    }

    // Tilemap mới tạo: ép sinh collider ngay để raycast frame này đã thấy
    private static void BuildColliders(GameObject terrain)
    {
        foreach (TilemapCollider2D tilemapCollider in terrain.GetComponentsInChildren<TilemapCollider2D>())
        {
            tilemapCollider.ProcessTilemapChanges();
        }

        foreach (CompositeCollider2D composite in terrain.GetComponentsInChildren<CompositeCollider2D>())
        {
            composite.GenerateGeometry();
        }
    }

    // Confiner bám theo collider "CameraBounds" của map mới; map mặc định thì trả về collider gốc của scene
    private void UpdateConfiner()
    {
        if (confiner == null)
        {
            confiner = Object.FindObjectOfType<CinemachineConfiner2D>(true);
            if (confiner == null) return;
            defaultConfinerShape = confiner.m_BoundingShape2D;
        }

        Collider2D shape = defaultConfinerShape;
        if (current != null)
        {
            Transform bounds = FindChild(current.transform, CameraBoundsName);
            shape = bounds != null ? bounds.GetComponent<Collider2D>() : null;
            if (shape == null) Debug.LogWarning("StageTerrainLoader: terrain '" + current.name + "' không có child '" + CameraBoundsName + "' (Collider2D) → camera không bị giới hạn.");
        }

        confiner.m_BoundingShape2D = shape;
        confiner.InvalidateCache();
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName) return t;
        }
        return null;
    }
}
