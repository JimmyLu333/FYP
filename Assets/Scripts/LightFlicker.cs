using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light targetLight;

    [Header("亮度范围")]
    public float minIntensity = 15f;
    public float maxIntensity = 35f;

    [Header("闪烁速度")]
    public float flickerSpeed = 2.5f;

    [Header("随机抖动")]
    public float randomStrength = 2f;

    [Header("左右移动")]
    public float moveAmount = 3f;
    public float moveSpeed = 1f;

    private Vector3 startPosition;

    private void Start()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        startPosition = transform.position;
    }

    private void Update()
    {
        if (targetLight == null) return;

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        intensity += Random.Range(-randomStrength, randomStrength);
        intensity = Mathf.Clamp(intensity, minIntensity, maxIntensity);

        targetLight.intensity = intensity;

        float xOffset = Mathf.Sin(Time.time * moveSpeed) * moveAmount;
        transform.position = startPosition + new Vector3(xOffset, 0f, 0f);
    }
}