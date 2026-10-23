using UnityEngine;

// Vùng nguy hiểm / vực: Player chạm vào là chết ngay.
// Prefab cần Collider2D isTrigger. Logic độc lập với sprite nên có thể đổi art thoải mái.
[RequireComponent(typeof(Collider2D))]
public class KillZone : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerCollision player = other.GetComponentInParent<PlayerCollision>();
        if (player != null) player.Kill();
    }
}
