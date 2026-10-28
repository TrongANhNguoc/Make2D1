using UnityEngine;
using UnityEngine.UI;

public static class TutorialSprites
{
    private const int Size = 64;
    private static Sprite circle;
    private static Sprite arrowDown;

    public static Sprite Circle => circle != null ? circle : (circle = Build((x, y) =>
    {
        float d = Vector2.Distance(new Vector2(x, y), new Vector2(Size / 2f - 0.5f, Size / 2f - 0.5f));
        return Mathf.Clamp01(Size / 2f - d);
    }));

    public static Sprite ArrowDown => arrowDown != null ? arrowDown : (arrowDown = Build((x, y) =>
    {
        float half = y < Size / 2 ? y * 0.97f : 8f;
        return Mathf.Clamp01(half - Mathf.Abs(x - (Size / 2f - 0.5f)) + 0.5f);
    }));

    private static Sprite Build(System.Func<int, int, float> alphaAt)
    {
        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        Color32[] pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                pixels[y * Size + x] = new Color(1f, 1f, 1f, alphaAt(x, y));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }
}

public class TutorialDemoUI : MonoBehaviour
{
    private enum Mode { None, Drag, Slam, Stomp }

    private static readonly Color PlayerColor = new Color(0.3f, 0.7f, 1f);
    private static readonly Color EnemyColor = new Color(0.9f, 0.25f, 0.25f);
    private static readonly Color ArrowColor = new Color(1f, 0.85f, 0.2f);

    private Mode mode;
    private bool built;
    private Image ground, enemy, player, hand, band, arrow;

    public bool Show(TutorialAction action)
    {
        Build();

        switch (action)
        {
            case TutorialAction.Launch:
            case TutorialAction.LaunchInAir: mode = Mode.Drag; break;
            case TutorialAction.GroundSlam: mode = Mode.Slam; break;
            case TutorialAction.EnemyStomped: mode = Mode.Stomp; break;
            default: mode = Mode.None; break;
        }

        gameObject.SetActive(mode != Mode.None);
        return mode != Mode.None;
    }

    public void Hide()
    {
        mode = Mode.None;
        gameObject.SetActive(false);
    }

    private void Build()
    {
        if (built) return;
        built = true;

        ground = Make("Ground", null, new Color(0.8f, 0.8f, 0.8f), new Vector2(160f, 8f));
        enemy = Make("Enemy", null, EnemyColor, new Vector2(36f, 36f));
        player = Make("Player", TutorialSprites.Circle, PlayerColor, new Vector2(26f, 26f));
        band = Make("Band", null, new Color(1f, 1f, 1f, 0.6f), new Vector2(10f, 3f));
        arrow = Make("Arrow", TutorialSprites.ArrowDown, ArrowColor, new Vector2(30f, 30f));
        hand = Make("Hand", TutorialSprites.Circle, new Color(1f, 1f, 1f, 0.9f), new Vector2(22f, 22f));
    }

    private Image Make(string objName, Sprite sprite, Color color, Vector2 size)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private void Update()
    {
        switch (mode)
        {
            case Mode.Drag: AnimateDrag(); break;
            case Mode.Slam: AnimateSlam(); break;
            case Mode.Stomp: AnimateStomp(); break;
        }
    }

    private void AnimateDrag()
    {
        float k = Time.unscaledTime % 2.6f;
        Vector2 origin = new Vector2(-15f, -25f);
        Vector2 pull = origin + new Vector2(-55f, -45f);
        Vector2 dir = (origin - pull).normalized;
        float pullAmount = Smooth(k, 0.3f, 1.2f);
        bool holding = k < 1.6f;

        SetVisible(ground, false);
        SetVisible(enemy, false);
        SetVisible(hand, holding);
        SetVisible(band, holding && pullAmount > 0f);
        SetVisible(arrow, holding && pullAmount > 0f);

        Vector2 handPos = Vector2.Lerp(origin, pull, pullAmount);
        hand.rectTransform.anchoredPosition = handPos;
        SetBand(origin, handPos);
        SetArrow(origin, dir, 20f + 70f * pullAmount);

        Vector2 playerPos = origin;
        if (!holding && k < 2.4f)
        {
            float f = (k - 1.6f) / 0.8f;
            playerPos = origin + dir * (f * 110f) + Vector2.down * (40f * f * f);
        }
        player.rectTransform.anchoredPosition = playerPos;
    }

    private void AnimateSlam()
    {
        float k = Time.unscaledTime % 1.6f;
        float f = Mathf.InverseLerp(0.2f, 0.7f, k);

        SetVisible(ground, true);
        SetVisible(enemy, false);
        SetVisible(hand, false);
        SetVisible(band, false);
        SetVisible(arrow, true);

        ground.rectTransform.anchoredPosition = new Vector2(0f, -66f);
        player.rectTransform.anchoredPosition = new Vector2(-25f, Mathf.Lerp(55f, -53f, f * f));
        SetArrow(new Vector2(35f, 55f), Vector2.down, 80f);
        arrow.color = new Color(ArrowColor.r, ArrowColor.g, ArrowColor.b, 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 8f));
    }

    private void AnimateStomp()
    {
        float k = Time.unscaledTime % 2.4f;

        SetVisible(ground, true);
        SetVisible(enemy, k < 0.6f || k >= 2.2f);
        SetVisible(hand, false);
        SetVisible(band, false);
        SetVisible(arrow, false);

        ground.rectTransform.anchoredPosition = new Vector2(0f, -66f);
        enemy.rectTransform.anchoredPosition = new Vector2(0f, -48f);

        float y;
        if (k < 0.6f)
        {
            float f = k / 0.6f;
            y = Mathf.Lerp(60f, -17f, f * f);
        }
        else if (k < 1.5f)
        {
            float b = (k - 0.6f) / 0.9f;
            y = -17f + 220f * b * (1f - b);
        }
        else
        {
            y = k < 2.2f ? -17f : 60f;
        }
        player.rectTransform.anchoredPosition = new Vector2(0f, y);
    }

    private static float Smooth(float k, float from, float to)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, k));
    }

    private static void SetVisible(Image img, bool visible)
    {
        if (img.enabled != visible) img.enabled = visible;
    }

    private void SetBand(Vector2 a, Vector2 b)
    {
        RectTransform rt = band.rectTransform;
        Vector2 d = b - a;
        rt.anchoredPosition = (a + b) * 0.5f;
        rt.sizeDelta = new Vector2(d.magnitude, 3f);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    private void SetArrow(Vector2 from, Vector2 dir, float length)
    {
        RectTransform rt = arrow.rectTransform;
        rt.anchoredPosition = from + dir * (length * 0.5f);
        rt.sizeDelta = new Vector2(30f, length);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);
    }
}
