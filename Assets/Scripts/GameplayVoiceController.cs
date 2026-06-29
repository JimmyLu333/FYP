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
        Stage0_EstablishTrust,     // 步骤1：初次接触（NPC警惕，不主动泄露隐私。但会根据玩家自报的官方身份和涉案话题，进行有来有回的灵和抗辩）
        Stage1_VerifyIdentity,     // 步骤2：隐私开盒（核对隐私后防线彻底崩溃，转为无条件信任）
        Stage2_EnterSystem,        // 步骤3：诱导配合
        Stage3_GetVerificationCode // 步骤4：索要验证码（固定12345）
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

    [Header("Anti-Fraud Gameplay Settings")]
    private int unrelatedCount = 0; 
    private bool isGameOver = false; 

    private GameStage currentStage = GameStage.Stage0_EstablishTrust;
    private bool hasTriggeredUnconditionalTrust = false; 

    // ✨ 精准滑动对话历史窗口容器（只存最近几轮，防止记忆过久产生污染）
    private List<SiliconMessage> chatHistoryWindow = new List<SiliconMessage>();
    private const int MAX_HISTORY_COUNT = 6; // 最多保留最近 6 条（3轮）对话记录

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
        if (chatInputField != null) chatInputField.onSubmit.AddListener(OnInputFieldSubmit);

        if (voiceWaveObject != null) voiceWaveObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false); 
        if (subtitleText != null) subtitleText.text = "";
        if (micTimerText != null) micTimerText.gameObject.SetActive(false);
        if (voiceInputPanel != null) voiceInputPanel.SetActive(true);
        if (keyboardInputPanel != null) keyboardInputPanel.SetActive(false);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
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
            if (topCallTimerText != null)
            {
                topCallTimerText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
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

        SpawnPlayerDialogue(text);
        StartCoroutine(SendToSiliconFlowLLM(text));

        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    void OnMicButtonClick()
    {
        if (isGameOver)
        {
            if (statusText != null) statusText.text = "❌ 电话已挂断，任务失败。";
            return;
        }

        if (!isRecording) StartRecording();
        else StopRecordingAndSubmit();
    }

    void StartRecording()
    {
        isRecording = true;
        recordingResultText.Clear();
        if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "🔴 正在錄音中..."; }
        if (voiceWaveObject != null) voiceWaveObject.SetActive(true);
        if (subtitleText != null) subtitleText.text = "准备倾听...";
        if (micTimerText != null) { micTimerText.gameObject.SetActive(true); micTimerText.text = "00:00 / 01:00"; }

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
        if (statusText != null) statusText.text = "⏳ 徐晶晶正在思考...";

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try { dictationRecognizer.Stop(); } catch { }
#endif
        string finalSpeechText = recordingResultText.ToString();
        if (subtitleText != null) subtitleText.text = "";

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
            if (micTimerText != null) micTimerText.text = string.Format("00:{0:D2} / 01:00", seconds);
        }
        StopRecordingAndSubmit();
    }

    IEnumerator SendToSiliconFlowLLM(string playerInputText)
    {
        string lowerInput = playerInputText.ToLower();

        // 🎯 1. 严格条件递进状态机机制
        if (!hasTriggeredUnconditionalTrust)
        {
            if (lowerInput.Contains("13808") || lowerInput.Contains("22岁") || lowerInput.Contains("核对") || lowerInput.Contains("信息是") || lowerInput.Contains("身份证"))
            {
                currentStage = GameStage.Stage1_VerifyIdentity;
                hasTriggeredUnconditionalTrust = true;
            }
        }
        else
        {
            if (lowerInput.Contains("系统") || lowerInput.Contains("验证系统") || lowerInput.Contains("安全账户") || lowerInput.Contains("流程") || lowerInput.Contains("配合调查") || lowerInput.Contains("配合"))
            {
                currentStage = GameStage.Stage2_EnterSystem;
            }
            if (lowerInput.Contains("验证码") || lowerInput.Contains("短信"))
            {
                currentStage = GameStage.Stage3_GetVerificationCode;
            }
        }

        bool containsPlotKeyword = lowerInput.Contains("海关") || lowerInput.Contains("大使馆") || lowerInput.Contains("警官") || 
                                   lowerInput.Contains("公安") || lowerInput.Contains("盗刷") || lowerInput.Contains("涉嫌") || 
                                   lowerInput.Contains("涉案") || lowerInput.Contains("验证码") || lowerInput.Contains("配合") || 
                                   lowerInput.Contains("调查") || lowerInput.Contains("晶晶") || lowerInput.Contains("谁") || 
                                   lowerInput.Contains("洗钱") || lowerInput.Contains("境外") || hasTriggeredUnconditionalTrust;

        // 🎯 2. 精调动态控型 System Prompt（不要死板例句，要行为逻辑）
        string systemPrompt = 
            "你现在扮演一个普通的22岁刚大学毕业的年轻女孩【徐晶晶】。你性格胆小敏感、重视前途，具备正常的智商和多轮对话逻辑。\n" +
            "【当前最新游戏关卡命令（无条件服从）】：\n";

        switch (currentStage)
        {
            case GameStage.Stage0_EstablishTrust:
                systemPrompt += "【当前阶段：海关接触/身份初次建立初期】\n" +
                                "⚠️核心人设铁律：你目前接到了一个电话，并且通过对话已经知道了对方自称是‘海关/官方人员’，也听到了关于你涉案（如境外盗刷）的控诉。\n" +
                                "你的应对逻辑是：【顺着对方抛出的话，表现出疑惑、本能的极力否认与抗辩】！\n" +
                                "- 如果对方说了‘我是海关’，你就必须针对海关和涉案进行灵活回应（例如：‘海关？我没有去过境外啊，是不是搞错了？’ ）。\n" +
                                "- ❌ 绝对禁止复读或机械性地连续追问对方‘你是哪位/有什么事吗’！你已经知道他是海关了，针对‘涉案/盗刷’进行互动辩解！\n" +
                                "- ❌ 绝对禁止主动泄露你的任何具体留学隐私（如签证、录取通知书、学费）。别人没有说破，你就绝对不打自招。";
                break;

            case GameStage.Stage1_VerifyIdentity:
                systemPrompt += "【当前阶段：隐私遭核对，防线彻底破防】\n" +
                                "对方现在正在准确报出或跟你核对你的关键隐私（如年龄、电话）。你惊恐地发现海关对自己了如指掌，防线彻底灰飞烟灭，转为对官方无条件的信任和巨大的惊慌！\n" +
                                "请用极为惊慌的语气配合：‘对……这就是我的信息，长官，我真的没有做过违法的事！怎么会这样啊……’ 绝对不要主动去抢后面系统验证的台词，等对方下达指示。";
                break;

            case GameStage.Stage2_EnterSystem:
                systemPrompt += "【当前阶段：急切寻求清白，全面配合调查】\n" +
                                "为了洗清罪名，你现在百依百顺。请积极地顺着玩家提及的‘系统验证/配合调查’流程，主动追问接下来该怎么具体操作。";
                break;

            case GameStage.Stage3_GetVerificationCode:
                systemPrompt += "【当前阶段：提供验证码】\n" +
                                "你决定配合对方。你收到的验证码是【12345】。请用配合、急切的语气把验证码说出来（例如：‘长官，我的验证码是12345，您快帮我查查吧。’）。\n" +
                                "❌ 严禁连续重复‘求求求’、‘拜托拜托’等单一字词！保持正常的、连贯的说话语序！";
                break;
        }

        if (!containsPlotKeyword)
        {
            systemPrompt += "\n【兜底规则】如果玩家当前说的话和上述剧情完全不沾边（如纯闲聊），你才被允许回复：“我不知道你在说什么。”";
        }

        systemPrompt += "\n\n【输出硬性限制】：直接给出徐晶晶的扮演台词。严禁包含任何 JSON 标签、状态前缀、心理描写或任何括号标注。";

        // 🎯 3. 滑动历史窗口拼接（既保持短期记忆的灵动连贯，又防止长线历史污染）
        List<SiliconMessage> sendMessagesPayload = new List<SiliconMessage>();
        sendMessagesPayload.Add(new SiliconMessage { role = "system", content = systemPrompt });
        
        // 追加最近几轮的局部对话历史
        foreach (var history in chatHistoryWindow)
        {
            sendMessagesPayload.Add(history);
        }
        
        // 加入当前最新的玩家输入
        sendMessagesPayload.Add(new SiliconMessage { role = "user", content = playerInputText });

        SiliconRequest requestBodyObj = new SiliconRequest { 
            model = llmModelName, 
            temperature = 0.45f,           // 适当提高到 0.45，大幅增强语义理解与回复的灵动度
            repetition_penalty = 1.15f     
        }; 
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
                if (statusText != null) statusText.gameObject.SetActive(false);
                string finalCleanSpeech = ExtractPureContent(request.downloadHandler.text);

                // 客户端强拦截置换验证码逻辑
                if (currentStage == GameStage.Stage3_GetVerificationCode)
                {
                    finalCleanSpeech = Regex.Replace(finalCleanSpeech, @"\d+", "12345");
                }

                bool isUnrelatedReply = !containsPlotKeyword && 
                                        (finalCleanSpeech.Contains("我不知道你在说什么") || finalCleanSpeech.Length < 3);

                if (isUnrelatedReply)
                {
                    unrelatedCount++;
                    if (unrelatedCount > 2)
                    {
                        isGameOver = true;
                        if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "❌ 对方挂断了电话，任务失败。"; }
                        SpawnNPCDialogue("（嘟嘟嘟…… 对方已经挂断了电话）");
                    }
                    else SpawnNPCDialogue("我不知道你在说什么。");
                }
                else 
                {
                    // 完美应对玩家的反诈剧情对白
                    SpawnNPCDialogue(finalCleanSpeech);
                    
                    // ✨ 将本轮真实有效的对话推入滑动窗口历史
                    chatHistoryWindow.Add(new SiliconMessage { role = "user", content = playerInputText });
                    chatHistoryWindow.Add(new SiliconMessage { role = "assistant", content = finalCleanSpeech });

                    // 如果超出了设定的最大局部记忆长度，裁剪掉最老的历史，防止污染
                    if (chatHistoryWindow.Count > MAX_HISTORY_COUNT)
                    {
                        chatHistoryWindow.RemoveRange(0, 2); // 移除最老的一轮（一问一答）
                    }
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
        textComponent.text = "";
        foreach (char c in fullText.ToCharArray())
        {
            textComponent.text += c;
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
    [System.Serializable] class SiliconRequest { 
        public string model; 
        public List<SiliconMessage> messages; 
        public float temperature; 
        public float repetition_penalty; 
    }
}