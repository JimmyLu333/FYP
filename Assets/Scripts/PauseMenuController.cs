using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PauseMenuController : MonoBehaviour
{
    [Header("Hierarchy Panels")]
    public GameObject pauseScene;
    public GameObject pauseSceneUi;
    public GameObject exitAskingPanel;
    public GameObject settingsPanel;

    [Header("Global Volume Settings")]
    public Volume globalVolume;
    public float pausedFocalLength = 144f;
    public float normalFocalLength = 1f;

    [Header("4 Main Menu Buttons")]
    public Button[] mainButtons;
    public GameObject[] mainGlowBackgrounds;

    [Header("Exit Asking Sub-Buttons (Only for Glow)")]
    public Button[] exitSubButtons;
    public GameObject[] exitSubGlowBackgrounds;

    private bool isPaused = false;

    void Start()
    {
        if (pauseScene != null) pauseScene.SetActive(false);
        if (pauseSceneUi != null) pauseSceneUi.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (exitAskingPanel != null) exitAskingPanel.SetActive(false);

        SetVolumeBlur(false);

        SetupMainMenuGlowEvents();
        SetupExitSubMenuGlowEvents();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            HandleEscape();
        }
    }

    private void HandleEscape()
    {
        // Exit Asking 界面按 ESC：回到暂停主菜单
        if (exitAskingPanel != null && exitAskingPanel.activeSelf)
        {
            exitAskingPanel.SetActive(false);

            if (pauseSceneUi != null)
                pauseSceneUi.SetActive(true);

            ToggleMainButtons(true);
            return;
        }

        // Settings 界面按 ESC：回到暂停主菜单
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            OnClickCloseSettings();
            return;
        }

        // 暂停主界面按 ESC：回到游戏
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void TogglePauseMenu()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;

        if (pauseScene != null) pauseScene.SetActive(true);
        if (pauseSceneUi != null) pauseSceneUi.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (exitAskingPanel != null) exitAskingPanel.SetActive(false);

        ToggleMainButtons(true);
        SetVolumeBlur(true);

        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (pauseScene != null) pauseScene.SetActive(false);
        if (pauseSceneUi != null) pauseSceneUi.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (exitAskingPanel != null) exitAskingPanel.SetActive(false);

        ToggleMainButtons(true);
        SetVolumeBlur(false);

        Time.timeScale = 1f;
    }

    private void ToggleMainButtons(bool show)
    {
        foreach (var btn in mainButtons)
        {
            if (btn != null)
                btn.gameObject.SetActive(show);
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

    public void OnClickMainMenuExit()
    {
        if (exitAskingPanel != null)
        {
            exitAskingPanel.SetActive(true);

            if (pauseSceneUi != null)
                pauseSceneUi.SetActive(false);

            ToggleMainButtons(false);
        }
    }

    public void OnClickMainMenuSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);

            if (pauseSceneUi != null)
                pauseSceneUi.SetActive(false);

            ToggleMainButtons(false);
        }
    }

    public void OnClickCloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pauseSceneUi != null)
            pauseSceneUi.SetActive(true);

        ToggleMainButtons(true);
    }

    public void OnClickQuitToDesktop()
    {
        Debug.Log("【执行】彻底退出到桌面...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClickCancelAndResumeGame()
    {
        Debug.Log("【执行】点击了返回游戏，正在关闭暂停面板...");

        ToggleMainButtons(true);
        ResumeGame();
    }

    public void OnClickReturnToMainMenu()
    {
        Debug.Log("【执行】正在准备返回主菜单页面...");

        Time.timeScale = 1f;
        SceneManager.LoadScene("main menu");
    }

    private void SetupMainMenuGlowEvents()
    {
        for (int i = 0; i < mainButtons.Length; i++)
        {
            int index = i;

            if (mainButtons[index] == null)
                continue;

            EventTrigger trigger = mainButtons[index].gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = mainButtons[index].gameObject.AddComponent<EventTrigger>();

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

            if (exitSubButtons[index] == null)
                continue;

            EventTrigger trigger = exitSubButtons[index].gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = exitSubButtons[index].gameObject.AddComponent<EventTrigger>();

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