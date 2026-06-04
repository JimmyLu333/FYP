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
    [Tooltip("依次拖入 Name, ID, Bank, Phone, Code 对应的绿色底框 Image 物体")]
    public GameObject[] itemGlowBackgrounds; // 长度为 5 的数组

    [Header("配色方案")]
    public Color normalTextColor = Color.green;     // 默认状态的绿色字
    public Color highlightedTextColor = Color.black; // 激活状态下的反色黑字

    [Header("按钮")]
    public Button confirmButton;

    [Header("下一步：CodeRain")]
    public CodeRainPuzzle codeRainPuzzle;

    [Header("🚨 速度设置")]
    [Tooltip("每行检查之间的等待间隔（秒）")]
    public float checkInterval = 0.8f;
    [Tooltip("打字机效果：每个字弹出来的间隔时间（秒）。数值越小字蹦得越快！")]
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

        // 初始化时：所有项全清空（等待打字机输入），且隐藏所有绿底
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

    // 辅助函数：启动前清空文本
    private void ClearItemBeforeType(TextMeshProUGUI textComponent, int glowIndex)
    {
        if (textComponent != null)
        {
            textComponent.text = "";
            textComponent.color = normalTextColor;
        }
        if (itemGlowBackgrounds != null && glowIndex < itemGlowBackgrounds.Length && itemGlowBackgrounds[glowIndex] != null)
        {
            itemGlowBackgrounds[glowIndex].SetActive(false);
        }
    }

    // 🌟 核心打字机协程：让指定的文本组件一个字一个字显示
    private IEnumerator TypeText(TextMeshProUGUI textComponent, string fullText)
    {
        textComponent.text = "";
        textComponent.color = normalTextColor; // 打字时保持绿字

        // 逐字追加显示
        for (int i = 0; i < fullText.Length; i++)
        {
            textComponent.text += fullText[i];
            yield return new WaitForSeconds(textSpeed); // 🚨 这里控制每个字出来的速度
        }
    }

    // 辅助函数：快速改变某一行的底层和颜色状态
    private void SetItemGlowState(TextMeshProUGUI textComponent, string text, int glowIndex, bool isHighlighted)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = isHighlighted ? highlightedTextColor : normalTextColor;
        }
        if (itemGlowBackgrounds != null && glowIndex < itemGlowBackgrounds.Length && itemGlowBackgrounds[glowIndex] != null)
        {
            itemGlowBackgrounds[glowIndex].SetActive(isHighlighted);
        }
    }

    IEnumerator CheckSequenceRoutine()
    {
        // ==========================================
        // 1. 姓名信息接收
        // ==========================================
        yield return StartCoroutine(TypeText(checkItemName, "> 姓名信息：✓ 已接收")); // 逐字打印
        SetItemGlowState(checkItemName, "> 姓名信息：✓ 已接收", 0, true);          // 打印完，亮起本行绿底
        yield return new WaitForSeconds(checkInterval);                             // 停留观察

        // ==========================================
        // 2. 身份证信息接收
        // ==========================================
        SetItemGlowState(checkItemName, "  姓名信息：✓ 已接收", 0, false);         // 关掉第一行绿底
        yield return StartCoroutine(TypeText(checkItemID, "> 身份证信息：✓ 已接收")); // 逐字打印第二行
        SetItemGlowState(checkItemID, "> 身份证信息：✓ 已接收", 1, true);           // 亮起第二行绿底
        yield return new WaitForSeconds(checkInterval);

        // ==========================================
        // 3. 银行卡号接收
        // ==========================================
        SetItemGlowState(checkItemID, "  身份证信息：✓ 已接收", 1, false);          // 关掉第二行绿底
        yield return StartCoroutine(TypeText(checkItemBank, "> 银行卡号：✓ 已接收")); // 逐字打印第三行
        SetItemGlowState(checkItemBank, "> 银行卡号：✓ 已接收", 2, true);           // 亮起第三行绿底
        yield return new WaitForSeconds(checkInterval);

        // ==========================================
        // 4. 手机号接收
        // ==========================================
        SetItemGlowState(checkItemBank, "  银行卡号：✓ 已接收", 2, false);          // 关掉第三行绿底
        yield return StartCoroutine(TypeText(checkItemPhone, "> 手机号：✓ 已接收"));  // 逐字打印第四行
        SetItemGlowState(checkItemPhone, "> 手机号：✓ 已接收", 3, true);            // 亮起第四行绿底
        yield return new WaitForSeconds(checkInterval);

        // ==========================================
        // 5. 验证码状态接收
        // ==========================================
        SetItemGlowState(checkItemPhone, "  手机号：✓ 已接收", 3, false);           // 关掉第四行绿底
        yield return StartCoroutine(TypeText(checkItemCode, "> 验证码状态：Pending...")); // 逐字打印第五行
        SetItemGlowState(checkItemCode, "> 验证码状态：Pending...", 4, true);          // 亮起第五行绿底
        yield return new WaitForSeconds(checkInterval);

        // ==========================================
        // 结束阶段
        // ==========================================
        SetItemGlowState(checkItemCode, "  验证码状态：Pending...", 4, false);         // 扫描完，关闭最后一行的绿底

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