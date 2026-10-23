using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [Tooltip("Tự đặt Portal ở đỉnh ải (StageStartY + targetHeight) mỗi khi ải đổi")]
    [SerializeField] private bool placeAtTargetHeight = true;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // Dùng Start để chắc chắn RunManager.Awake đã chạy; nếu RunManager.Start chạy sau thì event sẽ đặt vị trí
    private void Start()
    {
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

        Vector3 pos = transform.position;
        pos.y = RunManager.Instance.TargetY;
        transform.position = pos;
    }
}
