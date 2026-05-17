using UnityEngine;
using UnityEditor;

public class AddCollidersToSelection
{
    [MenuItem("Tools/给选中物体添加碰撞体")]
    static void AddColliders()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            Debug.LogWarning("请先在 Hierarchy 中选中物体（如 DoorFrameClosedB）");
            return;
        }

        int count = 0;
        foreach (GameObject go in selected)
        {
            // 给自身和所有子物体加碰撞体
            AddColliderRecursive(go, ref count);
        }
        Debug.Log($"完成！共添加了 {count} 个碰撞体");
    }

    static void AddColliderRecursive(GameObject go, ref int count)
    {
        // 如果已有碰撞体就跳过
        if (go.GetComponent<Collider>() == null)
        {
            // 有 MeshRenderer 的用 MeshCollider，没有的用 BoxCollider
            if (go.GetComponent<MeshRenderer>() != null)
            {
                go.AddComponent<MeshCollider>();
            }
            else
            {
                go.AddComponent<BoxCollider>();
            }
            count++;
        }

        // 递归处理子物体
        foreach (Transform child in go.transform)
        {
            AddColliderRecursive(child.gameObject, ref count);
        }
    }

    // 验证菜单是否可用（必须有选中物体）
    [MenuItem("Tools/给选中物体添加碰撞体", true)]
    static bool ValidateAddColliders()
    {
        return Selection.gameObjects.Length > 0;
    }
}
