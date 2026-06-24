using UnityEngine;
using UnityEngine.SceneManagement;
using PixelCrushers.DialogueSystem;

public class Case01EndingController : MonoBehaviour
{
    public string nextSceneName = "Case02";
    private bool hasTriggered = false;

    void Update()
    {
        if (hasTriggered) return;

        if (DialogueLua.GetVariable("StartCase01End").asBool)
        {
            hasTriggered = true;
            DialogueLua.SetVariable("StartCase01End", false);

            if (FadeManager.Instance != null)
            {
                FadeManager.Instance.LoadSceneWithFade(nextSceneName);
            }
            else
            {
                Debug.LogWarning("FadeManager.Instance 是 null，直接切换场景。");
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}