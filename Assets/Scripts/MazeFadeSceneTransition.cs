using UnityEngine;
using UnityEngine.SceneManagement;

public class MazeFadeSceneTransition : MonoBehaviour
{
    [Header("下一场景名")]
    public string nextSceneName = "Case03";

    [Header("延迟转场")]
    public float delayBeforeTransition = 1.5f;

    private bool hasTriggered = false;

    public void TriggerTransition()
    {
        if (hasTriggered) return;

        hasTriggered = true;
        Invoke(nameof(DoTransition), delayBeforeTransition);
    }

    private void DoTransition()
    {
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