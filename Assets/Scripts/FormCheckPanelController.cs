using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FormCheckPanelController : MonoBehaviour
{
    [Header("主面板")]
    public GameObject formCheckPanel;

    [Header("文字组件")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI checkItemName;
    public TextMeshProUGUI checkItemID;
    public TextMeshProUGUI checkItemBank;
    public TextMeshProUGUI checkItemPhone;
    public TextMeshProUGUI checkItemCode;
    public TextMeshProUGUI completeText;

    [Header("绿底背景条")]
    public GameObject[] itemGlowBackgrounds;

    [Header("配色方案")]
    public Color normalTextColor = Color.green;
    public Color highlightedTextColor = Color.black;

    [Header("按钮")]
    public Button confirmButton;

    [Header("完成后继续")]
    public PhoneCallDialogueBridge phoneCallDialogueBridge;
    public CodeRainPuzzle codeRainPuzzle;

    [Header("速度设置")]
    public float checkInterval = 0.8f;
    public float textSpeed = 0.03f;

    private Coroutine checkRoutine;

    void Start()
    {
        if (formCheckPanel != null)
            formCheckPanel.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
    }

    public void StartCheckSequence()
    {
        if (checkRoutine != null)
            StopCoroutine(checkRoutine);

        if (formCheckPanel != null)
            formCheckPanel.SetActive(true);

        ResetText();

        checkRoutine = StartCoroutine(CheckSequenceRoutine());
    }

    void ResetText()
    {
        if (statusText != null)
            statusText.text = "对方正在填写共享表单...";

        ClearItemBeforeType(checkItemName, 0);
        ClearItemBeforeType(checkItemID, 1);
        ClearItemBeforeType(checkItemBank, 2);
        ClearItemBeforeType(checkItemPhone, 3);
        ClearItemBeforeType(checkItemCode, 4);

        if (completeText != null)
            completeText.text = "";

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);
    }

    private void ClearItemBeforeType(TextMeshProUGUI textComponent, int glowIndex)
    {
        if (textComponent != null)
        {
            textComponent.text = "";
            textComponent.color = normalTextColor;
        }

        if (itemGlowBackgrounds != null &&
            glowIndex < itemGlowBackgrounds.Length &&
            itemGlowBackgrounds[glowIndex] != null)
        {
            itemGlowBackgrounds[glowIndex].SetActive(false);
        }
    }

    private IEnumerator TypeText(TextMeshProUGUI textComponent, string fullText)
    {
        if (textComponent == null) yield break;

        textComponent.text = "";
        textComponent.color = normalTextColor;

        for (int i = 0; i < fullText.Length; i++)
        {
            textComponent.text += fullText[i];
            yield return new WaitForSeconds(textSpeed);
        }
    }

    private void SetItemGlowState(TextMeshProUGUI textComponent, string text, int glowIndex, bool isHighlighted)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = isHighlighted ? highlightedTextColor : normalTextColor;
        }

        if (itemGlowBackgrounds != null &&
            glowIndex < itemGlowBackgrounds.Length &&
            itemGlowBackgrounds[glowIndex] != null)
        {
            itemGlowBackgrounds[glowIndex].SetActive(isHighlighted);
        }
    }

    IEnumerator CheckSequenceRoutine()
    {
        yield return StartCoroutine(TypeText(checkItemName, "> 姓名信息：已接收"));
        SetItemGlowState(checkItemName, "> 姓名信息：已接收", 0, true);
        yield return new WaitForSeconds(checkInterval);

        SetItemGlowState(checkItemName, "  姓名信息：已接收", 0, false);
        yield return StartCoroutine(TypeText(checkItemID, "> 身份证信息：已接收"));
        SetItemGlowState(checkItemID, "> 身份证信息：已接收", 1, true);
        yield return new WaitForSeconds(checkInterval);

        SetItemGlowState(checkItemID, "  身份证信息：已接收", 1, false);
        yield return StartCoroutine(TypeText(checkItemBank, "> 银行卡号：已接收"));
        SetItemGlowState(checkItemBank, "> 银行卡号：已接收", 2, true);
        yield return new WaitForSeconds(checkInterval);

        SetItemGlowState(checkItemBank, "  银行卡号：已接收", 2, false);
        yield return StartCoroutine(TypeText(checkItemPhone, "> 手机号：已接收"));
        SetItemGlowState(checkItemPhone, "> 手机号：已接收", 3, true);
        yield return new WaitForSeconds(checkInterval);

        SetItemGlowState(checkItemPhone, "  手机号：已接收", 3, false);
        yield return StartCoroutine(TypeText(checkItemCode, "> 验证码状态：Pending..."));
        SetItemGlowState(checkItemCode, "> 验证码状态：Pending...", 4, true);
        yield return new WaitForSeconds(checkInterval);

        SetItemGlowState(checkItemCode, "  验证码状态：Pending...", 4, false);

        if (statusText != null)
            statusText.text = "资料提交完成，等待后台接收。";

        if (completeText != null)
            completeText.text = "共享表单数据已同步。";

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
    }

    void OnConfirmClicked()
    {
        if (formCheckPanel != null)
            formCheckPanel.SetActive(false);

        if (phoneCallDialogueBridge != null)
            phoneCallDialogueBridge.ContinueAfterFormCheck();
        else
            Debug.LogError("PhoneCallDialogueBridge 没有绑定！");

        if (codeRainPuzzle != null)
            codeRainPuzzle.StartCodeRain();
        else
            Debug.LogError("CodeRainPuzzle 没有绑定！");
    }
}