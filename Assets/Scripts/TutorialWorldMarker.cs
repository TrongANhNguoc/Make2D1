using UnityEngine;

public class TutorialWorldMarker : MonoBehaviour
{
    [Tooltip("Ẩn mũi tên khi bước này hoàn thành (tùy chọn)")]
    [SerializeField] private TutorialStep step;
    [SerializeField] private Color color = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private float size = 1f;
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float bobSpeed = 5f;
    [SerializeField] private int sortingOrder = 50;

    private SpriteRenderer sr;
    private Vector3 basePosition;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        sr.sprite = TutorialSprites.ArrowDown;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        transform.localScale = Vector3.one * size;
        basePosition = transform.localPosition;
    }

    private void Update()
    {
        sr.enabled = step == null || !step.Completed;

        transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight);
    }
}
