using UnityEngine;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;

public class StartPhoneCallOnClick : MonoBehaviour
{
    public PhoneCallDialogueBridge phoneCallDialogueBridge;
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
            Debug.LogError("PhoneCallDialogueBridge Ã»ÓÐ°ó¶¨£¡");
            return;
        }

        if (DialogueManager.isConversationActive)
        {
            DialogueManager.StopConversation();
        }

        phoneCallDialogueBridge.OpenPhoneCallAndStartConversation(conversationName);

        hasStarted = true;
    }
}