using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light targetLight;

    [Header("亮度范围")]
    public float minIntensity = 3f;
    public float maxIntensity = 7f;

    [Header("闪烁速度")]
    public float flickerSpeed = 2f;

    [Header("随机抖动")]
    public float randomStrength = 0.2f;

    [Header("左右摆动")]
    public float rotationAmount = 20f; // 左右摆动角度
    public float rotationSpeed = 0.5f; // 摆动速度

    private Vector3 startRotation;

    private void Start()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        startRotation = transform.eulerAngles;
    }

    private void Update()
    {
        // 灯光闪烁
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

        intensity += Random.Range(-randomStrength, randomStrength);
        intensity = Mathf.Clamp(intensity, minIntensity, maxIntensity);

        targetLight.intensity = intensity;

        // 左右摆动（绕Y轴）
        float yRotation = Mathf.Sin(Time.time * rotationSpeed) * rotationAmount;

        transform.eulerAngles = startRotation + new Vector3(0f, yRotation, 0f);
    }
}