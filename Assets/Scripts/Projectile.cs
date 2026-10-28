using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 8f;
    [Tooltip("Đạn ra khỏi mép camera quá khoảng này (tỉ lệ viewport) thì quay về hàng chờ")]
    [SerializeField] private float offscreenMargin = 0.15f;

    private static readonly Dictionary<Projectile, Stack<Projectile>> pools = new Dictionary<Projectile, Stack<Projectile>>();

    private Projectile sourcePrefab;
    private Vector2 direction = Vector2.down;
    private float age;
    private bool hasBeenOnScreen;
    private bool released;
    private Camera cam;

    public static Projectile Spawn(Projectile prefab, Vector3 position, Vector2 dir)
    {
        Projectile p = null;

        if (pools.TryGetValue(prefab, out Stack<Projectile> pool))
        {
            while (pool.Count > 0 && p == null) p = pool.Pop();
        }

        if (p == null)
        {
            p = Instantiate(prefab, position, Quaternion.identity);
            p.sourcePrefab = prefab;
        }
        else
        {
            p.transform.position = position;
        }

        p.Activate(dir);
        return p;
    }

    private void Activate(Vector2 dir)
    {
        direction = dir.normalized;
        age = 0f;
        hasBeenOnScreen = false;
        released = false;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        age += Time.deltaTime;
        if (age >= lifeTime || IsOffscreen()) Release();
    }

    private bool IsOffscreen()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return false;

        Vector3 v = cam.WorldToViewportPoint(transform.position);
        bool inside = v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
        if (inside) hasBeenOnScreen = true;

        bool beyondMargin = v.x < -offscreenMargin || v.x > 1f + offscreenMargin
                            || v.y < -offscreenMargin || v.y > 1f + offscreenMargin;
        return hasBeenOnScreen && beyondMargin;
    }

    private void Release()
    {
        if (released) return;
        released = true;

        if (sourcePrefab == null)
        {
            Destroy(gameObject);
            return;
        }

        gameObject.SetActive(false);

        if (!pools.TryGetValue(sourcePrefab, out Stack<Projectile> pool))
        {
            pool = new Stack<Projectile>();
            pools[sourcePrefab] = pool;
        }
        pool.Push(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerCollision player = other.GetComponentInParent<PlayerCollision>();
        if (player == null) return;

        player.TakeDamage(damage, GetComponent<Collider2D>());
        Release();
    }
}
