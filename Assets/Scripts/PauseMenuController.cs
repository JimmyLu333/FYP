using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PauseMenuController : MonoBehaviour
{
    [Header("Hierarchy Panels")]
    public GameObject pauseScene;          // 对应你的 pause_scene 总父物体
    public GameObject pauseSceneUi;        // 对应你的 Pause_scene_ui 面板
    public GameObject exitAskingPanel;     // 对应你的 Exit_asking 二次确认面板
    public GameObject settingsPanel;       // 对应你的 Options Panel 面板

    [Header("Global Volume Settings")]
    public Volume globalVolume; 
    public float pausedFocalLength = 144f;  // 暂停时希望达到的模糊数值
    public float normalFocalLength = 1f;    // 游戏平时的清晰数值

    [Header("4 Main Menu Buttons")]
    [Tooltip("依次拖入 SAVE, LOAD, SETTINGS, EXIT 四个核心按钮")]
    public Button[] mainButtons;           
    [Tooltip("依次拖入上述 4 个按钮各自底部的渐变白底 Image 物体")]
    public GameObject[] mainGlowBackgrounds;  

    [Header("Exit Asking Sub-Buttons (Only for Glow)")]
    [Tooltip("把弹窗里的 退出桌面、返回游戏、返回菜单 3个按钮拖进来（仅用于悬停高亮）")]
    public Button[] exitSubButtons;        
    [Tooltip("依次拖入上述 3 个子按钮各自底部的渐变白底 Image 物体")]
    public GameObject[] exitSubGlowBackgrounds; 

    private bool isPaused = false;

    void Start()
    {
        // 游戏启动初始化：确保所有暂停相关的 UI 默认都是隐藏的
        if (pauseScene != null) pauseScene.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (exitAskingPanel != null) exitAskingPanel.SetActive(false);

        // 启动时强制将焦距初始化为清晰状态 (1)
        SetVolumeBlur(false);

        // 动态绑定主菜单和弹窗按钮的【鼠标悬停高亮】事件
        SetupMainMenuGlowEvents();
        SetupExitSubMenuGlowEvents();

        // 🚨 注意：为了防止错乱，点击跳转事件这次改在 Unity Inspector 里手动绑定！
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void TogglePauseMenu()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    // ==========================================
    // --- 核心状态控制 ---
    // ==========================================

    public void PauseGame()
    {
        isPaused = true;
        if (pauseScene != null) pauseScene.SetActive(true);
        if (pauseSceneUi != null) pauseSceneUi.SetActive(true);
        if (exitAskingPanel != null) exitAskingPanel.SetActive(false); 
        
        ToggleMainButtons(true);
        SetVolumeBlur(true);

        Time.timeScale = 0f; // 冻结游戏逻辑
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseScene != null) pauseScene.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (exitAskingPanel != null) exitAskingPanel.SetActive(false);
        
        SetVolumeBlur(false);

        Time.timeScale = 1f; // 恢复正常游戏
    }

    private void ToggleMainButtons(bool show)
    {
        foreach (var btn in mainButtons)
        {
            if (btn != null) btn.gameObject.SetActive(show);
        }
    }

    private void SetVolumeBlur(bool blurOn)
    {
        if (globalVolume != null && globalVolume.sharedProfile != null) 
        {
            if (globalVolume.sharedProfile.TryGet<DepthOfField>(out var dof))
            {
                dof.focalLength.overrideState = true;
                dof.focalLength.value = blurOn ? pausedFocalLength : normalFocalLength;
                globalVolume.weight = 0.999f; 
                globalVolume.weight = 1.0f;
            }
        }
    }

    // ==========================================
    // --- 🚨 核心修复：明明白白的功能函数（供外部独立绑定） ---
    // ==========================================

    // 主菜单点击 EXIT 按钮
    public void OnClickMainMenuExit()
    {
        if (exitAskingPanel != null)
        {
            exitAskingPanel.SetActive(true); 
            ToggleMainButtons(false); // 隐藏4个主按钮，保留暗色背景
        }
    }

    // 主菜单点击 SETTINGS 按钮
    public void OnClickMainMenuSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            ToggleMainButtons(false); 
        }
    }

    // 设置面板里的“返回/关闭”按钮
    public void OnClickCloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        ToggleMainButtons(true); 
    }

    // 弹窗选项一：彻底退出游戏到桌面
    public void OnClickQuitToDesktop()
    {
        Debug.Log("【执行】彻底退出到桌面...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 在编辑器里测试时停止运行
        #else
        Application.Quit(); // 打包后真正退出游戏
        #endif
    }

    // 弹窗选项二：【核心修好点】点击“返回游戏”，关闭暂停回复正常游戏
    public void OnClickCancelAndResumeGame()
    {
        Debug.Log("【执行】点击了返回游戏，正在关闭暂停面板...");
        ToggleMainButtons(true); // 把隐藏的主按钮预先找回来
        ResumeGame();            // 关闭整个 pauseScene，恢复画面清晰和时间
    }

    // 弹窗选项三：返回主菜单
    public void OnClickReturnToMainMenu()
    {
        Debug.Log("【执行】正在准备返回主菜单页面...");
        Time.timeScale = 1f; // 加载新场景前恢复时间轴
        // SceneManager.LoadScene("MainMenu"); // 如果有主菜单场景请取消注释
    }

    // ==========================================
    // --- 纯鼠标悬停高亮事件绑定 ---
    // ==========================================
    private void SetupMainMenuGlowEvents()
    {
        for (int i = 0; i < mainButtons.Length; i++)
        {
            int index = i; 
            EventTrigger trigger = mainButtons[index].gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = mainButtons[index].gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entryEnter.callback.AddListener((data) => { ToggleGlow(mainGlowBackgrounds, index, true); });
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entryExit.callback.AddListener((data) => { ToggleGlow(mainGlowBackgrounds, index, false); });
            trigger.triggers.Add(entryExit);
        }
    }

    private void SetupExitSubMenuGlowEvents()
    {
        for (int i = 0; i < exitSubButtons.Length; i++)
        {
            int index = i;
            EventTrigger trigger = exitSubButtons[index].gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = exitSubButtons[index].gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entryEnter.callback.AddListener((data) => { ToggleGlow(exitSubGlowBackgrounds, index, true); });
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entryExit.callback.AddListener((data) => { ToggleGlow(exitSubGlowBackgrounds, index, false); });
            trigger.triggers.Add(entryExit);
        }
    }

    private void ToggleGlow(GameObject[] glowArray, int index, bool state)
    {
        if (glowArray != null && index < glowArray.Length && glowArray[index] != null)
        {
            glowArray[index].SetActive(state);
        }
    }
}