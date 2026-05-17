using UnityEngine;

public class PLayerMove : MonoBehaviour
{
    // 玩家移动速度（可在编辑器中调整）
    public float moveSpeed = 5f;
    // 镜头旋转速度（用于鼠标移动灵敏度）
    public float lookSpeed = 2f;
    // 摄像机上下旋转的限制（防止过度仰望或俯视）
    public float upDownLimit = 90f;

    private Camera playerCamera;          // 存储玩家的摄像机引用
    private float rotationX = 0f;        // 当前的摄像机上下旋转角度

    void Start()
    {
        // 获取场景中主摄像机的引用
        playerCamera = Camera.main;
        // 锁定光标到游戏窗口的中心，并隐藏光标
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 在每一帧更新中，调用移动和旋转的方法
        MovePlayer();
        RotateCamera();
    }

    private void MovePlayer()
    {
        // 获取水平输入（A/D或箭头左/右）
        float moveHorizontal = Input.GetAxis("Horizontal");
        // 获取垂直输入（W/S或箭头上/下）
        float moveVertical = Input.GetAxis("Vertical");

        // 创建一个新的向量用于计算移动方向
        // 将输入归一化以避免对角线移动速度更快的问题
        Vector3 moveDirection = new Vector3(moveHorizontal, 0, moveVertical).normalized;
        // 将局部坐标方向转换为世界坐标方向
        moveDirection = transform.TransformDirection(moveDirection);

        // 根据计算出的方向和速度更新玩家的位置
        // Time.deltaTime 确保每帧移动保持平滑且与帧率无关
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    private void RotateCamera()
    {
        // 获取鼠标的水平移动量并应用旋转速度
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed * Time.deltaTime;
        // 获取鼠标的垂直移动量并应用旋转速度
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed * Time.deltaTime;

        // 旋转玩家对象（Y轴旋转）
        transform.Rotate(0, mouseX, 0);
        //float mouseY = -Input.GetAxisRaw("Mouse Y") * LookSpeed;


        //rotationX = rotationX + mouseY;
        //rotationX += mouseY;

        // 更新摄像机的上下旋转角度，向下滚动为负，向上滚动为正
        //rotationX = rotationX - mouseY;
        rotationX -= mouseY;

        // 限制摄像机的上下旋转角度，防止超出设定范围
        rotationX = Mathf.Clamp(rotationX, -upDownLimit, upDownLimit);
        // 设置摄像机本地旋转（仅Y轴，不影响Y轴）
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }
}
