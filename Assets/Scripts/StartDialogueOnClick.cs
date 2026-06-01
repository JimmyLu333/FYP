using UnityEngine;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;

public class StartDialogueOnClick : MonoBehaviour
{
    public DialogueChatBridge dialogueChatBridge;
    public DialogueEventRouter router;

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

        // 切换到聊天模式
        if (router != null)
            router.SetChatMode();

        // 清理残留对话
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