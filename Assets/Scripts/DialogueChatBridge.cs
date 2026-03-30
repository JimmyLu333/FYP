using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelCrushers.DialogueSystem;

public class DialogueChatBridge : MonoBehaviour
{
    [Header("聊天UI")]
    public GameObject chatPanel;
    public ChatUIManager chatUIManager;
    public TextMeshProUGUI nameText;

    [Header("选项按钮")]
    public Button choiceButton1;
    public Button choiceButton2;
    public Button choiceButton3;

    [Header("按钮文字")]
    public TextMeshProUGUI choiceButtonText1;
    public TextMeshProUGUI choiceButtonText2;
    public TextMeshProUGUI choiceButtonText3;

    private Response[] currentResponses;

    private void Start()
    {
        HideChoiceButtons();
    }

    public void OpenChatAndStartConversation(string conversationTitle)
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(true);
        }

        ClearOldChoices();
        DialogueManager.StartConversation(conversationTitle);
    }

    public void OnConversationLine(Subtitle subtitle)
    {
        if (subtitle == null) return;

        string speakerName = subtitle.speakerInfo.Name;
        string lineText = subtitle.formattedText.text;

        if (nameText != null && !string.IsNullOrEmpty(speakerName))
        {
            nameText.text = speakerName;
        }

        if (speakerName == "Player")
        {
            chatUIManager.AddRightMessage(lineText);
        }
        else
        {
            chatUIManager.AddLeftMessage(speakerName, lineText);
        }
    }

    public void OnConversationResponseMenu(Response[] responses)
    {
        currentResponses = responses;

        HideChoiceButtons();

        if (responses == null || responses.Length == 0) return;

        if (responses.Length > 0)
        {
            SetupChoiceButton(choiceButton1, choiceButtonText1, responses[0], 0);
        }

        if (responses.Length > 1)
        {
            SetupChoiceButton(choiceButton2, choiceButtonText2, responses[1], 1);
        }

        if (responses.Length > 2)
        {
            SetupChoiceButton(choiceButton3, choiceButtonText3, responses[2], 2);
        }
    }

    private void SetupChoiceButton(Button button, TextMeshProUGUI buttonText, Response response, int index)
    {
        if (button == null || buttonText == null || response == null) return;

        button.gameObject.SetActive(true);
        buttonText.text = response.formattedText.text;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnClickResponse(index));
    }

    public void OnClickResponse(int index)
    {
        if (currentResponses == null) return;
        if (index < 0 || index >= currentResponses.Length) return;

        Response selectedResponse = currentResponses[index];

        // 先把玩家点击的选项显示成右侧气泡
        chatUIManager.AddRightMessage(selectedResponse.formattedText.text);

        // 隐藏按钮
        HideChoiceButtons();

        // 关键：通过当前 active conversation 的 ConversationView 继续对话
        if (DialogueManager.instance != null &&
            DialogueManager.instance.activeConversations != null &&
            DialogueManager.instance.activeConversations.Count > 0)
        {
            var activeConversation = DialogueManager.instance.activeConversations[0];
            activeConversation.conversationView.SelectResponse(
                new SelectedResponseEventArgs(selectedResponse)
            );
        }
        else
        {
            Debug.LogWarning("没有找到 active conversation，无法继续对话。");
        }
    }

    private void HideChoiceButtons()
    {
        if (choiceButton1 != null) choiceButton1.gameObject.SetActive(false);
        if (choiceButton2 != null) choiceButton2.gameObject.SetActive(false);
        if (choiceButton3 != null) choiceButton3.gameObject.SetActive(false);
    }

    private void ClearOldChoices()
    {
        currentResponses = null;
        HideChoiceButtons();
    }
}