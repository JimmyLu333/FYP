using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// MetaHuman 贴图修复工具 + 第一人称设置
/// 匹配逻辑: 遍历 textures 目录 → 每个叶子文件夹 = 一个材质 → 按文件名后缀分配 shader 属性
/// </summary>
public class FixMattTextures
{
    [MenuItem("Tools/Fix Matt Textures")]
    static void Fix()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[FixMatt] 找不到 Player! 请先打开包含 Player 的场景");
            return;
        }

        // === 1. 收集 Player 上所有材质 ===
        SkinnedMeshRenderer[] renderers = player.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("[FixMatt] Player 上没有 SkinnedMeshRenderer!");
            return;
        }

        var allMaterials = new List<Material>();
        var processedMaterials = new HashSet<Material>();

        foreach (var smr in renderers)
        {
            foreach (var mat in smr.sharedMaterials)
            {
                if (mat != null && !processedMaterials.Contains(mat))
                {
                    processedMaterials.Add(mat);
                    allMaterials.Add(mat);
                }
            }
        }

        Debug.Log($"[FixMatt] 共发现 {allMaterials.Count} 个唯一材质");

        // === 2. 构建贴图索引: leafFolderName → {suffix → Texture2D} ===
        string texRoot = "Assets/Character/Matt/textures";
        var texIndex = BuildTextureIndex(texRoot);

        // === 3. 对每个材质尝试匹配贴图 ===
        int fixedCount = 0;
        foreach (var mat in allMaterials)
        {
            bool modified = AssignTexturesForMaterial(mat, texIndex);
            if (modified)
            {
                EditorUtility.SetDirty(mat);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[FixMatt] 完成! 共修复 {fixedCount}/{allMaterials.Count} 个材质");

        // === 4. 第一人称设置: 隐藏身体mesh ===
        SetupFirstPerson(player, renderers);
    }

    /// <summary>
    /// 构建贴图索引: key=叶子文件夹名(小写), value={文件名后缀→Texture}
    /// </summary>
    static Dictionary<string, Dictionary<string, Texture2D>> BuildTextureIndex(string rootPath)
    {
        var index = new Dictionary<string, Dictionary<string, Texture2D>>();

        if (!Directory.Exists(rootPath))
        {
            Debug.LogError($"[FixMatt] 贴图目录不存在: {rootPath}");
            return index;
        }

        // 找所有图片文件 (png, jpg, tga)
        var allFiles = new List<string>();
        allFiles.AddRange(Directory.GetFiles(rootPath, "*.png", SearchOption.AllDirectories));
        allFiles.AddRange(Directory.GetFiles(rootPath, "*.jpg", SearchOption.AllDirectories));
        allFiles.AddRange(Directory.GetFiles(rootPath, "*.tga", SearchOption.AllDirectories));

        int count = 0;
        foreach (string filePath in allFiles)
        {
            string assetPath = filePath.Replace('\\', '/');
            if (!assetPath.StartsWith(rootPath)) continue;

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null) continue;

            // 叶子文件夹名 = 材质标识符
            string folderName = Path.GetFileName(Path.GetDirectoryName(assetPath)).ToLower();
            // 文件名(无扩展名), 小写
            string fileName = Path.GetFileNameWithoutExtension(assetPath).ToLower();

            if (!index.ContainsKey(folderName))
                index[folderName] = new Dictionary<string, Texture2D>();

            index[folderName][fileName] = tex;
            count++;
        }

        Debug.Log($"[FixMatt] 索引了 {count} 个贴图到 {index.Count} 个材质组");
        return index;
    }

    static bool AssignTexturesForMaterial(Material mat, Dictionary<string, Dictionary<string, Texture2D>> texIndex)
    {
        bool modified = false;
        string matNameLower = mat.name.ToLower()
            .Replace("mi_", "").Replace("mat_", "").Replace("material", "")
            .Replace(" ", "").Replace("-", "");

        // 尝试多种匹配方式找到对应的贴图组
        Dictionary<string, Texture2D> texGroup = null;
        string matchedKey = null;

        // 方式1: 直接匹配材质名
        if (texIndex.ContainsKey(matNameLower)) { texGroup = texIndex[matNameLower]; matchedKey = matNameLower; }
        // 方式2: 材质名包含于某个key
        if (texGroup == null)
        {
            foreach (var kvp in texIndex)
            {
                if (kvp.Key.Contains(matNameLower) || matNameLower.Contains(kvp.Key))
                {
                    texGroup = kvp.Value; matchedKey = kvp.Key; break;
                }
            }
        }
        // 方式3: 更宽松的子串匹配
        if (texGroup == null)
        {
            foreach (var kvp in texIndex)
            {
                string cleanKey = kvp.Key.Replace("_", "");
                string cleanMat = matNameLower.Replace("_", "");
                if (cleanKey.Contains(cleanMat) || cleanMat.Contains(cleanKey))
                    if (cleanKey.Length > 3 && cleanMat.Length > 3)
                    {
                        texGroup = kvp.Value; matchedKey = kvp.Key; break;
                    }
            }
        }

        if (texGroup == null)
        {
            Debug.LogWarning($"[FixMatt] 材质 '{mat.name}' 未找到匹配的贴图组 (搜索名: '{matNameLower}')");
            return false;
        }

        Debug.Log($"[FixMatt] 材质 '{mat.name}' → 匹配到贴图组 '{matchedKey}' ({texGroup.Count} 张贴图)");

        // 打印贴图组中所有文件名，方便调试
        foreach (var kvp in texGroup)
            Debug.Log($"[FixMatt]   贴图文件key: '{kvp.Key}' → '{kvp.Value.name}'");

        // 分配各种贴图类型 (MetaHuman 命名约定)
        // 注意: 不检查已有贴图，直接覆盖

        // 基色/Albedo
        modified |= ForceAssign(mat, texGroup, "_MainTex", "resourcemap_position", "basecolor", "albedo", "base_color", "diffuse", "color");
        // 法线
        modified |= ForceAssign(mat, texGroup, "_BumpMap", "resourcemap_wsnormal", "normal", "_nrm", "_normal");
        // 金属度
        modified |= ForceAssign(mat, texGroup, "_MetallicGlossMap", "metallic", "metalness");
        // AO
        modified |= ForceAssign(mat, texGroup, "_OcclusionMap", "ao", "ambient_occlusion", "occlusion");
        // 粗糙度 → 存到金属度贴图的 alpha 通道 (Standard shader 用 _MetallicGlossMap 的 alpha 做 smoothness)
        // 或者如果单独的粗糙度贴图，只设置光滑度相关

        // 对于没有基色贴图的材质，设置默认颜色
        if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") == null)
        {
            Color defaultColor = GetDefaultColor(mat.name);
            if (defaultColor != Color.white)
            {
                mat.SetColor("_Color", defaultColor);
                Debug.Log($"[FixMatt]   ✓ _Color ← {defaultColor} (无基色贴图，使用默认颜色)");
                modified = true;
            }
        }

        // 设置光滑度来源：如果金属度贴图有 alpha，用 alpha 做 smoothness
        if (mat.HasProperty("_GlossMapScale"))
        {
            // 如果有粗糙度贴图但没有金属度贴图，可以把粗糙度贴图赋给 _MetallicGlossMap
            // 但粗糙度和金属度是不同的，这里简单处理
        }

        return modified;
    }

    /// <summary>
    /// 强制分配贴图到 shader 属性 (不检查已有贴图)
    /// </summary>
    static bool ForceAssign(Material mat, Dictionary<string, Texture2D> group, string shaderProp, params string[] textureKeywords)
    {
        if (!mat.HasProperty(shaderProp))
        {
            Debug.Log($"[FixMatt]   ✗ {shaderProp} — shader 不支持此属性");
            return false;
        }

        foreach (var kvp in group)
        {
            foreach (string kw in textureKeywords)
            {
                if (kvp.Key.EndsWith(kw) || kvp.Key.Contains(kw))
                {
                    mat.SetTexture(shaderProp, kvp.Value);
                    Debug.Log($"[FixMatt]   ✓ {shaderProp} ← {kvp.Value.name}");
                    return true;
                }
            }
        }

        Debug.Log($"[FixMatt]   ✗ {shaderProp} — 未找到匹配贴图 (搜索: {string.Join(", ", textureKeywords)})");
        return false;
    }

    /// <summary>
    /// 无基色贴图时的默认颜色
    /// </summary>
    static Color GetDefaultColor(string matName)
    {
        string name = matName.ToLower();
        if (name.Contains("boxers")) return new Color(0.15f, 0.15f, 0.15f); // 深灰
        if (name.Contains("jeans")) return new Color(0.15f, 0.2f, 0.45f);   // 牛仔蓝
        if (name.Contains("shirt")) return new Color(0.6f, 0.65f, 0.7f);    // 浅灰蓝
        if (name.Contains("shoes")) return new Color(0.85f, 0.85f, 0.82f);  // 白色
        if (name.Contains("skin"))  return new Color(0.87f, 0.72f, 0.58f);  // 肤色 (备选)
        return Color.white;
    }

    /// <summary>
    /// 第一人称设置: 隐藏身体等不应可见的部分，只保留手臂
    /// </summary>
    static void SetupFirstPerson(GameObject player, SkinnedMeshRenderer[] renderers)
    {
        // 第一人称游戏中应该隐藏的部位 (不要看到自己的身体/头/衣服)
        string[] hideKeywords = new[]
        {
            "body", "head", "spine", "hip", "pelvis", "torso",
            "leg", "thigh", "calf", "foot",
            "jeans", "pants", "shirt", "boxers",
            "shoes",
            "teeth", "tongue",  // 口腔内部
            "eyelash", "cornea", "eye_occlusion", "tearline",  // 眼部细节
            "nails",
            "scalp",  // 头皮
            "bangs"   // 刘海
        };

        // 需要保留可见的 (手臂/手 — 第一人称可见)
        string[] keepKeywords = new[]
        {
            "arm", "hand", "finger", "wrist", "sleeve"
        };

        int hiddenCount = 0;
        foreach (var smr in renderers)
        {
            string name = smr.gameObject.name.ToLower();
            bool shouldHide = false;
            bool shouldKeep = false;

            foreach (var kw in hideKeywords) { if (name.Contains(kw)) shouldHide = true; }
            foreach (var kw in keepKeywords) { if (name.Contains(kw)) shouldKeep = true; }

            // 如果同时命中 keep 和 hide，优先保留
            if (shouldKeep) shouldHide = false;

            if (shouldHide)
            {
                smr.enabled = false;
                EditorUtility.SetDirty(smr);
                hiddenCount++;
                Debug.Log($"[FixMatt] 隐藏(第一人称): {smr.gameObject.name}");
            }
            else
            {
                Debug.Log($"[FixMatt] 保留可见: {smr.gameObject.name}");
            }
        }

        if (hiddenCount > 0)
            Debug.Log($"[FixMatt] 已隐藏 {hiddenCount} 个身体部位 mesh (第一人称模式)");
    }
}
