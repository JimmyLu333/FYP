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
    // ==================== 🛠️ NPC 剧本与任务配置 ====================
    [System.Serializable]
    public class NPCRolePlayProfile
    {
        public string npcName;            // NPC名字，例如：徐晶晶
        [TextArea(3, 5)]
        public string personalityAndBg;  // 性格和背景（System Prompt）
        [TextArea(2, 4)]
        public string currentTask;        // 当前玩家必须完成的诈骗话题任务
        public string failWarning;        // 失败/怀疑时的兜底台词
    }

    [Header("Game Stage Config (多NPC剧本配置)")]
    public List<NPCRolePlayProfile> npcStages = new List<NPCRolePlayProfile>();
    public int currentStageIndex = 0;    // 当前进行到第几个NPC关卡

    [Header("API Config")]
    public string apiKey = "sk-xxxxxxxxxxxxxxxxxxxx"; // 👈 确保填入你真实的 sk-d...jutv
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

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private DictationRecognizer dictationRecognizer;
#endif
    private bool isRecording = false;
    private StringBuilder recordingResultText = new StringBuilder();
    private bool isGameOver = false;

    void Start()
    {
        micButton.onClick.AddListener(OnMicButtonClick);
        if (voiceWaveObject != null) voiceWaveObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false); 

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationResult += (text, confidence) => { recordingResultText.Append(text); };
        dictationRecognizer.DictationHypothesis += (text) => { if (statusText != null) statusText.text = "正在倾听: " + text; };
#endif
        // 游戏启动，自动拉起第一个 NPC 的开场白
        StartCurrentStage();
    }

    // 🎯 启动当前关卡 NPC
    void StartCurrentStage()
    {
        if (currentStageIndex >= npcStages.Count)
        {
            SpawnNPCDialogue("【系统提示】：你已成功骗过所有目标，完成了跨国电信网络犯罪的所有初步线索……游戏通关。");
            if (statusText != null) statusText.text = "🎉 达成全通关";
            isGameOver = true;
            return;
        }

        NPCRolePlayProfile currentNPC = npcStages[currentStageIndex];
        isGameOver = false;
        
        // 渲染引导状态
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = $"<color=orange>当前任务：对【{currentNPC.npcName}】实施诈骗。核心话题要求：{currentNPC.currentTask}</color>";
        }

        // 让 NPC 主动说话（第一句问候）
        SpawnNPCDialogue($"（电话接通中...）喂？你好，请问你是哪位？");
    }

    void OnMicButtonClick()
    {
        if (isGameOver) return;

        if (!isRecording)
        {
            isRecording = true;
            recordingResultText.Clear();
            if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "🔴 正在录音中，请对准任务线索说话..."; }
            if (voiceWaveObject != null) voiceWaveObject.SetActive(true);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            dictationRecognizer.Start();
#endif
        }
        else
        {
            isRecording = false;
            if (voiceWaveObject != null) voiceWaveObject.SetActive(false);
            if (statusText != null) statusText.text = "⏳ NPC 正在判定你的话术逻辑...";

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            dictationRecognizer.Stop();
#endif
            string finalSpeechText = recordingResultText.ToString();

            if (!string.IsNullOrWhiteSpace(finalSpeechText))
            {
                // 先在界面上蹦出玩家说的话（右侧气泡）
                SpawnPlayerDialogue(finalSpeechText);
                // 送入大模型进行“剧本+任务双重判定”
                StartCoroutine(SendToSiliconFlowLLM(finalSpeechText));
            }
            else
            {
                if (statusText != null) statusText.text = "未能听清，请重试";
            }
        }
    }

    IEnumerator SendToSiliconFlowLLM(string speechText)
    {
        NPCRolePlayProfile currentNPC = npcStages[currentStageIndex];

        // 🎯 超强系统 Prompt：强行命令大模型在思考性格的同时，切入裁判视角判定 Task 是否完成
        string systemPrompt = $"你现在扮演模拟反诈serious game中的受害者NPC【{currentNPC.npcName}】。 " +
                              $"你的角色设定和背景为：{currentNPC.personalityAndBg}\n\n" +
                              $"【🚨 核心关卡任务规则】：目前玩家（诈骗犯）对你展开了对话。你必须严格审查玩家刚才说的话，是否包含或切中了以下话题或要求：\"{currentNPC.currentTask}\"。\n\n" +
                              $"你必须严格且仅以下列标准的 JSON 格式做出回应，严禁包含任何 markdown 标签或多余正文：\n" +
                              "{" +
                              "\"isTaskPassed\": true或false (如果玩家说的话完全符合或者切中了核心话题要求，填true；如果完全扯淡、跑题、没切中话术核心，填false)," +
                              "\"isHangUp\": true或false (如果上面的isTaskPassed为false，说明玩家露馅了，你感到非常怀疑或生气，直接强行挂断电话填true；如果为true，则继续填false)," +
                              $"\"reply\": \"符合你性格的回复台词。注意，如果isHangUp为true，请留下一句愤怒或惊慌的挂断狠话，例如：'{currentNPC.failWarning}'\"" +
                              "}";

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
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawResponse = request.downloadHandler.text;
                string contentJson = ExtractJsonContent(rawResponse);
                
                try
                {
                    // 🎯 解析裁判结果
                    DecisionModel decision = JsonUtility.FromJson<DecisionModel>(contentJson);
                    
                    // 1. 让 NPC 渲染说话
                    SpawnNPCDialogue(decision.reply);

                    // 2. 状态检查：NPC 决定挂断（任务失败）
                    if (decision.isHangUp)
                    {
                        isGameOver = true;
                        if (statusText != null) statusText.text = "<color=red>❌ 诈骗露馅，对方已挂断！请重新开始本关卡。</color>";
                        // 3秒后自动重置当前关卡
                        StartCoroutine(ResetStageDelayed());
                    }
                    // 3. 状态检查：任务顺利完成，晋级下一步
                    else if (decision.isTaskPassed)
                    {
                        if (statusText != null) statusText.text = "<color=green>🟢 成功切中弱点！目标已上钩，正在转入下一阶段...</color>";
                        StartCoroutine(NextStageDelayed());
                    }
                    else
                    {
                        if (statusText != null) statusText.text = $"<color=yellow>提示：聊得还行，但尚未切中任务核心（需提到：{currentNPC.currentTask}）</color>";
                    }
                }
                catch
                {
                    // 强力兜底
                    SpawnNPCDialogue("（对方听着你的话，陷入了长久的沉默...）");
                }
            }
            else
            {
                if (statusText != null) statusText.text = "❌ 信号中断...";
            }
        }
    }

    IEnumerator ResetStageDelayed()
    {
        yield return new WaitForSeconds(3.5f);
        StartCurrentStage(); // 重新加载当前关卡
    }

    IEnumerator NextStageDelayed()
    {
        yield return new WaitForSeconds(4f);
        currentStageIndex++; // 晋级到下一个 NPC 关卡
        StartCurrentStage();
    }

    string ExtractJsonContent(string rawJson)
    {
        try
        {
            int firstIndex = rawJson.IndexOf("\"content\":\"");
            if (firstIndex < 0) firstIndex = rawJson.IndexOf("\"content\": \"");
            
            if (firstIndex >= 0)
            {
                int startPos = rawJson.IndexOf(":", firstIndex) + 1;
                string contentSnippet = rawJson.Substring(startPos);
                int endPos = contentSnippet.LastIndexOf("\"");
                if (endPos > 0) contentSnippet = contentSnippet.Substring(0, endPos);
                
                contentSnippet = contentSnippet.Replace("\\\"", "\"").Replace("\\n", "").Replace("\\t", "");
                Match match = Regex.Match(contentSnippet, @"\{.*\}", RegexOptions.Singleline);
                if (match.Success) return match.Value.Trim();
                return contentSnippet.Trim();
            }
            return rawJson;
        }
        catch { return rawJson; }
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
    
    // 🎯 裁判映射实体
    [System.Serializable] 
    class DecisionModel 
    { 
        public bool isTaskPassed; 
        public bool isHangUp; 
        public string reply; 
    }
}