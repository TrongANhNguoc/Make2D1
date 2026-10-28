using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class StageTerrainLoader
{
    public const string StartPointName = "StartPoint";
    public const string PortalPointName = "PortalPoint";
    public const string CameraBoundsName = "CameraBounds";

    private readonly string defaultTerrainName;

    private GameObject current;
    private GameObject defaultTerrain;
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

        Physics2D.SyncTransforms();
    }

    private void Unload()
    {
        StartPoint = null;
        PortalPoint = null;
        if (current == null) return;

        current.SetActive(false);
        Object.Destroy(current);
        current = null;
    }

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
