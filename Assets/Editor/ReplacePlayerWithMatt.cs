using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ReplacePlayerWithMatt
{
    [MenuItem("Tools/替换角色为 Matt")]
    public static void Replace()
    {
        // 1. 找到旧 player
        GameObject oldPlayer = GameObject.FindGameObjectWithTag("Player");
        if (oldPlayer == null)
        {
            oldPlayer = GameObject.Find("player");
        }
        if (oldPlayer == null)
        {
            Debug.LogError("找不到旧 player 对象！");
            return;
        }

        // 2. 找到旧摄像机（不限制在 player 下）
        Camera existingCam = Camera.main;
        if (existingCam == null)
        {
            // 找场景中任意一个 Camera
            Camera[] allCams = Object.FindObjectsOfType<Camera>();
            if (allCams.Length > 0) existingCam = allCams[0];
        }

        // 3. 找到 Matt 模型
        string[] mattGuids = AssetDatabase.FindAssets("Matt t:Model");
        if (mattGuids.Length == 0)
        {
            Debug.LogError("找不到 Matt.Fbx 模型！");
            return;
        }
        string mattPath = AssetDatabase.GUIDToAssetPath(mattGuids[0]);
        GameObject mattPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mattPath);
        if (mattPrefab == null)
        {
            Debug.LogError("无法加载 Matt 模型: " + mattPath);
            return;
        }

        // 4. 记录位置
        Vector3 position = oldPlayer.transform.position;
        Quaternion rotation = oldPlayer.transform.rotation;

        // 5. 实例化 Matt
        GameObject newPlayer = Object.Instantiate(mattPrefab);
        newPlayer.transform.position = position;
        newPlayer.transform.rotation = rotation;
        newPlayer.name = "player";
        newPlayer.tag = "Player";

        // 6. 添加组件
        PLayerMove moveScript = newPlayer.AddComponent<PLayerMove>();
        CharacterController cc = newPlayer.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.center = new Vector3(0f, 1f, 0f);
            cc.height = 2f;
            cc.radius = 0.5f;
        }

        // 7. 处理摄像机
        if (existingCam != null)
        {
            existingCam.transform.SetParent(newPlayer.transform, false);
            existingCam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            existingCam.transform.localRotation = Quaternion.identity;
            Debug.Log("已将现有摄像机移动到 Matt 下");
        }
        else
        {
            CreateCameraUnder(newPlayer);
        }

        // 8. 删除旧 player
        Object.DestroyImmediate(oldPlayer);

        EditorSceneManager.MarkSceneDirty(newPlayer.scene);
        Selection.activeGameObject = newPlayer;
        Debug.Log("✅ 角色替换完成！Matt 已就位，含 PLayerMove + CharacterController + Camera");
    }

    [MenuItem("Tools/修复 Player 摄像机")]
    public static void FixCamera()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("player");

        if (player == null)
        {
            Debug.LogError("找不到 player 对象！");
            return;
        }

        // 检查是否已有摄像机
        Camera existingCam = player.GetComponentInChildren<Camera>();

        if (existingCam != null)
        {
            Debug.Log("Player 下已有摄像机，无需修复。位置: " + existingCam.transform.localPosition);
            return;
        }

        // 尝试找场景中其他摄像机
        Camera sceneCam = Camera.main;
        if (sceneCam == null)
        {
            Camera[] allCams = Object.FindObjectsOfType<Camera>();
            if (allCams.Length > 0) sceneCam = allCams[0];
        }

        if (sceneCam != null)
        {
            sceneCam.transform.SetParent(player.transform, false);
            sceneCam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            sceneCam.transform.localRotation = Quaternion.identity;
            Debug.Log("✅ 已将场景中的摄像机移到 Player 下");
        }
        else
        {
            CreateCameraUnder(player);
        }

        EditorSceneManager.MarkSceneDirty(player.scene);
        Selection.activeGameObject = player;
    }

    static void CreateCameraUnder(GameObject parent)
    {
        GameObject camObj = new GameObject("Main Camera");
        camObj.transform.SetParent(parent.transform, false);
        camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        camObj.transform.localRotation = Quaternion.identity;
        Camera newCam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();
        camObj.tag = "MainCamera";
        Debug.Log("✅ 已在 Player 下创建新摄像机");
    }
}
