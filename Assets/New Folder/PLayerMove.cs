using UnityEngine;

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
    public float mouseSensitivity = 400f;   // 大幅提高灵敏度
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
    private float defaultCameraY;
    private float defaultCameraX;
    private float stepTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        defaultCameraY = playerCamera.transform.localPosition.y;
        defaultCameraX = playerCamera.transform.localPosition.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
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
