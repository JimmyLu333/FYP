using UnityEngine;

public class 移动 : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 7f;
    public float upDownSpeed = 5f;
    public Camera cam;

    [Header("视角设置")]
    public float mouseSensitivity = 100f;
    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 鼠标视角旋转（相机自己左右+上下转头）
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -89f, 89f);

        cam.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0);

        // WASD 前后左右（跟着相机视角）
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = cam.transform.right * h + cam.transform.forward * v;
        move.Normalize();
        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);

        // 重点：上下升降 【空格上升  Ctrl下降】
        if (Input.GetKey(KeyCode.Space))
        {
            transform.Translate(Vector3.up * upDownSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            transform.Translate(Vector3.down * upDownSpeed * Time.deltaTime, Space.World);
        }
    }
}