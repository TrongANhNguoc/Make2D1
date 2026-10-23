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
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Ngừng spawn cách Portal bao nhiêu unit để đỉnh ải thoáng")]
    [SerializeField] private float stopSpawnMargin = 6f;

    [Header("Spawn Rates (dùng khi không có RunManager/LevelConfig)")]
    [Range(0, 100)][SerializeField] private int coinSpawnChance = 70;
    [Range(0, 100)][SerializeField] private int airEnemySpawnChance = 15;
    [Range(0, 100)][SerializeField] private int groundEnemySpawnChance = 15;

    private float nextSpawnY;
    private float spawnLimitY = float.MaxValue;
    private bool movingTraps;
    private bool warnedMissingTrap;
    private readonly List<GameObject> spawned = new List<GameObject>();

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

    // Ải mới (hoặc run mới): xóa vật thể ải cũ, spawn lại từ điểm xuất phát tới Portal
    private void ResetForStage()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null) Destroy(spawned[i]);
        }
        spawned.Clear();

        RunManager run = RunManager.Instance;
        LevelConfig level = run != null ? run.CurrentLevel : null;

        if (level != null)
        {
            nextSpawnY = run.StageStartY + spawnIntervalY;
            spawnLimitY = run.TargetY - stopSpawnMargin;
            movingTraps = level.stageType == StageType.Survival || level.stageType == StageType.Boss;
            SpawnFixedTraps(level, run.StageStartY);

            if (level.bossPrefab != null)
            {
                // Boss bám camera nên vị trí ban đầu không quan trọng
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

    // Bẫy ở vị trí cho trước: không random, không kiểm tra va chạm, người thiết kế tự đặt offset
    private void SpawnFixedTraps(LevelConfig level, float stageStartY)
    {
        if (level.fixedTraps == null) return;

        foreach (FixedTrapSpawn fixedTrap in level.fixedTraps)
        {
            if (fixedTrap.prefab == null) continue;

            Vector3 pos = new Vector3(fixedTrap.offset.x, stageStartY + fixedTrap.offset.y, 0f);
            Track(Instantiate(fixedTrap.prefab, pos, Quaternion.identity));
        }
    }

    private void Track(GameObject obj)
    {
        if (obj != null) spawned.Add(obj);
    }

    private void SpawnRandomObject(float spawnY)
    {
        Vector3 validSpawnPos = Vector3.zero;
        bool foundValidPosition = false;

        for (int i = 0; i < 10; i++)
        {
            float randomX = Random.Range(minX, maxX);
            Vector3 checkPos = new Vector3(randomX, spawnY, 0f);

            Collider2D hitGround = Physics2D.OverlapCircle(checkPos, 0.25f, groundLayer);

            if (hitGround == null)
            {
                validSpawnPos = checkPos;
                foundValidPosition = true;
                break;
            }
        }

        if (!foundValidPosition)
        {
            validSpawnPos = new Vector3(0f, spawnY, 0f);
        }

        // Tỉ lệ coin / quái / bẫy lấy từ LevelConfig của ải hiện tại
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

        if (randomRoll < coinRate)
        {
            SqawnCoin(validSpawnPos);
        }
        else if (randomRoll < coinRate + enemyRate)
        {
            // Chia quái bay / quái đất theo tỉ lệ 2 trường serialized
            int enemySplit = airEnemySpawnChance + groundEnemySpawnChance;
            bool spawnAir = enemySplit <= 0 ? Random.value < 0.5f : Random.Range(0, enemySplit) < airEnemySpawnChance;

            if (spawnAir) SpawnAirEnemy(validSpawnPos);
            else SpawnGroundEnemy(validSpawnPos);
        }
        else
        {
            SpawnTrap(validSpawnPos);
        }
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

        Vector2 boxSize = Vector2.one;
        BoxCollider2D boxCol = trapPrefab.GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            boxSize = boxCol.size * trapPrefab.transform.localScale;
        }

        if (!TryGetAirPosition(spawnPosition, boxSize, out Vector3 finalPos)) return;

        GameObject trap = Instantiate(trapPrefab, finalPos, Quaternion.identity);
        Track(trap);

        Trap trapScript = trap.GetComponent<Trap>();
        if (trapScript != null) trapScript.SetMoving(movingTraps);
    }

    // Tìm vị trí lơ lửng đủ khoảng trống so với đất phía trên/dưới
    private bool TryGetAirPosition(Vector3 spawnPosition, Vector2 boxSize, out Vector3 finalPos)
    {
        finalPos = spawnPosition;
        float airClearance = 4.5f;

        RaycastHit2D hitUnder = Physics2D.Raycast(spawnPosition, Vector2.down, 5.0f, groundLayer);
        RaycastHit2D hitAbove = Physics2D.Raycast(spawnPosition, Vector2.up, 5.0f, groundLayer);

        float minY = float.MinValue;
        float maxY = float.MaxValue;

        if (hitUnder.collider != null) minY = hitUnder.point.y + airClearance;
        if (hitAbove.collider != null) maxY = hitAbove.point.y - airClearance;

        if (maxY <= minY) return false;

        finalPos.y = Mathf.Clamp(spawnPosition.y, minY, maxY);

        return Physics2D.OverlapBox(finalPos, boxSize * 0.9f, 0f, groundLayer) == null;
    }


    private void SpawnAirEnemy(Vector3 spawnPosition)
    {
        if (airEnemyPrefab == null) return;

        Vector3 finalAirPos = spawnPosition;
        float airClearance = 4.5f; 

        RaycastHit2D hitUnder = Physics2D.Raycast(spawnPosition, Vector2.down, 5.0f, groundLayer);

        RaycastHit2D hitAbove = Physics2D.Raycast(spawnPosition, Vector2.up, 5.0f, groundLayer);

        float minY = float.MinValue;
        float maxY = float.MaxValue;

        if (hitUnder.collider != null)
        {
            minY = hitUnder.point.y + airClearance;
        }

        if (hitAbove.collider != null)
        {
            maxY = hitAbove.point.y - airClearance;
        }

        if (maxY <= minY) return;

        finalAirPos.y = Mathf.Clamp(spawnPosition.y, minY, maxY);

        Vector2 boxSize = Vector2.one;
        BoxCollider2D boxCol = airEnemyPrefab.GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            boxSize = boxCol.size * airEnemyPrefab.transform.localScale;
        }

        Collider2D hitCheck = Physics2D.OverlapBox(finalAirPos, boxSize * 0.9f, 0f, groundLayer);

        if (hitCheck == null)
        {
            Track(Instantiate(airEnemyPrefab, finalAirPos, Quaternion.identity));
        }
    }
    private void SpawnGroundEnemy(Vector3 spawnPosition)
    {
        if (groundEnemyPrefab == null) return;

        RaycastHit2D hit = Physics2D.Raycast(spawnPosition, Vector2.down, 4.0f, groundLayer);

        if (hit.collider != null && hit.normal.y > 0.7f)
        {
            float enemyHeight = 1.0f;
            BoxCollider2D boxCol = groundEnemyPrefab.GetComponent<BoxCollider2D>();
            if (boxCol != null)
            {
                enemyHeight = (boxCol.size.y * groundEnemyPrefab.transform.localScale.y) / 2f;
            }

            Vector3 groundSpawnPos = new Vector3(spawnPosition.x, hit.point.y + enemyHeight, 0f);

            Vector2 boxSize = boxCol != null ? boxCol.size * groundEnemyPrefab.transform.localScale : new Vector2(1f, 1f);
            Collider2D hitWall = Physics2D.OverlapBox(groundSpawnPos, boxSize * 0.9f, 0f, groundLayer);

            if (hitWall == null)
            {
                Track(Instantiate(groundEnemyPrefab, groundSpawnPos, Quaternion.identity));
            }
        }
    }
    // Chọn prefab quả: ưu tiên loại đang cần của Fruit Challenge, không thì random trong fruitPrefabs
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
        GameObject coinPrefab = PickFruitPrefab();
        if (coinPrefab == null) return;

        Vector3 finalCoinPos = spawnPosition;
        float clearance = 4.5f;

        RaycastHit2D hitUnder = Physics2D.Raycast(spawnPosition, Vector2.down, 5.0f, groundLayer);
        RaycastHit2D hitAbove = Physics2D.Raycast(spawnPosition, Vector2.up, 5.0f, groundLayer);

        float minY = float.MinValue;
        float maxY = float.MaxValue;

        if (hitUnder.collider != null)
        {
            minY = hitUnder.point.y + clearance;
        }

        if (hitAbove.collider != null)
        {
            maxY = hitAbove.point.y - clearance;
        }

        if (maxY <= minY) return;

        finalCoinPos.y = Mathf.Clamp(spawnPosition.y, minY, maxY);

        float coinRadius = 0.5f;
        CircleCollider2D circleCol = coinPrefab.GetComponent<CircleCollider2D>();
        if (circleCol != null)
        {
            coinRadius = circleCol.radius * Mathf.Max(coinPrefab.transform.localScale.x, coinPrefab.transform.localScale.y);
        }

        Collider2D hitCheck = Physics2D.OverlapCircle(finalCoinPos, coinRadius * 0.9f, groundLayer);

        if (hitCheck == null)
        {
            Track(Instantiate(coinPrefab, finalCoinPos, Quaternion.identity));
        }
    }
}