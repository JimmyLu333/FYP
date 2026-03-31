using TMPro;
using UnityEngine;

public class LeftBubbleUI : MonoBehaviour
{
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI messageText;

    public void SetData(string speakerName, string message)
    {
        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
        }

        if (messageText != null)
        {
            messageText.text = message;
        }
    }
}