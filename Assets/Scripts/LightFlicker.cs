using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light targetLight;

    [Header("亮度范围")]
    public float minIntensity = 3.2f;
    public float maxIntensity = 5.2f;

    [Header("闪烁速度")]
    public float flickerSpeed = 2.5f;

    [Header("随机抖动")]
    public float randomStrength = 0.25f;

    [Header("左右移动")]
    public float moveAmount = 1.2f;  // 左右移动幅度
    public float moveSpeed = 0.6f;   // 移动速度

    private Vector3 startPosition;

    private void Start()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        startPosition = transform.position;
    }

    private void Update()
    {
        // 灯光闪烁
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

        intensity += Random.Range(-randomStrength, randomStrength);
        intensity = Mathf.Clamp(intensity, minIntensity, maxIntensity);

        targetLight.intensity = intensity;

        // 左右移动灯的位置
        float xOffset = Mathf.Sin(Time.time * moveSpeed) * moveAmount;

        transform.position = startPosition + new Vector3(xOffset, 0f, 0f);
    }
}