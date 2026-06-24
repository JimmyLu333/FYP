using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using PixelCrushers.DialogueSystem;

public class MazeFadeSceneTransition : MonoBehaviour
{
    [Header("下一场景")]
    public string nextSceneName = "Case02";

    [Header("延迟转场")]
    public float delayBeforeTransition = 1.5f;

    private bool hasTriggered = false;

    public void TriggerTransition()
    {
        if (hasTriggered) return;

        hasTriggered = true;
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        yield return new WaitForSeconds(delayBeforeTransition);

        // 1. 停止当前 Case01 的 Dialogue
        if (DialogueManager.isConversationActive)
        {
            Debug.Log("MazeFadeSceneTransition: 停止旧 Conversation");
            DialogueManager.StopConversation();
        }

        // 2. 删除旧 Dialogue Manager
        DialogueSystemController[] oldManagers =
            FindObjectsOfType<DialogueSystemController>(true);

        foreach (DialogueSystemController manager in oldManagers)
        {
            Debug.Log("MazeFadeSceneTransition: 删除旧 Dialogue Manager - " + manager.name);
            Destroy(manager.gameObject);
        }

        // 3. 等一帧，确保旧 Manager 真正销毁
        yield return null;

        // 4. 转场
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