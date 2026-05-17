using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisE : MonoBehaviour
{
    [Header("检测设置")]
    [Tooltip("检测距离")]
    public float detectionRadius = 3f;
    [Tooltip("切换目标场景名称")]
    public string targetSceneName = "";
    [Tooltip("提示文本（可选）")]
    public string promptText = "按 E 进入";

    private GameObject player;
    private bool isPlayerInRange = false;
    private bool canSwitchScene = false;

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
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    void OnGUI()
    {
        if (isPlayerInRange && !string.IsNullOrEmpty(promptText))
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
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