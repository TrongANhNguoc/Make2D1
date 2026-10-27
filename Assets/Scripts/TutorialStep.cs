using UnityEngine;

// Hành động Player cần làm để hoàn thành 1 bước hướng dẫn
public enum TutorialAction
{
    None,           // Chỉ hiện gợi ý khi ở trong vùng, hết khi rời vùng (vd. bước Portal)
    Launch,         // Bắn (kéo-thả chuột) lần đầu
    LaunchInAir,    // Bắn khi chưa chạm đất (nhảy lần 2 trên không)
    Land,           // Chạm đất
    GroundSlam,     // Lao thẳng xuống
    EnemyStomped,   // Giẫm chết quái
    FruitCollected, // Ăn 1 quả
    Damaged         // Mất tim (chạm bẫy) hoặc rơi dung nham/KillZone
}

// Vùng trigger đặt trong prefab terrain của Tutorial (cần Collider2D isTrigger).
// Player vào vùng → TutorialManager hiện gợi ý; làm đúng hành động → ẩn và đánh dấu xong (không hiện lại).
// Rời vùng khi chưa xong → ẩn tạm, vào lại thì hiện lại. Nhiều vùng chồng nhau: bước có `order` nhỏ hơn hiện trước.
[RequireComponent(typeof(Collider2D))]
public class TutorialStep : MonoBehaviour
{
    [Tooltip("Thứ tự bước (a=0, b=1, ...). Khi nhiều vùng chồng nhau, bước chưa xong có order nhỏ nhất được hiện")]
    [SerializeField] private int order = 0;
    [SerializeField] private TutorialAction action = TutorialAction.Launch;
    [Tooltip("Nội dung gợi ý. Để trống = dùng câu mặc định theo action")]
    [TextArea(2, 4)] [SerializeField] private string message;
    [Tooltip("Icon chuột/phím hiện kèm (tùy chọn)")]
    [SerializeField] private Sprite icon;

    public int Order => order;
    public TutorialAction Action => action;
    public Sprite Icon => icon;
    public bool Completed { get; private set; }
    public string Message => string.IsNullOrEmpty(message) ? DefaultMessage(action) : message;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Completed || other.GetComponentInParent<PlayerController>() == null) return;
        if (TutorialManager.Instance != null) TutorialManager.Instance.EnterStep(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (TutorialManager.Instance != null) TutorialManager.Instance.ExitStep(this);
    }

    private void OnDisable()
    {
        // Terrain bị hủy khi đổi ải: OnTriggerExit không chắc được gọi
        if (TutorialManager.Instance != null) TutorialManager.Instance.ExitStep(this);
    }

    public void MarkCompleted()
    {
        Completed = true;
    }

    private static string DefaultMessage(TutorialAction action)
    {
        switch (action)
        {
            case TutorialAction.Launch:
                return "Giữ chuột trái và KÉO NGƯỢC hướng muốn bay, rồi thả ra để bắn.\n(Game chậm lại khi ngắm)";
            case TutorialAction.LaunchInAir:
                return "Đang trên không vẫn nhảy được lần 2 (tối đa 2 lần).\nKéo-thả thêm lần nữa!";
            case TutorialAction.Land:
                return "Chạm đất để hồi lại lượt nhảy.";
            case TutorialAction.GroundSlam:
                return "Nhấn S, mũi tên xuống hoặc chuột phải để lao thẳng xuống (Ground Slam).";
            case TutorialAction.EnemyStomped:
                return "Giẫm lên đầu quái để tiêu diệt và được thêm 1 lượt nhảy.\nChạm ngang sẽ mất 1 tim!";
            case TutorialAction.FruitCollected:
                return "Ăn quả để có điểm.\nNhảy liên tục trên không để nhân điểm combo!";
            case TutorialAction.Damaged:
                return "Chạm bẫy sẽ mất tim. Rơi vào dung nham là chết ngay\n(trong hướng dẫn bạn sẽ về checkpoint).";
            default:
                return "Bay vào Portal để qua ải.";
        }
    }
}
