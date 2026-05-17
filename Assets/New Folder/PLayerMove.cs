using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PLayerMove : MonoBehaviour
{
    [Header("移动设置")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float gravity = -20f;            // 加大重力，落地更干脆
    public float jumpHeight = 1.2f;
    public float acceleration = 8f;
    public float deceleration = 6f;

    [Header("视角设置")]
    public float mouseSensitivity = 800f;   // 灵敏度
    public float upDownLimit = 80f;

    [Header("走路晃动")]
    public float walkBobFrequency = 12f;    // 走路步频
    public float runBobFrequency = 16f;     // 跑步步频更快
    public float walkBobAmount = 0.08f;     // 走路晃动幅度
    public float runBobAmount = 0.12f;      // 跑步晃动更大
    public float tiltAmount = 3f;           // 走路时视角微微倾斜

    [Header("脚步声")]
    public float stepInterval = 0.5f;       // 脚步间隔（秒）

    private CharacterController controller;
    private Camera playerCamera;
    private float rotationX = 0f;
    private Vector3 velocity;
    private Vector3 currentMoveVelocity;
    private float bobTimer = 0f;
    private float defaultCameraY = 1.6f;
    private float defaultCameraX = 0f;
    private float stepTimer = 0f;
    private bool cameraInitialized = false;

    void Start()
    {
        // 让 player 跨场景存活，切换场景时不被销毁
        DontDestroyOnLoad(gameObject);

        controller = GetComponent<CharacterController>();
        TryFindCamera();

        // 监听场景加载事件，切换场景后传送到出生点
        SceneManager.sceneLoaded += OnSceneLoaded;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 删除新场景中多余的 Audio Listener 和 Camera，只保留 Player 上的
        RemoveExtraAudioListeners();
        DisableExtraCameras();

        if (DisE.hasSpawnPoint)
        {
            StartCoroutine(TeleportToSpawnPoint());
        }

        // 重置速度，防止场景切换后继续滑动
        velocity = Vector3.zero;
        currentMoveVelocity = Vector3.zero;
    }

    void RemoveExtraAudioListeners()
    {
        AudioListener[] allListeners = FindObjectsOfType<AudioListener>();
        AudioListener playerListener = GetComponentInChildren<AudioListener>();
        foreach (AudioListener al in allListeners)
        {
            if (al != playerListener)
            {
                Destroy(al);
            }
        }
    }

    void DisableExtraCameras()
    {
        Camera[] allCameras = FindObjectsOfType<Camera>();
        Camera playerCam = GetComponentInChildren<Camera>();
        foreach (Camera cam in allCameras)
        {
            if (cam != playerCam)
            {
                cam.enabled = false;
                Debug.Log($"PLayerMove: 禁用多余摄像机 {cam.name}");
            }
        }
        if (playerCam != null)
        {
            playerCam.enabled = true;
        }
    }

    System.Collections.IEnumerator TeleportToSpawnPoint()
    {
        // 等一帧，确保新场景中所有 Awake/Start 都已执行
        yield return null;

        string targetName = DisE.nextSpawnPoint;
        GameObject spawnPoint = GameObject.Find(targetName);
        Vector3 spawnPos;

        if (spawnPoint != null)
        {
            spawnPos = spawnPoint.transform.position;
            transform.rotation = spawnPoint.transform.rotation;
            Debug.Log($"PLayerMove: 找到出生点 {targetName} 位置 {spawnPos}");
        }
        else
        {
            Debug.LogWarning($"PLayerMove: 找不到出生点 '{targetName}'，使用备选");
            SpawnPoint anySpawn = FindObjectOfType<SpawnPoint>();
            if (anySpawn != null)
            {
                spawnPos = anySpawn.transform.position;
                transform.rotation = anySpawn.transform.rotation;
                Debug.Log($"PLayerMove: 使用备选出生点 {anySpawn.spawnName}");
            }
            else
            {
                DisE.hasSpawnPoint = false;
                yield break;
            }
        }

        // 从出生点位置向下打射线找地面（不从上方打，避免打到天花板）
        if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 10f))
        {
            spawnPos.y = hit.point.y + 0.1f;
            Debug.Log($"PLayerMove: 贴地修正后位置 {spawnPos}");
        }
        else
        {
            // 如果向下打不到，试试从出生点稍下方开始打
            Debug.Log($"PLayerMove: 向下未找到地面，使用出生点原始位置 {spawnPos}");
        }

        controller.enabled = false;
        transform.position = spawnPos;
        controller.enabled = true;

        // 重置视角和鼠标锁定
        rotationX = 0f;
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        DisE.hasSpawnPoint = false;

        Debug.Log($"PLayerMove: 传送完成！最终位置 {transform.position}");
    }

    void TryFindCamera()
    {
        if (playerCamera != null) return;
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera != null && !cameraInitialized)
        {
            defaultCameraY = playerCamera.transform.localPosition.y;
            defaultCameraX = playerCamera.transform.localPosition.x;
            cameraInitialized = true;
        }
    }

    void Update()
    {
        TryFindCamera();
        if (playerCamera == null) return;
        RotateCamera();
        MovePlayer();
        ApplyGravity();
        CameraBob();
    }

    private void RotateCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 水平旋转：旋转玩家本体（左右看）
        transform.Rotate(0f, mouseX, 0f);

        // 垂直旋转：旋转摄像机（上下看）
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -upDownLimit, upDownLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    private void MovePlayer()
    {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S

        bool running = Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = running ? runSpeed : walkSpeed;

        Vector3 moveDir = transform.right * h + transform.forward * v;
        if (moveDir.magnitude > 1f) moveDir.Normalize();
        Vector3 targetVelocity = moveDir * targetSpeed;

        float accel = (moveDir.magnitude > 0.1f) ? acceleration : deceleration;
        currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, accel * Time.deltaTime);

        controller.Move(currentMoveVelocity * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -4f; // 贴地更紧，防止浮空
        }

        if (controller.isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void CameraBob()
    {
        float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
        bool running = Input.GetKey(KeyCode.LeftShift);
        float freq = running ? runBobFrequency : walkBobFrequency;
        float amount = running ? runBobAmount : walkBobAmount;

        if (controller.isGrounded && horizontalSpeed > 0.5f)
        {
            bobTimer += Time.deltaTime * freq;
            float bobY = Mathf.Sin(bobTimer) * amount;
            float bobX = Mathf.Cos(bobTimer * 0.5f) * amount * 0.6f;

            // 走路时视角微微左右倾斜，更像人
            float tiltZ = Mathf.Cos(bobTimer) * tiltAmount * (running ? 1.5f : 1f);

            Vector3 pos = playerCamera.transform.localPosition;
            pos.y = defaultCameraY + bobY;
            pos.x = defaultCameraX + bobX;
            playerCamera.transform.localPosition = pos;

            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, tiltZ);
        }
        else
        {
            bobTimer = 0f;
            Vector3 pos = playerCamera.transform.localPosition;
            pos.y = Mathf.Lerp(pos.y, defaultCameraY, 8f * Time.deltaTime);
            pos.x = Mathf.Lerp(pos.x, defaultCameraX, 8f * Time.deltaTime);
            playerCamera.transform.localPosition = pos;

            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, Mathf.Lerp(playerCamera.transform.localEulerAngles.z, 0f, 8f * Time.deltaTime));
        }
    }

    // 确保碰撞正常工作
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // CharacterController 自带碰撞，这里可以处理推物体等逻辑
    }
}
