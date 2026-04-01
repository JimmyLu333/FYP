using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using PixelCrushers.DialogueSystem; 
using System.Collections;
using UnityEngine.SceneManagement;

// 必须保留这个定义，否则会报 CS0246 错误
[System.Serializable]
public class DialogueView
{
    public void SelectResponse(SelectedResponseEventArgs args)
    {
        Debug.Log("选择了分支");
    }
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    [System.Serializable]
    public struct DialogueLine
    {
        public string characterName;     
        [TextArea(3, 10)]
        public string dialogueText;      
        public DialogueView conversationView; // 刚才报错就是因为找不到上面的类
    }

    [Header("--- 对话数据 ---")]
    public List<DialogueLine> activeConversations = new List<DialogueLine>(); 
    private int currentLineIndex = 0;
    public float typingSpeed = 0.05f; 

    [Header("--- UI 组件绑定 ---")]
    public TextMeshProUGUI nameTMP;       
    public TextMeshProUGUI contentTMP;    
    public GameObject characterIconsPanel; // 包含两个角色立绘的父物体
    public GameObject dialoguePanel;     

    private Coroutine typingCoroutine;
    private bool isTyping = false;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public static void StartConversation(string rawData)
    {
        if (instance != null) Debug.Log("收到对话请求: " + rawData);
    }

    public void StartConversation(List<DialogueLine> newLines)
    {
        activeConversations = newLines;
        currentLineIndex = 0;
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (characterIconsPanel != null) characterIconsPanel.SetActive(true);
        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        if (currentLineIndex < activeConversations.Count)
        {
            DialogueLine line = activeConversations[currentLineIndex];
            if (nameTMP != null) nameTMP.text = line.characterName;

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(line.dialogueText));
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        contentTMP.text = "";
        foreach (char c in text.ToCharArray())
        {
            contentTMP.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    public void OnContinueButtonClicked()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            contentTMP.text = activeConversations[currentLineIndex].dialogueText;
            isTyping = false;
        }
        else
        {
            currentLineIndex++;
            DisplayCurrentLine();
        }
    }

    public void EndDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (characterIconsPanel != null) characterIconsPanel.SetActive(false);

        // 👉 对话结束后跳转场景
        SceneManager.LoadScene("TutorialScene"); // 改成你的场景名
    }


}