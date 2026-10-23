using UnityEngine;

// Bẫy cố định kiểu "đá khổng lồ lăn xuống": đứng yên ở vị trí đặt sẵn, khi Player lại gần thì rơi/lăn xuống.
// Prefab cần: Rigidbody2D, Collider2D (Circle), Trap (gây sát thương). Không gắn tag "Enemy".
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Trap))]
public class RollingBoulder : MonoBehaviour
{
    [Header("Activation")]
    [Tooltip("Player lại gần trong bán kính này thì đá bắt đầu lăn")]
    [SerializeField] private float activateRadius = 8f;

    [Header("Rolling")]
    [Tooltip("Vận tốc ngang ban đầu (âm = sang trái, dương = sang phải, 0 = chỉ rơi thẳng)")]
    [SerializeField] private float initialHorizontalSpeed = 2f;
    [SerializeField] private float gravityScale = 2f;
    [SerializeField] private float lifeTime = 15f;

    private Rigidbody2D rb;
    private Transform player;
    private bool released;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Đứng yên cho tới khi được thả
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.zero;
    }

    private void Update()
    {
        if (released) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        if (player == null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null) player = pc.transform;
            return;
        }

        if (Vector2.Distance(player.position, transform.position) <= activateRadius)
        {
            Release();
        }
    }

    private void Release()
    {
        released = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityScale;
        rb.velocity = new Vector2(initialHorizontalSpeed, 0f);

        Destroy(gameObject, lifeTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, activateRadius);
    }
}
