using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PLayerMove : MonoBehaviour
{
    [Header("移动设置")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;
    public float acceleration = 10f;
    public float deceleration = 8f;
    public float airControl = 0.3f;           // 空中操控能力

    [Header("视角设置")]
    public float mouseSensitivity = 800f;
    public float upDownLimit = 80f;

    [Header("FOV 设置")]
    public float normalFOV = 60f;
    public float runFOV = 75f;
    public float fovLerpSpeed = 6f;

    [Header("走路晃动")]
    public float walkBobFrequency = 12f;
    public float runBobFrequency = 16f;
    public float walkBobAmount = 0.06f;
    public float runBobAmount = 0.10f;
    public float tiltAmount = 2f;

    [Header("脚步声")]
    public float stepInterval = 0.5f;

    [Header("跳跃设置")]
    public float jumpCooldown = 0.2f;
    public float landPauseTime = 0.15f;       // 着陆后短暂停顿
    public float jumpFOV = 65f;
    public float jumpBobAmount = 0.03f;       // 起跳相机抖动

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
    private Animator animator;
    private bool wasGrounded = true;
    private float lastJumpTime = -1f;
    private float landPauseTimer = 0f;
    private bool isJumping = false;
    private bool jumpQueued = false;

    void Start()
    {
        DontDestroyOnLoad(gameObject);

        controller = GetComponent<CharacterController>();
        TryFindCamera();
        TryFindAnimator();

        SceneManager.sceneLoaded += OnSceneLoaded;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RemoveExtraAudioListeners();
        DisableExtraCameras();

        if (DisE.hasSpawnPoint)
        {
            StartCoroutine(TeleportToSpawnPoint());
        }

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
            }
        }
        if (playerCam != null)
        {
            playerCam.enabled = true;
        }
    }

    System.Collections.IEnumerator TeleportToSpawnPoint()
    {
        yield return null;

        string targetName = DisE.nextSpawnPoint;
        GameObject spawnPoint = GameObject.Find(targetName);
        Vector3 spawnPos;

        if (spawnPoint != null)
        {
            spawnPos = spawnPoint.transform.position;
            transform.rotation = spawnPoint.transform.rotation;
        }
        else
        {
            SpawnPoint anySpawn = FindObjectOfType<SpawnPoint>();
            if (anySpawn != null)
            {
                spawnPos = anySpawn.transform.position;
                transform.rotation = anySpawn.transform.rotation;
            }
            else
            {
                DisE.hasSpawnPoint = false;
                yield break;
            }
        }

        if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 10f))
        {
            spawnPos.y = hit.point.y + 0.1f;
        }

        controller.enabled = false;
        transform.position = spawnPos;
        controller.enabled = true;

        rotationX = 0f;
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        DisE.hasSpawnPoint = false;
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

    void TryFindAnimator()
    {
        if (animator != null) return;
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        TryFindCamera();
        TryFindAnimator();
        if (playerCamera == null) return;

        RotateCamera();
        MovePlayer();
        ApplyGravity();
        CameraBob();
        UpdateFOV();
        UpdateAnimator();
        HandleJumpInput();
    }

    void UpdateAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;

        animator.SetFloat("Speed", horizontalSpeed);
        animator.SetBool("IsGrounded", controller.isGrounded);

        // 跳跃触发：刚离地且是主动跳跃（不是走路掉下边缘）
        if (wasGrounded && !controller.isGrounded && isJumping)
        {
            animator.SetTrigger("Jump");
        }
        // 着陆触发
        if (!wasGrounded && controller.isGrounded)
        {
            animator.SetTrigger("Land");
            isJumping = false;
        }
        wasGrounded = controller.isGrounded;
    }

    void HandleJumpInput()
    {
        // 检测跳跃输入（缓冲输入：在着陆前一小段时间按也能跳）
        if (Input.GetButtonDown("Jump"))
        {
            if (controller.isGrounded && Time.time - lastJumpTime > jumpCooldown)
            {
                // 立即跳跃
                DoJump();
            }
            else
            {
                // 缓冲：记录按下，着陆时自动跳
                jumpQueued = true;
            }
        }

        // 着陆后检查缓冲跳跃
        if (jumpQueued && controller.isGrounded && Time.time - lastJumpTime > jumpCooldown)
        {
            DoJump();
            jumpQueued = false;
        }

        // 着陆暂停计时
        if (landPauseTimer > 0f)
        {
            landPauseTimer -= Time.deltaTime;
        }
    }

    void DoJump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        isJumping = true;
        lastJumpTime = Time.time;
        landPauseTimer = 0f;

        // 跳跃相机微抖动
        if (playerCamera != null)
        {
            Vector3 pos = playerCamera.transform.localPosition;
            pos.y -= jumpBobAmount;
            playerCamera.transform.localPosition = pos;
        }
    }

    void UpdateFOV()
    {
        if (playerCamera == null) return;

        float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && horizontalSpeed > walkSpeed * 0.8f;
        
        float targetFOV;
        if (!controller.isGrounded && velocity.y > 0f)
            targetFOV = jumpFOV;
        else if (isRunning)
            targetFOV = runFOV;
        else
            targetFOV = normalFOV;

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovLerpSpeed * Time.deltaTime);
    }

    private void RotateCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(0f, mouseX, 0f);

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -upDownLimit, upDownLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    private void MovePlayer()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        bool running = Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = running ? runSpeed : walkSpeed;

        Vector3 moveDir = transform.right * h + transform.forward * v;
        if (moveDir.magnitude > 1f) moveDir.Normalize();
        Vector3 targetVelocity = moveDir * targetSpeed;

        // 空中操控减弱
        float accel;
        if (!controller.isGrounded)
        {
            accel = acceleration * airControl;
        }
        else
        {
            accel = (moveDir.magnitude > 0.1f) ? acceleration : deceleration;
        }

        currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, accel * Time.deltaTime);
        controller.Move(currentMoveVelocity * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -4f;
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

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
    }
}
