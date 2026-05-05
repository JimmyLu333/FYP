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
        // 验证码已经成功过，之后点击图标没有反应
        if (codeCompleted) return;

        if (codeInputPanel != null)
            codeInputPanel.SetActive(true);

        if (codeInputField != null)
            codeInputField.text = "";

        if (feedbackText != null)
            feedbackText.text = "";

        // 打开输入框后，暂时禁用图标，防止重复开
        if (codeAppButton != null)
            codeAppButton.interactable = false;
    }

    public void CloseCodePanel()
    {
        if (codeInputPanel != null)
            codeInputPanel.SetActive(false);

        // 如果还没成功输入验证码，关闭后恢复图标可点
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

            // 成功后图标保持不可用
            if (codeAppButton != null)
                codeAppButton.interactable = false;

            Debug.Log("验证码正确，下一步可以开启迷宫。");
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