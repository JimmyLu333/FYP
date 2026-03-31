using UnityEngine;
using UnityEngine.UI;

public class ChatUIManager : MonoBehaviour
{
    [Header("聊天内容容器")]
    public Transform contentRoot;

    [Header("气泡预制体")]
    public GameObject leftBubblePrefab;
    public GameObject rightBubblePrefab;

    [Header("滚动区域")]
    public ScrollRect scrollRect;

    public void AddLeftMessage(string speakerName, string message)
    {
        GameObject bubble = Instantiate(leftBubblePrefab, contentRoot);

        LeftBubbleUI bubbleUI = bubble.GetComponent<LeftBubbleUI>();
        if (bubbleUI != null)
        {
            bubbleUI.SetData(speakerName, message);
        }
        else
        {
            Debug.LogError("LeftBubble prefab 上没有 LeftBubbleUI 组件！");
        }

        Canvas.ForceUpdateCanvases();
        ScrollToBottom();
    }

    public void AddRightMessage(string speakerName, string message)
    {
        GameObject bubble = Instantiate(rightBubblePrefab, contentRoot);

        RightBubbleUI bubbleUI = bubble.GetComponent<RightBubbleUI>();
        if (bubbleUI != null)
        {
            bubbleUI.SetData(speakerName, message);
        }
        else
        {
            Debug.LogError("RightBubble prefab 上没有 RightBubbleUI 组件！");
        }

        Canvas.ForceUpdateCanvases();
        ScrollToBottom();
    }

    public void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}