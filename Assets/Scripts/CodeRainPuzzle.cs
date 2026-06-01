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

    [Header("输入框")]
    public TMP_InputField[] codeInputs;

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
    public float targetRespawnDelay = 2.5f;

    [Header("成功设置")]
    public float successDelay = 1.2f;
    public UIMazeController uiMazeController;

    private bool isRunning = false;
    private bool puzzleCompleted = false;

    private Coroutine normalRainCoroutine;
    private Coroutine targetRainCoroutine;

    private List<GameObject> spawnedNumbers = new List<GameObject>();

    void Start()
    {
        if (codeRainPanel != null)
            codeRainPanel.SetActive(false);

        if (feedbackText != null)
            feedbackText.text = "";

        SetupInputs();
    }

    void SetupInputs()
    {
        if (codeInputs == null) return;

        for (int i = 0; i < codeInputs.Length; i++)
        {
            int index = i;

            if (codeInputs[i] == null) continue;

            codeInputs[i].characterLimit = 1;
            codeInputs[i].contentType = TMP_InputField.ContentType.IntegerNumber;

            codeInputs[i].onValueChanged.RemoveAllListeners();
            codeInputs[i].onValueChanged.AddListener((value) => OnInputChanged(index, value));
        }
    }

    void Update()
    {
        if (!isRunning || puzzleCompleted) return;

        HandleBackspace();
    }

    public void StartCodeRain()
    {
        if (codeRainPanel != null)
            codeRainPanel.SetActive(true);

        ClearOldNumbers();
        ClearInputs();

        if (feedbackText != null)
            feedbackText.text = "";

        isRunning = true;
        puzzleCompleted = false;

        normalRainCoroutine = StartCoroutine(SpawnNormalRainRoutine());
        targetRainCoroutine = StartCoroutine(SpawnTargetRainRoutine());

        FocusInput(0);
    }

    void ClearInputs()
    {
        if (codeInputs == null) return;

        foreach (TMP_InputField input in codeInputs)
        {
            if (input != null)
                input.text = "";
        }
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
                int index = Random.Range(0, Mathf.Min(targetCode.Length, codeInputs.Length));
                SpawnTargetNumber(index, targetCode[index].ToString());
            }

            yield return new WaitForSeconds(Random.Range(targetSpawnIntervalMin, targetSpawnIntervalMax));
        }
    }

    void SpawnTargetNumber(int index, string digit)
    {
        if (numberTextPrefab == null || rainArea == null) return;
        if (codeInputs == null || index >= codeInputs.Length) return;
        if (codeInputs[index] == null) return;

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

        RectTransform inputRT = codeInputs[index].GetComponent<RectTransform>();

        // ✅ 把 InputField 的世界坐标转换成 RainArea 里的局部坐标
        Vector3 inputWorldPos = inputRT.TransformPoint(inputRT.rect.center);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rainArea,
            RectTransformUtility.WorldToScreenPoint(null, inputWorldPos),
            null,
            out localPoint
        );

        float targetX = localPoint.x + Random.Range(-targetXRandomOffset, targetXRandomOffset);

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
            float t = time / duration;

            c.a = Mathf.Lerp(from, to, t);
            text.color = c;

            yield return null;
        }

        c.a = to;
        text.color = c;
    }

    void OnInputChanged(int index, string value)
    {
        if (!isRunning || puzzleCompleted) return;
        if (codeInputs[index] == null) return;

        if (value.Length > 1)
        {
            value = value.Substring(value.Length - 1, 1);
            codeInputs[index].text = value;
        }

        if (value.Length == 1)
        {
            if (index < codeInputs.Length - 1)
            {
                FocusInput(index + 1);
            }
        }

        CheckInputAuto();
    }

    void HandleBackspace()
    {
        if (!Input.GetKeyDown(KeyCode.Backspace)) return;
        if (codeInputs == null) return;

        int activeIndex = GetActiveInputIndex();

        if (activeIndex <= 0) return;

        if (codeInputs[activeIndex] != null && string.IsNullOrEmpty(codeInputs[activeIndex].text))
        {
            FocusInput(activeIndex - 1);
            codeInputs[activeIndex - 1].text = "";
        }
    }

    int GetActiveInputIndex()
    {
        for (int i = 0; i < codeInputs.Length; i++)
        {
            if (codeInputs[i] != null && codeInputs[i].isFocused)
                return i;
        }

        return -1;
    }

    void FocusInput(int index)
    {
        if (codeInputs == null) return;
        if (index < 0 || index >= codeInputs.Length) return;
        if (codeInputs[index] == null) return;

        codeInputs[index].Select();
        codeInputs[index].ActivateInputField();
    }

    void CheckInputAuto()
    {
        string result = "";

        foreach (TMP_InputField input in codeInputs)
        {
            if (input != null)
                result += input.text.Trim();
        }

        if (result.Length < targetCode.Length)
            return;

        if (result == targetCode)
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

    IEnumerator SuccessRoutine()
    {
        puzzleCompleted = true;
        isRunning = false;

        if (normalRainCoroutine != null)
            StopCoroutine(normalRainCoroutine);

        if (targetRainCoroutine != null)
            StopCoroutine(targetRainCoroutine);

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