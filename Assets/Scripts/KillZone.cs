using UnityEngine;

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
