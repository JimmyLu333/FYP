using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class FilterFBXObjects : MonoBehaviour
{
    const string FBX_PATH = "Assets/Models/学校  教学楼 操场 建筑群 配楼.fbx";
    const int KEEP_START = 1272;
    const int KEEP_END = 1337;
    const string OUTPUT_PREFAB = "Assets/Models/教学楼_筛选.prefab";
    const string MATERIALS_FOLDER = "Assets/Models/教学楼_Materials";

    [MenuItem("Tools/筛选教学楼FBX物体")]
    static void FilterObjects()
    {
        GameObject fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_PATH);
        if (fbxRoot == null)
        {
            Debug.LogError($"找不到 FBX: {FBX_PATH}");
            return;
        }

        // 实例化到场景（用 Object.Instantiate，断开 prefab 连接）
        GameObject instance = Object.Instantiate(fbxRoot);
        instance.name = "教学楼_筛选";

        // ========== 第一步：收集所有子物体 ==========
        List<Transform> children = new List<Transform>();
        foreach (Transform child in instance.transform)
            children.Add(child);

        int totalCount = children.Count;
        Debug.Log($"FBX 共有 {totalCount} 个子物体");

        // ========== 第二步：复制所有保留物体的 Material 到独立 .mat 文件 ==========
        // 确保 Materials 目录存在
        if (!AssetDatabase.IsValidFolder(MATERIALS_FOLDER))
            AssetDatabase.CreateFolder("Assets/Models", "教学楼_Materials");

        Dictionary<Material, Material> materialRemap = new Dictionary<Material, Material>();
        int matCopiedCount = 0;

        for (int i = KEEP_START; i <= KEEP_END && i < totalCount; i++)
        {
            Transform t = children[i];
            Renderer renderer = t.GetComponent<Renderer>();
            if (renderer == null) continue;

            Material[] originalMats = renderer.sharedMaterials;
            List<Material> newMatsList = new List<Material>();

            foreach (Material origMat in originalMats)
            {
                if (origMat == null) { newMatsList.Add(null); continue; }

                // 已复制过则复用
                if (materialRemap.TryGetValue(origMat, out Material existingCopy))
                {
                    newMatsList.Add(existingCopy);
                    continue;
                }

                // 创建新的独立 Material（完整复制属性和纹理）
                string safeName = string.Join("_", origMat.name.Split(System.IO.Path.GetInvalidFileNameChars()));
                string destPath = AssetDatabase.GenerateUniqueAssetPath($"{MATERIALS_FOLDER}/{safeName}.mat");

                Material newMat = new Material(origMat.shader);
                newMat.name = origMat.name;
                EditorUtility.CopySerialized(origMat, newMat);

                AssetDatabase.CreateAsset(newMat, destPath);
                materialRemap[origMat] = newMat;
                newMatsList.Add(newMat);
                matCopiedCount++;
            }

            // 将复制好的 Material 赋值回 Renderer
            renderer.sharedMaterials = newMatsList.ToArray();
        }

        Debug.Log($"已复制 {matCopiedCount} 个 Material 到 {MATERIALS_FOLDER}");

        // ========== 第三步：删除范围外的子物体 ==========
        int removedCount = 0;
        for (int i = totalCount - 1; i >= 0; i--)
        {
            if (i < KEEP_START || i > KEEP_END)
            {
                DestroyImmediate(children[i].gameObject);
                removedCount++;
            }
        }

        Debug.Log($"已删除 {removedCount} 个子物体，保留 {KEEP_END - KEEP_START + 1} 个");

        // ========== 第四步：保存 Prefab ==========
        PrefabUtility.SaveAsPrefabAsset(instance, OUTPUT_PREFAB);
        Debug.Log($"已保存: {OUTPUT_PREFAB}");

        DestroyImmediate(instance);

        Object prefab = AssetDatabase.LoadAssetAtPath<Object>(OUTPUT_PREFAB);
        EditorGUIUtility.PingObject(prefab);

        AssetDatabase.Refresh();
        Debug.Log("完成！Material 已全部复制为独立 .mat 文件，Prefab 直接引用这些文件，无需重新上材质");
    }
}
