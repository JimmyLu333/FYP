using UnityEngine;

public class AudioRecordManager : MonoBehaviour
{
    private string deviceName;
    private AudioClip recordingClip;
    private int samplingRate = 16000;

    public bool HasMicrophone { get; private set; }

    void Start()
    {
        RefreshMicrophone();
    }

    private void RefreshMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            deviceName = Microphone.devices[0];
            HasMicrophone = true;
            Debug.Log($"【系统提示】成功联通麦克风设备: {deviceName}");
        }
        else
        {
            deviceName = null;
            HasMicrophone = false;
            Debug.LogWarning("【系统提示】未检测到麦克风，语音输入将不可用，但键盘输入仍可使用。");
        }
    }

    public void StartRecording()
    {
        RefreshMicrophone();

        if (!HasMicrophone)
        {
            Debug.LogWarning("【录音提示】没有麦克风，跳过录音。");
            return;
        }

        recordingClip = Microphone.Start(deviceName, false, 20, samplingRate);
    }

    public AudioClip StopRecording()
    {
        if (!HasMicrophone || string.IsNullOrEmpty(deviceName))
        {
            Debug.LogWarning("【录音提示】没有麦克风，返回空音频。");
            return null;
        }

        int lastPos = Microphone.GetPosition(deviceName);
        Microphone.End(deviceName);

        if (recordingClip == null || lastPos <= 0)
            return null;

        AudioClip trimmedClip = AudioClip.Create(
            "TrimmedSpeech",
            lastPos,
            recordingClip.channels,
            samplingRate,
            false
        );

        float[] tempSamples = new float[lastPos * recordingClip.channels];
        recordingClip.GetData(tempSamples, 0);
        trimmedClip.SetData(tempSamples, 0);

        Debug.Log($"【裁剪系统】已保留有效语音: {trimmedClip.samples / (float)samplingRate:F2}秒");
        return trimmedClip;
    }
}