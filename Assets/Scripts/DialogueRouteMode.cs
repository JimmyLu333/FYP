using UnityEngine;
using PixelCrushers.DialogueSystem;

public enum DialogueRouteMode
{
    Chat,
    Phone
}

public class DialogueEventRouter : MonoBehaviour
{
    [Header("当前对话模式")]
    public DialogueRouteMode currentMode = DialogueRouteMode.Chat;

    [Header("普通聊天")]
    public DialogueChatBridge chatBridge;

    [Header("电话聊天")]
    public PhoneCallDialogueBridge phoneBridge;

    public void SetChatMode()
    {
        currentMode = DialogueRouteMode.Chat;
        Debug.Log("Dialogue Router: 切换到 Chat 模式");
    }

    public void SetPhoneMode()
    {
        currentMode = DialogueRouteMode.Phone;
        Debug.Log("Dialogue Router: 切换到 Phone 模式");
    }

    public void OnConversationLine(Subtitle subtitle)
    {
        if (currentMode == DialogueRouteMode.Chat)
        {
            if (chatBridge != null)
                chatBridge.OnConversationLine(subtitle);
        }
        else if (currentMode == DialogueRouteMode.Phone)
        {
            if (phoneBridge != null)
                phoneBridge.OnConversationLine(subtitle);
        }
    }

    public void OnConversationResponseMenu(Response[] responses)
    {
        if (currentMode == DialogueRouteMode.Chat)
        {
            if (chatBridge != null)
                chatBridge.OnConversationResponseMenu(responses);
        }
        else if (currentMode == DialogueRouteMode.Phone)
        {
            if (phoneBridge != null)
                phoneBridge.OnConversationResponseMenu(responses);
        }
    }
    public void OnConversationEnd(Transform actor)
    {
        if (currentMode == DialogueRouteMode.Chat)
        {
            // 暂时不用处理
        }
        else if (currentMode == DialogueRouteMode.Phone)
        {
            if (phoneBridge != null)
                phoneBridge.OnPhoneConversationEnd(actor);
        }
    }
}