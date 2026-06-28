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
    [Header("API Config (极速轻量对齐版)")]
    public string apiKey = "sk-xxxxxxxxxxxxxxxxxxxx"; 
    private string llmUrl = "https://api.siliconflow.cn/v1/chat/completions";
    private string llmModelName = "Qwen/Qwen2.5-7B-Instruct";

    [Header("UI Prefabs (氣泡動態渲染)")]
    public GameObject playerBubblePrefab; 
    public GameObject npcBubblePrefab;    
    public Transform chatContentTrans;    

    [Header("Visual Elements")]
    public TMP_Text statusText;            
    public GameObject voiceWaveObject;     
    public Button micButton;

    [Header("New UI Elements (⚠️ 请在面板中拖拽赋值)")]
    [SerializeField] private TMP_Text subtitleText; // 麦克风上方的实时文字
    [SerializeField] private TMP_Text timerText;    // 麦克风下方的倒计时组件

    [Header("Anti-Fraud Gameplay Settings")]
    private int unrelatedCount = 0; 
    private bool isGameOver = false; 

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private DictationRecognizer dictationRecognizer;
#endif
    private bool isRecording = false;
    private StringBuilder recordingResultText = new StringBuilder();
    private Coroutine recordingTimerCoroutine;
    private const float MAX_RECORDING_TIME = 60f; 

    void Start()
    {
        micButton.onClick.AddListener(OnMicButtonClick);
        if (voiceWaveObject != null) voiceWaveObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false); 
        
        // 初始化新 UI
        if (subtitleText != null) subtitleText.text = "";
        if (timerText != null) timerText.gameObject.SetActive(false);

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
            if (statusText != null) statusText.text = "正在傾聽...";
        };
        dictationRecognizer.DictationComplete += (completionCause) => {
            // 修复编译报错，当非正常完结且仍在录音状态时重启录音组件
            if (isRecording && completionCause != DictationCompletionCause.Complete)
            {
                try { dictationRecognizer.Start(); } catch { }
            }
        };
#endif
        SpawnNPCDialogue("喂？您好，请问您是哪位？有什么事吗？");
    }

    void OnMicButtonClick()
    {
        if (isGameOver)
        {
            if (statusText != null) statusText.text = "❌ 电话已挂断，任务失败，请重新开始游戏。";
            return;
        }

        if (!isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecordingAndSubmit();
        }
    }

    void StartRecording()
    {
        isRecording = true;
        recordingResultText.Clear();
        
        if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "🔴 正在錄音中..."; }
        if (voiceWaveObject != null) voiceWaveObject.SetActive(true);
        if (subtitleText != null) subtitleText.text = "准备倾听...";
        
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = "00:00 / 01:00";
        }

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
        if (timerText != null) timerText.gameObject.SetActive(false);
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
        else
        {
            if (statusText != null) statusText.text = "未能聽清話術，請重試";
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
            if (timerText != null)
            {
                timerText.text = string.Format("00:{0:D2} / 01:00", seconds);
            }
        }

        if (timerText != null) timerText.text = "01:00 / 01:00";
        StopRecordingAndSubmit();
    }

    IEnumerator SendToSiliconFlowLLM(string speechText)
    {
        // 简化大模型的输出负担，不再逼迫它吐出复杂的自定义前缀标记
        string systemPrompt = 
            "你现在扮演模拟反诈游戏中的受害者NPC【徐晶晶】。\n" +
            "【NPC画像】：\n" +
            "- 姓名：徐晶晶，年龄：22岁，国际留学生，刚获得海外研究生Offer。\n" +
            "- 心理状态：第一次长期独自在海外生活，学费由父母准备。遇到突发事件极容易紧张。\n" +
            "- 软肋：高度重视研究生录取资格；担心签证和身份问题；缺乏法律知识，容易被权威身份影响和恐吓。\n\n" +
            "【剧情背景与判定规则】：\n" +
            "玩家是一名伪装身份的诈骗犯。流程步骤包括：1.冒充海关/大使馆建立信任；2.引导其相信身份被盗用；3.诱导进入案件验证系统；4.索要验证码（信任后可给出'123456'）。\n" +
            "如果玩家说的话与上述反诈剧情主题（公检法、海关、大使馆、核对身份、涉及案件、验证码等）完全无关（例如聊到天气、日常寒暄、或者不知所云），你必须严格且仅回复这句话，不要带任何多余字眼：“我不知道你在说什么。”\n" +
            "如果玩家说的话顺应上述诈骗流程（例如：‘我是中国海关’、‘我是大使馆的’、‘你涉嫌信息泄露’），请结合徐晶晶胆小、容易被权威吓到的性格，给出合理的、害怕又配合的对话反应。\n\n" +
            "【输出格式限制】：\n" +
            "请直接输出你对玩家的回话台词，严禁包含任何 JSON 标签、markdown 标记或前缀。";

        SiliconRequest requestBodyObj = new SiliconRequest();
        requestBodyObj.model = llmModelName;
        requestBodyObj.messages = new List<SiliconMessage>()
        {
            new SiliconMessage { role = "system", content = systemPrompt },
            new SiliconMessage { role = "user", content = speechText }
        };
        requestBodyObj.temperature = 0.2f; // 调低随机性，确保严格遵从剧本约束

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
                
                string rawResponse = request.downloadHandler.text;
                string finalCleanSpeech = ExtractPureContent(rawResponse);

                // ✨ 客户端鲁棒性安全双重判定机制
                bool isUnrelatedReply = finalCleanSpeech.Contains("我不知道你在说什么") || 
                                        finalCleanSpeech == "我不知道你在说什么。" || 
                                        finalCleanSpeech.Length < 3; // 过滤极端过短返回

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
                    // 顺应正常反诈剧情，输出徐晶晶害怕、动摇的对白
                    SpawnNPCDialogue(finalCleanSpeech);
                }
            }
            else
            {
                if (statusText != null) statusText.text = "❌ 信号不良，对方似乎挂断了...";
                Debug.LogError($"【网络拦截异常】: {request.downloadHandler.text}");
            }
        }
    }

    string ExtractPureContent(string json)
    {
        try
        {
            Match match = Regex.Match(json, @"_?\""content\""\s*:\s*\""([^\""\\]*(?:\\.[^\""\\]*)*)\""");
            if (match.Success)
            {
                string content = match.Groups[1].Value;
                content = Regex.Unescape(content);
                
                content = content.Replace("`", "").Replace("{", "").Replace("}", "").Replace("\"", "");
                return content.Trim();
            }
        }
        catch { }
        return "（喂？说话呀，不说我挂了啊...）";
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
    [System.Serializable] class SiliconRequest { public string model; public List<SiliconMessage> messages; public float temperature; }
}