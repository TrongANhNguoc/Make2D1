using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
public class PlayerCollision : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private int maxJumps = 2;

    [Header("Health Player")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("Attack Enemy")]
    [SerializeField] private float bounceForce = 6f;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private bool isDead = false;
    private int remainingJumps;
    
    // Start is called before the first frame update

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        remainingJumps = maxJumps;
        playerCollider = GetComponent<Collider2D>();
        UpdateHealthUI();
    }

    // Update is called once per frame
    void Update()
    {
        
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
                AddExtraJump(1);
                Destroy(collision.gameObject);
            }
            else
            {
                TakeDamge(1);

            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Coin"))
        {
            Debug.Log("Hit Coin");
        }
    }
    public void AddExtraJump(int amount = 1)
    {
        remainingJumps = Mathf.Min(remainingJumps + amount, maxJumps);
    }
    public void TakeDamge(int damge)
    {
        currentHealth -= damge;
        UpdateHealthUI();
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        isDead = true;
        playerCollider.enabled = false;
        rb.velocity = new Vector2(0, 5f);
        GetComponent<PlayerController>().enabled = false;
        Invoke(nameof(ReloadScene), 2f);
    }

private void UpdateHealthUI()
    {
        if(healthText != null)
        {
            healthText.text = "HP: " + currentHealth + "/" + maxHealth;
        }
    }

    private void ReloadScene()
    {
        
        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}
