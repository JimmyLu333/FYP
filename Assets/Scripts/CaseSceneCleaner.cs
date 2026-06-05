using UnityEngine;
using PixelCrushers.DialogueSystem;

public class CaseSceneCleaner : MonoBehaviour
{
    [Header("聊天系统")]
    public ChatUIManager chatUIManager;
    public DialogueChatBridge dialogueChatBridge;

    [Header("是否进入场景自动清理")]
    public bool cleanOnStart = true;

    void Start()
    {
        if (cleanOnStart)
            CleanScene();
    }

    public void CleanScene()
    {
        if (DialogueManager.isConversationActive)
        {
            Debug.Log("CaseSceneCleaner: 关闭旧 Dialogue Conversation");
            DialogueManager.StopConversation();
        }

        if (chatUIManager != null)
        {
            Debug.Log("CaseSceneCleaner: 清空聊天记录");
            chatUIManager.ClearChat();
        }

        if (dialogueChatBridge != null)
        {
            Debug.Log("CaseSceneCleaner: 重置 DialogueChatBridge");
            dialogueChatBridge.ResetChatStateOnly();
        }
    }
}