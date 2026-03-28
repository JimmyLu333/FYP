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

    [Header("设置")]
    public string correctPassword = "88888888";

    [Header("气泡设置")]
    public float fadeDuration = 0.5f;   // 淡入淡出时间
    public float stayDuration = 3f;     // 停留时间

    private CanvasGroup npcCanvasGroup;
    private Coroutine bubbleCoroutine;   // 用来记录当前气泡协程

    void Start()
    {
        feedbackText.text = "";
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
            feedbackText.text = "密码正确，进入电脑桌面！";
            feedbackText.color = Color.green;
            UnlockDesktop();
        }
        else
        {
            feedbackText.text = "密码错误，请再试一次。";
            feedbackText.color = Color.red;

            ShowBubbleSafely();
        }
    }

    void ShowBubbleSafely()
    {
        // 如果之前已经有气泡动画在运行，先停掉
        if (bubbleCoroutine != null)
        {
            StopCoroutine(bubbleCoroutine);
        }

        // 重新开始一次新的完整显示流程
        bubbleCoroutine = StartCoroutine(BubbleRoutine());
    }

    IEnumerator BubbleRoutine()
    {
        npcBubble.SetActive(true);

        // 每次重新显示时，先从当前透明度继续往 1 走
        float startAlpha = npcCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            npcCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, time / fadeDuration);
            yield return null;
        }

        npcCanvasGroup.alpha = 1f;

        // 停留一段时间
        yield return new WaitForSeconds(stayDuration);

        // 淡出
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

    void UnlockDesktop()
    {
        inputField.gameObject.SetActive(false);
        submitButton.gameObject.SetActive(false);

        desktopIconsPanel.SetActive(true);
    }
}