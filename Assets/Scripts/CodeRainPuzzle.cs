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

    [Header("新·黑客单行输入框架")]
    public TMP_InputField masterInputField;
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
    public float targetXRandomOffset = 10f;

    [Header("红色数字固定X轴")]
    public float[] targetFixedXPositions = new float[6]
    {
        -360f, -220f, -80f, 60f, 200f, 340f
    };

    [Header("成功设置")]
    public float successDelay = 1.2f;
    public UIMazeController uiMazeController;

    private bool isRunning = false;
    private bool puzzleCompleted = false;

    private Coroutine normalRainCoroutine;
    private Coroutine targetRainCoroutine;
    private Coroutine cursorBlinkCoroutine;

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

        masterInputField.characterLimit = targetCode.Length;
        masterInputField.contentType = TMP_InputField.ContentType.IntegerNumber;

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
            masterInputField.ActivateInputField();
        }

        if (terminalTextDisplay != null)
            terminalTextDisplay.text = "_";

        if (feedbackText != null)
            feedbackText.text = "";

        isRunning = true;
        puzzleCompleted = false;

        normalRainCoroutine = StartCoroutine(SpawnNormalRainRoutine());
        targetRainCoroutine = StartCoroutine(SpawnTargetRainRoutine());

        if (cursorBlinkCoroutine != null)
            StopCoroutine(cursorBlinkCoroutine);

        cursorBlinkCoroutine = StartCoroutine(TerminalCursorBlinkRoutine());
    }

    void OnTerminalInputChanged(string currentText)
    {
        if (!isRunning || puzzleCompleted) return;

        UpdateTerminalDisplay(currentText, true);

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

    void UpdateTerminalDisplay(string currentInput, bool showCursor)
    {
        if (terminalTextDisplay == null) return;

        string formattedText = "";

        for (int i = 0; i < currentInput.Length; i++)
        {
            formattedText += currentInput[i] + "  ";
        }

        if (currentInput.Length < targetCode.Length && showCursor)
        {
            formattedText += "_";
        }

        terminalTextDisplay.text = formattedText;
    }

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

        if (rt != null)
            rt.anchoredPosition = new Vector2(x, y);

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
        if (numberTextPrefab == null || rainArea == null) return;

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

        float targetX = 0f;

        if (targetFixedXPositions != null && index < targetFixedXPositions.Length)
        {
            targetX = targetFixedXPositions[index];
        }

        targetX += Random.Range(-targetXRandomOffset, targetXRandomOffset);

        float areaHeight = rainArea.rect.height;
        float spawnY = areaHeight / 2f + 30f;

        if (rt != null)
            rt.anchoredPosition = new Vector2(targetX, spawnY);

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

        c.a = to;
        text.color = c;
    }

    void ClearOldNumbers()
    {
        foreach (GameObject obj in spawnedNumbers)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedNumbers.Clear();
    }

    IEnumerator SuccessRoutine()
    {
        puzzleCompleted = true;
        isRunning = false;

        if (normalRainCoroutine != null)
            StopCoroutine(normalRainCoroutine);

        if (targetRainCoroutine != null)
            StopCoroutine(targetRainCoroutine);

        if (cursorBlinkCoroutine != null)
            StopCoroutine(cursorBlinkCoroutine);

        if (feedbackText != null)
        {
            feedbackText.text = "数据拦截成功，验证码已获取。";
            feedbackText.color = Color.green;
        }

        yield return new WaitForSeconds(successDelay);

        if (codeRainPanel != null)
            codeRainPanel.SetActive(false);

        ClearOldNumbers();

        if (uiMazeController != null)
            uiMazeController.StartMaze();
    }
}