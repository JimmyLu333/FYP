using UnityEngine;
using TMPro;

public class ScamScoreManager : MonoBehaviour
{
    public static ScamScoreManager Instance;
    public TextMeshProUGUI scamRateText;

    [Header("诈骗成功率")]
    public int currentScamRate = 0;
    public int maxScamRate = 100;

    [Header("所有需要显示诈骗分的UI")]
    public TextMeshProUGUI[] scamRateTexts;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddScore(int amount)
    {
        currentScamRate += amount;
        currentScamRate = Mathf.Clamp(currentScamRate, 0, maxScamRate);

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (scamRateTexts == null) return;

        foreach (TextMeshProUGUI text in scamRateTexts)
        {
            if (text != null)
                text.text = "诈骗成功率：" + currentScamRate + "%";
        }
    }

    public int GetScore()
    {
        return currentScamRate;
    }


}