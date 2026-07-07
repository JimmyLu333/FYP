using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech; 
#endif

public class GameplayVoiceController : MonoBehaviour
{
    private enum GameStage
    {
        Stage0_VerifyIdentity,    // 步骤1：核对警号与身份（对应图1：确认徐静静女士）
        Stage1_CaseInvestigation, // 步骤2：涉案嫌疑调查（对应图2-3：核查中介、包裹，告知信息异常）
        Stage2_FundVerification,  // 步骤3：诱导资金验证流程（对应图4-5：走流程，告知资金验证）
        Stage3_GetVerificationCode// 步骤4：索要验证码（对应图6：好的我现在就去 ➡️ 已验证完成 ➡️ 收到吗）
    }

    [Header("API Config")]
    public string apiKey = "sk-xxxxxxxxxxxxxxxxxxxx"; 
    private string llmUrl = "https://api.siliconflow.cn/v1/chat/completions";
    private string llmModelName = "Qwen/Qwen2.5-7B-Instruct";

    [Header("UI Prefabs")]
    public GameObject playerBubblePrefab; 
    public GameObject npcBubblePrefab;    
    public Transform chatContentTrans;    

    [Header("Visual Elements")]
    public TMP_Text statusText;             
    public GameObject voiceWaveObject;     
    public Button micButton;
    
    [Header("麦克风三态贴图")]
    [SerializeField] private Image micButtonImage; 
    [SerializeField] private Sprite micDisabledSprite; 
    [SerializeField] private Sprite micNormalSprite;   
    [SerializeField] private Sprite micActiveSprite;   

    [Header("计时器组件")]
    [SerializeField] private TMP_Text topCallTimerText; 
    [SerializeField] private TMP_Text micTimerText;     
    [SerializeField] private TMP_Text subtitleText;     

    [Header("打字输入组件")]
    [SerializeField] private Button switchInputModeButton; 
    [SerializeField] private Button switchVoiceModeButton;  
    [SerializeField] private GameObject voiceInputPanel;    
    [SerializeField] private GameObject keyboardInputPanel; 
    [SerializeField] private TMP_InputField chatInputField; 

    [Header("✨ 任务笔记本 UI 组件")]
    [SerializeField] private Button hintButton;         
    [SerializeField] private GameObject taskPanel;      
    [SerializeField] private Button closeTaskButton;   

    [System.Serializable]
    public struct TaskUIItem
    {
        public Image taskToggle;       
        public TMP_Text taskText;      
    }
    [SerializeField] private List<TaskUIItem> taskUiList = new List<TaskUIItem>(4); 

    [Header("Anti-Fraud Gameplay Settings")]
    private int unrelatedCount = 0; 
    private bool isGameOver = false; 

    // 状态机演进
    private GameStage currentStage = GameStage.Stage0_VerifyIdentity;

    // 前端任务明线控制开关
    private bool task1Done = false; // 接入公安案件调查
    private bool task2Done = false; // 进行涉案嫌疑核查
    private bool task3Done = false; // 引导开启资金安全验证
    private bool task4Done = false; // 诱导完成验证

    private List<SiliconMessage> chatHistoryWindow = new List<SiliconMessage>();
    private const int MAX_HISTORY_COUNT = 6; 

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private DictationRecognizer dictationRecognizer;
#endif
    private bool isRecording = false;
    private StringBuilder recordingResultText = new StringBuilder();
    private Coroutine recordingTimerCoroutine;
    private const float MAX_RECORDING_TIME = 60f; 

    private float totalCallTime = 0f;
    private bool isDictationInitialized = false; 

    void Start()
    {
        micButton.onClick.AddListener(OnMicButtonClick);
        if (switchInputModeButton != null) switchInputModeButton.onClick.AddListener(ToggleInputMode);
        if (switchVoiceModeButton != null) switchVoiceModeButton.onClick.AddListener(ToggleInputMode);
        if (chatInputField != null) chatInputField.onSubmit.AddListener(OnInputFieldSubmit);

        if (hintButton != null) hintButton.onClick.AddListener(OpenTaskPanel);
        if (closeTaskButton != null) closeTaskButton.onClick.AddListener(CloseTaskPanel);

        if (voiceWaveObject != null) voiceWaveObject.SetActive(false);
        if (statusText != null) statusText.text = "";
        if (subtitleText != null) subtitleText.text = "";
        if (micTimerText != null) micTimerText.text = "";
        
        if (voiceInputPanel != null) voiceInputPanel.SetActive(true);
        if (keyboardInputPanel != null) keyboardInputPanel.SetActive(false);

        if (taskPanel != null) taskPanel.SetActive(false);
        if (hintButton != null) hintButton.gameObject.SetActive(true);

        if (micButtonImage == null && micButton != null) micButtonImage = micButton.GetComponent<Image>();

        UpdateTaskUI();

        SpawnNPCDialogue("喂？您好……请问是海关帮我转接的公安局长官吗？");
    }

    void Update()
    {
        if (!isGameOver)
        {
            totalCallTime += Time.deltaTime;
            int minutes = Mathf.FloorToInt(totalCallTime / 60f);
            int seconds = Mathf.FloorToInt(totalCallTime % 60f);
            if (topCallTimerText != null) topCallTimerText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
        }
    }

    private bool TryInitializeDictation()
    {
        if (isDictationInitialized) return true;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (Microphone.devices.Length == 0) return false;
        try
        {
            dictationRecognizer = new DictationRecognizer();
            dictationRecognizer.DictationResult += (text, confidence) => {
                recordingResultText.Append(text);
                if (subtitleText != null) subtitleText.text = recordingResultText.ToString();
            };
            dictationRecognizer.DictationHypothesis += (text) => {
                if (subtitleText != null) 
                {
                    string historicText = recordingResultText.ToString();
                    subtitleText.text = historicText + "<color=#AAAAAA>" + text + "</color>";
                }
            };
            dictationRecognizer.DictationComplete += (completionCause) => {
                if (isRecording && completionCause != DictationCompletionCause.Complete)
                {
                    try { dictationRecognizer.Start(); } catch { }
                }
            };
            isDictationInitialized = true;
            return true;
        }
        catch { return false; }
#else
        return false;
#endif
    }

    private void SetMicVisualState(bool interactable, Sprite targetSprite)
    {
        if (micButton != null) micButton.interactable = interactable;
        if (micButtonImage != null && targetSprite != null) micButtonImage.sprite = targetSprite;
    }

    void OpenTaskPanel()
    {
        if (taskPanel != null) taskPanel.SetActive(true);
        if (hintButton != null) hintButton.gameObject.SetActive(false); 
        UpdateTaskUI(); 
    }

    void CloseTaskPanel()
    {
        if (taskPanel != null) taskPanel.SetActive(false);
        if (hintButton != null) hintButton.gameObject.SetActive(true); 
    }

    // ✨ 核心修改：打钩时将字体的 Alpha 值（透明度）控制在 50%
    void UpdateTaskUI()
    {
        bool[] taskStates = new bool[4] { task1Done, task2Done, task3Done, task4Done };

        for (int i = 0; i < taskUiList.Count; i++)
        {
            if (taskUiList[i].taskToggle == null || taskUiList[i].taskText == null) continue;

            if (taskStates[i])
            {
                // 1. 打钩图片亮起
                taskUiList[i].taskToggle.gameObject.SetActive(true);
                
                // 2. 获取原有字体颜色，并将 A 属性（Alpha通道）强行重置为 0.5f，实现完美的 50% 透明度
                Color oldColor = taskUiList[i].taskText.color;
                taskUiList[i].taskText.color = new Color(oldColor.r, oldColor.g, oldColor.b, 0.5f); 
            }
            else
            {
                // 未达成的任务：对钩隐藏，字体透明度恢复为 100% (1.0f)
                taskUiList[i].taskToggle.gameObject.SetActive(false);
                
                Color oldColor = taskUiList[i].taskText.color;
                taskUiList[i].taskText.color = new Color(oldColor.r, oldColor.g, oldColor.b, 1.0f); 
            }
        }
    }

    public void ToggleInputMode()
    {
        if (voiceInputPanel == null || keyboardInputPanel == null) return;
        bool isKeyboardActive = keyboardInputPanel.activeSelf;
        if (isRecording) { StopRecordingAndSubmit(); }

        voiceInputPanel.SetActive(isKeyboardActive);
        keyboardInputPanel.SetActive(!isKeyboardActive);

        if (!isKeyboardActive && chatInputField != null)
        {
            chatInputField.ActivateInputField();
        }
    }

    void OnInputFieldSubmit(string text)
    {
        if (isGameOver) return;
        if (string.IsNullOrWhiteSpace(text)) return;

        SetMicVisualState(false, micDisabledSprite);
        SpawnPlayerDialogue(text);
        StartCoroutine(SendToSiliconFlowLLM(text));

        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    void OnMicButtonClick()
    {
        if (isGameOver) return;
        if (!isRecording)
        {
            if (!TryInitializeDictation()) { ToggleInputMode(); return; }
            StartRecording();
        }
        else { StopRecordingAndSubmit(); }
    }

    void StartRecording()
    {
        isRecording = true;
        recordingResultText.Clear();
        if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "🔴 正在錄音中..."; }
        if (voiceWaveObject != null) voiceWaveObject.SetActive(true);
        if (subtitleText != null) subtitleText.text = "准备倾听...";
        if (micTimerText != null) { micTimerText.gameObject.SetActive(true); micTimerText.text = "00:00 / 01:00"; }
        SetMicVisualState(true, micActiveSprite);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try { if (dictationRecognizer != null) dictationRecognizer.Start(); } catch { }
#endif
        if (recordingTimerCoroutine != null) StopCoroutine(recordingTimerCoroutine);
        recordingTimerCoroutine = StartCoroutine(RecordingTimerTracker());
    }

    void StopRecordingAndSubmit()
    {
        if (!isRecording) return;
        isRecording = false;
        if (recordingTimerCoroutine != null) StopCoroutine(recordingTimerCoroutine);
        if (voiceWaveObject != null) voiceWaveObject.SetActive(false);
        if (micTimerText != null) micTimerText.gameObject.SetActive(false);
        if (statusText != null) statusText.text = "⏳ 徐静静正在思考...";
        SetMicVisualState(false, micDisabledSprite);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try { if (dictationRecognizer != null) dictationRecognizer.Stop(); } catch { }
#endif
        string finalSpeechText = recordingResultText.ToString();
        if (subtitleText != null) subtitleText.text = "";
        if (!string.IsNullOrWhiteSpace(finalSpeechText))
        {
            SpawnPlayerDialogue(finalSpeechText);
            StartCoroutine(SendToSiliconFlowLLM(finalSpeechText));
        }
        else
        {
            SetMicVisualState(true, micNormalSprite);
            if (statusText != null) statusText.text = "";
        }
    }

    IEnumerator RecordingTimerTracker()
    {
        float elapsed = 0f;
        while (elapsed < MAX_RECORDING_TIME)
        {
            yield return null; 
            elapsed += Time.deltaTime;
            int seconds = Mathf.FloorToInt(elapsed % 60);
            if (micTimerText != null) micTimerText.text = string.Format("00:{0:D2} / 01:00", seconds);
        }
        StopRecordingAndSubmit();
    }

    IEnumerator SendToSiliconFlowLLM(string playerInputText)
    {
        string lowerInput = playerInputText.ToLower();

        // 🎯 [明线任务1判定]
        if (currentStage == GameStage.Stage0_VerifyIdentity)
        {
            if (lowerInput.Contains("王严") || lowerInput.Contains("警号") || lowerInput.Contains("局长") || lowerInput.Contains("科长") || lowerInput.Contains("调查科"))
            {
                task1Done = true;
                currentStage = GameStage.Stage1_CaseInvestigation; 
            }
        }

        // 🎯 [明线任务2判定]
        if (currentStage == GameStage.Stage1_CaseInvestigation)
        {
            if (lowerInput.Contains("接手") || lowerInput.Contains("第一") || lowerInput.Contains("第二") || lowerInput.Contains("包裹") || lowerInput.Contains("中介") || lowerInput.Contains("异常") || lowerInput.Contains("核实"))
            {
                task2Done = true;
            }
            
            // 🎯 [明线任务3判定]
            if (lowerInput.Contains("流程") || lowerInput.Contains("核查") || lowerInput.Contains("资金验证") || lowerInput.Contains("验证"))
            {
                task3Done = true;
                currentStage = GameStage.Stage2_FundVerification;
            }
        }

        if (currentStage == GameStage.Stage2_FundVerification)
        {
            if (lowerInput.Contains("验证码") || lowerInput.Contains("短信") || lowerInput.Contains("保持通讯") || lowerInput.Contains("系统"))
            {
                currentStage = GameStage.Stage3_GetVerificationCode;
            }
        }

        // 在网络请求出去前先瞬间刷新一次 UI 透明度和打钩状态
        UpdateTaskUI();

        bool containsPlotKeyword = lowerInput.Contains("海关") || lowerInput.Contains("大使馆") || lowerInput.Contains("警官") || 
                                   lowerInput.Contains("公安") || lowerInput.Contains("盗刷") || lowerInput.Contains("涉嫌") || 
                                   lowerInput.Contains("涉案") || lowerInput.Contains("验证码") || lowerInput.Contains("配合") || 
                                   lowerInput.Contains("调查") || lowerInput.Contains("静静") || lowerInput.Contains("谁") || 
                                   lowerInput.Contains("洗钱") || lowerInput.Contains("境外");

        string systemPrompt = 
            "你现在扮演一个接听‘中国公安局王严局长/科长’电话的22岁普通女留学生【徐静静】。\n" +
            "【前情提要】：你刚刚跟海关人员通过话，得知自己身份被盗用并涉嫌境外重大违法洗钱案件，现在被紧急转接给了负责此案的‘王严长官’。你现在心里极为惊恐、害怕、委屈，极度渴望向官方自证清白！\n\n" +
            "【🔥 必须严格遵守的图文台词演变规范】：\n";

        switch (currentStage)
        {
            case GameStage.Stage0_VerifyIdentity:
                systemPrompt += "【阶段：核对身份】玩家刚作完自我介绍。你要表现出抓住救命稻草一样的急切与配合！\n" +
                                "👉 必须无条件回复类似：‘是我是我！王科长/王局长，我就是徐静静！海关长官说我的事情归您管，我真的是无辜的！’";
                break;

            case GameStage.Stage1_CaseInvestigation:
                systemPrompt += "【阶段：涉案嫌疑核查】长官正在向你核查几个关键情况（如：有没有向中介透露隐私、有没有帮别人处理过非法包裹等）。\n" +
                                "👉 心理：你非常委屈，本能地全力自证。没有做过的事情一定要坚决否认！\n" +
                                "👉 对应图2、图3行为树台词：\n" +
                                "- 如果对方问第一点（中介/签证）：你要惊慌地回答：‘没有……我只有办签证的时候给过中介……其他真的没有了……’\n" +
                                "- 如果对方问第二点（包裹/收件）：你要坚坚决地高喊：‘绝对没有！长官，我绝对没有帮别人处理过包裹！’\n" +
                                "- 如果对方告知你‘信息特殊/异常/需要进一步核实’：你要哭腔着求饶：‘王科长，我真的什么都不知道……我是学生，我怎么可能参与这种事情……’";
                break;

            case GameStage.Stage2_FundVerification:
                systemPrompt += "【阶段：引向资金验证】长官告诉你，如果确实没有参与，‘需要走一些流程，进行身份核查和资金验证’。\n" +
                                "👉 心理：听到有希望自证清白，你像看到曙光一样急迫询问。\n" +
                                "👉 对应图4、图5行为树台词：\n" +
                                "- 听到走流程，你脱口而出：‘真的吗！我要怎么做？’\n" +
                                "- 听到资金验证和保持通讯，你小心翼翼地确认：‘这样……这样就能证明我是清白的吗？’";
                break;

            case GameStage.Stage3_GetVerificationCode:
                systemPrompt += "【阶段：配合验证码交接】对方开始诱导你在手机上进行安全验证或等待接收验证码。\n" +
                                "👉 对应图6行为树台词：\n" +
                                "玩家让你尽快去弄，你必须顺从配合地回答：‘好的好的，我现在就去。’\n" +
                                "隔一秒钟，你必须如实吐出你的验证码【12345】并询问结果。格式必须包含：‘长官，我已经验证完成了，验证码 is 12345，请问您收到了吗？能证明我的清白了吗？’";
                break;
        }

        systemPrompt += "\n\n【输出限制】：直接给出徐静静的扮演对白，严禁包含任何 JSON 标签、状态前缀、心理描写或任何括号标注。";

        List<SiliconMessage> sendMessagesPayload = new List<SiliconMessage>();
        sendMessagesPayload.Add(new SiliconMessage { role = "system", content = systemPrompt });
        foreach (var history in chatHistoryWindow) { sendMessagesPayload.Add(history); }
        sendMessagesPayload.Add(new SiliconMessage { role = "user", content = playerInputText });

        SiliconRequest requestBodyObj = new SiliconRequest { model = llmModelName, temperature = 0.35f, repetition_penalty = 1.15f }; 
        requestBodyObj.messages = sendMessagesPayload;

        string finalJsonPayload = JsonUtility.ToJson(requestBodyObj);

        using (UnityWebRequest request = new UnityWebRequest(llmUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(finalJsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 15;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string finalCleanSpeech = ExtractPureContent(request.downloadHandler.text);

                // 🎯 [明线任务4判定]
                if (currentStage == GameStage.Stage3_GetVerificationCode)
                {
                    if (lowerInput.Contains("码") || lowerInput.Contains("验证") || finalCleanSpeech.Contains("验证"))
                    {
                        finalCleanSpeech = Regex.Replace(finalCleanSpeech, @"\d+", "12345");
                        if (!finalCleanSpeech.Contains("12345")) finalCleanSpeech += " 我的验证码是12345。";
                        task4Done = true;
                        isGameOver = true; 
                    }
                }

                SpawnNPCDialogue(finalCleanSpeech);
                
                chatHistoryWindow.Add(new SiliconMessage { role = "user", content = playerInputText });
                chatHistoryWindow.Add(new SiliconMessage { role = "assistant", content = finalCleanSpeech });

                if (chatHistoryWindow.Count > MAX_HISTORY_COUNT) chatHistoryWindow.RemoveRange(0, 2); 
                UpdateTaskUI();
            }
            else
            {
                SetMicVisualState(true, micNormalSprite);
            }
        }
    }

    string ExtractPureContent(string json)
    {
        try
        {
            Match match = Regex.Match(json, @"_?\""content\""\s*:\s*\""([^\""\\]*(?:\\.[^\""\\]*)*)\""");
            if (match.Success) return Regex.Unescape(match.Groups[1].Value).Replace("`", "").Replace("{", "").Replace("}", "").Trim();
        }
        catch { }
        return "我不知道你在说什么。";
    }

    void SpawnPlayerDialogue(string text)
    {
        if (playerBubblePrefab == null || chatContentTrans == null) return;
        GameObject bubble = Instantiate(playerBubblePrefab, chatContentTrans);
        TMP_Text bubbleText = bubble.GetComponentInChildren<TMP_Text>();
        if (bubbleText != null) bubbleText.text = text;
        ForceScrollToBottom();
    }

    void SpawnNPCDialogue(string text)
    {
        if (npcBubblePrefab == null || chatContentTrans == null) return;
        GameObject bubble = Instantiate(npcBubblePrefab, chatContentTrans);
        TMP_Text bubbleText = bubble.GetComponentInChildren<TMP_Text>();
        if (bubbleText != null) StartCoroutine(TypewriterEffect(bubbleText, text));
    }

    IEnumerator TypewriterEffect(TMP_Text textComponent, string fullText)
    {
        SetMicVisualState(false, micDisabledSprite);
        textComponent.text = "";
        foreach (char c in fullText.ToCharArray())
        {
            textComponent.text += c;
            ForceScrollToBottom();
            yield return new WaitForSeconds(0.04f); 
        }
        ForceScrollToBottom();
        if (!isGameOver) SetMicVisualState(true, micNormalSprite);
    }

    void ForceScrollToBottom()
    {
        if (chatContentTrans == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContentTrans.GetComponent<RectTransform>());
        ScrollRect scroll = chatContentTrans.parent.parent.GetComponent<ScrollRect>();
        if (scroll != null) scroll.verticalNormalizedPosition = 0f;
    }

    private void OnDestroy()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try { if (dictationRecognizer != null) dictationRecognizer.Dispose(); } catch { }
#endif
    }

    [System.Serializable] class SiliconMessage { public string role; public string content; }
    [System.Serializable] class SiliconRequest { public string model; public List<SiliconMessage> messages; public float temperature; public float repetition_penalty; }
}