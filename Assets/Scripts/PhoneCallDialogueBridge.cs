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

    private Response[] currentResponses;
    private bool suppressNextPlayerLine = false;

    private void Start()
    {
        HideChoiceButtons();

        if (callerNameText != null)
            callerNameText.text = npcName;

        if (callStatusText != null)
            callStatusText.text = "通话中...";
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
        HideChoiceButtons();
    }

    public void ClosePhoneCallPanel()
    {
        if (phoneCallPanel != null)
            phoneCallPanel.SetActive(false);
    }

    [Header("电话结束后的资料检查")]
    public FormCheckPanelController formCheckPanelController;

    public void OnPhoneConversationEnd(Transform actor)
    {
        if (phoneCallPanel != null)
            phoneCallPanel.SetActive(false);

        if (formCheckPanelController != null)
            formCheckPanelController.StartCheckSequence();
    }

}