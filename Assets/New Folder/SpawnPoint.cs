using UnityEngine;

// 出生点标记脚本，挂在空 GameObject 上即可
// 场景切换时玩家会出现在这个位置
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("出生点名称，与 DisE 的 targetSpawnPointName 对应")]
    public string spawnName = "SpawnPoint";

    void Awake()
    {
        // 在 Awake 中设置名称，确保 OnSceneLoaded 回调时能被 GameObject.Find 找到
        gameObject.name = spawnName;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        // 画一个箭头表示朝向
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
