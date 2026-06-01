using UnityEngine;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;

public class StartPhoneCallOnClick : MonoBehaviour
{
    public PhoneCallDialogueBridge phoneCallDialogueBridge;
    public DialogueEventRouter router;

    public string conversationName = "Case02PhoneCall";

    private Button button;
    private bool hasStarted = false;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    public void StartPhoneCall()
    {
        if (phoneCallDialogueBridge == null)
        {
            Debug.LogError("PhoneCallDialogueBridge 没有绑定！");
            return;
        }

        // 切换到电话模式
        if (router != null)
            router.SetPhoneMode();

        if (DialogueManager.isConversationActive)
        {
            DialogueManager.StopConversation();
        }

        phoneCallDialogueBridge.OpenPhoneCallAndStartConversation(conversationName);

        hasStarted = true;
    }
}