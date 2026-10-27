using UnityEngine;

// Điểm hồi sinh trong ải Tutorial: Player chạm vùng trigger thì ghi nhớ vị trí này,
// chết (hết máu / rơi KillZone) sẽ được đưa về checkpoint gần nhất đã đi qua.
// transform.position = chỗ player sẽ đứng khi hồi sinh (như StartPoint); chỉnh thêm bằng respawnOffset.
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Vector2 respawnOffset = Vector2.zero;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (RunManager.Instance == null) return;

        RunManager.Instance.SetCheckpoint(transform.position + (Vector3)respawnOffset);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + (Vector3)respawnOffset, 0.4f);
    }
}
