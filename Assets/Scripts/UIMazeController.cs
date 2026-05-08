using UnityEngine;
using TMPro;

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

    [Header("设置")]
    public float moveSpeed = 250f;
    public float timeLimit = 60f;
    public float goalDistance = 35f;

    private float currentTime;
    private bool isPlaying;
    private Vector2 ballStartPos;

    void Start()
    {
        if (ball != null)
            ballStartPos = ball.anchoredPosition;

        StartMaze();
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
        {
            ball.anchoredPosition = oldPos;
        }
    }

    bool IsBallCollidingWithWall()
    {
        Rect ballRect = GetRect(ball);

        foreach (RectTransform wall in walls)
        {
            if (wall == null) continue;

            Rect wallRect = GetRect(wall);

            if (ballRect.Overlaps(wallRect))
            {
                return true;
            }
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
        {
            SuccessMaze();
        }
    }

    void SuccessMaze()
    {
        if (!isPlaying) return;

        isPlaying = false;

        if (resultText != null)
            resultText.text = "破解成功！到账 ¥50,000";
    }

    void FailMaze()
    {
        if (!isPlaying) return;

        isPlaying = false;

        if (resultText != null)
            resultText.text = "时间耗尽，破解失败。";
    }
}