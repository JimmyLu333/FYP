using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using PixelCrushers.DialogueSystem;

public class LoadVideoScene : MonoBehaviour
{
    [Header("ÏÂÒ»¸ö³¡¾°")]
    public string sceneName = "BeginningCG";

    public void LoadScene()
    {
        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        if (DialogueManager.isConversationActive)
        {
            Debug.Log("LoadVideoScene: Í£Ö¹¾É Conversation");
            DialogueManager.StopConversation();
        }

        DialogueSystemController[] oldManagers =
            FindObjectsOfType<DialogueSystemController>(true);

        foreach (DialogueSystemController manager in oldManagers)
        {
            Debug.Log("LoadVideoScene: É¾³ý¾É Dialogue Manager - " + manager.name);
            Destroy(manager.gameObject);
        }

        yield return null;

        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.LoadSceneWithFade(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}

