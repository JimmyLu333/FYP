using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioManager : MonoBehaviour
{
    [Header("UI 引用")]
    public Slider masterSlider;
    public TextMeshProUGUI volumeValueText;

    void Start()
    {
        // 初始化：从系统读取当前音量并同步给 Slider
        float currentVol = AudioListener.volume;
        masterSlider.value = currentVol;
        UpdateVolumeText(currentVol);
    }

    // 在 Slider 的 On Value Changed 事件中绑定这个方法
    public void OnMasterVolumeChanged(float value)
    {
        // 1. 实际修改游戏的主音量 (范围是 0 到 1)
        AudioListener.volume = value;

        // 2. 更新百分比文字显示
        UpdateVolumeText(value);
    }

    void UpdateVolumeText(float value)
    {
        // 将 0-1 的小数转为 0-100 的整数
        int percentage = Mathf.RoundToInt(value * 100);
        volumeValueText.text = percentage.ToString() + "%";
    }
}