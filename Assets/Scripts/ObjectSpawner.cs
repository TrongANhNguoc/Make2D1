using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject groundEnemyPrefab;
    [SerializeField] private GameObject airEnemyPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float spawnDistanceY = 15f;
    [SerializeField] private float spawnIntervalY = 5f;
    [SerializeField] private float minX = -4f;
    [SerializeField] private float maxX = 4f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Spawn Rates")]
    [Range(0, 100)][SerializeField] private int coinSpawnChance = 70;
    [Range(0, 100)][SerializeField] private int airEnemySpawnChance = 15;
    [Range(0, 100)][SerializeField] private int groundEnemySpawnChance = 15;

    private float nextSpawnY;

    private void Start()
    {
        if (playerTransform != null)
        {
            nextSpawnY = playerTransform.position.y + spawnIntervalY;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        if (playerTransform.position.y + spawnDistanceY >= nextSpawnY)
        {
            SpawnRandomObject(nextSpawnY);
            nextSpawnY += spawnIntervalY;
        }
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

        int totalChance = coinSpawnChance + airEnemySpawnChance + groundEnemySpawnChance;
        if (totalChance <= 0) totalChance = 100;

        int randomRoll = Random.Range(0, totalChance);

        if (randomRoll < coinSpawnChance)
        {
            SqawnCoin(validSpawnPos);
        }
        else if (randomRoll < coinSpawnChance + airEnemySpawnChance)
        {
            SpawnAirEnemy(validSpawnPos);
        }
        else
        {
            SpawnGroundEnemy(validSpawnPos);
        }
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
            Instantiate(airEnemyPrefab, finalAirPos, Quaternion.identity);
        }
    }
    private void SpawnGroundEnemy(Vector3 spawnPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(spawnPosition, Vector2.down, 4.0f, groundLayer);


        if (hit.collider != null && hit.normal.y > 1.0f && groundEnemyPrefab != null)
        {

            Vector3 groundSpawnPos = new Vector3(spawnPosition.x, hit.point.y + 2.0f, 0f);


            Collider2D hitWall = Physics2D.OverlapCircle(groundSpawnPos, 1.2f, groundLayer);

            if (hitWall == null)
            {
                Instantiate(groundEnemyPrefab, groundSpawnPos, Quaternion.identity);
            }

        }
    }
    private void SqawnCoin(Vector3 spawnPosition)
    {
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
            Instantiate(coinPrefab, finalCoinPos, Quaternion.identity);
        }
    }
}