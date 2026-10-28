using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class FakeRewardedAd : MonoBehaviour, IRewardedAd
{
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float duration = 5f;

    private bool isShowing;

    private void Start()
    {
        if (videoPanel != null) videoPanel.SetActive(false);
    }

    public void Show(Action onCompleted)
    {
        if (isShowing) return;
        StartCoroutine(PlayRoutine(onCompleted));
    }

    private IEnumerator PlayRoutine(Action onCompleted)
    {
        isShowing = true;

        if (videoPanel != null)
        {
            videoPanel.transform.SetAsLastSibling();
            videoPanel.SetActive(true);
        }

        float remaining = duration;
        while (remaining > 0f)
        {
            if (countdownText != null) countdownText.text = Mathf.CeilToInt(remaining).ToString();
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (videoPanel != null) videoPanel.SetActive(false);
        isShowing = false;

        onCompleted?.Invoke();
    }
}
