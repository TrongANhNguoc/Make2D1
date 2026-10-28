using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [Tooltip("Tự đặt Portal ở đỉnh ải (StageStartY + targetHeight) mỗi khi ải đổi")]
    [SerializeField] private bool placeAtTargetHeight = true;

    private float defaultX;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

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
