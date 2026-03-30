using UnityEngine;
using PixelCrushers.DialogueSystem;

public class StartDialogueOnClick : MonoBehaviour
{
    public string conversationName = "ScamChatExample";

    public void StartChat()
    {
        DialogueManager.StartConversation(conversationName);
    }
}
