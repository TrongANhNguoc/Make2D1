using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [Tooltip("Tự đặt Portal ở đỉnh ải (StageStartY + targetHeight) mỗi khi ải đổi")]
    [SerializeField] private bool placeAtTargetHeight = true;

    // Vị trí x đặt trong scene: ải không có PortalPoint thì quay về x này
    private float defaultX;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // Dùng Start để chắc chắn RunManager.Awake đã chạy; nếu RunManager.Start chạy sau thì event sẽ đặt vị trí
    private void Start()
    {
        defaultX = transform.position.x;

        if (RunManager.Instance == null) return;

        RunManager.Instance.OnStageChanged += HandleStageChanged;
        PlaceAtTarget();
    }

    private void OnDestroy()
    {
        if (RunManager.Instance != null) RunManager.Instance.OnStageChanged -= HandleStageChanged;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (RunManager.Instance == null) return;

        RunManager.Instance.LoadNextStage();
    }

    private void HandleStageChanged(BiomeData biome, LevelConfig level)
    {
        PlaceAtTarget();
    }

    private void PlaceAtTarget()
    {
        if (!placeAtTargetHeight || RunManager.Instance == null || RunManager.Instance.CurrentLevel == null) return;

        RunManager run = RunManager.Instance;
        Vector3 pos = transform.position;

        // Map có PortalPoint: đặt đúng vị trí đó (cả x và y); không thì chỉ chỉnh độ cao
        if (run.HasPortalPoint)
        {
            pos.x = run.PortalPointPosition.x;
            pos.y = run.PortalPointPosition.y;
        }
        else
        {
            pos.x = defaultX;
            pos.y = run.TargetY;
        }

        transform.position = pos;
    }
}
