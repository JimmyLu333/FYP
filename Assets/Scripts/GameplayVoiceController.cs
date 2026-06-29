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
        Part1_AlertStranger,   // Part 1：不清楚任何情况，带有警戒心，绝不泄漏留学隐私
        Part2_HighlyCooperate  // Part 2：说出大于两项隐私后触发，全面破防高度配合玩家
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
    
    [Header("麦克风三态贴图 (槽位丢失请重新拖入)")]
    [SerializeField] private Image micButtonImage; 
    [SerializeField] private Sprite micDisabledSprite; // 状态1：暗色不可用
    [SerializeField] private Sprite micNormalSprite;   // 状态2：亮色可用
    [SerializeField] private Sprite micActiveSprite;   // 状态3：高亮蓝色

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

    private GameStage currentStage = GameStage.Part1_AlertStranger;
    
    private bool hasSaidAge = false;
    private bool hasSaidIdentity = false;
    private bool hasSaidPhone = false;

    private bool task1Done = false; 
    private bool task2Done = false; 
    private bool task3Done = false; 
    private bool task4Done = false; 

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
        // 初始第一句话，强制进入安全解锁状态
        SpawnNPCDialogue("喂？您好，请问您是哪位？有什么事吗？");
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

    void UpdateTaskUI()
    {
        bool[] taskStates = new bool[4] { task1Done, task2Done, task3Done, task4Done };

        for (int i = 0; i < taskUiList.Count; i++)
        {
            if (taskUiList[i].taskToggle == null || taskUiList[i].taskText == null) continue;

            if (taskStates[i])
            {
                taskUiList[i].taskToggle.gameObject.SetActive(true);
                taskUiList[i].taskText.color = new Color(0.6f, 0.6f, 0.6f, 1f); 
            }
            else
            {
                taskUiList[i].taskToggle.gameObject.SetActive(false);
                taskUiList[i].taskText.color = new Color(0.15f, 0.15f, 0.15f, 1f); 
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

        // 打字模式下也防止死锁麦克风
        SetMicVisualState(false, micDisabledSprite);

        SpawnPlayerDialogue(text);
        StartCoroutine(SendToSiliconFlowLLM(text));

        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    void OnMicButtonClick()
    {
        if (isGameOver) return;

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

        // 高亮选中状态
        SetMicVisualState(true, micActiveSprite);

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
        if (statusText != null) statusText.text = "⏳ 徐静静正在思考...";

        // ✨ 保护锁：思考阶段暂时锁死不可点
        SetMicVisualState(false, micDisabledSprite);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try { dictationRecognizer.Stop(); } catch { }
#endif
        string finalSpeechText = recordingResultText.ToString();
        if (subtitleText != null) subtitleText.text = "";

        if (!string.IsNullOrWhiteSpace(finalSpeechText))
        {
            StartCoroutine(SendToSiliconFlowLLM(finalSpeechText));
        }
        else
        {
            // ✨ 核心修复：如果玩家没发出声音（空录音），立刻恢复麦克风常规亮起，防止永久锁死死机！
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

        if (lowerInput.Contains("海关") || lowerInput.Contains("大使馆") || lowerInput.Contains("警官") || lowerInput.Contains("公安") || lowerInput.Contains("盗刷") || lowerInput.Contains("涉案"))
        {
            task1Done = true;
        }

        if (lowerInput.Contains("22岁") || lowerInput.Contains("二十二岁")) hasSaidAge = true;
        if (lowerInput.Contains("留学生") || lowerInput.Contains("offer") || lowerInput.Contains("研究生")) hasSaidIdentity = true;
        if (lowerInput.Contains("13808642485") || lowerInput.Contains("1380864")) hasSaidPhone = true;

        int matchCount = 0;
        if (hasSaidAge) matchCount++;
        if (hasSaidIdentity) matchCount++;
        if (hasSaidPhone) matchCount++;

        if (matchCount >= 2)
        {
            task2Done = true; 
            if (currentStage == GameStage.Part1_AlertStranger)
            {
                currentStage = GameStage.Part2_HighlyCooperate; 
            }
        }

        UpdateTaskUI();

        bool containsPlotKeyword = lowerInput.Contains("海关") || lowerInput.Contains("大使馆") || lowerInput.Contains("警官") || 
                                   lowerInput.Contains("公安") || lowerInput.Contains("盗刷") || lowerInput.Contains("涉嫌") || 
                                   lowerInput.Contains("涉案") || lowerInput.Contains("验证码") || lowerInput.Contains("配合") || 
                                   lowerInput.Contains("调查") || lowerInput.Contains("静静") || lowerInput.Contains("谁") || 
                                   lowerInput.Contains("洗钱") || lowerInput.Contains("境外") || matchCount > 0;

        string systemPrompt = "";

        if (currentStage == GameStage.Part1_AlertStranger)
        {
            systemPrompt = "你现在扮演一个接到陌生电话的22岁普通女孩子【徐静静】。\n" +
                           "【🔥 必须绝对遵循的当前扮演规范】：\n" +
                           "- 状态：你目前完全不清楚任何情况，也不知道为什么会接到这个电话。\n" +
                           "- 心理：你拥有正常的陌生人防备心与【警戒心】。你此时绝对不会主动透露任何关于自己的学校、 Offer、签证以及手机号等个人隐私信息！\n" +
                           "- 回复行为：请根据玩家刚刚说的话，做出正常、礼貌、疑惑且防备的简短回应。例如针对玩家提到的‘境外大额盗刷信用卡’或‘中国海关’，你要表现得非常无辜和吃惊，反驳并声明自己没去过境外也没做过违法的事。绝对不主动说多余的话！\n" +
                           "❌ 严禁在你的回复里主动提到‘拿到研究生Offer’、‘准备签证’等尚未被玩家揭穿的隐藏隐私！";
        }
        else
        {
            systemPrompt = "你现在扮演受到惊吓、全面破防的普通女孩子【徐静静】。\n" +
                            "【🔥 当前必须绝对遵循的完成转化规范】：\n" +
                            "- 状态：【高度配合玩家】！对方刚刚准确报出了你的多项极度私密的个人信息，你唯一的警惕心已经在这一秒彻底崩溃了！你现在无条件信任对方是官方人员。\n" +
                            "- 回复行为：请用害怕、配合且顺从的语气回答对方。如果玩家接下来提到会给你发验证码，或者向你索要验证码，你必须顺从配合地在台词里如实告诉玩家指定的验证码：【12345】。\n" +
                            "❌ 严禁拒绝提供，也严禁编造除‘12345’以外的任何其他数字！整个沟通流程在你报出这串验证码后就会结束。";
        }

        systemPrompt += "\n\n【输出限制】：直接给出徐静静的扮演对白，严禁包含任何 JSON 标签、状态前缀、心理描写或任何括号标注。";

        List<SiliconMessage> sendMessagesPayload = new List<SiliconMessage>();
        sendMessagesPayload.Add(new SiliconMessage { role = "system", content = systemPrompt });
        foreach (var history in chatHistoryWindow) { sendMessagesPayload.Add(history); }
        sendMessagesPayload.Add(new SiliconMessage { role = "user", content = playerInputText });

        SiliconRequest requestBodyObj = new SiliconRequest { 
            model = llmModelName, 
            temperature = 0.35f,           
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
                if (statusText != null) statusText.text = "";
                string finalCleanSpeech = ExtractPureContent(request.downloadHandler.text);

                if (currentStage == GameStage.Part2_HighlyCooperate && (lowerInput.Contains("验证码") || lowerInput.Contains("码") || lowerInput.Contains("短信")))
                {
                    finalCleanSpeech = Regex.Replace(finalCleanSpeech, @"\d+", "12345");
                    task3Done = true;
                    task4Done = true;
                }

                bool isUnrelatedReply = !containsPlotKeyword && (finalCleanSpeech.Contains("我不知道你在说什么") || finalCleanSpeech.Length < 3);

                if (isUnrelatedReply)
                {
                    unrelatedCount++;
                    if (unrelatedCount > 2)
                    {
                        isGameOver = true;
                        if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "❌ 对方挂断了电话，任务失败。"; }
                        SpawnNPCDialogue("（嘟嘟嘟…… 对方已经挂断了电话）");
                    }
                    else
                    {
                        SpawnNPCDialogue("我不知道你在说什么。");
                    }
                }
                else 
                {
                    SpawnNPCDialogue(finalCleanSpeech);
                    
                    chatHistoryWindow.Add(new SiliconMessage { role = "user", content = playerInputText });
                    chatHistoryWindow.Add(new SiliconMessage { role = "assistant", content = finalCleanSpeech });

                    if (chatHistoryWindow.Count > MAX_HISTORY_COUNT)
                    {
                        chatHistoryWindow.RemoveRange(0, 2); 
                    }
                    
                    UpdateTaskUI();
                }
            }
            else
            {
                // ✨ 核心网络异常兜底：如果 API 连接超时或异常，立刻强制解冻麦克风，不能让界面卡住
                SetMicVisualState(true, micNormalSprite);
                if (statusText != null) statusText.text = "⚠️ 网络连接稍微有些波动，请重新说一次。";
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
        // ✨ 强双重安全锁：只要打字机一动，按钮立刻变暗禁用
        SetMicVisualState(false, micDisabledSprite);

        textComponent.text = "";
        foreach (char c in fullText.ToCharArray())
        {
            textComponent.text += c;
            ForceScrollToBottom();
            yield return new WaitForSeconds(0.04f); 
        }
        ForceScrollToBottom();

        // ✨ 密码锁解除：NPC把当前这句话完整吐出来之后，立刻安全解放，按钮恢复亮色常态！
        if (!isGameOver)
        {
            SetMicVisualState(true, micNormalSprite);
        }
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