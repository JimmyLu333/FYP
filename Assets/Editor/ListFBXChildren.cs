using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ListFBXChildren : MonoBehaviour
{
    const string FBX_PATH = "Assets/Models/学校  教学楼 操场 建筑群 配楼.fbx";

    [MenuItem("Tools/列出教学楼FBX子物体")]
    static void ListChildren()
    {
        GameObject fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_PATH);
        if (fbxRoot == null)
        {
            Debug.LogError($"找不到 FBX: {FBX_PATH}");
            return;
        }

        GameObject instance = Object.Instantiate(fbxRoot);

        // 第一层子物体
        List<Transform> children = new List<Transform>();
        foreach (Transform child in instance.transform)
            children.Add(child);

        Debug.Log($"========== 第一层子物体共 {children.Count} 个 ==========");
        for (int i = 0; i < children.Count; i++)
        {
            int meshCount = 0;
            var renderer = children[i].GetComponent<MeshRenderer>();
            var smr = children[i].GetComponent<SkinnedMeshRenderer>();
            if (renderer != null) meshCount = 1;
            if (smr != null) meshCount = 1;
            Debug.Log($"[{i}] {children[i].name} (有Mesh:{meshCount > 0})");
        }

        // 递归统计所有节点
        int totalNodes = 0;
        int totalMeshes = 0;
        CountRecursive(instance.transform, ref totalNodes, ref totalMeshes);
        Debug.Log($"========== 递归统计: 总节点 {totalNodes}，有Mesh的节点 {totalMeshes} ==========");

        // 也列出所有递归节点（带索引）
        Debug.Log("========== 递归列出所有节点 ==========");
        List<Transform> allNodes = new List<Transform>();
        CollectAllRecursive(instance.transform, allNodes);
        for (int i = 0; i < allNodes.Count; i++)
        {
            var r = allNodes[i].GetComponent<MeshRenderer>();
            var s = allNodes[i].GetComponent<SkinnedMeshRenderer>();
            string meshInfo = (r != null || s != null) ? " [MESH]" : "";
            Debug.Log($"[{i}] {GetPath(allNodes[i])}{meshInfo}");
        }

        DestroyImmediate(instance);
    }

    static void CountRecursive(Transform t, ref int nodes, ref int meshes)
    {
        nodes++;
        if (t.GetComponent<MeshRenderer>() || t.GetComponent<SkinnedMeshRenderer>())
            meshes++;
        foreach (Transform child in t)
            CountRecursive(child, ref nodes, ref meshes);
    }

    static void CollectAllRecursive(Transform t, List<Transform> list)
    {
        list.Add(t);
        foreach (Transform child in t)
            CollectAllRecursive(child, list);
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
