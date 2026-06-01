using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FormCheckPanelController : MonoBehaviour
{
    [Header("主面板")]
    public GameObject formCheckPanel;

    [Header("文字")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI checkItemName;
    public TextMeshProUGUI checkItemID;
    public TextMeshProUGUI checkItemBank;
    public TextMeshProUGUI checkItemPhone;
    public TextMeshProUGUI checkItemCode;
    public TextMeshProUGUI completeText;

    [Header("按钮")]
    public Button confirmButton;

    [Header("下一步：CodeRain")]
    public CodeRainPuzzle codeRainPuzzle;

    [Header("速度设置")]
    public float checkInterval = 0.8f;

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

        if (checkItemName != null)
            checkItemName.text = "姓名信息：等待中...";

        if (checkItemID != null)
            checkItemID.text = "身份证信息：等待中...";

        if (checkItemBank != null)
            checkItemBank.text = "银行卡号：等待中...";

        if (checkItemPhone != null)
            checkItemPhone.text = "手机号：等待中...";

        if (checkItemCode != null)
            checkItemCode.text = "验证码状态：等待中...";

        if (completeText != null)
            completeText.text = "";

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);
    }

    IEnumerator CheckSequenceRoutine()
    {
        yield return new WaitForSeconds(checkInterval);
        if (checkItemName != null)
            checkItemName.text = "姓名信息：✓ 已接收";

        yield return new WaitForSeconds(checkInterval);
        if (checkItemID != null)
            checkItemID.text = "身份证信息：✓ 已接收";

        yield return new WaitForSeconds(checkInterval);
        if (checkItemBank != null)
            checkItemBank.text = "银行卡号：✓ 已接收";

        yield return new WaitForSeconds(checkInterval);
        if (checkItemPhone != null)
            checkItemPhone.text = "手机号：✓ 已接收";

        yield return new WaitForSeconds(checkInterval);
        if (checkItemCode != null)
            checkItemCode.text = "验证码状态：Pending...";

        yield return new WaitForSeconds(checkInterval);
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

        if (codeRainPuzzle != null)
            codeRainPuzzle.StartCodeRain();
        else
            Debug.LogError("CodeRainPuzzle 没有绑定！");
    }
}