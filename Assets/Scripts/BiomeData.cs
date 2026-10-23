using UnityEngine;

[CreateAssetMenu(fileName = "BiomeData", menuName = "Make2D1/Biome Data")]
public class BiomeData : ScriptableObject
{
    public const int StagesPerBiome = 4;

    public string biomeName = "Khu Vườn";
    public Color backgroundColor = Color.white;

    [Tooltip("4 LevelConfig theo thứ tự ải 1 → 4")]
    public LevelConfig[] levels = new LevelConfig[StagesPerBiome];

    public int StageCount => levels != null ? levels.Length : 0;

    // index 0-based; trả về null nếu ngoài phạm vi
    public LevelConfig GetLevel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length) return null;
        return levels[index];
    }

    private void OnValidate()
    {
        if (levels == null || levels.Length != StagesPerBiome)
        {
            Debug.LogWarning(name + ": Biome cần đúng " + StagesPerBiome + " LevelConfig.", this);
        }
    }
}
