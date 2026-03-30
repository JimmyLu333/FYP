using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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

    [Header("角色名")]
    public string playerName = "陈默";
    public string npcName = "王从心";

    [Header("节奏设置")]
    public float npcReplyDelay = 1.2f;

    private Response[] currentResponses;

    // 防止玩家点击后，系统又重复生成一次玩家气泡
    private bool suppressNextPlayerLine = false;

    private void Start()
    {
        HideChoiceButtons();

        // 顶部固定显示聊天对象
        if (nameText != null)
        {
            nameText.text = npcName;
        }
    }

    public void OpenChatAndStartConversation(string conversationTitle)
    {
        HideDefaultDialogueUI();

        if (chatPanel != null)
        {
            chatPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("chatPanel 没有绑定！");
        }

        ClearOldChoices();

        if (nameText != null)
        {
            nameText.text = npcName;
        }

        DialogueManager.StartConversation(conversationTitle);
    }

    private void HideDefaultDialogueUI()
    {
        GameObject defaultUI = GameObject.Find("Default Dialogue UI");
        if (defaultUI != null)
        {
            defaultUI.SetActive(false);
        }
    }

    public void OnConversationLine(Subtitle subtitle)
    {
        if (subtitle == null) return;

        string speakerName = subtitle.speakerInfo.Name;
        string lineText = subtitle.formattedText.text;

        // 1. 如果这是玩家刚刚点击过的那句，就跳过，避免重复显示
        if ((speakerName == playerName || speakerName == "Player") && suppressNextPlayerLine)
        {
            suppressNextPlayerLine = false;
            return;
        }

        // 2. 玩家台词不在这里处理（玩家台词在点按钮时自己显示）
        if (speakerName == playerName || speakerName == "Player")
        {
            return;
        }

        // 3. NPC 回复延迟显示在左边
        StartCoroutine(ShowNpcReplyAfterDelay(speakerName, lineText));
    }

    private IEnumerator ShowNpcReplyAfterDelay(string speakerName, string lineText)
    {
        yield return new WaitForSeconds(npcReplyDelay);

        if (chatUIManager != null)
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

        // 玩家点击后，立即在右边显示
        if (chatUIManager != null)
        {
            chatUIManager.AddRightMessage(playerName, selectedResponse.formattedText.text);
        }

        // 隐藏当前选项
        HideChoiceButtons();

        // 标记：接下来系统如果回调一次玩家行，不要再显示
        suppressNextPlayerLine = true;

        // 继续对话
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