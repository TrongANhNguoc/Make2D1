using UnityEngine;

public class Trap : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCooldown = 1f;

    [Header("Moving")]
    [SerializeField] private bool moving = false;
    [SerializeField] private float moveDistance = 2.5f;
    [SerializeField] private float moveSpeed = 2f;

    private Vector3 startPos;
    private float phase;
    private float nextHitTime;

    private void Start()
    {
        startPos = transform.position;
        phase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        if (!moving) return;

        float offset = Mathf.Sin(Time.time * moveSpeed + phase) * moveDistance;
        transform.position = startPos + Vector3.right * offset;
    }

    public void SetMoving(bool value)
    {
        moving = value;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (Time.time < nextHitTime) return;

        PlayerCollision player = other.GetComponentInParent<PlayerCollision>();
        if (player == null) return;

        nextHitTime = Time.time + hitCooldown;
        player.TakeDamage(damage, GetComponent<Collider2D>());
    }
}
