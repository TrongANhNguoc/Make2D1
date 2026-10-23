using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Đồng hồ đếm ngược dạng quay cho vòng Fruit Challenge (ải 1): hình tròn quét theo chiều kim đồng hồ,
// kim (tùy chọn) quay theo. Tự ẩn khi ải không có challenge.
public class RoundTimerUI : MonoBehaviour
{
    [Tooltip("Image hình tròn; script tự đặt Type = Filled, Radial 360, xuất phát từ đỉnh")]
    [SerializeField] private Image clockFill;
    [Tooltip("Kim đồng hồ (tùy chọn): RectTransform có pivot ở tâm đồng hồ, hướng lên trên")]
    [SerializeField] private RectTransform clockHand;
    [SerializeField] private TextMeshProUGUI secondsText;
    [SerializeField] private GameObject root;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.3f, 0.8f, 0.4f);
    [SerializeField] private Color warningColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private float warningSeconds = 1.5f;

    private void Awake()
    {
        if (clockFill == null) return;

        clockFill.type = Image.Type.Filled;
        clockFill.fillMethod = Image.FillMethod.Radial360;
        clockFill.fillOrigin = (int)Image.Origin360.Top;
        // Ngược chiều kim đồng hồ: phần còn lại thu dần theo chiều kim đồng hồ như kim quay
        clockFill.fillClockwise = false;
    }

    // Tắt `root` chỉ an toàn khi script nằm NGOÀI root. Nếu script là con của root (hoặc chính root) thì tắt root
    // sẽ tắt luôn Update → đồng hồ không bao giờ hiện lại ở ải sau, nên chỉ ẩn từng phần.
    private void SetVisible(bool visible)
    {
        bool scriptInsideRoot = root != null && transform.IsChildOf(root.transform);

        if (root != null && !scriptInsideRoot)
        {
            if (root.activeSelf != visible) root.SetActive(visible);
            return;
        }

        if (scriptInsideRoot)
        {
            // Nền của root (nếu có) cũng ẩn theo
            UnityEngine.UI.Graphic rootGraphic = root.GetComponent<UnityEngine.UI.Graphic>();
            if (rootGraphic != null) rootGraphic.enabled = visible;
        }

        if (clockFill != null) clockFill.enabled = visible;
        if (clockHand != null) clockHand.gameObject.SetActive(visible);
        if (secondsText != null) secondsText.gameObject.SetActive(visible);
    }

    private void Update()
    {
        FruitChallengeManager challenge = FruitChallengeManager.Instance;
        bool show = challenge != null && challenge.IsActive && challenge.RoundTime > 0f;

        SetVisible(show);
        if (!show) return;

        float fraction = Mathf.Clamp01(challenge.RoundTimeRemaining / challenge.RoundTime);

        if (clockFill != null)
        {
            clockFill.fillAmount = fraction;
            clockFill.color = challenge.RoundTimeRemaining <= warningSeconds ? warningColor : normalColor;
        }

        if (clockHand != null) clockHand.localEulerAngles = new Vector3(0f, 0f, fraction * 360f);

        if (secondsText != null) secondsText.text = Mathf.CeilToInt(challenge.RoundTimeRemaining).ToString();
    }
}
