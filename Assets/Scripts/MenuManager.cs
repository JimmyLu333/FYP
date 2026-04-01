using UnityEngine;
using UnityEngine.SceneManagement; // 👈 一定要加这个

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;

    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }

    // ✅ 新增：开始游戏
    public void StartGame()
    {
        SceneManager.LoadScene("Beganing scenes"); // 👈 改成你的诈骗场景名字
    }
}