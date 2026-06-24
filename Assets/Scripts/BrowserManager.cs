using UnityEngine;
using UnityEngine.UI;
using TMPro; // 👈 確保這行絕對有寫，且沒有拼錯

public class BrowserManager : MonoBehaviour
{
    [Header("Top Bar")]
    public TMP_InputField urlInputField; 

    [Header("Panels")]
    public GameObject panelSearch;   
    public GameObject panelResult;   
    public GameObject panel404;      

    [Header("Search Inputs")]
    public TMP_InputField searchInputField; 

    [Header("Result Components")]
    public ScrollRect resultScrollRect; 

    // 模擬的數據庫：存放合法的角色編號（例如 MOLT 遊戲中的主角陳默編號，或其它測試編號）
    private string[] validCharacterIDs = { "1001", "1002", "劉福來", "ChenMo" }; 

    void Start()
    {
        ShowSearchPage();
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
}