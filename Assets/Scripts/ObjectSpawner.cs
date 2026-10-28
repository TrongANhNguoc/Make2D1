using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject coinPrefab;
    [Tooltip("Các prefab quả (mỗi prefab có coin với 1 FruitType khác nhau). Trống = chỉ dùng coinPrefab")]
    [SerializeField] private GameObject[] fruitPrefabs;
    [Tooltip("Khi Fruit Challenge đang chạy: xác suất (%) sinh đúng loại quả đang cần")]
    [Range(0, 100)][SerializeField] private int neededFruitBias = 70;
    [SerializeField] private GameObject groundEnemyPrefab;
    [SerializeField] private GameObject airEnemyPrefab;
    [Tooltip("Prefab có component Trap. Để trống thì bỏ qua bẫy")]
    [SerializeField] private GameObject trapPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float spawnDistanceY = 15f;
    [SerializeField] private float spawnIntervalY = 5f;
    [SerializeField] private float minX = -4f;
    [SerializeField] private float maxX = 4f;
    [Tooltip("Layer cản spawn. Layer Ground và Wall luôn được tính thêm, kể cả khi để trống")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Ngừng spawn cách Portal bao nhiêu unit để đỉnh ải thoáng")]
    [SerializeField] private float stopSpawnMargin = 6f;

    [Header("Spawn Rates (dùng khi không có RunManager/LevelConfig)")]
    [Range(0, 100)][SerializeField] private int coinSpawnChance = 70;
    [Range(0, 100)][SerializeField] private int airEnemySpawnChance = 15;
    [Range(0, 100)][SerializeField] private int groundEnemySpawnChance = 15;

    private float currentMinX;
    private float currentMaxX;
    private float nextSpawnY;
    private float spawnLimitY = float.MaxValue;
    private bool movingTraps;
    private bool warnedMissingTrap;
    private readonly List<GameObject> spawned = new List<GameObject>();

    private int ObstacleMask => groundLayer.value | LayerMask.GetMask("Ground", "Wall");

    private void Start()
    {
        if (RunManager.Instance != null) RunManager.Instance.OnStageChanged += HandleStageChanged;
        ResetForStage();
    }

    private void OnDestroy()
    {
        if (RunManager.Instance != null) RunManager.Instance.OnStageChanged -= HandleStageChanged;
    }

    private void Update()
    {
        if (playerTransform == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (nextSpawnY > spawnLimitY) return;

        if (playerTransform.position.y + spawnDistanceY >= nextSpawnY)
        {
            SpawnRandomObject(nextSpawnY);
            nextSpawnY += spawnIntervalY;
        }
    }

    private void HandleStageChanged(BiomeData biome, LevelConfig level)
    {
        ResetForStage();
    }

    private void ResetForStage()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null) Destroy(spawned[i]);
        }
        spawned.Clear();

        RunManager run = RunManager.Instance;
        LevelConfig level = run != null ? run.CurrentLevel : null;

        if (run != null && run.IsTutorial)
        {
            nextSpawnY = 0f;
            spawnLimitY = float.NegativeInfinity;
            return;
        }

        bool overrideX = level != null && level.overrideSpawnX;
        currentMinX = overrideX ? Mathf.Min(level.spawnMinX, level.spawnMaxX) : minX;
        currentMaxX = overrideX ? Mathf.Max(level.spawnMinX, level.spawnMaxX) : maxX;

        if (level != null)
        {
            nextSpawnY = run.StageStartY + spawnIntervalY;
            spawnLimitY = run.TargetY - stopSpawnMargin;
            movingTraps = level.stageType == StageType.Survival || level.stageType == StageType.Boss;
            SpawnFixedTraps(level, run.HasStartPoint ? run.StageStartX : 0f, run.StageStartY);

            if (level.bossPrefab != null)
            {
                Track(Instantiate(level.bossPrefab, new Vector3(0f, run.StageStartY, 0f), Quaternion.identity));
            }
        }
        else
        {
            nextSpawnY = (playerTransform != null ? playerTransform.position.y : 0f) + spawnIntervalY;
            spawnLimitY = float.MaxValue;
            movingTraps = false;
        }
    }

    private void SpawnFixedTraps(LevelConfig level, float stageStartX, float stageStartY)
    {
        if (level.fixedTraps == null) return;

        foreach (FixedTrapSpawn fixedTrap in level.fixedTraps)
        {
            if (fixedTrap.prefab == null) continue;

            Vector3 pos = new Vector3(stageStartX + fixedTrap.offset.x, stageStartY + fixedTrap.offset.y, 0f);
            Track(Instantiate(fixedTrap.prefab, pos, Quaternion.identity));
        }
    }

    private void Track(GameObject obj)
    {
        if (obj != null) spawned.Add(obj);
    }

    private void SpawnRandomObject(float spawnY)
    {
        int coinRate = coinSpawnChance;
        int enemyRate = airEnemySpawnChance + groundEnemySpawnChance;
        int trapRate = 0;

        LevelConfig level = RunManager.Instance != null ? RunManager.Instance.CurrentLevel : null;
        if (level != null)
        {
            coinRate = level.spawnCoinRate;
            enemyRate = level.spawnEnemyRate;
            trapRate = level.spawnTrapRate;
        }

        int totalChance = coinRate + enemyRate + trapRate;
        if (totalChance <= 0) return;

        int randomRoll = Random.Range(0, totalChance);
        bool isEnemy = randomRoll >= coinRate && randomRoll < coinRate + enemyRate;

        if (isEnemy)
        {
            int enemySplit = airEnemySpawnChance + groundEnemySpawnChance;
            bool spawnAir = enemySplit <= 0 ? Random.value < 0.5f : Random.Range(0, enemySplit) < airEnemySpawnChance;

            if (!spawnAir)
            {
                SpawnGroundEnemy(spawnY);
                return;
            }
        }

        if (!TryFindFreePosition(spawnY, out Vector3 validSpawnPos)) return;

        if (randomRoll < coinRate) SqawnCoin(validSpawnPos);
        else if (isEnemy) SpawnAirEnemy(validSpawnPos);
        else SpawnTrap(validSpawnPos);
    }

    private bool TryFindFreePosition(float spawnY, out Vector3 pos)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 checkPos = new Vector3(Random.Range(currentMinX, currentMaxX), spawnY, 0f);
            if (Physics2D.OverlapCircle(checkPos, 0.5f, ObstacleMask) == null)
            {
                pos = checkPos;
                return true;
            }
        }

        pos = Vector3.zero;
        return false;
    }

    private static void GetColliderBox(GameObject prefab, out Vector2 offset, out Vector2 size)
    {
        Vector2 scale = new Vector2(Mathf.Abs(prefab.transform.localScale.x), Mathf.Abs(prefab.transform.localScale.y));
        offset = Vector2.zero;
        size = Vector2.one;

        BoxCollider2D box = prefab.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            offset = Vector2.Scale(box.offset, scale);
            size = Vector2.Scale(box.size, scale);
            return;
        }

        CircleCollider2D circle = prefab.GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            offset = Vector2.Scale(circle.offset, scale);
            size = Vector2.one * (circle.radius * 2f * Mathf.Max(scale.x, scale.y));
            return;
        }

        CapsuleCollider2D capsule = prefab.GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            offset = Vector2.Scale(capsule.offset, scale);
            size = Vector2.Scale(capsule.size, scale);
        }
    }

    private bool TryGetAirPosition(Vector3 spawnPosition, GameObject prefab, out Vector3 finalPos)
    {
        finalPos = spawnPosition;
        float airClearance = 4.5f;

        RaycastHit2D hitUnder = Physics2D.Raycast(spawnPosition, Vector2.down, 5.0f, ObstacleMask);
        RaycastHit2D hitAbove = Physics2D.Raycast(spawnPosition, Vector2.up, 5.0f, ObstacleMask);

        float minY = float.MinValue;
        float maxY = float.MaxValue;

        if (hitUnder.collider != null) minY = hitUnder.point.y + airClearance;
        if (hitAbove.collider != null) maxY = hitAbove.point.y - airClearance;

        if (maxY <= minY) return false;

        finalPos.y = Mathf.Clamp(spawnPosition.y, minY, maxY);

        GetColliderBox(prefab, out Vector2 offset, out Vector2 size);
        Vector2 center = (Vector2)finalPos + offset;
        return Physics2D.OverlapBox(center, size + Vector2.one * 0.2f, 0f, ObstacleMask) == null;
    }

    private void SpawnTrap(Vector3 spawnPosition)
    {
        if (trapPrefab == null)
        {
            if (!warnedMissingTrap)
            {
                Debug.LogWarning("ObjectSpawner: chưa gán trapPrefab, bỏ qua spawn bẫy.");
                warnedMissingTrap = true;
            }
            return;
        }

        if (!TryGetAirPosition(spawnPosition, trapPrefab, out Vector3 finalPos)) return;

        GameObject trap = Instantiate(trapPrefab, finalPos, Quaternion.identity);
        Track(trap);

        Trap trapScript = trap.GetComponent<Trap>();
        if (trapScript != null) trapScript.SetMoving(movingTraps);
    }

    private void SpawnAirEnemy(Vector3 spawnPosition)
    {
        if (airEnemyPrefab == null) return;
        if (!TryGetAirPosition(spawnPosition, airEnemyPrefab, out Vector3 finalPos)) return;

        Track(Instantiate(airEnemyPrefab, finalPos, Quaternion.identity));
    }

    private void SpawnGroundEnemy(float spawnY)
    {
        if (groundEnemyPrefab == null) return;

        GetColliderBox(groundEnemyPrefab, out Vector2 offset, out Vector2 size);
        float bottomOffset = offset.y - size.y * 0.5f;
        float halfWidth = size.x * 0.5f;

        for (int i = 0; i < 12; i++)
        {
            Vector2 origin = new Vector2(Random.Range(currentMinX, currentMaxX), spawnY);

            if (Physics2D.OverlapPoint(origin, ObstacleMask) != null) continue;

            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 8.0f, ObstacleMask);
            if (hit.collider == null || hit.normal.y <= 0.7f) continue;

            float probeY = hit.point.y + 0.1f;
            bool leftSupported = Physics2D.Raycast(new Vector2(origin.x - halfWidth * 0.9f, probeY), Vector2.down, 0.3f, ObstacleMask).collider != null;
            bool rightSupported = Physics2D.Raycast(new Vector2(origin.x + halfWidth * 0.9f, probeY), Vector2.down, 0.3f, ObstacleMask).collider != null;
            if (!leftSupported || !rightSupported) continue;

            Vector3 pos = new Vector3(origin.x, hit.point.y - bottomOffset + 0.02f, 0f);

            Vector2 center = (Vector2)pos + offset + Vector2.up * 0.05f;
            if (Physics2D.OverlapBox(center, size * 0.85f, 0f, ObstacleMask) != null) continue;

            Track(Instantiate(groundEnemyPrefab, pos, Quaternion.identity));
            return;
        }
    }

    private GameObject PickFruitPrefab()
    {
        if (fruitPrefabs == null || fruitPrefabs.Length == 0) return coinPrefab;

        FruitChallengeManager challenge = FruitChallengeManager.Instance;
        if (challenge != null && Random.Range(0, 100) < neededFruitBias && challenge.TryGetNeededType(out FruitType wanted))
        {
            foreach (GameObject prefab in fruitPrefabs)
            {
                if (prefab == null) continue;
                coin c = prefab.GetComponent<coin>();
                if (c != null && c.Type == wanted) return prefab;
            }
        }

        GameObject random = fruitPrefabs[Random.Range(0, fruitPrefabs.Length)];
        return random != null ? random : coinPrefab;
    }

    private void SqawnCoin(Vector3 spawnPosition)
    {
        GameObject fruit = PickFruitPrefab();
        if (fruit == null) return;
        if (!TryGetAirPosition(spawnPosition, fruit, out Vector3 finalCoinPos)) return;

        Track(Instantiate(fruit, finalCoinPos, Quaternion.identity));
    }
}
