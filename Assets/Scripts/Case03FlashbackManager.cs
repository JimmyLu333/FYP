using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Case03FlashbackManager : MonoBehaviour
{
    [Header("闪回主面板")]
    public GameObject flashbackPanel;
    public CanvasGroup flashbackCanvasGroup;

    [Header("闪回内容")]
    public Image flashbackImage;
    public TextMeshProUGUI flashbackText;

    [Header("时间设置")]
    public float fadeInDuration = 0.08f;
    public float holdDuration = 0.6f;
    public float fadeOutDuration = 0.25f;

    [Header("抖动设置")]
    public RectTransform flashbackRoot;
    public float shakeAmount = 12f;
    public float shakeSpeed = 0.02f;

    private Vector2 originalPos;
    private Coroutine flashbackCoroutine;

    void Start()
    {
        if (flashbackPanel != null)
            flashbackPanel.SetActive(false);

        if (flashbackCanvasGroup != null)
            flashbackCanvasGroup.alpha = 0f;

        if (flashbackRoot != null)
            originalPos = flashbackRoot.anchoredPosition;
    }

    public void PlayFlashback()
    {
        if (flashbackCoroutine != null)
            StopCoroutine(flashbackCoroutine);

        flashbackCoroutine = StartCoroutine(FlashbackRoutine());
    }

    public void PlayFlashbackWithText(string text)
    {
        if (flashbackText != null)
            flashbackText.text = text;

        PlayFlashback();
    }

    private IEnumerator FlashbackRoutine()
    {
        if (flashbackPanel != null)
            flashbackPanel.SetActive(true);

        yield return StartCoroutine(FadeCanvasGroup(flashbackCanvasGroup, 0f, 1f, fadeInDuration));

        float timer = 0f;

        while (timer < holdDuration)
        {
            timer += shakeSpeed;

            if (flashbackRoot != null)
            {
                float x = Random.Range(-shakeAmount, shakeAmount);
                float y = Random.Range(-shakeAmount, shakeAmount);
                flashbackRoot.anchoredPosition = originalPos + new Vector2(x, y);
            }

            yield return new WaitForSeconds(shakeSpeed);
        }

        if (flashbackRoot != null)
            flashbackRoot.anchoredPosition = originalPos;

        yield return StartCoroutine(FadeCanvasGroup(flashbackCanvasGroup, 1f, 0f, fadeOutDuration));

        if (flashbackPanel != null)
            flashbackPanel.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float time = 0f;
        group.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }

        group.alpha = to;
    }
}