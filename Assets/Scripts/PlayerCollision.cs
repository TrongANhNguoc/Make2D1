using System;
using UnityEngine;
using TMPro;

public class PlayerCollision : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Health Player")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("Attack Enemy")]
    [SerializeField] private float bounceForce = 6f;
    [SerializeField] private int enemyKillPoints = 100;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private PlayerController playerController;
    private bool isDead = false;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    // (máu hiện tại, máu tối đa) — HeartsUI đăng ký để vẽ trái tim
    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        playerController = GetComponent<PlayerController>();
        UpdateHealthUI();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag("Enemy"))
        {
            ContactPoint2D contact = collision.contacts[0];
            if (contact.normal.y > 0.5f)
            {
                rb.velocity = new Vector2(rb.velocity.x, bounceForce);
                playerController.AddExtraJump(1);
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddPoints(enemyKillPoints);
                Destroy(collision.gameObject);
            }
            else
            {
                TakeDamage(1);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        UpdateHealthUI();
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Chết ngay (vd. hết giờ ải Speed Rush)
    public void Kill()
    {
        TakeDamage(currentHealth);
    }

    private void Die()
    {
        isDead = true;
        playerCollider.enabled = false;
        rb.velocity = new Vector2(0, 5f);
        playerController.enabled = false;

        if (GameManager.Instance != null) GameManager.Instance.GameOver();
        Invoke(nameof(ReloadScene), 2f);
    }

    private void UpdateHealthUI()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (healthText != null)
        {
            healthText.text = "HP: " + currentHealth + "/" + maxHealth;
        }
    }

    private void ReloadScene()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            Time.timeScale = 1f;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}
