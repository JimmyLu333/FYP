using UnityEngine;
using TMPro;
using System.Collections;
using PixelCrushers.DialogueSystem;

public enum PressureStage
{
    Normal,
    InnerVoice,
    MouseShake,
    Flashback,
    Glitch,
    Dizzy,
    Collapse
}

public class Case03PressureManager : MonoBehaviour
{
    [Header("当前压力阶段")]
    public PressureStage currentStage = PressureStage.Normal;

    [Header("Case03系统")]
    public Case03ChoicePressure choicePressure;

    [Header("内心独白 UI")]
    public GameObject innerVoicePanel;
    public TextMeshProUGUI innerVoiceText;

    [Header("内心独白数组")]
    [TextArea(2, 4)]
    public string[] innerVoiceLines;

    [Header("打字设置")]
    public float typingSpeed = 0.04f;
    public float stayDuration = 2f;

    [Header("淡入淡出设置")]
    public CanvasGroup innerVoiceCanvasGroup;
    public float fadeDuration = 0.35f;

    private Coroutine innerVoiceCoroutine;

    private bool lastPlayInnerVoice = false;


    void Start()
    {
        if (innerVoicePanel != null)
            innerVoicePanel.SetActive(false);

        if (innerVoiceText != null)
            innerVoiceText.text = "";

        if (innerVoiceCanvasGroup != null)
            innerVoiceCanvasGroup.alpha = 0f;
    }

    void Update()
    {
        bool playInnerVoice = DialogueLua.GetVariable("PlayInnerVoice").asBool;

        if (playInnerVoice && !lastPlayInnerVoice)
        {
            int index = DialogueLua.GetVariable("InnerVoiceIndex").asInt;
            PlayInnerVoiceByIndex(index);

            DialogueLua.SetVariable("PlayInnerVoice", false);
        }

        lastPlayInnerVoice = playInnerVoice;
    }

    public void SetStage(PressureStage newStage)
    {
        currentStage = newStage;
        Debug.Log("Case03 Stage changed to: " + newStage);
    }

    public void PlayInnerVoiceByIndex(int index)
    {
        if (innerVoiceLines == null || innerVoiceLines.Length == 0)
        {
            Debug.LogWarning("InnerVoiceLines 还没有填写。");
            return;
        }

        if (index < 0 || index >= innerVoiceLines.Length)
        {
            Debug.LogWarning("InnerVoice index 超出范围：" + index);
            return;
        }

        ShowInnerVoice(innerVoiceLines[index]);
    }

    public void PlayInnerVoiceSequence(int startIndex, int count)
    {
        if (innerVoiceLines == null || innerVoiceLines.Length == 0)
        {
            Debug.LogWarning("InnerVoiceLines 还没有填写。");
            return;
        }

        if (startIndex < 0 || startIndex >= innerVoiceLines.Length)
        {
            Debug.LogWarning("InnerVoice startIndex 超出范围：" + startIndex);
            return;
        }

        int endIndex = Mathf.Min(startIndex + count, innerVoiceLines.Length);
        string[] selectedLines = new string[endIndex - startIndex];

        for (int i = 0; i < selectedLines.Length; i++)
        {
            selectedLines[i] = innerVoiceLines[startIndex + i];
        }

        ShowInnerVoices(selectedLines);
    }

    public void ShowInnerVoice(string text)
    {
        SetStage(PressureStage.InnerVoice);

        if (innerVoiceCoroutine != null)
            StopCoroutine(innerVoiceCoroutine);

        innerVoiceCoroutine = StartCoroutine(InnerVoiceRoutine(text));
    }

    public void ShowInnerVoices(string[] texts)
    {
        SetStage(PressureStage.InnerVoice);

        if (innerVoiceCoroutine != null)
            StopCoroutine(innerVoiceCoroutine);

        innerVoiceCoroutine = StartCoroutine(InnerVoicesRoutine(texts));
    }

    private IEnumerator InnerVoiceRoutine(string text)
    {
        if (innerVoicePanel != null)
            innerVoicePanel.SetActive(true);
        yield return FadeCanvasGroup(innerVoiceCanvasGroup, 0f, 1f, fadeDuration);

        if (innerVoiceText != null)
            innerVoiceText.text = "";

        foreach (char c in text)
        {
            if (innerVoiceText != null)
                innerVoiceText.text += c;

            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(stayDuration);

        yield return StartCoroutine(HideInnerVoiceRoutine());
    }

    private IEnumerator InnerVoicesRoutine(string[] texts)
    {
        if (innerVoicePanel != null)
            innerVoicePanel.SetActive(true);
        yield return FadeCanvasGroup(innerVoiceCanvasGroup, 0f, 1f, fadeDuration);

        foreach (string line in texts)
        {
            if (innerVoiceText != null)
                innerVoiceText.text = "";

            foreach (char c in line)
            {
                if (innerVoiceText != null)
                    innerVoiceText.text += c;

                yield return new WaitForSeconds(typingSpeed);
            }

            yield return new WaitForSeconds(stayDuration);
        }

        yield return StartCoroutine(HideInnerVoiceRoutine());
    }

    private IEnumerator HideInnerVoiceRoutine()
    {
        yield return FadeCanvasGroup(innerVoiceCanvasGroup, 1f, 0f, fadeDuration);

        if (innerVoicePanel != null)
            innerVoicePanel.SetActive(false);

        if (innerVoiceText != null)
            innerVoiceText.text = "";
    }

    public void StartMouseShakeStage()
    {
        SetStage(PressureStage.MouseShake);

        if (choicePressure != null)
            choicePressure.EnablePressure();

        Debug.Log("鼠标颤抖阶段开始");
    }

    public void StopMouseShakeStage()
    {
        if (choicePressure != null)
            choicePressure.DisablePressure();

        Debug.Log("鼠标颤抖阶段结束");
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float time = 0f;
        group.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }

        group.alpha = to;
    }
}