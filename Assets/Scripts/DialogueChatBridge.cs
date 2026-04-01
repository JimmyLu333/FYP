using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PixelCrushers.DialogueSystem;

[System.Serializable]
public class ChoiceScoreRule
{
    [TextArea(2, 4)]
    public string choiceText;
    public int scoreGain;
}

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

    [Header("自动输出节点Actor")]
    public string autoActorName = "PlayerAutoActor"; // 新建自动输出 Actor
    public string displayNameForAutoActor = "王从心"; // 右边显示名字

    [Header("节奏设置")]
    public float npcReplyDelay = 1.2f;

    [Header("诈骗成功率UI")]
    public TextMeshProUGUI scamRateText;

    [Header("诈骗成功率设置")]
    public int currentScamRate = 0;
    public int maxScamRate = 100;

    [Header("选项加分规则")]
    public ChoiceScoreRule[] choiceScoreRules;

    private Response[] currentResponses;
    private bool suppressNextPlayerLine = false;

    private void Start()
    {
        HideChoiceButtons();

        if (nameText != null)
        {
            nameText.text = npcName;
        }

        currentScamRate = 0;
        UpdateScamRateUI();
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

        // 跳过玩家已显示的台词
        if ((speakerName == playerName || speakerName == "Player") && suppressNextPlayerLine)
        {
            suppressNextPlayerLine = false;
            return;
        }

        // 玩家点击的台词在这里不处理
        if (speakerName == playerName || speakerName == "Player") return;

        // 自动输出节点（王从心自动台词）也显示在右边
        if (speakerName == autoActorName)
        {
            StartCoroutine(ShowRightAutoLine(lineText));
            return;
        }

        // NPC 回复延迟显示在左边
        StartCoroutine(ShowNpcReplyAfterDelay(speakerName, lineText));
    }

    private IEnumerator ShowRightAutoLine(string lineText)
    {
        yield return new WaitForSeconds(1.2f); // 延迟显示
        if (chatUIManager != null)
        {
            chatUIManager.AddRightMessage(displayNameForAutoActor, lineText);
        }
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
        string selectedText = selectedResponse.formattedText.text;

        // 玩家（王从心）点击后，显示在右边
        if (chatUIManager != null)
        {
            chatUIManager.AddRightMessage(playerName, selectedText);
        }

        // 根据选项内容加减诈骗分数
        int scoreGain = GetScoreGainForChoice(selectedText);
        currentScamRate += scoreGain;
        currentScamRate = Mathf.Clamp(currentScamRate, 0, maxScamRate);
        UpdateScamRateUI();

        HideChoiceButtons();

        suppressNextPlayerLine = true;

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

    private void UpdateScamRateUI()
    {
        if (scamRateText != null)
        {
            scamRateText.text = "诈骗成功率：" + currentScamRate + "%";
        }
    }

    private int GetScoreGainForChoice(string selectedChoiceText)
    {
        if (choiceScoreRules == null || choiceScoreRules.Length == 0)
        {
            return 0;
        }

        for (int i = 0; i < choiceScoreRules.Length; i++)
        {
            if (choiceScoreRules[i] != null &&
                choiceScoreRules[i].choiceText == selectedChoiceText)
            {
                return choiceScoreRules[i].scoreGain;
            }
        }

        return 0;
    }
}