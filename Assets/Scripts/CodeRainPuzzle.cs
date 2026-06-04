using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CodeRainPuzzle : MonoBehaviour
{
    [Header("主面板")]
    public GameObject codeRainPanel;

    [Header("数字雨区域")]
    public RectTransform rainArea;
    public GameObject numberTextPrefab;

    [Header(" 新·黑客单行输入框架")]
    [Tooltip("核心驱动：把场景里原来的任意一个 InputField 拖进来，用来接管键盘输入")]
    public TMP_InputField masterInputField;
    
    [Tooltip("终端显示：对应图2中负责显示 [ 6  3  _ ] 的那个单条 TextMeshPro 物体")]
    public TextMeshProUGUI terminalTextDisplay;

    [Header("提示")]
    public TextMeshProUGUI feedbackText;

    [Header("目标验证码")]
    public string targetCode = "734921";

    [Header("普通数字设置")]
    public float normalSpawnInterval = 0.08f;
    public float normalFallSpeedMin = 120f;
    public float normalFallSpeedMax = 220f;
    public int maxNormalNumbers = 70;

    [Header("红色目标数字设置")]
    public float targetFallSpeed = 80f;
    public float targetSpawnIntervalMin = 0.8f;
    public float targetSpawnIntervalMax = 1.6f;
    public float targetXRandomOffset = 12f;

    [Header("成功设置")]
    public float successDelay = 1.2f;
    public UIMazeController uiMazeController;

    private bool isRunning = false;
    private bool puzzleCompleted = false;

    private Coroutine normalRainCoroutine;
    private Coroutine targetRainCoroutine;
    private Coroutine cursorBlinkCoroutine; // 🚨 用于控制下划线光标闪烁

    private List<GameObject> spawnedNumbers = new List<GameObject>();

    void Start()
    {
        if (codeRainPanel != null)
            codeRainPanel.SetActive(false);

        if (feedbackText != null)
            feedbackText.text = "";

        SetupMasterInput();
    }

    void SetupMasterInput()
    {
        if (masterInputField == null) return;

        // 强行将核心输入框限制为验证码的真实长度，并只允许输入纯数字
        masterInputField.characterLimit = targetCode.Length;
        masterInputField.contentType = TMP_InputField.ContentType.IntegerNumber;

        // 监听玩家打字状态
        masterInputField.onValueChanged.RemoveAllListeners();
        masterInputField.onValueChanged.AddListener(OnTerminalInputChanged);
    }

    public void StartCodeRain()
    {
        if (codeRainPanel != null)
            codeRainPanel.SetActive(true);

        ClearOldNumbers();

        if (masterInputField != null)
        {
            masterInputField.text = "";
            masterInputField.Select();
            masterInputField.ActivateInputField(); // 强行让隐形输入框吃焦
        }

        if (feedbackText != null)
            feedbackText.text = "";

        isRunning = true;
        puzzleCompleted = false;

        normalRainCoroutine = StartCoroutine(SpawnNormalRainRoutine());
        targetRainCoroutine = StartCoroutine(SpawnTargetRainRoutine());
        
        //  启动终端光标闪烁效果
        if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
        cursorBlinkCoroutine = StartCoroutine(TerminalCursorBlinkRoutine());
    }

    // 【核心修复】：当玩家按键打字或退格时，实时刷新图2的终端黑客文本样式
    void OnTerminalInputChanged(string currentText)
    {
        if (!isRunning || puzzleCompleted) return;

        UpdateTerminalDisplay(currentText, true);

        // 自动校验密码是否填满且正确
        if (currentText.Length >= targetCode.Length)
        {
            if (currentText == targetCode)
            {
                StartCoroutine(SuccessRoutine());
            }
            else
            {
                if (feedbackText != null)
                {
                    feedbackText.text = "验证码顺序错误。";
                    feedbackText.color = Color.red;
                }
            }
        }
    }

    // 格式化输出字符串：缩减空格间距
    void UpdateTerminalDisplay(string currentInput, bool showCursor)
    {
        if (terminalTextDisplay == null) return;

        string formattedText = "";

        // 1. 铺设已经打出来的数字，中间只留 1 个普通空格
        for (int i = 0; i < currentInput.Length; i++)
        {
            formattedText += currentInput[i] + "  "; // 💡 核心改动：把原来的 "   " 改成了 " "
        }

        // 2. 如果还没输满，并且光标状态为可见，则在末尾追加黑客光标
        if (currentInput.Length < targetCode.Length && showCursor)
        {
            formattedText += "_";
        }

        terminalTextDisplay.text = formattedText;
    }

    //  经典的下划线终端光标每隔 0.45 秒闪烁一次的协程逻辑
    IEnumerator TerminalCursorBlinkRoutine()
    {
        bool cursorOn = true;
        while (isRunning && !puzzleCompleted)
        {
            string currentText = masterInputField != null ? masterInputField.text : "";
            UpdateTerminalDisplay(currentText, cursorOn);
            cursorOn = !cursorOn;
            yield return new WaitForSeconds(0.45f);
        }
    }

    // ==========================================
    // --- 数字雨生成逻辑（完美兼容原逻辑） ---
    // ==========================================
    IEnumerator SpawnNormalRainRoutine()
    {
        while (isRunning)
        {
            if (spawnedNumbers.Count < maxNormalNumbers)
            {
                SpawnNormalNumber();
            }
            yield return new WaitForSeconds(normalSpawnInterval);
        }
    }

    void SpawnNormalNumber()
    {
        if (numberTextPrefab == null || rainArea == null) return;

        GameObject obj = Instantiate(numberTextPrefab, rainArea);
        spawnedNumbers.Add(obj);

        RectTransform rt = obj.GetComponent<RectTransform>();
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();

        if (text != null)
        {
            text.text = Random.Range(0, 10).ToString();
            text.color = new Color(0.45f, 1f, 0.45f, 0.65f);
            text.fontSize = 30;
        }

        float areaWidth = rainArea.rect.width;
        float areaHeight = rainArea.rect.height;

        float x = Random.Range(-areaWidth / 2f + 20f, areaWidth / 2f - 20f);
        float y = areaHeight / 2f + 30f;

        if (rt != null) rt.anchoredPosition = new Vector2(x, y);

        float speed = Random.Range(normalFallSpeedMin, normalFallSpeedMax);
        StartCoroutine(FallRoutine(obj, speed));
    }

    IEnumerator SpawnTargetRainRoutine()
    {
        while (isRunning && !puzzleCompleted)
        {
            if (!string.IsNullOrEmpty(targetCode))
            {
                int index = Random.Range(0, targetCode.Length);
                SpawnTargetNumber(index, targetCode[index].ToString());
            }
            yield return new WaitForSeconds(Random.Range(targetSpawnIntervalMin, targetSpawnIntervalMax));
        }
    }

    void SpawnTargetNumber(int index, string digit)
    {
        if (numberTextPrefab == null || rainArea == null || terminalTextDisplay == null) return;

        GameObject obj = Instantiate(numberTextPrefab, rainArea);
        spawnedNumbers.Add(obj);

        RectTransform rt = obj.GetComponent<RectTransform>();
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();

        if (text != null)
        {
            text.text = digit;
            text.color = new Color(1f, 0.15f, 0.15f, 1f);
            text.fontSize = 38;
        }

        // 红色线索数字直接在输入框上方范围随机降落
        float areaWidth = rainArea.rect.width;
        float targetX = Random.Range(-areaWidth / 3f, areaWidth / 3f);

        float areaHeight = rainArea.rect.height;
        float spawnY = areaHeight / 2f + 30f;

        if (rt != null) rt.anchoredPosition = new Vector2(targetX, spawnY);

        StartCoroutine(FallRoutine(obj, targetFallSpeed));
        StartCoroutine(TargetFadeRoutine(text));
    }

    IEnumerator FallRoutine(GameObject obj, float speed)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) yield break;

        float bottomY = -rainArea.rect.height / 2f - 50f;

        while (obj != null && rt.anchoredPosition.y > bottomY)
        {
            if (!isRunning) yield break;
            rt.anchoredPosition += Vector2.down * speed * Time.deltaTime;
            yield return null;
        }

        if (obj != null)
        {
            spawnedNumbers.Remove(obj);
            Destroy(obj);
        }
    }

    IEnumerator TargetFadeRoutine(TextMeshProUGUI text)
    {
        if (text == null) yield break;
        while (isRunning && text != null)
        {
            yield return FadeAlpha(text, 1f, 0.25f, 0.45f);
            yield return FadeAlpha(text, 0.25f, 1f, 0.45f);
        }
    }

    IEnumerator FadeAlpha(TextMeshProUGUI text, float from, float to, float duration)
    {
        if (text == null) yield break;
        float time = 0f;
        Color c = text.color;
        while (time < duration)
        {
            time += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, time / duration);
            text.color = c;
            yield return null;
        }
    }

    void ClearOldNumbers()
    {
        foreach (GameObject obj in spawnedNumbers)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedNumbers.Clear();
    }

    IEnumerator SuccessRoutine()
    {
        puzzleCompleted = true;
        isRunning = false;

        if (normalRainCoroutine != null) StopCoroutine(normalRainCoroutine);
        if (targetRainCoroutine != null) StopCoroutine(targetRainCoroutine);
        if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);

        if (feedbackText != null)
        {
            feedbackText.text = "数据拦截成功，验证码已获取。";
            feedbackText.color = Color.green;
        }

        yield return new WaitForSeconds(successDelay);

        if (codeRainPanel != null) codeRainPanel.SetActive(false);
        ClearOldNumbers();

        if (uiMazeController != null) uiMazeController.StartMaze();
    }
}