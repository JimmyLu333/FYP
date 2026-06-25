using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BrowserManager : MonoBehaviour
{
    [Header("Top Bar")]
    public TMP_InputField urlInputField; 

    [Header("Panels")]
    public GameObject panelSearch;   
    public GameObject panelResult;   
    public GameObject panel404;      

    public ScrollRect resultScrollRect; // 👈 加上这行，用来绑定图2的滚动组件

    [Header("Search Inputs")]
    public TMP_InputField searchInputField; 
    public Button searchButton; // 👈 1. 在这里新增一个搜索按钮的引用

    private string[] validCharacterIDs = { "1001", "1002", "刘福来", "ChenMo" }; 

    void Start()
    {
        ShowSearchPage();

        // 👈 2. 核心：在游戏开始时，实时监听输入框的文字改变事件
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnInputValueChanged);
        }
        
        // 👈 3. 初始化：刚开局输入框是空的，调用一次确保按钮默认按不动
        OnInputValueChanged(searchInputField.text);
    }

    // 👈 4. 新增：当输入框内容发生变化时，会自动触发这个函数
    void OnInputValueChanged(string currentText)
    {
        if (searchButton != null)
        {
            // 去除空格后，如果输入框不为空，则按钮可以交互（Interactable = true），同时视觉自动切到 Normal 亮蓝色
            // 如果为空，则按钮无法交互（Interactable = false），视觉自动切到 Disabled 灰色
            searchButton.interactable = !string.IsNullOrWhiteSpace(currentText);
        }
    }

    public void OnSearchButtonClick()
    {
        string input = searchInputField.text.Trim();
        if (string.IsNullOrEmpty(input)) return;

        bool isCharacterFound = false;
        foreach (string id in validCharacterIDs)
        {
            if (input == id)
            {
                isCharacterFound = true;
                break;
            }
        }

        if (isCharacterFound)
        {
            ShowResultPage(input);
        }
        else
        {
            Show404Page();
        }
    }

    public void ShowSearchPage()
    {
        panelSearch.SetActive(true);
        panelResult.SetActive(false);
        panel404.SetActive(false);
        urlInputField.text = "http://Mogle.mainpage.com";
        searchInputField.text = ""; 
    }

    void ShowResultPage(string characterID)
    {
        panelSearch.SetActive(false);
        panelResult.SetActive(true);
        panel404.SetActive(false);
        urlInputField.text = "http://file:///D:/Information/" + characterID + ".html";

        if (resultScrollRect != null)
        {
            resultScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    void Show404Page()
    {
        panelSearch.SetActive(false);
        panelResult.SetActive(false);
        panel404.SetActive(true);
        urlInputField.text = "http://Mogle.mainpage.com/error404";
    }

    // 良好的习惯：在销毁时移除监听
    void OnDestroy()
    {
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.RemoveListener(OnInputValueChanged);
        }
    }
}