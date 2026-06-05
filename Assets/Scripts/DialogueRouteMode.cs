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

    [Header("Case02 电话App解锁")]
    public Case02PhoneAppUnlocker case02PhoneAppUnlocker;

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
        Debug.Log("Router 收到台词：" + subtitle.formattedText.text);

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
            // Case02 第一段 Chat 结束后，解锁电话 App4
            if (case02PhoneAppUnlocker != null)
                case02PhoneAppUnlocker.UnlockApp4();
        }
        else if (currentMode == DialogueRouteMode.Phone)
        {
            if (phoneBridge != null)
                phoneBridge.OnPhoneConversationEnd(actor);
        }
    }
}