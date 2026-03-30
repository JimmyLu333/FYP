using UnityEngine;

public class StartDialogueOnClick : MonoBehaviour
{
    public DialogueChatBridge dialogueChatBridge;
    public string conversationName = "ScamChatExample";

    public void StartChat()
    {
        if (dialogueChatBridge != null)
        {
            dialogueChatBridge.OpenChatAndStartConversation(conversationName);
        }
    }
}