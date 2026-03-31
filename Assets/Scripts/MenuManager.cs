using UnityEngine;

public class MenuManager : MonoBehaviour
{
    // 在 Inspector 里把这两个面板拖进去
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;

    // 点击 Options 按钮时调用
    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false); // 隐藏主菜单
        optionsPanel.SetActive(true);    // 显示设置界面
    }

    // 点击返回（Back）按钮时调用
    public void CloseOptions()
    {
        mainMenuPanel.SetActive(true);  // 显示主菜单
        optionsPanel.SetActive(false);  // 隐藏设置界面
    }
}