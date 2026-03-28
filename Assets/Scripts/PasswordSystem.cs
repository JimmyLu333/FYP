using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PasswordSystem : MonoBehaviour
{
    [Header("UI 元素")]
    public TMP_InputField inputField;
    public Button submitButton;
    public TextMeshProUGUI feedbackText;
    public GameObject npcBubble;
    public GameObject desktopIconsPanel;
    public RectTransform inputFieldTransform;

    [Header("设置")]
    public string correctPassword = "88888888";

    [Header("气泡设置")]
    public float fadeDuration = 0.5f;
    public float stayDuration = 3f;

    [Header("震动设置")]
    public float shakeDuration = 0.25f;
    public float shakeMagnitude = 8f;

    private CanvasGroup npcCanvasGroup;
    private Coroutine bubbleCoroutine;
    private Coroutine shakeCoroutine;

    void Start()
    {
        feedbackText.text = "";
        feedbackText.gameObject.SetActive(false);
        desktopIconsPanel.SetActive(false);

        npcBubble.SetActive(true);
        npcCanvasGroup = npcBubble.GetComponent<CanvasGroup>();

        if (npcCanvasGroup == null)
        {
            Debug.LogError("NPCBubblePanel 上没有 CanvasGroup 组件！");
            return;
        }

        npcCanvasGroup.alpha = 0f;
        npcBubble.SetActive(false);

        submitButton.onClick.AddListener(CheckPassword);
    }

    void CheckPassword()
    {
        if (inputField.text == correctPassword)
        {
            UnlockDesktop();
        }
        else
        {
            ShowErrorFeedback();
            ShowBubbleSafely();
            StartShake();
        }
    }

    void ShowErrorFeedback()
    {
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = "密码错误，请再试一次。";
        feedbackText.color = Color.red;
    }

    void ShowBubbleSafely()
    {
        if (bubbleCoroutine != null)
        {
            StopCoroutine(bubbleCoroutine);
        }

        bubbleCoroutine = StartCoroutine(BubbleRoutine());
    }

    IEnumerator BubbleRoutine()
    {
        npcBubble.SetActive(true);

        float startAlpha = npcCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            npcCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, time / fadeDuration);
            yield return null;
        }

        npcCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(stayDuration);

        time = 0f;
        startAlpha = npcCanvasGroup.alpha;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            npcCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, time / fadeDuration);
            yield return null;
        }

        npcCanvasGroup.alpha = 0f;
        npcBubble.SetActive(false);
        bubbleCoroutine = null;
    }

    void StartShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(ShakeUI());
    }

    IEnumerator ShakeUI()
    {
        Vector3 originalPos = inputFieldTransform.localPosition;
        float time = 0f;

        while (time < shakeDuration)
        {
            time += Time.deltaTime;

            float offsetX = Random.Range(-1f, 1f) * shakeMagnitude;
            inputFieldTransform.localPosition = originalPos + new Vector3(offsetX, 0f, 0f);

            yield return null;
        }

        inputFieldTransform.localPosition = originalPos;
        shakeCoroutine = null;
    }

    void UnlockDesktop()
    {
        inputField.gameObject.SetActive(false);
        submitButton.gameObject.SetActive(false);
        feedbackText.gameObject.SetActive(false);
        npcBubble.gameObject.SetActive(false);
        desktopIconsPanel.SetActive(true);
    }
}