using UnityEngine;
using UnityEngine.SceneManagement;

public class DisE : MonoBehaviour
{
    [Header("检测设置")]
    [Tooltip("检测距离")]
    public float detectionRadius = 3f;
    [Tooltip("切换目标场景名称")]
    public string targetSceneName = "";
    [Tooltip("目标场景的出生点名称")]
    public string targetSpawnPointName = "SpawnPoint";
    [Tooltip("提示文本（可选）")]
    public string promptText = "按 E 进入";

    private GameObject player;
    private bool isPlayerInRange = false;

    // 静态变量：跨场景保存目标出生点名称
    public static string nextSpawnPoint = "";
    public static bool hasSpawnPoint = false;

    void Start()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("DisE: 未设置目标场景名称！");
        }
    }

    void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            return;
        }

        float distance = Vector3.Distance(player.transform.position, transform.position);
        isPlayerInRange = distance <= detectionRadius;

        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            SwitchScene();
        }
    }

    void SwitchScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName) && player != null)
        {
            // 保存目标出生点名称
            nextSpawnPoint = targetSpawnPointName;
            hasSpawnPoint = true;

            Debug.Log($"DisE: 切换到场景 {targetSceneName}，出生点 {targetSpawnPointName}");
            SceneManager.LoadScene(targetSceneName);
        }
    }

    void OnGUI()
    {
        if (isPlayerInRange && !string.IsNullOrEmpty(promptText))
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 screenPos = cam.WorldToScreenPoint(transform.position);
            if (screenPos.z > 0)
            {
                GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 30, 100, 30), promptText);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
    }
}
