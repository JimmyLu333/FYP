using UnityEngine;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;

public class StartDialogueOnClick : MonoBehaviour
{
    public DialogueChatBridge dialogueChatBridge;
    public string conversationName = "ScamChatExample";

    private Button button;
    private bool hasStarted = false;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    public void StartChat()
    {
        if (hasStarted) return;

        hasStarted = true;

        if (button != null)
            button.interactable = false;

        // 如果之前还有残留对话，先停掉
        if (DialogueManager.isConversationActive)
        {
            DialogueManager.StopConversation();
        }

        if (dialogueChatBridge != null)
        {
            dialogueChatBridge.OpenChatAndStartConversation(conversationName);
        }
        else
        {
            Debug.LogError("DialogueChatBridge 没有绑定！");
        }
    }
}