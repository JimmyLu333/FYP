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

[System.Serializable]
public class WordStop
{
    [TextArea(2, 4)]
    public string triggerText;
    public bool pauseForMaze = true;
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
    public string playerName = "陈大夫";
    public string npcName = "刘福来";

    [Header("自动输出节点Actor")]
    public string autoActorName = "PlayerAutoActor";
    public string displayNameForAutoActor = "陈大夫";

    [Header("节奏设置")]
    public float npcReplyDelay = 1.2f;

    [Header("诈骗成功率UI")]
    public TextMeshProUGUI scamRateText;

    [Header("诈骗成功率设置")]
    public int currentScamRate = 0;
    public int maxScamRate = 100;

    [Header("选项加分规则")]
    public ChoiceScoreRule[] choiceScoreRules;

    [Header("暂停触发列表")]
    public WordStop[] wordStops;

    [Header("迷宫等待")]
    public bool waitingForMaze = false;

    private Response[] currentResponses;
    private Response[] pendingResponses;
    private bool suppressNextPlayerLine = false;

    private void Start()
    {
        HideChoiceButtons();

        if (nameText != null)
            nameText.text = npcName;

        currentScamRate = 0;
        UpdateScamRateUI();
    }

    public void OpenChatAndStartConversation(string conversationTitle)
    {
        HideDefaultDialogueUI();

        if (chatPanel != null)
            chatPanel.SetActive(true);

        ClearOldChoices();

        if (nameText != null)
            nameText.text = npcName;

        DialogueManager.StartConversation(conversationTitle);
    }

    private void HideDefaultDialogueUI()
    {
        GameObject defaultUI = GameObject.Find("Default Dialogue UI");
        if (defaultUI != null)
            defaultUI.SetActive(false);
    }

    public void OnConversationLine(Subtitle subtitle)
    {
        if (subtitle == null) return;

        string speakerName = subtitle.speakerInfo.Name;
        string lineText = subtitle.formattedText.text;

        CheckWordStop(lineText);

        if ((speakerName == playerName || speakerName == "Player") && suppressNextPlayerLine)
        {
            suppressNextPlayerLine = false;
            return;
        }

        if (speakerName == playerName || speakerName == "Player")
            return;

        if (speakerName == autoActorName)
        {
            StartCoroutine(ShowRightAutoLine(lineText));
            return;
        }

        StartCoroutine(ShowNpcReplyAfterDelay(speakerName, lineText));
    }

    private void CheckWordStop(string lineText)
    {
        if (wordStops == null) return;

        foreach (WordStop ws in wordStops)
        {
            if (ws == null || string.IsNullOrEmpty(ws.triggerText)) continue;

            if (lineText.Contains(ws.triggerText))
            {
                if (ws.pauseForMaze)
                {
                    waitingForMaze = true;
                    HideChoiceButtons();
                    Debug.Log("触发迷宫暂停：" + ws.triggerText);
                }

                return;
            }
        }
    }

    private IEnumerator ShowRightAutoLine(string lineText)
    {
        yield return new WaitForSeconds(1.2f);

        if (chatUIManager != null)
            chatUIManager.AddRightMessage(displayNameForAutoActor, lineText);
    }

    private IEnumerator ShowNpcReplyAfterDelay(string speakerName, string lineText)
    {
        yield return new WaitForSeconds(npcReplyDelay);

        if (chatUIManager != null)
            chatUIManager.AddLeftMessage(speakerName, lineText);
    }

    public void OnConversationResponseMenu(Response[] responses)
    {
        currentResponses = responses;
        HideChoiceButtons();

        if (responses == null || responses.Length == 0)
            return;

        if (waitingForMaze)
        {
            pendingResponses = responses;
            Debug.Log("正在等待迷宫完成，选项已暂存。");
            return;
        }

        ShowResponses(responses);
    }

    private void ShowResponses(Response[] responses)
    {
        HideChoiceButtons();

        if (responses == null || responses.Length == 0)
            return;

        if (responses.Length > 0)
            SetupChoiceButton(choiceButton1, choiceButtonText1, responses[0], 0);

        if (responses.Length > 1)
            SetupChoiceButton(choiceButton2, choiceButtonText2, responses[1], 1);

        if (responses.Length > 2)
            SetupChoiceButton(choiceButton3, choiceButtonText3, responses[2], 2);
    }

    public void ContinueAfterMaze()
    {
        waitingForMaze = false;

        if (pendingResponses != null && pendingResponses.Length > 0)
        {
            currentResponses = pendingResponses;
            ShowResponses(pendingResponses);
            pendingResponses = null;
        }
    }

    private void SetupChoiceButton(Button button, TextMeshProUGUI buttonText, Response response, int index)
    {
        if (button == null || buttonText == null || response == null)
            return;

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

        if (chatUIManager != null)
            chatUIManager.AddRightMessage(playerName, selectedText);

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
        pendingResponses = null;
        HideChoiceButtons();
    }

    private void UpdateScamRateUI()
    {
        if (scamRateText != null)
            scamRateText.text = "诈骗成功率：" + currentScamRate + "%";
    }

    private int GetScoreGainForChoice(string selectedChoiceText)
    {
        if (choiceScoreRules == null) return 0;

        foreach (ChoiceScoreRule rule in choiceScoreRules)
        {
            if (rule != null && rule.choiceText == selectedChoiceText)
                return rule.scoreGain;
        }

        return 0;
    }
}