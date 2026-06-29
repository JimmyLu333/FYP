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

    [Header("✨ 任务笔记本 UI 组件 (请在面板中拖拽赋值)")]
    [SerializeField] private Button hintButton;         
    [SerializeField] private GameObject taskPanel;      
    [SerializeField] private Button closeTaskButton;   

    [System.Serializable]
    public struct TaskUIItem
    {
        public Image taskToggle;       // 对应对钩图片组件
        public TMP_Text taskText;      // 任务文本组件，用于控制变灰色
    }
    [SerializeField] private List<TaskUIItem> taskUiList = new List<TaskUIItem>(4); 

    [Header("Anti-Fraud Gameplay Settings")]
    private int unrelatedCount = 0; 
    private bool isGameOver = false; 

    // 后台大模型状态机阶段
    private GameStage currentStage = GameStage.Part1_AlertStranger;
    
    // 隐私项计算金手指
    private bool hasSaidAge = false;
    private bool hasSaidIdentity = false;
    private bool hasSaidPhone = false;

    // ✨ 新增：独立控制前端 4 个任务打钩状态的明线布尔开关
    private bool task1Done = false; // 冒充海关工作人员
    private bool task2Done = false; // 告诉她身份信息被盗用并取得信任
    private bool task3Done = false; // 获得她的手机验证码
    private bool task4Done = false; // 破解防火墙

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

    // ✨ 彻底重构：用独立的布尔条件控制前端秒打钩与文本颜色变浅
    void UpdateTaskUI()
    {
        bool[] taskStates = new bool[4] { task1Done, task2Done, task3Done, task4Done };

        for (int i = 0; i < taskUiList.Count; i++)
        {
            if (taskUiList[i].taskToggle == null || taskUiList[i].taskText == null) continue;

            if (taskStates[i])
            {
                // 已完成的任务：左边显示对钩，字体颜色变为灰色（参考图2效果）
                taskUiList[i].taskToggle.gameObject.SetActive(true);
                taskUiList[i].taskText.color = new Color(0.6f, 0.6f, 0.6f, 1f); 
            }
            else
            {
                // 未完成的任务：隐藏对钩，维持原色
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

        // 🎯 [明线任务1判定]：只要玩家提到了海关或官方机构，任务1立刻在前端秒勾
        if (lowerInput.Contains("海关") || lowerInput.Contains("大使馆") || lowerInput.Contains("警官") || lowerInput.Contains("公安") || lowerInput.Contains("盗刷") || lowerInput.Contains("涉案"))
        {
            task1Done = true;
        }

        // 🎯 [明线任务2中间条件计算]：收集说出的个人隐私项
        if (lowerInput.Contains("22岁") || lowerInput.Contains("二十二岁")) hasSaidAge = true;
        if (lowerInput.Contains("留学生") || lowerInput.Contains("offer") || lowerInput.Contains("研究生")) hasSaidIdentity = true;
        if (lowerInput.Contains("13808642485") || lowerInput.Contains("1380864")) hasSaidPhone = true;

        int matchCount = 0;
        if (hasSaidAge) matchCount++;
        if (hasSaidIdentity) matchCount++;
        if (hasSaidPhone) matchCount++;

        // 🎯 [明线任务2与暗线Part2状态机合流转换点]：当说出的隐私信息大于或等于两项时
        if (matchCount >= 2)
        {
            task2Done = true; // 任务2（取得信任）满足条件，立刻秒打钩变灰
            if (currentStage == GameStage.Part1_AlertStranger)
            {
                currentStage = GameStage.Part2_HighlyCooperate; // 暗线推进模型转换至配合阶段
            }
        }

        // 🎯 [明线任务3和4前置条件判定]：如果当前阶段在 Part 2 且玩家开始索要验证码或短信
        if (currentStage == GameStage.Part2_HighlyCooperate)
        {
            if (lowerInput.Contains("验证码") || lowerInput.Contains("短信") || lowerInput.Contains("发给你了"))
            {
                // 玩家执行了发送/索要操作，将任务3、4的前端打钩标记在随后的回复判定中激活
            }
        }

        // ✨ 界面秒刷新：网络层发包尚未抵达前，立刻刷新对应的勾选框！
        UpdateTaskUI();

        bool containsPlotKeyword = lowerInput.Contains("海关") || lowerInput.Contains("大使馆") || lowerInput.Contains("警官") || 
                                   lowerInput.Contains("公安") || lowerInput.Contains("盗刷") || lowerInput.Contains("涉嫌") || 
                                   lowerInput.Contains("涉案") || lowerInput.Contains("验证码") || lowerInput.Contains("配合") || 
                                   lowerInput.Contains("调查") || lowerInput.Contains("晶晶") || lowerInput.Contains("谁") || 
                                   lowerInput.Contains("洗钱") || lowerInput.Contains("境外") || matchCount > 0;

        // 🎯 3. 干净隔离的提示词系统
        string systemPrompt = "";

        if (currentStage == GameStage.Part1_AlertStranger)
        {
            systemPrompt = "你现在扮演一个接到陌生电话的22岁普通女孩子【徐晶晶】。\n" +
                           "【🔥 必须绝对遵循的当前扮演规范】：\n" +
                           "- 状态：你目前完全不清楚任何情况，也不知道为什么会接到这个电话。\n" +
                           "- 心理：你拥有正常的陌生人防备心与【警戒心】。你此时绝对不会主动透露任何关于自己的学校、 Offer、签证以及手机号等个人隐私信息！\n" +
                           "- 回复行为：请根据玩家刚刚说的话，做出正常、礼貌、疑惑且防备的简短回应。例如针对玩家提到的‘境外大额盗刷信用卡’或‘中国海关’，你要表现得非常无辜和吃惊，反驳并声明自己没去过境外也没做过违法的事。绝对不主动说多余的话！\n" +
                           "❌ 严禁在你的回复里主动提到‘拿到研究生Offer’、‘准备签证’等尚未被玩家揭穿的隐藏隐私！";
        }
        else
        {
            systemPrompt = "你现在扮演受到惊吓、全面破防的普通女孩子【徐晶晶】。\n" +
                            "【🔥 当前必须绝对遵循的完成转化规范】：\n" +
                            "- 状态：【高度配合玩家】！对方刚刚准确报出了你的多项极度私密的个人信息，你唯一的警惕心已经在这一秒彻底崩溃了！你现在无条件信任对方是官方人员。\n" +
                            "- 回复行为：请用害怕、配合且顺从的语气回答对方。如果玩家接下来提到会给你发验证码，或者向你索要验证码，你必须顺从配合地在台词里如实告诉玩家指定的验证码：【12345】。\n" +
                            "❌ 严禁拒绝提供，也严禁编造除‘12345’以外的任何其他数字！整个沟通流程在你报出这串验证码后就会结束。";
        }

        systemPrompt += "\n\n【输出限制】：直接给出徐晶晶的扮演对白，严禁包含任何 JSON 标签、状态前缀、心理描写或任何括号标注。";

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
                if (statusText != null) statusText.gameObject.SetActive(false);
                string finalCleanSpeech = ExtractPureContent(request.downloadHandler.text);

                // 客户端正则表达式物理硬强行拦截验证码替换
                if (currentStage == GameStage.Part2_HighlyCooperate && (lowerInput.Contains("验证码") || lowerInput.Contains("码") || lowerInput.Contains("短信")))
                {
                    finalCleanSpeech = Regex.Replace(finalCleanSpeech, @"\d+", "12345");
                    
                    // 🎯 [明线任务3和4判定]：既然模型在 Part 2 已经吐出了数字，证明任务顺利大功告成
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
                    else SpawnNPCDialogue("我不知道你在说什么。");
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
                    
                    // 最终刷新一遍 UI 状态
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
    [System.Serializable] class SiliconRequest { public string model; public List<SiliconMessage> messages; public float temperature; public float repetition_penalty; }
}