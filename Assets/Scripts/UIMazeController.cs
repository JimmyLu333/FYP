using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using PixelCrushers.DialogueSystem;

public class UIMazeController : MonoBehaviour
{
    [Header("迷宫UI")]
    public GameObject mazeWindowPanel;
    public RectTransform mazeArea;
    public RectTransform ball;
    public RectTransform goal;

    [Header("墙")]
    public RectTransform[] walls;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI resultText;

    [Header("结果文本")]
    [TextArea(2, 5)]
    public string successMessage = "破解成功！到账 ¥800,000";

    [TextArea(2, 5)]
    public string failMessage = "连接超时，正在重新尝试...";

    [Header("设置")]
    public float moveSpeed = 250f;
    public float timeLimit = 60f;
    public float goalDistance = 35f;

    [Header("成功后设置")]
    public float closeDelayAfterSuccess = 3f;

    [Header("碰撞设置")]
    [Range(0.1f, 1f)]
    public float ballCollisionScale = 0.6f;

    [Header("聊天系统")]
    public DialogueChatBridge dialogueChatBridge;

    [Header("可选：迷宫成功后转场")]
    public MazeFadeSceneTransition mazeFadeSceneTransition;

    [Header("失败后重开")]
    public bool restartSceneOnFail = true;
    public float restartDelay = 2f;
    public bool cleanDialogueSystemBeforeRestart = true;

    private float currentTime;
    private bool isPlaying;
    private Vector2 ballStartPos;
    private Coroutine closeCoroutine;
    private Coroutine restartCoroutine;

    void Start()
    {
        if (ball != null)
            ballStartPos = ball.anchoredPosition;

        if (mazeWindowPanel != null)
            mazeWindowPanel.SetActive(false);

        isPlaying = false;
    }

    void Update()
    {
        if (!isPlaying) return;

        MoveBall();
        UpdateTimer();
        CheckGoal();
    }

    public void StartMaze()
    {
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }

        if (restartCoroutine != null)
        {
            StopCoroutine(restartCoroutine);
            restartCoroutine = null;
        }

        if (mazeWindowPanel != null)
            mazeWindowPanel.SetActive(true);

        if (ball != null)
            ball.anchoredPosition = ballStartPos;

        currentTime = timeLimit;
        isPlaying = true;

        if (resultText != null)
            resultText.text = "";

        UpdateTimerText();
    }

    void MoveBall()
    {
        if (ball == null) return;

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 input = new Vector2(x, y).normalized;
        if (input == Vector2.zero) return;

        Vector2 oldPos = ball.anchoredPosition;
        Vector2 newPos = oldPos + input * moveSpeed * Time.deltaTime;

        ball.anchoredPosition = newPos;

        if (IsBallCollidingWithWall())
            ball.anchoredPosition = oldPos;
    }

    bool IsBallCollidingWithWall()
    {
        Rect ballRect = GetScaledRect(ball, ballCollisionScale);

        foreach (RectTransform wall in walls)
        {
            if (wall == null) continue;

            Rect wallRect = GetRect(wall);

            if (ballRect.Overlaps(wallRect))
                return true;
        }

        return false;
    }

    Rect GetRect(RectTransform rectTransform)
    {
        Vector2 pos = rectTransform.anchoredPosition;
        Vector2 size = rectTransform.rect.size;

        return new Rect(
            pos.x - size.x * rectTransform.pivot.x,
            pos.y - size.y * rectTransform.pivot.y,
            size.x,
            size.y
        );
    }

    Rect GetScaledRect(RectTransform rectTransform, float scale)
    {
        Vector2 pos = rectTransform.anchoredPosition;
        Vector2 size = rectTransform.rect.size * scale;

        return new Rect(
            pos.x - size.x * rectTransform.pivot.x,
            pos.y - size.y * rectTransform.pivot.y,
            size.x,
            size.y
        );
    }

    void UpdateTimer()
    {
        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            UpdateTimerText();
            FailMaze();
            return;
        }

        UpdateTimerText();
    }

    void UpdateTimerText()
    {
        if (timerText != null)
            timerText.text = "剩余时间：" + Mathf.CeilToInt(currentTime) + "秒";
    }

    void CheckGoal()
    {
        if (ball == null || goal == null) return;

        float distance = Vector2.Distance(ball.anchoredPosition, goal.anchoredPosition);

        if (distance <= goalDistance)
            SuccessMaze();
    }

    void SuccessMaze()
    {
        if (!isPlaying) return;

        isPlaying = false;

        if (resultText != null)
            resultText.text = successMessage;

        closeCoroutine = StartCoroutine(CloseAfterSuccess());
    }

    IEnumerator CloseAfterSuccess()
    {
        yield return new WaitForSeconds(closeDelayAfterSuccess);

        if (mazeWindowPanel != null)
            mazeWindowPanel.SetActive(false);

        if (mazeFadeSceneTransition != null)
        {
            mazeFadeSceneTransition.TriggerTransition();
            yield break;
        }

        if (dialogueChatBridge != null)
            dialogueChatBridge.ContinueAfterMaze();
    }

    void FailMaze()
    {
        if (!isPlaying) return;

        isPlaying = false;

        if (resultText != null)
            resultText.text = failMessage;

        if (restartSceneOnFail)
            restartCoroutine = StartCoroutine(RestartSceneRoutine());
    }

    IEnumerator RestartSceneRoutine()
    {
        yield return new WaitForSeconds(restartDelay);

        if (cleanDialogueSystemBeforeRestart)
        {
            if (DialogueManager.isConversationActive)
            {
                Debug.Log("UIMazeController: 重启前停止 Dialogue Conversation");
                DialogueManager.StopConversation();
            }

            DialogueSystemController[] oldManagers =
                FindObjectsOfType<DialogueSystemController>(true);

            foreach (DialogueSystemController manager in oldManagers)
            {
                Debug.Log("UIMazeController: 重启前删除 Dialogue Manager - " + manager.name);
                Destroy(manager.gameObject);
            }

            yield return null;
        }

        string currentSceneName = SceneManager.GetActiveScene().name;

        if (FadeManager.Instance != null)
            FadeManager.Instance.LoadSceneWithFade(currentSceneName);
        else
            SceneManager.LoadScene(currentSceneName);
    }
}