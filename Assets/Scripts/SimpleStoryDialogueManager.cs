using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SimpleStoryDialogueManager : MonoBehaviour
{
    [System.Serializable]
    public class StoryLine
    {
        public string speakerName;

        [TextArea(2, 5)]
        public string text;
    }

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Button continueButton;

    [Header("剧情文本")]
    public List<StoryLine> storyLines = new List<StoryLine>();

    [Header("打字速度")]
    public float typingSpeed = 0.03f;

    private int currentIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    public TutorialGuideManager tutorialGuideManager;

    void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnClickContinue);

        Invoke(nameof(StartStory), 0.5f);
    }

    public void StartStory()
    {
        currentIndex = 0;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (currentIndex >= storyLines.Count)
        {
            EndStory();
            return;
        }

        StoryLine line = storyLines[currentIndex];

        if (nameText != null)
            nameText.text = line.speakerName;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line.text));
    }

    System.Collections.IEnumerator TypeText(string text)
    {
        isTyping = true;

        if (dialogueText != null)
            dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    void OnClickContinue()
    {
        if (isTyping)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            dialogueText.text = storyLines[currentIndex].text;
            isTyping = false;
            return;
        }

        currentIndex++;
        ShowCurrentLine();
    }

    void EndStory()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (tutorialGuideManager != null)
            tutorialGuideManager.StartGuide();
    }

    public void SetSingleLine(string speakerName, string text)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (nameText != null)
            nameText.text = speakerName;

        if (dialogueText != null)
            dialogueText.text = text;
    }

    public void HideDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
}