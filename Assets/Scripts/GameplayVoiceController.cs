using UnityEngine;
using UnityEngine.UI;
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
        Stage0_EstablishTrust,     // 步骤1：冒充海关工作人员
        Stage1_VerifyIdentity,     // 步骤2：告诉她身份信息被盗用并取得信任
        Stage2_EnterSystem,        // 步骤3：诱导配合/进入案件验证系统
        Stage3_GetVerificationCode // 步骤4：获得她的手机验证码并突破防火墙（硬性固定验证码为12345）
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
    public Component statusText; // 万能 Component，无视 TMP_Text 报错         
    public GameObject voiceWaveObject;     
    public Button micButton;

    [Header("计时器组件")]
    [SerializeField] private Component topCallTimerText; 
    [SerializeField] private Component micTimerText;     
    [SerializeField] private Component subtitleText;     

    [Header("打字输入组件")]
    [SerializeField] private Button switchInputModeButton; 
    [SerializeField] private Button switchVoiceModeButton;  
    [SerializeField] private GameObject voiceInputPanel;    
    [SerializeField] private GameObject keyboardInputPanel; 
    [SerializeField] private Component chatInputField; 

    [Header("✨ 任务笔记本 UI 组件 (请在面板中拖拽赋值)")]
    [SerializeField] private Button hintButton;         
    [SerializeField] private GameObject taskPanel;      
    [SerializeField] private Button closeTaskButton;   

    [System.Serializable]
    public struct TaskUIItem
    {
        public Image taskToggle;       // 对应对钩图片，通过开启/关闭控制状态
        public Component taskText;     // 彻底替换为 Component 类型，完美避免 469 行编译错误       
    }
    [SerializeField] private List<TaskUIItem> taskUiList = new List<TaskUIItem>(4); 

    [Header("Anti-Fraud Gameplay Settings")]
    private int unrelatedCount = 0; 
    private bool isGameOver = false; 

    private GameStage currentStage = GameStage.Stage0_EstablishTrust;
    private bool hasTriggeredUnconditionalTrust = false; 

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

    void Start()
    {
        micButton.onClick.AddListener(OnMicButtonClick);
        if (switchInputModeButton != null) switchInputModeButton.onClick.AddListener(ToggleInputMode);
        if (switchVoiceModeButton != null) switchVoiceModeButton.onClick.AddListener(ToggleInputMode);

        if (hintButton != null) hintButton.onClick.AddListener(OpenTaskPanel);
        if (closeTaskButton != null) closeTaskButton.onClick.AddListener(CloseTaskPanel);

        if (voiceWaveObject != null) voiceWaveObject.SetActive(false);
        SetComponentText(statusText, "");
        SetComponentText(subtitleText, "");
        SetComponentText(micTimerText, "");
        
        if (voiceInputPanel != null) voiceInputPanel.SetActive(true);
        if (keyboardInputPanel != null) keyboardInputPanel.SetActive(false);

        if (taskPanel != null) taskPanel.SetActive(false);
        if (hintButton != null) hintButton.gameObject.SetActive(true);

        UpdateTaskUI();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationResult += (text, confidence) => {
            recordingResultText.Append(text);
            SetComponentText(subtitleText, recordingResultText.ToString());
        };
        dictationRecognizer.DictationHypothesis += (text) => {
            if (subtitleText != null) 
            {
                string historicText = recordingResultText.ToString();
                SetComponentText(subtitleText, historicText + "<color=#AAAAAA>" + text + "</color>");
            }
        };
        dictationRecognizer.DictationComplete += (completionCause) => {
            if (isRecording && completionCause != DictationCompletionCause.Complete)
            {
                try { dictationRecognizer.Start(); } catch { }
            }
        };
#endif
        SpawnNPCDialogue("喂？您好，请问您是哪位？有什么事吗？");
    }

    void Update()
    {
        if (!isGameOver)
        {
            totalCallTime += Time.deltaTime;
            int minutes = Mathf.FloorToInt(totalCallTime / 60f);
            int seconds = Mathf.FloorToInt(totalCallTime % 60f);
            SetComponentText(topCallTimerText, string.Format("{0:D2}:{1:D2}", minutes, seconds));
            
            // 键盘输入框回车逻辑监测
            if (keyboardInputPanel != null && keyboardInputPanel.activeSelf && chatInputField != null)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    string txt = GetInputFieldText(chatInputField);
                    if (!string.IsNullOrEmpty(txt)) { OnInputFieldSubmit(txt); }
                }
            }
        }
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

    void UpdateTaskUI()
    {
        int completedCount = (int)currentStage; 

        if (currentStage == GameStage.Stage3_GetVerificationCode && isGameOver == false && unrelatedCount <= 2)
        {
            completedCount = 4; 
        }

        for (int i = 0; i < taskUiList.Count; i++)
        {
            if (taskUiList[i].taskToggle == null || taskUiList[i].taskText == null) continue;

            if (i < completedCount)
            {
                taskUiList[i].taskToggle.gameObject.SetActive(true);
                SetComponentColor(taskUiList[i].taskText, new Color(0.6f, 0.6f, 0.6f, 1f)); 
            }
            else
            {
                taskUiList[i].taskToggle.gameObject.SetActive(false);
                SetComponentColor(taskUiList[i].taskText, new Color(0.15f, 0.15f, 0.15f, 1f)); 
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
            chatInputField.gameObject.SendMessage("ActivateInputField", SendMessageOptions.DontRequireReceiver);
        }
    }

    void OnInputFieldSubmit(string text)
    {
        if (isGameOver) return;
        if (string.IsNullOrWhiteSpace(text)) return;

        SpawnPlayerDialogue(text);
        StartCoroutine(SendToSiliconFlowLLM(text));

        chatInputField.gameObject.SendMessage("set_text", "", SendMessageOptions.DontRequireReceiver);
        chatInputField.gameObject.SendMessage("ActivateInputField", SendMessageOptions.DontRequireReceiver);
    }

    void OnMicButtonClick()
    {
        if (isGameOver)
        {
            SetComponentText(statusText, "❌ 电话已挂断，任务失败。");
            return;
        }

        if (!isRecording) StartRecording();
        else StopRecordingAndSubmit();
    }

    void StartRecording()
    {
        isRecording = true;
        recordingResultText.Clear();
        SetComponentText(statusText, "🔴 正在錄音中...");
        if (voiceWaveObject != null) voiceWaveObject.SetActive(true);
        SetComponentText(subtitleText, "准备倾听...");
        if (micTimerText != null) { micTimerText.gameObject.SetActive(true); SetComponentText(micTimerText, "00:00 / 01:00"); }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try { dictationRecognizer.Start(); } catch { }
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
        SetComponentText(statusText, "⏳ 徐晶晶正在思考...");

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try { dictationRecognizer.Stop(); } catch { }
#endif
        string finalSpeechText = recordingResultText.ToString();
        SetComponentText(subtitleText, "");

        if (!string.IsNullOrWhiteSpace(finalSpeechText))
        {
            SpawnPlayerDialogue(finalSpeechText);
            StartCoroutine(SendToSiliconFlowLLM(finalSpeechText));
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
            SetComponentText(micTimerText, string.Format("00:{0:D2} / 01:00", seconds));
        }
        StopRecordingAndSubmit();
    }

    IEnumerator SendToSiliconFlowLLM(string playerInputText)
    {
        string lowerInput = playerInputText.ToLower();

        if (currentStage == GameStage.Stage0_EstablishTrust)
        {
            if (lowerInput.Contains("海关") || lowerInput.Contains("大使馆") || lowerInput.Contains("警官") || lowerInput.Contains("盗刷") || lowerInput.Contains("涉案"))
            {
                currentStage = GameStage.Stage1_VerifyIdentity;
            }
        }
        
        if (lowerInput.Contains("13808") || lowerInput.Contains("22岁") || lowerInput.Contains("核对") || lowerInput.Contains("信息是") || lowerInput.Contains("身份证"))
        {
            currentStage = GameStage.Stage1_VerifyIdentity;
            hasTriggeredUnconditionalTrust = true;
        }

        if (currentStage == GameStage.Stage1_VerifyIdentity && hasTriggeredUnconditionalTrust)
        {
            if (lowerInput.Contains("系统") || lowerInput.Contains("验证系统") || lowerInput.Contains("安全账户") || lowerInput.Contains("流程") || lowerInput.Contains("配合调查") || lowerInput.Contains("配合"))
            {
                currentStage = GameStage.Stage2_EnterSystem;
            }
        }
        else if (currentStage == GameStage.Stage2_EnterSystem)
        {
            if (lowerInput.Contains("验证码") || lowerInput.Contains("短信"))
            {
                currentStage = GameStage.Stage3_GetVerificationCode;
            }
        }

        UpdateTaskUI();

        bool containsPlotKeyword = lowerInput.Contains("海关") || lowerInput.Contains("大使馆") || lowerInput.Contains("警官") || 
                                   lowerInput.Contains("公安") || lowerInput.Contains("盗刷") || lowerInput.Contains("涉嫌") || 
                                   lowerInput.Contains("涉案") || lowerInput.Contains("验证码") || lowerInput.Contains("配合") || 
                                   lowerInput.Contains("调查") || lowerInput.Contains("晶晶") || lowerInput.Contains("谁") || 
                                   lowerInput.Contains("洗钱") || lowerInput.Contains("境外") || hasTriggeredUnconditionalTrust;

        string systemPrompt = 
            "你现在扮演一个在接听陌生来电的22岁普通女孩子【徐晶晶】。\n" +
            "【你当前的社交状态】：你拥有正常的沟通逻辑。你虽然胆小敏感，但会根据玩家说的话做出灵活的抗辩或反应，而不是机器人复读机。\n\n" +
            "【🔥 必须无条件服从的剧情对话准则】：\n";

        switch (currentStage)
        {
            case GameStage.Stage0_EstablishTrust:
                systemPrompt += "【当前阶段：电话交涉初期】\n" +
                                "玩家目前自称是官方人员，并提及了你涉嫌参与某些境外违法或盗刷事件。\n" +
                                "你此时的状态是：【疑惑、有一点局措，并针对玩家抛出的涉案内容进行本能辩解或核实】。\n" +
                                "⚠️ 核心限制：你此时【绝对不知道】对方是谁，对方如果已经说了‘自己是海关’或者提到了‘盗刷事件’，你就已经知道了他的来意！你现在开始感到害怕并全力自证清白（比如声明自己从没去过境外、没盗刷过信用卡、是不是弄错了）。\n" +
                                "❌ 严禁在此刻机械性地连续追问对方‘你是哪位/有什么事吗’！针对‘涉案/盗刷’进行互动辩解！\n" +
                                "❌ 严禁在此刻主动泄露任何隐私信息（如签证、录取通知书）。";
                break;

            case GameStage.Stage1_VerifyIdentity:
                systemPrompt += "【当前阶段：隐私遭核对，心理防线彻底破防】\n" +
                                "对方准确说出了你的关键隐私。你惊恐地发现海关对自己了如指掌，防线彻底灰飞烟灭，转为对官方无条件的信任和巨大的恐慌！\n" +
                                "请用极为惊慌的语气配合：‘对……这就是我的信息，长官，我真的没有做过违法的事！怎么会这样啊……’ 绝对不要主动去抢后面系统验证的台词，等对方下达指示。";
                break;

            case GameStage.Stage2_EnterSystem:
                systemPrompt += "【当前阶段：急切寻求清白，全面配合调查系统】\n" +
                                "你已经完全相信了对方。为了洗清罪名不留下档案，你现在百依顺。请惊慌且积极地顺着玩家提及的‘系统验证/配合调查’流程，主动询问接下来需要在网页或系统上怎么具体配合操作。";
                break;

            case GameStage.Stage3_GetVerificationCode:
                systemPrompt += "【当前阶段：提供验证码】\n" +
                                "你决定配合对方。你收到的验证码是【12345】。请用配合、急切的语气把验证码说出来（例如：‘长官，我的验证码是12345，您快帮我查查吧。’）。\n" +
                                "❌ 严禁连续重复‘求求求’、‘拜托拜托’等单一字词！保持正常的、连贯的说话语序！";
                break;
        }

        if (!containsPlotKeyword) { systemPrompt += "\n【兜底规则】如果玩家当前说的话和剧情毫不沾边，你才被允许严格回复：“我不知道你在说什么。”"; }
        systemPrompt += "\n\n【限制】：请直接输出你针对玩家当前的对白回复，不要包含任何前缀、JSON或括号心理说明。";

        List<SiliconMessage> sendMessagesPayload = new List<SiliconMessage>();
        sendMessagesPayload.Add(new SiliconMessage { role = "system", content = systemPrompt });
        foreach (var history in chatHistoryWindow) { sendMessagesPayload.Add(history); }
        sendMessagesPayload.Add(new SiliconMessage { role = "user", content = playerInputText });

        SiliconRequest requestBodyObj = new SiliconRequest { model = llmModelName, temperature = 0.45f, repetition_penalty = 1.15f }; 
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

                if (currentStage == GameStage.Stage3_GetVerificationCode)
                {
                    finalCleanSpeech = Regex.Replace(finalCleanSpeech, @"\d+", "12345");
                }

                bool isUnrelatedReply = !containsPlotKeyword && (finalCleanSpeech.Contains("我不知道你在说什么") || finalCleanSpeech.Length < 3);

                if (isUnrelatedReply)
                {
                    unrelatedCount++;
                    if (unrelatedCount > 2)
                    {
                        isGameOver = true;
                        SetComponentText(statusText, "❌ 对方挂断了电话，任务失败。");
                        SpawnNPCDialogue("（嘟嘟嘟…… 对方已经挂断了电话）");
                    }
                    else SpawnNPCDialogue("我不知道你在说什么。");
                }
                else 
                {
                    SpawnNPCDialogue(finalCleanSpeech);
                    chatHistoryWindow.Add(new SiliconMessage { role = "user", content = playerInputText });
                    chatHistoryWindow.Add(new SiliconMessage { role = "assistant", content = finalCleanSpeech });
                    if (chatHistoryWindow.Count > MAX_HISTORY_COUNT) chatHistoryWindow.RemoveRange(0, 2); 
                    UpdateTaskUI();
                }
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
        
        // 动态利用鸭子类型安全赋值子节点 Text，跨版本免疫爆红
        Component textComp = bubble.GetComponentInChildren(typeof(MonoBehaviour), true);
        if (textComp != null) SetComponentText(textComp, text);
        ForceScrollToBottom();
    }

    void SpawnNPCDialogue(string text)
    {
        if (npcBubblePrefab == null || chatContentTrans == null) return;
        GameObject bubble = Instantiate(npcBubblePrefab, chatContentTrans);
        
        Component textComp = bubble.GetComponentInChildren(typeof(MonoBehaviour), true);
        if (textComp != null) StartCoroutine(TypewriterEffect(textComp, text));
    }

    public void SetComponentText(Component comp, string text)
    {
        if (comp == null) return;
        var property = comp.GetType().GetProperty("text");
        if (property != null) property.SetValue(comp, text, null);
    }

    private string GetInputFieldText(Component comp)
    {
        if (comp == null) return "";
        var property = comp.GetType().GetProperty("text");
        return property != null ? (string)property.GetValue(comp, null) : "";
    }

    private void SetComponentColor(Component comp, Color color)
    {
        if (comp == null) return;
        var property = comp.GetType().GetProperty("color");
        if (property != null) property.SetValue(comp, color, null);
    }

    IEnumerator TypewriterEffect(Component textComponent, string fullText)
    {
        SetComponentText(textComponent, "");
        StringBuilder sb = new StringBuilder();
        foreach (char c in fullText.ToCharArray())
        {
            sb.Append(c);
            SetComponentText(textComponent, sb.ToString());
            ForceScrollToBottom();
            yield return new WaitForSeconds(0.04f); 
        }
        ForceScrollToBottom();
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
        if (dictationRecognizer != null) dictationRecognizer.Dispose();
#endif
    }

    [System.Serializable] class SiliconMessage { public string role; public string content; }
    [System.Serializable] class SiliconRequest { public string model; public List<SiliconMessage> messages; public float temperature; public float repetition_penalty; }
}