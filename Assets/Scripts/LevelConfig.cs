using UnityEngine;

public enum StageType
{
    SpeedRush,  // Ải 1: chạy đua thời gian ăn quả
    Survival,   // Ải 2: né bẫy và chướng ngại vật di động
    Hybrid,     // Ải 3: 50% thu thập quả + 50% né bẫy
    Boss        // Ải 4: như Hybrid + boss tung skill
}

// Bẫy đặt ở vị trí cố định trong ải (vd. đá lăn xuống)
[System.Serializable]
public struct FixedTrapSpawn
{
    public GameObject prefab;
    [Tooltip("Tính từ điểm xuất phát của ải. Có StartPoint: cả x và y đều cộng vào vị trí StartPoint. " +
             "Không có StartPoint: x = tọa độ thế giới, y = độ cao từ điểm xuất phát")]
    public Vector2 offset;
}

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Make2D1/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Stage")]
    [Tooltip("Số thứ tự ải trong biome (1-4)")]
    [Range(1, 4)] public int stageIndex = 1;
    public StageType stageType = StageType.SpeedRush;

    [Tooltip("Độ cao (Unity units) tính từ điểm bắt đầu ải tới Portal")]
    public float targetHeight = 150f;

    [Tooltip("Giới hạn thời gian ải (giây). 0 = không giới hạn. Hết giờ = thua (dùng cho ải Speed Rush)")]
    public float timeLimit = 0f;

    [Tooltip("Prefab boss (có BossController) xuất hiện ở góc trên màn hình suốt ải. Để trống = không có boss")]
    public GameObject bossPrefab;

    [Header("Terrain")]
    [Tooltip("Prefab địa hình riêng của ải (Grid + Tilemap layer Ground, có thể có child StartPoint / PortalPoint / CameraBounds). " +
             "Trống = dùng Grid có sẵn trong scene. Có PortalPoint thì độ cao ải tính tới PortalPoint thay vì targetHeight")]
    public GameObject terrainPrefab;

    [Header("Spawn Area")]
    [Tooltip("Bật để dùng vùng spawn ngang riêng của ải thay cho minX/maxX của ObjectSpawner (tọa độ thế giới)")]
    public bool overrideSpawnX = false;
    public float spawnMinX = -4f;
    public float spawnMaxX = 4f;

    [Header("Fruit Challenge (ải Speed Rush)")]
    [Tooltip("Bật cơ chế: ăn đủ các loại quả yêu cầu trong thời gian của mỗi vòng, lặp lại tới khi qua ải")]
    public bool fruitChallengeEnabled = false;
    [Tooltip("Số giây cho mỗi vòng")]
    public float fruitRoundTime = 5f;
    [Tooltip("Số loại quả khác nhau cần ăn mỗi vòng")]
    [Range(1, 5)] public int fruitTypesPerRound = 3;
    [Tooltip("Mỗi loại cần ăn tối đa bao nhiêu quả (random 1..giá trị này)")]
    [Range(1, 5)] public int fruitMaxCountPerType = 2;
    [Tooltip("Số vòng phải hoàn thành để qua ải")]
    public int fruitRoundsToClear = 5;
    [Tooltip("Máu bị trừ khi hết giờ mà chưa đủ quả (0 = không phạt)")]
    public int fruitFailDamage = 1;

    [Header("Spawn Rates (%)")]
    [Range(0, 100)] public int spawnCoinRate = 70;
    [Range(0, 100)] public int spawnEnemyRate = 30;
    [Range(0, 100)] public int spawnTrapRate = 0;

    [Header("Fixed Traps")]
    [Tooltip("Bẫy luôn xuất hiện đúng vị trí này mỗi lần vào ải (spawnTrapRate là bẫy random vị trí)")]
    public FixedTrapSpawn[] fixedTraps;
}
