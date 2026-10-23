using UnityEngine;

// Đạn / đá rơi do Boss bắn. Cần Collider2D (isTrigger). Player có Rigidbody2D nên trigger sẽ được gọi.
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 8f;

    private Vector2 direction = Vector2.down;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    public void Launch(Vector2 newDirection)
    {
        direction = newDirection.normalized;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerCollision player = other.GetComponentInParent<PlayerCollision>();
        if (player == null) return;

        player.TakeDamage(damage);
        Destroy(gameObject);
    }
}
