using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Launch Settings")]
    [SerializeField] private float launchForce = 12f;
    [SerializeField] private float maxDragDistance = 3f;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float timeSlowFactor = 0.2f;

    [Header("Visual Indicators")]
    [SerializeField] private LineRenderer rangeCircleRenderer;
    [SerializeField] private Transform dragPointIndicator;

    [Header("Ground Slam")]
    [SerializeField] private float slamForce = 25f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private float playerRadius = 0.5f;
    [SerializeField] private bool debugJumps = false;
    private Rigidbody2D rb;
    private LineRenderer lineRenderer;

    private bool isUnstuckRoutineRunning = false;

    private Vector2 dragStartPos;
    private Vector2 dragCurrentPos;
    private bool isDragging = false;
    private int remainingJumps;
    private bool isGrounded;
    private ContactFilter2D contactFilter;
    private readonly ContactPoint2D[] groundContacts = new ContactPoint2D[8];

    public bool IsGrounded => isGrounded;
    public int RemainingJumps => remainingJumps;

    // Bắn (tham số: lúc bắn đang đứng trên đất hay đã ở trên không) — TutorialManager lắng nghe
    public event Action<bool> OnLaunched;
    public event Action OnGroundSlam;
    public event Action OnLanded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        lineRenderer = GetComponent<LineRenderer>();
        remainingJumps = maxJumps;

        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
        if (dragPointIndicator != null)
        {
            dragPointIndicator.gameObject.SetActive(false);
        }

    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            if (isDragging) CancelDrag();
            return;
        }

        if (rb.velocity.y <= 0.1f && !isDragging && !isUnstuckRoutineRunning)
        {
            StartCoroutine(DelayedUnstuckRoutine());
        }
        CheckGround();
        HandleInput();
    }


    // Pause/Victory giữa lúc đang kéo: hủy lần ngắm để không kẹt trạng thái kéo
    public void CancelDrag()
    {
        isDragging = false;
        lineRenderer.enabled = false;
        if (rangeCircleRenderer != null) rangeCircleRenderer.enabled = false;
        if (dragPointIndicator != null) dragPointIndicator.gameObject.SetActive(false);
        SetSlowMotion(1f);
    }

    private System.Collections.IEnumerator DelayedUnstuckRoutine()
    {
        isUnstuckRoutineRunning = true;
       
        yield return new WaitForSeconds(0.2f);
        
        if (rb.velocity.y <= 0.1f && !isDragging)
        {
            CheckAndUnstuckPlayer();
        }

        isUnstuckRoutineRunning = false;
    }

    private void CheckAndUnstuckPlayer()
    {
        Collider2D stuckCollider = Physics2D.OverlapCircle(transform.position, playerRadius * 0.8f, groundLayer);

        if (stuckCollider != null)
        {
            // Nền one-way (PlatformEffector2D) cho phép bay xuyên: chỉ coi là kẹt khi đứng yên hẳn,
            // và không hồi nhảy ở đây (hạ cánh thật sẽ được CheckGround hồi)
            bool isOneWay = stuckCollider.usedByEffector;
            if (isOneWay && Mathf.Abs(rb.velocity.y) > 0.5f) return;

            Vector3 currentPos = transform.position;
            Vector2 closestPoint = stuckCollider.ClosestPoint(currentPos);

            Vector2 pushDirection = ((Vector2)currentPos - closestPoint);

            if (pushDirection.sqrMagnitude < 0.001f)
            {
                pushDirection = Vector2.up;
            }
            else
            {
                pushDirection.Normalize();
            }

            Vector3 safePosition = currentPos + (Vector3)(pushDirection * (playerRadius * 1.2f));

            Collider2D doubleCheck = Physics2D.OverlapCircle(safePosition, playerRadius * 0.7f, groundLayer);

            if (doubleCheck == null)
            {
                rb.MovePosition(safePosition);
                rb.velocity = Vector2.zero;
                // Chỉ hồi nhảy khi bị đẩy lên mặt đất, không hồi khi kẹt tường/trần
                if (!isOneWay && pushDirection.y > 0.5f) ResetJumps();
            }
            else
            {
                Vector3 emergencyPos = new Vector3(currentPos.x, closestPoint.y + (playerRadius * 1.6f), 0f);
                rb.MovePosition(emergencyPos);
                rb.velocity = Vector2.zero;
                if (!isOneWay) ResetJumps();
            }
        }
    }
    private void CheckGround()
    {
        bool wasGrounded = isGrounded;

        contactFilter.SetLayerMask(groundLayer);
        contactFilter.useTriggers = false;
        int contactCount = rb.GetContacts(contactFilter, groundContacts);

        isGrounded = false;

        // SỬA Ở ĐÂY: Thêm điều kiện rb.velocity.y <= 0.05f để khi đang bay lên, 
        // tuyệt đối không bị tính là chạm đất/reset lượt nhảy khi lướt qua bệ.
        if (rb.velocity.y <= 0.05f)
        {
            for (int i = 0; i < contactCount; i++)
            {
                ContactPoint2D contact = groundContacts[i];
                // Mặt tiếp xúc nằm ngang, nằm dưới player và không bị lún quá sâu
                if (Mathf.Abs(contact.normal.y) > 0.5f && contact.point.y < rb.position.y && contact.separation > -0.05f)
                {
                    isGrounded = true;
                    break;
                }
            }
        }

        if (isGrounded && !wasGrounded)
        {
            OnLanded?.Invoke();
            ResetJumps();
        }
    }

    private void HandleInput()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Bắt đầu kéo
        // Click trúng UI (nút menu, nút Victory...) không được tính là bắt đầu kéo
        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Input.GetMouseButtonDown(0) && remainingJumps > 0 && !pointerOverUI)
        {
            isDragging = true;
            dragStartPos = mouseWorldPos;

            SetSlowMotion(timeSlowFactor);

            lineRenderer.enabled = true;

            if (rangeCircleRenderer != null)
            {
                rangeCircleRenderer.enabled = true;
                DrawRangeCircle(dragStartPos, maxDragDistance);
            }

            if (dragPointIndicator != null)
            {
                dragPointIndicator.gameObject.SetActive(true);
                dragPointIndicator.position = dragStartPos;
            }
        }

        // Đang kéo nhắm
        if (Input.GetMouseButton(0) && isDragging)
        {
            dragCurrentPos = mouseWorldPos;
            Vector2 dragVector = dragStartPos - dragCurrentPos;

            if (dragVector.magnitude > maxDragDistance)
            {
                dragVector = dragVector.normalized * maxDragDistance;
            }

            DrawTrajectory(transform.position, dragVector * launchForce);

            if (rangeCircleRenderer != null)
            {
                DrawRangeCircle(dragStartPos, maxDragDistance);
            }

            if (dragPointIndicator != null)
            {
                dragPointIndicator.position = dragStartPos - dragVector;
            }
        }

        // Thả tay -> Bắn
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            lineRenderer.enabled = false;

            SetSlowMotion(1f);

            if (rangeCircleRenderer != null)
            {
                rangeCircleRenderer.enabled = false;
            }

            Vector2 dragVector = dragStartPos - dragCurrentPos;
            if (dragVector.magnitude > maxDragDistance)
            {
                dragVector = dragVector.normalized * maxDragDistance;
            }

            if (dragPointIndicator != null)
            {
                dragPointIndicator.gameObject.SetActive(false);
            }

            if (rangeCircleRenderer != null)
            {
                rangeCircleRenderer.enabled = false;
            }

            LaunchPlayer(dragVector);
        }

        // Dậm đất (Ground Slam) 
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetMouseButton(1) )
        {
            GroundSlam(); 
        }
    }

    private void LaunchPlayer(Vector2 direction)
    {
        rb.velocity = Vector2.zero;
        rb.AddForce(direction * launchForce, ForceMode2D.Impulse);
        remainingJumps--;
        if (ScoreManager.Instance != null) ScoreManager.Instance.RegisterJump();
        OnLaunched?.Invoke(isGrounded);
    }

    private void SetSlowMotion(float scale)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSlowMotion(scale);
        }
        else
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = 0.02f * scale;
        }
    }

    private void GroundSlam()
    {
        if (!isGrounded)
        {
            rb.velocity = new Vector2(0f, -slamForce);
            OnGroundSlam?.Invoke();
        }
    }

    public void ResetJumps()
    {
        if (debugJumps && remainingJumps != maxJumps)
        {
            Debug.Log("ResetJumps | y=" + transform.position.y + " vy=" + rb.velocity.y + " grounded=" + isGrounded + "\n" + System.Environment.StackTrace);
        }
        remainingJumps = maxJumps;
        // Hồi đủ lượt nhảy nghĩa là chuỗi nhảy liên tục kết thúc
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCombo();
    }

    public void AddExtraJump(int amount = 1)
    {
        remainingJumps = Mathf.Min(remainingJumps + amount, maxJumps);
    }

    // Trả về trạng thái bình thường khi chết/pause giữa lúc đang kéo
    private void OnDisable()
    {
        if (isDragging)
        {
            isDragging = false;
            SetSlowMotion(1f);
        }
    }


    private void DrawTrajectory(Vector3 startPoint, Vector2 launchVelocity)
    {
        int segmentCount = 15;
        lineRenderer.positionCount = segmentCount;

        Vector3[] points = new Vector3[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            float time = i * 0.05f;
            points[i] = startPoint + (Vector3)(launchVelocity * time) + 0.5f * (Vector3)Physics2D.gravity * rb.gravityScale * time * time;
        }

        lineRenderer.SetPositions(points);
    }

    
    private void DrawRangeCircle(Vector3 center, float radius)
    {
        int steps = 40;
        rangeCircleRenderer.positionCount = steps;

        for (int i = 0; i < steps; i++)
        {
            float progress = (float)i / steps;
            float currentAngle = progress * 2 * Mathf.PI;

            float x = Mathf.Cos(currentAngle) * radius;
            float y = Mathf.Sin(currentAngle) * radius;

            Vector3 point = center + new Vector3(x, y, 0f);
            rangeCircleRenderer.SetPosition(i, point);
        }
    }
 
}