using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech; // Windows 本地免流量语音识别核心
#endif

public class GameplayVoiceController : MonoBehaviour
{
    [Header("API Config (极速轻量对齐版)")]
    // 📢 确保在 Unity 面板中填入你那串正确的 sk-d701e663da59432f831jutv 密鑰
    public string apiKey = "sk-xxxxxxxxxxxxxxxxxxxx"; 
    private string llmUrl = "https://api.siliconflow.cn/v1/chat/completions";
    
    // 🎯 终极升级：换用响应速度极快的 1.5B 节点，专治公共/免费通道的响应超时问题
    // 🎯 换用全网最稳、对免费用户完全不设防的核心旗舰节点
    private string llmModelName = "Qwen/Qwen2.5-7B-Instruct";
    
    [Header("UI Prefabs (氣泡動態渲染)")]
    public GameObject playerBubblePrefab; 
    public GameObject npcBubblePrefab;    
    public Transform chatContentTrans;    

    [Header("Visual Elements")]
    public TMP_Text statusText;            
    public GameObject voiceWaveObject;     
    public Button micButton;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private DictationRecognizer dictationRecognizer;
#endif
    private bool isRecording = false;
    private StringBuilder recordingResultText = new StringBuilder();

    void Start()
    {
        micButton.onClick.AddListener(OnMicButtonClick);
        if (voiceWaveObject != null) voiceWaveObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false); 

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationResult += (text, confidence) => {
            recordingResultText.Append(text);
        };
        dictationRecognizer.DictationHypothesis += (text) => {
            if (statusText != null) statusText.text = "正在傾聽: " + text;
        };
#endif
        SpawnNPCDialogue("你好，你是？");
    }

    void OnMicButtonClick()
    {
        if (!isRecording)
        {
            isRecording = true;
            recordingResultText.Clear();
            if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "🔴 正在錄音中，請說話..."; }
            if (voiceWaveObject != null) voiceWaveObject.SetActive(true);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            dictationRecognizer.Start();
#endif
        }
        else
        {
            isRecording = false;
            if (voiceWaveObject != null) voiceWaveObject.SetActive(false);
            if (statusText != null) statusText.text = "⏳ 徐晶晶正在思考...";

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            dictationRecognizer.Stop();
#endif
            string finalSpeechText = recordingResultText.ToString();

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
    }

    IEnumerator SendToSiliconFlowLLM(string speechText)
    {
        // 🎯 锚点控型 Prompt：让大模型在最开始死死绑住前缀，瞬间掐断冗长的无用思考链
        string systemPrompt = "你现在扮演模拟反诈游戏中的受害者NPC【徐晶晶】。她是一个普通女孩，目前接到了一个陌生电话。玩家是一名企图通过伪造身份或理由欺骗她的诈骗犯。" +
                              "请对玩家刚刚说的话进行合理性逻辑判定。你必须严格且仅以下列格式返回她的心理台词，严禁包含任何 JSON 或 markdown 标签：\n" +
                              "徐晶晶回复：这里写你要对诈骗犯说的回话台词";

        SiliconRequest requestBodyObj = new SiliconRequest();
        requestBodyObj.model = llmModelName;
        requestBodyObj.messages = new List<SiliconMessage>()
        {
            new SiliconMessage { role = "system", content = systemPrompt },
            new SiliconMessage { role = "user", content = speechText }
        };
        requestBodyObj.temperature = 0.4f;

        string finalJsonPayload = JsonUtility.ToJson(requestBodyObj);

        using (UnityWebRequest request = new UnityWebRequest(llmUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(finalJsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // 设置超时时间为 15 秒，防止免费账户被无限期挂起
            request.timeout = 15;

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return request.SendWebRequest();

          if (request.result == UnityWebRequest.Result.Success)
            {
                if (statusText != null) statusText.gameObject.SetActive(false);
                
                string rawResponse = request.downloadHandler.text;
                Debug.Log($"【LLM 原生网络返回】: {rawResponse}");

                // 🎯 核心修复：只清洗大模型返回的 NPC 文本，不触碰、也不覆盖已经生成的玩家气泡
                string finalCleanSpeech = ExtractPureContent(rawResponse);
                
                // 100% 顺畅生成左侧受害者徐晶晶的独立回话气泡
                SpawnNPCDialogue(finalCleanSpeech);
            }
            else
            {
                if (statusText != null) statusText.text = "❌ 信号不良，对方似乎挂断了...";
                Debug.LogError($"【网络拦截异常】代碼: {request.responseCode}, 詳情: {request.downloadHandler.text}");
            }
        }
    }

    // 🎯 极简物理剥离器：安全切掉所有冗余的 OpenAI 包装壳和格式前缀
    string ExtractPureContent(string json)
    {
        try
        {
            Match match = Regex.Match(json, @"_?\""content\""\s*:\s*\""([^\""\\]*(?:\\.[^\""\\]*)*)\""");
            if (match.Success)
            {
                string content = match.Groups[1].Value;
                content = Regex.Unescape(content);
                
                // 强力拔出我们规定的“徐晶晶回复：”后面的真正剧本文字
                if (content.Contains("徐晶晶回复："))
                {
                    int index = content.IndexOf("徐晶晶回复：") + 6;
                    return content.Substring(index).Replace("`", "").Trim();
                }
                
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
            yield return new WaitForSeconds(0.05f); 
        }
    }

    void ForceScrollToBottom()
    {
        if (chatContentTrans == null) return;
        Canvas.ForceUpdateCanvases();
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