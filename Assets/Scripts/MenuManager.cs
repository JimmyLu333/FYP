using UnityEngine;
using UnityEngine.SceneManagement;
using PixelCrushers.DialogueSystem;

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

    public void StartGame()
    {
        var oldManagers = FindObjectsOfType<DialogueSystemController>();

        foreach (var manager in oldManagers)
        {
            Destroy(manager.gameObject);
        }

        SceneManager.LoadScene("Beganing scenes");
    }

    public void ExitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();
    }
}