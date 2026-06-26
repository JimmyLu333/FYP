using UnityEngine;

public class AudioRecordManager : MonoBehaviour
{
    private string deviceName;
    private AudioClip recordingClip;
    private int samplingRate = 16000; 

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            deviceName = Microphone.devices[0];
            Debug.Log($"【系统提示】成功联通麦克风设备: {deviceName}");
        }
        else
        {
            Debug.LogError("【系统错误】未检测到任何麦克风输入设备！");
        }
    }

    public void StartRecording()
    {
        if (string.IsNullOrEmpty(deviceName)) return;
        // 开启录音，最大长度20秒
        recordingClip = Microphone.Start(deviceName, false, 20, samplingRate);
    }

    // 🎯 核心升级：结束录音时，只裁剪出真正说话的那一段音频
    public AudioClip StopRecording()
    {
        if (string.IsNullOrEmpty(deviceName)) return null;

        // 1. 获取玩家松开按钮时，麦克风实际录到了哪一个采样点
        int lastPos = Microphone.GetPosition(deviceName);
        Microphone.End(deviceName);

        if (lastPos <= 0) return null;

        // 2. 建立一个“刚刚好长”的全新全干净的音频容器
        AudioClip trimmedClip = AudioClip.Create("TrimmedSpeech", lastPos, recordingClip.channels, samplingRate, false);
        
        // 3. 把原音频里的有效段落复制出来
        float[] tempSamples = new float[lastPos * recordingClip.channels];
        recordingClip.GetData(tempSamples, 0);
        trimmedClip.SetData(tempSamples, 0);

        Debug.Log($"【裁剪系统】已自动切除空白尾巴，保留有效语音: {trimmedClip.samples / (float)samplingRate:F2}秒");
        return trimmedClip; // 返回这个剪裁好的完美音频
    }
}