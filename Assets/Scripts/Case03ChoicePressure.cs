using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Case03ChoicePressure : MonoBehaviour
{
    public RectTransform fakeCursor;

    [Header("危险选项关键词")]
    public string[] dangerousKeywords;

    [Header("范围设置")]
    public float detectRadius = 250f;
    public float blockRadius = 90f;

    [Header("抖动设置")]
    public float maxShakeAmount = 25f;

    private bool pressureEnabled = false;

    private Button[] currentButtons;
    private TextMeshProUGUI[] currentTexts;
    private bool[] isDangerous;

    void Start()
    {
        if (fakeCursor != null)
            fakeCursor.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!pressureEnabled) return;

        Vector2 mousePos = Input.mousePosition;

        float shake = GetShakeAmount(mousePos);
        bool nearDanger = shake > 0.1f;

        if (nearDanger)
        {
            Cursor.visible = false;

            if (fakeCursor != null)
            {
                fakeCursor.gameObject.SetActive(true);
                Vector2 jitter = Random.insideUnitCircle * shake;
                fakeCursor.position = mousePos + jitter;
            }
        }
        else
        {
            Cursor.visible = true;

            if (fakeCursor != null)
                fakeCursor.gameObject.SetActive(false);
        }

        UpdateButtonBlock(mousePos);
    }

    public void EnablePressure()
    {
        pressureEnabled = true;

        Cursor.visible = true;

        if (fakeCursor != null)
            fakeCursor.gameObject.SetActive(false);
    }

    public void DisablePressure()
    {
        pressureEnabled = false;
        Cursor.visible = true;

        if (fakeCursor != null)
            fakeCursor.gameObject.SetActive(false);

        RestoreDangerousButtons();
    }

    public void RegisterChoices(Button[] buttons, TextMeshProUGUI[] texts)
    {
        currentButtons = buttons;
        currentTexts = texts;

        if (buttons == null || texts == null) return;

        isDangerous = new bool[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null || texts[i] == null)
            {
                isDangerous[i] = false;
                continue;
            }

            string choiceText = texts[i].text;
            isDangerous[i] = IsDangerousChoice(choiceText);

            if (isDangerous[i])
            {
                buttons[i].interactable = true;
                Debug.Log("Case03 危险选项：" + choiceText);
            }
        }
    }

    bool IsDangerousChoice(string choiceText)
    {
        if (string.IsNullOrEmpty(choiceText)) return false;
        if (dangerousKeywords == null) return false;

        foreach (string keyword in dangerousKeywords)
        {
            if (string.IsNullOrEmpty(keyword)) continue;

            if (choiceText.Contains(keyword))
                return true;
        }

        return false;
    }
    /*
    void UpdateFakeCursor(Vector2 mousePos)
    {
        if (fakeCursor == null) return;

        float shake = GetShakeAmount(mousePos);
        Vector2 jitter = Random.insideUnitCircle * shake;

        fakeCursor.position = mousePos + jitter;
    }
    */
    void UpdateButtonBlock(Vector2 mousePos)
    {
        if (currentButtons == null || isDangerous == null) return;

        for (int i = 0; i < currentButtons.Length; i++)
        {
            if (currentButtons[i] == null) continue;
            if (i >= isDangerous.Length) continue;

            if (!isDangerous[i]) continue;

            RectTransform rect = currentButtons[i].GetComponent<RectTransform>();

            bool mouseInsideButton = RectTransformUtility.RectangleContainsScreenPoint(
                rect,
                mousePos,
                null
            );

            currentButtons[i].interactable = !mouseInsideButton;
        }
    }

    float GetShakeAmount(Vector2 mousePos)
    {
        if (currentButtons == null || isDangerous == null) return 0f;

        for (int i = 0; i < currentButtons.Length; i++)
        {
            if (currentButtons[i] == null) continue;
            if (i >= isDangerous.Length) continue;
            if (!isDangerous[i]) continue;

            RectTransform rect = currentButtons[i].GetComponent<RectTransform>();

            bool mouseInsideButton = RectTransformUtility.RectangleContainsScreenPoint(
                rect,
                mousePos,
                null
            );

            if (mouseInsideButton)
            {
                return maxShakeAmount;
            }
        }

        return 0f;
    }

    void RestoreDangerousButtons()
    {
        if (currentButtons == null || isDangerous == null) return;

        for (int i = 0; i < currentButtons.Length; i++)
        {
            if (currentButtons[i] == null) continue;
            if (i >= isDangerous.Length) continue;

            if (isDangerous[i])
                currentButtons[i].interactable = true;
        }
    }
}