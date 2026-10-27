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

[RequireComponent(typeof(Collider2D))]
public class TutorialStep : MonoBehaviour
{
    [SerializeField] private int order = 0;
    [SerializeField] private TutorialAction action = TutorialAction.Launch;
    [TextArea(2, 4)] [SerializeField] private string message;
    [SerializeField] private Sprite icon;

    [SerializeField] private float lingerSeconds = 1.5f;

    [SerializeField] private bool repeatable = false;

    public bool Repeatable => repeatable || action == TutorialAction.Land;
    public float LingerSeconds => lingerSeconds;
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
        if (!Repeatable) Completed = true;
    }

    private static string DefaultMessage(TutorialAction action)
    {
        switch (action)
        {
            case TutorialAction.Launch:
                return "Hold left mouse and drag BACKWARD.\nRelease to launch!";
            case TutorialAction.LaunchInAir:
                return "Jump again in mid-air!\nDrag and release (max 2 jumps).";
            case TutorialAction.Land:
                return "Touch the ground to refill your jumps.";
            case TutorialAction.GroundSlam:
                return "Press S or right mouse\nto dive straight down!";
            case TutorialAction.EnemyStomped:
                return "Land on an enemy's head to defeat it.\nSide hits cost 1 heart!";
            case TutorialAction.FruitCollected:
                return "Collect fruit for points.\nChain jumps for a combo bonus!";
            case TutorialAction.Damaged:
                return "Traps cost 1 heart.\nLava = instant death!";
            default:
                return "Fly into the Portal to finish!";
        }
    }
}
