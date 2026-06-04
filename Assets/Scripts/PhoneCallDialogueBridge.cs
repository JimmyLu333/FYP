using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PixelCrushers.DialogueSystem;

public class PhoneCallDialogueBridge : MonoBehaviour
{
    [Header("电话UI")]
    public GameObject phoneCallPanel;
    public PhoneCallUIManager phoneCallUIManager;
    public TextMeshProUGUI callerNameText;
    public TextMeshProUGUI callStatusText;

    [Header("诈骗成功率UI")]
    public TextMeshProUGUI scamRateText;
    public int maxScamRate = 100;

    [Header("选项加分规则")]
    public ChoiceScoreRule[] choiceScoreRules;

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
    public string npcName = "Walker";

    [Header("自动输出节点Actor")]
    public string autoActorName = "PlayerAutoActor";
    public string displayNameForAutoActor = "陈默";

    [Header("节奏设置")]
    public float npcReplyDelay = 1.2f;
    public float playerAutoDelay = 1.0f;

    [Header("电话结束后的资料检查")]
    public Button phoneAppButton;
    public FormCheckPanelController formCheckPanelController;
    public float formCheckDelay = 2f;

    private Response[] currentResponses;
    private bool suppressNextPlayerLine = false;
    private Coroutine phoneEndCoroutine;

    private void Start()
    {
        HideChoiceButtons();

        if (callerNameText != null)
            callerNameText.text = npcName;

        if (callStatusText != null)
            callStatusText.text = "通话中...";

        UpdateScamRateUI();
    }

    public void OpenPhoneCallAndStartConversation(string conversationTitle)
    {
        HideDefaultDialogueUI();

        if (phoneCallPanel != null)
            phoneCallPanel.SetActive(true);

        ClearOldChoices();

        if (callerNameText != null)
            callerNameText.text = npcName;

        if (callStatusText != null)
            callStatusText.text = "通话中...";

        UpdateScamRateUI();

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

    private IEnumerator ShowRightAutoLine(string lineText)
    {
        yield return new WaitForSeconds(playerAutoDelay);

        if (phoneCallUIManager != null)
            phoneCallUIManager.AddRightMessage(displayNameForAutoActor, lineText);
    }

    private IEnumerator ShowNpcReplyAfterDelay(string speakerName, string lineText)
    {
        if (callStatusText != null)
            callStatusText.text = speakerName + " 正在说话...";

        yield return new WaitForSeconds(npcReplyDelay);

        if (phoneCallUIManager != null)
            phoneCallUIManager.AddLeftMessage(speakerName, lineText);

        if (callStatusText != null)
            callStatusText.text = "通话中...";
    }

    public void OnConversationResponseMenu(Response[] responses)
    {
        currentResponses = responses;
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

        if (phoneCallUIManager != null)
            phoneCallUIManager.AddRightMessage(playerName, selectedText);

        int scoreGain = GetScoreGainForChoice(selectedText);
        ScamScoreData.CurrentScamRate += scoreGain;
        ScamScoreData.CurrentScamRate = Mathf.Clamp(ScamScoreData.CurrentScamRate, 0, maxScamRate);
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

    private void UpdateScamRateUI()
    {
        if (scamRateText != null)
            scamRateText.text = "诈骗成功率：" + ScamScoreData.CurrentScamRate + "%";
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

    public void ClosePhoneCallPanel()
    {
        if (phoneCallPanel != null)
            phoneCallPanel.SetActive(false);
    }

    public void OnPhoneConversationEnd(Transform actor)
    {
        if (phoneEndCoroutine != null)
            StopCoroutine(phoneEndCoroutine);

        phoneEndCoroutine = StartCoroutine(PhoneEndRoutine());
    }

    private IEnumerator PhoneEndRoutine()
    {
        if (phoneCallPanel != null)
            phoneCallPanel.SetActive(false);

        if (phoneAppButton != null)
            phoneAppButton.interactable = false;

        yield return new WaitForSeconds(formCheckDelay);

        if (formCheckPanelController != null)
            formCheckPanelController.StartCheckSequence();
        else
            Debug.LogError("FormCheckPanelController 没有绑定！");
    }
}