using UnityEngine;
using UnityEngine.SceneManagement;
using PixelCrushers.DialogueSystem;

public class DialogueEndSceneTransition : MonoBehaviour
{
    [Header("下一个场景名")]
    public string nextSceneName = "EndingScene";

    [Header("触发变量名")]
    public string triggerVariableName = "StartCase03End";

    [Header("延迟转场")]
    public float delayBeforeTransition = 1.5f;

    private bool hasTriggered = false;

    void Update()
    {
        if (hasTriggered) return;

        if (DialogueLua.GetVariable(triggerVariableName).asBool)
        {
            hasTriggered = true;
            DialogueLua.SetVariable(triggerVariableName, false);

            Invoke(nameof(DoTransition), delayBeforeTransition);
        }
    }

    public void TriggerTransition()
    {
        if (hasTriggered) return;

        hasTriggered = true;
        Invoke(nameof(DoTransition), delayBeforeTransition);
    }

    private void DoTransition()
    {
        if (DialogueManager.isConversationActive)
            DialogueManager.StopConversation();

        if (FadeManager.Instance != null)
            FadeManager.Instance.LoadSceneWithFade(nextSceneName);
        else
            SceneManager.LoadScene(nextSceneName);
    }
}