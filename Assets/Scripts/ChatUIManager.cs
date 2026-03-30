using UnityEngine;
using TMPro;

public class ChatUIManager : MonoBehaviour
{
    [Header("聊天内容容器")]
    public Transform contentRoot;

    [Header("气泡预制体")]
    public GameObject leftBubblePrefab;
    public GameObject rightBubblePrefab;

    public void AddLeftMessage(string speakerName, string message)
    {
        GameObject bubble = Instantiate(leftBubblePrefab, contentRoot);

        Transform nameText = bubble.transform.Find("BubbleBG/SpeakerNameText");
        Transform messageText = bubble.transform.Find("BubbleBG/MessageText");

        if (nameText != null)
        {
            nameText.GetComponent<TextMeshProUGUI>().text = speakerName;
        }

        if (messageText != null)
        {
            messageText.GetComponent<TextMeshProUGUI>().text = message;
        }
    }

    public void AddRightMessage(string message)
    {
        GameObject bubble = Instantiate(rightBubblePrefab, contentRoot);

        Transform messageText = bubble.transform.Find("BubbleBG/MessageText");

        if (messageText != null)
        {
            messageText.GetComponent<TextMeshProUGUI>().text = message;
        }
    }
}