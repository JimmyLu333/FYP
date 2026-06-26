using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BrowserManager : MonoBehaviour
{
    [Serializable]
    public struct CharacterData
    {
        public string characterID;       // 搜索关键词/编号（如：1001）
        public Sprite photo;            // 角色头像
        public string characterName;     // 角色名字

        // 👈 数据库里只需要保留纯内容文本
        [TextArea(2, 5)]
        public string infoContent;       // 姓名、年龄、电话等
        [TextArea(3, 10)]
        public string backgroundContent; // 刚获得一笔拆迁款等
        [TextArea(3, 10)]
        public string usefulContent;     // 高度溺爱孙子等
        [TextArea(3, 10)]
        public string targetContent;     // 1: 欺骗他... 2: 假装医生...
    }

    [Header("Database")]
    public CharacterData[] allCharacters; 

    [Header("Top Bar")]
    public TMP_InputField urlInputField; 

    [Header("Panels")]
    public GameObject panelSearch;   
    public GameObject panelResult;   
    public GameObject panel404;      
    public ScrollRect resultScrollRect; 

    [Header("Search Inputs")]
    public TMP_InputField searchInputField; 
    public Button searchButton; 

    [Header("Result UI Components (只绑定内容Text)")]
    public Image resultPhotoImage;       
    public TMP_Text resultNameText;      
    public TMP_Text resultInfoText;       // 👈 绑定 Value_Info
    public TMP_Text resultBackgroundText; // 👈 绑定 Value_Bg
    public TMP_Text resultUsefulText;     // 👈 绑定 Value_Useful
    public TMP_Text resultTargetText;     // 👈 绑定 Value_Target

    void Start()
    {
        ShowSearchPage();
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnInputValueChanged);
        }
        OnInputValueChanged(searchInputField.text);
    }

    void OnInputValueChanged(string currentText)
    {
        if (searchButton != null)
        {
            searchButton.interactable = !string.IsNullOrWhiteSpace(currentText);
        }
    }

    public void OnSearchButtonClick()
    {
        string input = searchInputField.text.Trim();
        if (string.IsNullOrEmpty(input)) return;

        CharacterData? foundCharacter = null;
        foreach (var character in allCharacters)
        {
            if (character.characterID == input || character.characterName == input)
            {
                foundCharacter = character;
                break;
            }
        }

        if (foundCharacter != null)
        {
            ShowResultPage(foundCharacter.Value);
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

    void ShowResultPage(CharacterData data)
    {
        panelSearch.SetActive(false);
        panelResult.SetActive(true);
        panel404.SetActive(false);
        
        urlInputField.text = "http://file:///D:/Information/" + data.characterID + ".html";

        if (resultPhotoImage != null) resultPhotoImage.sprite = data.photo;
        if (resultNameText != null) resultNameText.text = data.characterName;
        
        // 👈 动态刷新四个纯内容区域
        if (resultInfoText != null) resultInfoText.text = data.infoContent;
        if (resultBackgroundText != null) resultBackgroundText.text = data.backgroundContent;
        if (resultUsefulText != null) resultUsefulText.text = data.usefulContent;
        if (resultTargetText != null) resultTargetText.text = data.targetContent;

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

    void OnDestroy()
    {
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.RemoveListener(OnInputValueChanged);
        }
    }
}