using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialGuideManager : MonoBehaviour
{
    public RectTransform guideArrow;
    public RectTransform informationButton;
    public RectTransform chatButton;
    public SimpleStoryDialogueManager storyDialogueManager;
    public Vector2 arrowOffset = new Vector2(80f, 40f);
    public float chatGuideHideDelay = 3f;

    private int guideStep = 0;
    private bool guideStarted = false;
    private bool guideFinished = false;
    private Coroutine hideCoroutine;

    public string informationSpeaker = "系统提示";
    [TextArea(2, 4)] public string informationGuideText = "请先点击 Information，查看目标资料。";
    public string chatSpeaker = "系统提示";
    [TextArea(2, 4)] public string chatGuideText = "资料已经确认。现在点击 Chat，开始联系目标。";

    void Start()
    {
        if (guideArrow != null)
            guideArrow.gameObject.SetActive(false);
    }

    public void StartGuide()
    {
        if (guideStarted || guideFinished) return;

        guideStarted = true;
        guideStep = 1;
        ShowInformationGuide();
    }

    void ShowInformationGuide()
    {
        MoveArrowTo(informationButton);

        if (storyDialogueManager != null)
            storyDialogueManager.SetSingleLine(informationSpeaker, informationGuideText);
    }

    void ShowChatGuide()
    {
        MoveArrowTo(chatButton);

        if (storyDialogueManager != null)
            storyDialogueManager.SetSingleLine(chatSpeaker, chatGuideText);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideChatGuideAfterDelay());
    }

    IEnumerator HideChatGuideAfterDelay()
    {
        yield return new WaitForSeconds(chatGuideHideDelay);

        if (storyDialogueManager != null)
            storyDialogueManager.HideDialogue();
    }

    void MoveArrowTo(RectTransform target)
    {
        if (guideArrow == null || target == null) return;

        guideArrow.gameObject.SetActive(true);
        guideArrow.position = target.position + new Vector3(arrowOffset.x, arrowOffset.y, 0f);
    }

    public void OnInformationClicked()
    {
        if (guideStep != 1) return;

        guideStep = 2;
        ShowChatGuide();
    }

    public void OnChatClicked()
    {
        if (guideStep != 2) return;

        guideStep = 3;
        guideFinished = true;

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        if (guideArrow != null)
            guideArrow.gameObject.SetActive(false);

        if (storyDialogueManager != null)
            storyDialogueManager.HideDialogue();
    }
}