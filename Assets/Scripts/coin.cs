using System;
using UnityEngine;

public class coin : MonoBehaviour
{
    [SerializeField] private int points = 10;
    [Tooltip("Loại quả, dùng cho cơ chế Fruit Challenge ở ải Speed Rush")]
    [SerializeField] private FruitType fruitType = FruitType.Apple;

    private bool collected = false;

    public FruitType Type => fruitType;

    // Bắn khi Player ăn quả — TutorialManager lắng nghe (static: quả là prefab sinh động, không có reference sẵn)
    public static event Action<FruitType> OnCollected;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collected) return;

        if (collision.CompareTag("Player"))
        {
            collected = true;
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddPoints(points);
            if (FruitChallengeManager.Instance != null) FruitChallengeManager.Instance.OnFruitCollected(fruitType);
            OnCollected?.Invoke(fruitType);
            Destroy(gameObject);
        }
    }
}
