using UnityEngine;

// Mũi tên nhún nhảy trong thế giới, chỉ vào quái / quả / Portal trong prefab Tutorial.
// Tự tạo SpriteRenderer (không cần art). Đặt làm CON của vật cần chỉ (vd. quái) kèm localPosition ~ (0, 1.5):
// vật bị hủy (quái bị giẫm, quả bị ăn) thì mũi tên biến mất theo. Gán `step` để mũi tên ẩn khi bước đó xong.
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

        // Thời gian thật: vẫn nhún đều khi slow-mo lúc ngắm
        transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight);
    }
}
