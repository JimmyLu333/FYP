using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhoneDialSystem : MonoBehaviour
{
    [Header("电话输入 UI")]
    public GameObject phoneInputPanel;
    public TMP_InputField phoneInputField;
    public TextMeshProUGUI feedbackText;
    public Button confirmButton;

    [Header("正确电话")]
    public string correctPhoneNumber = "13228865437";

    [Header("聊天系统")]
    public DialogueChatBridge dialogueChatBridge;
    public string conversationName = "Case01Chat";

    void Start()
    {
        if (phoneInputPanel != null)
            phoneInputPanel.SetActive(false);

        if (feedbackText != null)
            feedbackText.text = "";

        if (confirmButton != null)
            confirmButton.onClick.AddListener(CheckPhoneNumber);
    }

    public void OpenPhoneInput()
    {
        if (phoneInputPanel != null)
            phoneInputPanel.SetActive(true);

        if (phoneInputField != null)
            phoneInputField.text = "";

        if (feedbackText != null)
            feedbackText.text = "";
    }

    public void CheckPhoneNumber()
    {
        if (phoneInputField == null) return;

        string input = phoneInputField.text.Trim();

        if (input == correctPhoneNumber)
        {
            if (phoneInputPanel != null)
                phoneInputPanel.SetActive(false);

            if (dialogueChatBridge != null)
                dialogueChatBridge.OpenChatAndStartConversation(conversationName);
            else
                Debug.LogError("DialogueChatBridge 没有绑定！");
        }
        else
        {
            if (feedbackText != null)
            {
                feedbackText.text = "电话号码错误，请重新查看目标资料。";
                feedbackText.color = Color.red;
            }
        }
    }
}