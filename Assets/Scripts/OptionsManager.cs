using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;

public class OptionsManager : MonoBehaviour
{
    [Header("--- 面板管理 ---")]
    public List<GameObject> allPanels;
    public List<Animator> leftButtonAnimators;
    
    [Header("--- UI 状态切换 ---")]
    public GameObject mainMenuRoot; // 拖入主菜单的根节点（包含 Start/Options/Exit 按钮的那个）
    public GameObject saveNotifyUI; // 保存成功的提示文字

    [Header("--- 音频设置 (0-100 Slider) ---")]
    public Slider masterSlider;
    public TextMeshProUGUI masterValueText;
    public Slider sfxSlider;
    public TextMeshProUGUI sfxValueText;
    public Slider voiceSlider;
    public TextMeshProUGUI voiceValueText;

    [Header("--- 语言/画面显示 ---")]
    public TextMeshProUGUI languageText; 
    public TextMeshProUGUI windowModeText;
    public TextMeshProUGUI resolutionText;

    private List<string> languages = new List<string> { "Simplified Chinese", "Traditional Chinese", "English" };
    private List<string> windowModes = new List<string> { "Full Screen", "Windowed" };
    private List<string> resolutions = new List<string> { "1920 * 1080", "1600 * 900", "1280 * 720" };
    private int langIndex, windowModeIndex, resIndex;

    private void OnEnable()
    {
        ShowPanel(0);
        if (saveNotifyUI != null) saveNotifyUI.SetActive(false);
    }

    private void Start()
    {
        UpdateLanguageUI();
        UpdateWindowModeUI();
        UpdateResolutionUI();
        // 初始化音量显示
        InitVolume(masterSlider, masterValueText, "MasterVol");
        InitVolume(sfxSlider, sfxValueText, "SFXVol");
        InitVolume(voiceSlider, voiceValueText, "VoiceVol");
    }

    private void Update()
    {
        // 按下 ESC 键执行返回逻辑
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackToMainMenu();
        }

        // 按下 Enter 键执行保存逻辑
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SaveSettings();
        }
    }

    // ================= 核心逻辑：丝滑返回主菜单 =================

    public void SaveSettings()
    {
        PlayerPrefs.Save();
        if (saveNotifyUI != null)
        {
            saveNotifyUI.SetActive(true);
            CancelInvoke("HideNotify");
            Invoke("HideNotify", 1.5f);
        }
    }

    private void HideNotify() { if (saveNotifyUI != null) saveNotifyUI.SetActive(false); }

    public void BackToMainMenu()
    {
        // 1. 先存一下
        SaveSettings(); 

        // 2. 显示主菜单的 UI 内容（Start, Options, Exit 那些）
        if (mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(true);
        }

        // 3. 关闭 Options 面板自身
        this.gameObject.SetActive(false);
        
        Debug.Log("已切回主菜单视图");
    }

    // ================= 基础功能 (保持不变) =================
    public void ShowPanel(int panelIndex)
    {
        for (int i = 0; i < allPanels.Count; i++)
            if (allPanels[i] != null) allPanels[i].SetActive(i == panelIndex);

        for (int i = 0; i < leftButtonAnimators.Count; i++)
            if (leftButtonAnimators[i] != null) leftButtonAnimators[i].SetBool("IsActive", i == panelIndex);
    }

    // 音量与切换逻辑... (此处省略，保持你之前运行正常的代码即可)
    public void OnMasterVolumeChanged(float v) { AudioListener.volume = v / 100f; if (masterValueText != null) masterValueText.text = Mathf.RoundToInt(v) + "%"; PlayerPrefs.SetFloat("MasterVol", v); }
    public void OnSFXVolumeChanged(float v) { if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(v) + "%"; PlayerPrefs.SetFloat("SFXVol", v); }
    public void OnVoiceVolumeChanged(float v) { if (voiceValueText != null) voiceValueText.text = Mathf.RoundToInt(v) + "%"; PlayerPrefs.SetFloat("VoiceVol", v); }
    public void OnLanguageLeft() { langIndex = (langIndex - 1 + languages.Count) % languages.Count; UpdateLanguageUI(); }
    public void OnLanguageRight() { langIndex = (langIndex + 1) % languages.Count; UpdateLanguageUI(); }
    void UpdateLanguageUI() { if (languageText != null) languageText.text = languages[langIndex]; }
    public void OnWindowModeLeft() { windowModeIndex = (windowModeIndex - 1 + windowModes.Count) % windowModes.Count; UpdateWindowModeUI(); }
    public void OnWindowModeRight() { windowModeIndex = (windowModeIndex + 1) % windowModes.Count; UpdateWindowModeUI(); }
    void UpdateWindowModeUI() { if (windowModeText != null) windowModeText.text = windowModes[windowModeIndex]; Screen.fullScreenMode = (windowModes[windowModeIndex] == "Full Screen") ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed; }
    public void OnResolutionLeft() { resIndex = (resIndex - 1 + resolutions.Count) % resolutions.Count; UpdateResolutionUI(); }
    public void OnResolutionRight() { resIndex = (resIndex + 1) % resolutions.Count; UpdateResolutionUI(); }
    void UpdateResolutionUI() { if (resolutionText != null) resolutionText.text = resolutions[resIndex]; string[] parts = resolutions[resIndex].Split('*'); if(parts.Length == 2) Screen.SetResolution(int.Parse(parts[0].Trim()), int.Parse(parts[1].Trim()), Screen.fullScreen); }
    private void InitVolume(Slider slider, TextMeshProUGUI text, string key) { if (slider != null) { float v = 50f; slider.value = v; if (text != null) text.text = "50%"; } }
}