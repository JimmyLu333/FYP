using UnityEngine;
using PixelCrushers.DialogueSystem;

public class DialogueSystemHardReset : MonoBehaviour
{
    [Header("进入场景时强制重置 Dialogue System")]
    public bool resetOnStart = true;

    void Start()
    {
        if (resetOnStart)
            HardReset();
    }

    public void HardReset()
    {
        if (DialogueManager.isConversationActive)
        {
            DialogueManager.StopConversation();
        }

        if (DialogueManager.instance != null)
        {
            Destroy(DialogueManager.instance.gameObject);
        }

        Debug.Log("DialogueSystemHardReset: 已清理旧 DialogueManager 和旧 Conversation。");
    }
}