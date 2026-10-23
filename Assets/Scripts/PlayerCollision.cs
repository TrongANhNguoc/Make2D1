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

    [Header("Revive")]
    [SerializeField] private float invincibleSeconds = 2f;
    [Tooltip("Hồi sinh cao hơn vị trí chết bao nhiêu unit")]
    [SerializeField] private float reviveLiftHeight = 1.5f;
    [Tooltip("Bước dịch lên khi chỗ hồi sinh bị chặn (KillZone / Ground / Wall)")]
    [SerializeField] private float reviveSearchStep = 0.5f;
    [SerializeField] private int reviveSearchSteps = 40;
    [Tooltip("Layer coi là kẹt khi hồi sinh. Ground và Wall luôn được tính thêm")]
    [SerializeField] private LayerMask reviveBlockLayers;
    [SerializeField] private float blinkInterval = 0.1f;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private PlayerController playerController;
    private SpriteRenderer[] sprites;
    private Color[] spriteColors;
    private bool isDead = false;
    private bool reviveUsedThisStage = false;
    private float invincibleTimer;
    private float blinkTimer;
    private float checkRadius = 0.5f;
    private Vector3 deathPosition;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool IsInvincible => invincibleTimer > 0f;
    // Mỗi ải chỉ được hồi sinh bằng video 1 lần
    public bool CanRevive => !reviveUsedThisStage;

    // (máu hiện tại, máu tối đa) — HeartsUI đăng ký để vẽ trái tim
    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        playerController = GetComponent<PlayerController>();

        sprites = GetComponentsInChildren<SpriteRenderer>();
        spriteColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) spriteColors[i] = sprites[i].color;

        Vector3 extents = playerCollider.bounds.extents;
        checkRadius = Mathf.Max(0.1f, Mathf.Max(extents.x, extents.y) * 0.9f);

        UpdateHealthUI();
    }

    private void Start()
    {
        if (RunManager.Instance != null) RunManager.Instance.OnStageChanged += HandleStageChanged;
    }

    private void OnDestroy()
    {
        if (RunManager.Instance != null) RunManager.Instance.OnStageChanged -= HandleStageChanged;
    }

    // Sang ải mới thì được quyền hồi sinh lại
    private void HandleStageChanged(BiomeData biome, LevelConfig level)
    {
        reviveUsedThisStage = false;
    }

    private void Update()
    {
        if (invincibleTimer <= 0f) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        // Dùng thời gian thật: ngắm bắn slow-mo không kéo dài thời gian bất tử
        invincibleTimer -= Time.unscaledDeltaTime;
        blinkTimer += Time.unscaledDeltaTime;

        if (invincibleTimer <= 0f)
        {
            invincibleTimer = 0f;
            SetSpriteAlpha(1f);
            return;
        }

        bool faded = Mathf.FloorToInt(blinkTimer / blinkInterval) % 2 == 1;
        SetSpriteAlpha(faded ? 0.25f : 1f);
    }

    private void SetSpriteAlpha(float factor)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;
            Color c = spriteColors[i];
            c.a *= factor;
            sprites[i].color = c;
        }
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
        if (isDead || IsInvincible) return;

        currentHealth -= damage;
        UpdateHealthUI();
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Chết ngay (vd. hết giờ ải Speed Rush, KillZone). Bị bỏ qua khi đang bất tử
    public void Kill()
    {
        TakeDamage(currentHealth);
    }

    // Hết máu: dừng game ở state Dead và chờ DeathUI (Chơi lại / Xem video hồi sinh)
    private void Die()
    {
        isDead = true;
        deathPosition = transform.position;
        playerCollider.enabled = false;
        rb.velocity = Vector2.zero;
        playerController.enabled = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDead();
        }
        else
        {
            // Không có GameManager (scene test): giữ hành vi cũ, reload sau 2s
            Invoke(nameof(ReloadScene), 2f);
        }
    }

    // Nút "Chơi lại": lúc này mới tính thua (RunManager cộng coin, random biome) rồi reload scene
    public void RestartRun()
    {
        if (GameManager.Instance != null) GameManager.Instance.GameOver();
        ReloadScene();
    }

    // Hồi sinh sau khi xem video: đầy máu, bất tử tạm thời, trả game về Playing
    public void Revive()
    {
        if (!isDead) return;

        reviveUsedThisStage = true;
        isDead = false;
        currentHealth = maxHealth;
        UpdateHealthUI();

        transform.position = FindSafeRevivePosition(deathPosition + Vector3.up * reviveLiftHeight);
        rb.position = transform.position;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        playerCollider.enabled = true;
        playerController.enabled = true;
        playerController.ResetJumps();

        invincibleTimer = invincibleSeconds;
        blinkTimer = 0f;

        if (RunManager.Instance != null) RunManager.Instance.HandleRevive();
        if (FruitChallengeManager.Instance != null) FruitChallengeManager.Instance.RestartCurrentRound();
        if (GameManager.Instance != null) GameManager.Instance.Revive();
    }

    // Dịch lên từng bước cho tới khi chỗ đó không chạm KillZone và không kẹt trong Ground/Wall
    private Vector3 FindSafeRevivePosition(Vector3 start)
    {
        for (int i = 0; i <= reviveSearchSteps; i++)
        {
            Vector3 candidate = start + Vector3.up * (reviveSearchStep * i);
            if (!IsBlocked(candidate)) return candidate;
        }

        Debug.LogWarning("PlayerCollision: không tìm được chỗ hồi sinh trống, dùng vị trí ban đầu.");
        return start;
    }

    private bool IsBlocked(Vector3 position)
    {
        int mask = reviveBlockLayers.value | LayerMask.GetMask("Ground", "Wall");
        if (Physics2D.OverlapCircle(position, checkRadius, mask) != null) return true;

        // OverlapCircleAll gồm cả trigger (KillZone là trigger)
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, checkRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.GetComponentInParent<KillZone>() != null) return true;
        }
        return false;
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
