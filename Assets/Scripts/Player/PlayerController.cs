using UnityEngine;

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
    private Rigidbody2D rb;
    private LineRenderer lineRenderer;

    private bool isUnstuckRoutineRunning = false;

    private Vector2 dragStartPos;
    private Vector2 dragCurrentPos;
    private bool isDragging = false;
    private int remainingJumps;
    private bool isGrounded;

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
        if (rb.velocity.y <= 0.1f && !isDragging && !isUnstuckRoutineRunning)
        {
            StartCoroutine(DelayedUnstuckRoutine());
        }
        CheckGround();
        HandleInput();
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

            Vector3 safePosition = currentPos + (Vector3)(pushDirection * (playerRadius * 1.1f));

            Collider2D doubleCheck = Physics2D.OverlapCircle(safePosition, playerRadius * 0.7f, groundLayer);

            if (doubleCheck == null)
            {
                rb.MovePosition(safePosition);
                rb.velocity = Vector2.zero;
                ResetJumps();
            }
            else
            {
                Vector3 emergencyPos = new Vector3(currentPos.x, closestPoint.y + (playerRadius * 1.6f), 0f);
                rb.MovePosition(emergencyPos);
                rb.velocity = Vector2.zero;
                ResetJumps();
            }
        }
    }
    private void CheckGround()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            ResetJumps();
        }
    }

    private void HandleInput()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Bắt đầu kéo
        if (Input.GetMouseButtonDown(0) && remainingJumps > 0)
        {
            isDragging = true;
            dragStartPos = mouseWorldPos;

            Time.timeScale = timeSlowFactor;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

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

            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;

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
    }

    private void GroundSlam()
    {
        if (!isGrounded)
        {
            rb.velocity = new Vector2(0f, -slamForce);
        }
    }

    public void ResetJumps()
    {
        remainingJumps = maxJumps;
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