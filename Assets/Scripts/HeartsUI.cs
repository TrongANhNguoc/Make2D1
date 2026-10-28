using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsUI : MonoBehaviour
{
    [SerializeField] private Image heartTemplate;
    [Tooltip("Sprite trái tim đầy. Trống = dùng sprite của template")]
    [SerializeField] private Sprite fullHeart;
    [Tooltip("Sprite trái tim mất. Trống = ẩn hẳn trái tim khi mất máu")]
    [SerializeField] private Sprite emptyHeart;

    private readonly List<Image> hearts = new List<Image>();
    private PlayerCollision player;

    private void Start()
    {
        player = FindObjectOfType<PlayerCollision>();
        if (player == null || heartTemplate == null) return;

        heartTemplate.gameObject.SetActive(false);
        if (fullHeart == null) fullHeart = heartTemplate.sprite;

        BuildHearts(player.MaxHealth);
        player.OnHealthChanged += Refresh;
        Refresh(player.CurrentHealth, player.MaxHealth);
    }

    private void OnDestroy()
    {
        if (player != null) player.OnHealthChanged -= Refresh;
    }

    private void BuildHearts(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Image heart = Instantiate(heartTemplate, heartTemplate.transform.parent);
            heart.gameObject.SetActive(true);
            hearts.Add(heart);
        }
    }

    private void Refresh(int current, int max)
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            bool alive = i < current;

            if (emptyHeart != null)
            {
                hearts[i].enabled = true;
                hearts[i].sprite = alive ? fullHeart : emptyHeart;
            }
            else
            {
                hearts[i].enabled = alive;
            }
        }
    }
}
