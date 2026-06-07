using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class DGManager : MonoBehaviour
{
    [System.Serializable]
    public struct DialogueLine
    {
        public string characterName;

        [TextArea(3, 10)]
        public string dialogueText;
    }

    [Header("--- 对话数据 ---")]
    public List<DialogueLine> activeConversations = new List<DialogueLine>();

    [Header("--- 五张开头背景图 ---")]
    public Sprite openingImage1;
    public Sprite openingImage2;
    public Sprite openingImage3;
    public Sprite openingImage4;
    public Sprite openingImage5;

    [Header("--- 第几句开始切图 ---")]
    public int switchToImage2AtLine = 2;
    public int switchToImage3AtLine = 4;
    public int switchToImage4AtLine = 6;
    public int switchToImage5AtLine = 8;

    [Header("--- UI 组件绑定 ---")]
    public Image backgroundImage;
    public Image fadeImage;
    public TextMeshProUGUI nameTMP;
    public TextMeshProUGUI contentTMP;
    public GameObject characterIconsPanel;
    public GameObject dialoguePanel;

    [Header("--- 打字速度 ---")]
    public float typingSpeed = 0.05f;

    [Header("--- 背景切换淡入淡出 ---")]
    public float fadeDuration = 0.4f;

    [Range(0f, 1f)]
    public float darkAlpha = 0.45f;

    [Header("--- 场景跳转 ---")]
    public string nextSceneName = "TutorialScene";

    private int currentLineIndex = 0;
    private Sprite currentBackgroundSprite;

    private Coroutine typingCoroutine;
    private Coroutine backgroundCoroutine;

    private bool isTyping = false;
    private bool isChangingBackground = false;

    private void Start()
    {
        currentLineIndex = 0;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (characterIconsPanel != null)
            characterIconsPanel.SetActive(true);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
        }

        Sprite firstSprite = GetSpriteForCurrentLine();

        if (backgroundImage != null && firstSprite != null)
        {
            backgroundImage.sprite = firstSprite;
            currentBackgroundSprite = firstSprite;
        }

        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        if (currentLineIndex >= activeConversations.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = activeConversations[currentLineIndex];

        if (nameTMP != null)
            nameTMP.text = line.characterName;

        UpdateBackgroundImage();

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line.dialogueText));
    }

    private Sprite GetSpriteForCurrentLine()
    {
        if (currentLineIndex >= switchToImage5AtLine)
            return openingImage5;

        if (currentLineIndex >= switchToImage4AtLine)
            return openingImage4;

        if (currentLineIndex >= switchToImage3AtLine)
            return openingImage3;

        if (currentLineIndex >= switchToImage2AtLine)
            return openingImage2;

        return openingImage1;
    }

    private void UpdateBackgroundImage()
    {
        Sprite targetSprite = GetSpriteForCurrentLine();

        if (targetSprite == null || backgroundImage == null)
            return;

        if (currentBackgroundSprite == null)
        {
            backgroundImage.sprite = targetSprite;
            currentBackgroundSprite = targetSprite;
            return;
        }

        if (currentBackgroundSprite == targetSprite)
            return;

        if (backgroundCoroutine != null)
            StopCoroutine(backgroundCoroutine);

        backgroundCoroutine = StartCoroutine(ChangeBackgroundWithDarkFade(targetSprite));
    }

    private IEnumerator ChangeBackgroundWithDarkFade(Sprite newSprite)
    {
        isChangingBackground = true;

        if (fadeImage == null)
        {
            backgroundImage.sprite = newSprite;
            currentBackgroundSprite = newSprite;
            isChangingBackground = false;
            yield break;
        }

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, darkAlpha, t / fadeDuration);

            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;

            yield return null;
        }

        backgroundImage.sprite = newSprite;
        currentBackgroundSprite = newSprite;

        t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(darkAlpha, 0f, t / fadeDuration);

            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;

            yield return null;
        }

        Color finalColor = fadeImage.color;
        finalColor.a = 0f;
        fadeImage.color = finalColor;

        isChangingBackground = false;
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;

        if (contentTMP != null)
            contentTMP.text = "";

        foreach (char c in text)
        {
            if (contentTMP != null)
                contentTMP.text += c;

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    public void OnContinueButtonClicked()
    {
        if (isChangingBackground)
            return;

        if (isTyping)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            if (contentTMP != null)
                contentTMP.text = activeConversations[currentLineIndex].dialogueText;

            isTyping = false;
            return;
        }

        currentLineIndex++;
        DisplayCurrentLine();
    }

    private void EndDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (characterIconsPanel != null)
            characterIconsPanel.SetActive(false);

        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.LoadSceneWithFade(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}