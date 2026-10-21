using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    private const float PLAYER_DISTANCE_SPANW_LEVEL = 100f;
    [SerializeField] private Transform level_Start;
    [SerializeField] private List<Transform> levelList;
    [SerializeField] private Player player;
    private Vector3 lastEndPosition;
    private void Awake()
    {
        lastEndPosition = level_Start.Find("EndPosition").position;
        
        int startingSpawnLevel = 5;
        for(int i = 0; i < startingSpawnLevel; i++)
        {
        SpawnLevel();
        }
    }
    private void Update()
    {
        if(Vector3.Distance(player.GetPosition(), lastEndPosition) < PLAYER_DISTANCE_SPANW_LEVEL)
        {
            SpawnLevel();
        }
        
    }

    private void SpawnLevel()
    {
        Transform chosenLevel = levelList[Random.Range(0, levelList.Count)];
        Transform lastLevelTransform = SpawnLevel(chosenLevel, lastEndPosition);
        lastEndPosition = lastLevelTransform.Find("EndPosition").position;
    }
        private Transform SpawnLevel(Transform level, Vector3 spawnPosition)
    {
            Transform levelTransform = Instantiate (level, spawnPosition, Quaternion.identity);
            return levelTransform;
    }
}
