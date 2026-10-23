using UnityEngine;

// Boss ải 4: bám góc trên màn hình, tung skill liên tục. Boss không bị tiêu diệt, ải kết thúc ở Portal.
// Prefab chỉ cần sprite + script này; `projectilePrefab` là prefab có Projectile.
public class BossController : MonoBehaviour
{
    [Header("Position")]
    [Tooltip("Vị trí neo trên viewport camera (0-1)")]
    [SerializeField] private Vector2 viewportAnchor = new Vector2(0.82f, 0.88f);
    [SerializeField] private float followSmooth = 6f;

    [Header("Skills")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float skillInterval = 2f;

    [Header("Skill: Aimed Shot (bắn thẳng vào Player)")]
    [SerializeField] private int aimedShotCount = 3;
    [SerializeField] private float aimedShotDelay = 0.25f;

    [Header("Skill: Rain (mưa đạn từ trên xuống)")]
    [SerializeField] private int rainCount = 6;

    private Camera cam;
    private Transform player;
    private float nextSkillTime;
    private int skillCycle;

    private void Start()
    {
        cam = Camera.main;
        nextSkillTime = Time.time + skillInterval;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (cam == null) return;

        FollowAnchor();

        if (Time.time >= nextSkillTime)
        {
            CastNextSkill();
            nextSkillTime = Time.time + skillInterval;
        }
    }

    private void FollowAnchor()
    {
        Vector3 target = cam.ViewportToWorldPoint(new Vector3(viewportAnchor.x, viewportAnchor.y, -cam.transform.position.z));
        target.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, target, followSmooth * Time.deltaTime);
    }

    // Luân phiên 2 skill
    private void CastNextSkill()
    {
        if (projectilePrefab == null) return;

        if (skillCycle % 2 == 0) StartCoroutine(AimedShotRoutine());
        else RainSkill();

        skillCycle++;
    }

    private System.Collections.IEnumerator AimedShotRoutine()
    {
        for (int i = 0; i < aimedShotCount; i++)
        {
            if (player == null)
            {
                PlayerController pc = FindObjectOfType<PlayerController>();
                if (pc != null) player = pc.transform;
            }

            if (player != null)
            {
                Projectile p = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
                p.Launch(player.position - transform.position);
            }

            yield return new WaitForSeconds(aimedShotDelay);
        }
    }

    // Đạn rơi thẳng xuống từ mép trên màn hình, rải đều theo chiều ngang
    private void RainSkill()
    {
        float topY = cam.ViewportToWorldPoint(new Vector3(0f, 1f, -cam.transform.position.z)).y + 1f;

        for (int i = 0; i < rainCount; i++)
        {
            float viewportX = Random.Range(0.05f, 0.95f);
            float worldX = cam.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, -cam.transform.position.z)).x;

            Projectile p = Instantiate(projectilePrefab, new Vector3(worldX, topY, 0f), Quaternion.identity);
            p.Launch(Vector2.down);
        }
    }
}
