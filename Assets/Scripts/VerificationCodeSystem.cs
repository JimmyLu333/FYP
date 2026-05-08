using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VerificationCodeSystem : MonoBehaviour
{
    [Header("验证码 UI")]
    public GameObject codeInputPanel;
    public TMP_InputField codeInputField;
    public TextMeshProUGUI feedbackText;

    [Header("按钮")]
    public Button codeAppButton;
    public Button confirmButton;
    public Button closeButton;

    [Header("验证码设置")]
    public string correctCode = "734921";

    [Header("迷宫系统")]
    public UIMazeController uiMazeController;

    private bool codeCompleted = false;

    void Start()
    {
        if (codeInputPanel != null)
            codeInputPanel.SetActive(false);

        if (feedbackText != null)
            feedbackText.text = "";

        if (confirmButton != null)
            confirmButton.onClick.AddListener(CheckCode);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseCodePanel);

        if (codeAppButton != null)
            codeAppButton.onClick.AddListener(OpenCodePanel);
    }

    public void OpenCodePanel()
    {
        if (codeCompleted) return;

        if (codeInputPanel != null)
            codeInputPanel.SetActive(true);

        if (codeInputField != null)
            codeInputField.text = "";

        if (feedbackText != null)
            feedbackText.text = "";

        if (codeAppButton != null)
            codeAppButton.interactable = false;
    }

    public void CloseCodePanel()
    {
        if (codeInputPanel != null)
            codeInputPanel.SetActive(false);

        if (!codeCompleted && codeAppButton != null)
            codeAppButton.interactable = true;
    }

    public void CheckCode()
    {
        if (codeInputField == null) return;

        string input = codeInputField.text.Trim();

        if (input == correctCode)
        {
            codeCompleted = true;

            if (codeInputPanel != null)
                codeInputPanel.SetActive(false);

            if (codeAppButton != null)
                codeAppButton.interactable = false;

            // ✅ 验证码正确后启动迷宫
            if (uiMazeController != null)
            {
                uiMazeController.StartMaze();
            }
            else
            {
                Debug.LogError("UIMazeController 没有绑定！");
            }
        }
        else
        {
            if (feedbackText != null)
            {
                feedbackText.text = "验证码错误，请重新输入。";
                feedbackText.color = Color.red;
            }
        }
    }
}