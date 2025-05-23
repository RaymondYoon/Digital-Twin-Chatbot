using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;
using System.Text;

public class GPTChatbot : MonoBehaviour
{
    public InputField inputField;
    public Text modeStatusText;
    public Dropdown roleDropdown;
    public ARMWaypointMovement amr;
    public MESStatusReader mesReader;

    [TextArea]
    public string systemPrompt = "너는 공장 시뮬레이터의 지능형 제어 챗봇이야. AMR 상태와 설비 센서 값을 참고해서 도와줘.";

    private string apiKey;
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    private bool awaitingResponse = false;
    private string currentRole = "작업자";

    [Header("Chat UI")]
    public Button sendButton;
    public RectTransform content;
    public ScrollRect scrollRect;
    public GameObject userBubblePrefab;
    public GameObject botBubblePrefab;

    [Header("Right Detail Panel")]
    public GraphUpdater graphUpdater;
    public Text detailText;

    void Start()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("apikey");
        if (jsonText != null)
        {
            ApiKeyData data = JsonUtility.FromJson<ApiKeyData>(jsonText.text);
            apiKey = data.openai_api_key;
            Debug.Log("API Key 로드 성공");
        }
        else
        {
            Debug.LogError("apikey.json 로드 실패! Resources 폴더에 위치하고 있는지 확인하세요.");
        }

        amr.enabled = false;
        var manual = amr.GetComponent<ARMMovement>();
        if (manual != null) manual.enabled = true;
        modeStatusText.text = "모드 상태: 수동";

        OnRoleChanged(roleDropdown.value);
        roleDropdown.onValueChanged.AddListener(OnRoleChanged);
        sendButton.onClick.AddListener(OnSubmit);
    }

    public void OnRoleChanged(int idx)
    {
        switch (idx)
        {
            case 0: currentRole = "작업자"; break;
            case 1: currentRole = "관리자"; break;
            case 2: currentRole = "엔지니어"; break;
        }
    }

    private bool HasPermission(string action)
    {
        switch (currentRole)
        {
            case "작업자": return action == "정지" || action == "시작";
            case "관리자": return true;
            case "엔지니어": return action == "정지" || action == "진단";
            default: return false;
        }
    }

    public void OnSubmit()
    {
        string msg = inputField.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        AddUserBubble(msg);
        inputField.text = "";
        StartCoroutine(SendChatRequest(msg, ComposePrompt(msg, false)));
    }

    private void AddUserBubble(string text)
    {
        var go = Instantiate(userBubblePrefab, content);
        go.transform.SetAsLastSibling();
        var txt = go.GetComponentInChildren<Text>();
        if (txt != null) txt.text = text;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void AddBotBubble(string text)
    {
        var go = Instantiate(botBubblePrefab, content);
        go.transform.SetAsLastSibling();
        var txt = go.GetComponentInChildren<Text>();
        if (txt != null) txt.text = text;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    IEnumerator SendChatRequest(string message, string prompt)
    {
        // 명령어 먼저 처리 (정지, 시작 등)
        if (message.Contains("정지")) { HandleAction("정지"); yield break; }
        if (message.Contains("시작")) { HandleAction("시작"); yield break; }
        if (message.Contains("진단")) { HandleAction("진단"); yield break; }
        if (message.Contains("수동")) { HandleAction("수동"); yield break; }
        if (message.Contains("자동")) { HandleAction("자동"); yield break; }
        if (message.Contains("하역")) { HandleAction("하역"); yield break; }
        if (message.Contains("적재")) { HandleAction("적재"); yield break; }

        // GPT API 요청
        ChatRequest reqData = new ChatRequest(prompt, message);
        string bodyJson = JsonUtility.ToJson(reqData);

        UnityWebRequest req = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<GPTResponse>(req.downloadHandler.text);
            string reply = (res.choices != null && res.choices.Length > 0)
                ? res.choices[0].message.content.Trim()
                : "응답이 없습니다.";

            AddBotBubble(reply);

            // 오른쪽 상세창 업데이트
            if (detailText != null)
            {
                string summary = ExtractSummaryForDetail(reply);
                string prediction = PredictiveInsight();
                string logs = string.Join(", ", graphUpdater.lastVibrationLogs.Select(v => v.ToString("F2")));
                detailText.text = summary + "\n\n최근 진동 로그: " + logs + "\n" + prediction;
            }

            // 그래프 ON/OFF
            if (message.Contains("진동") || message.Contains("그래프") || message.Contains("현황"))
            {
                if (graphUpdater != null)
                {
                    graphUpdater.gameObject.SetActive(true);
                    graphUpdater.UpdateGraph();
                }
            }
            else
            {
                if (graphUpdater != null)
                {
                    graphUpdater.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            AddBotBubble($"<Error {req.responseCode}>");
        }
    }


    private string ExtractSummaryForDetail(string gptReply)
    {
        if (string.IsNullOrEmpty(gptReply)) return "";

        int idx = gptReply.IndexOf(".");
        if (idx >= 0 && idx < gptReply.Length)
            return gptReply.Substring(0, idx + 1);

        return gptReply.Length > 100 ? gptReply.Substring(0, 100) + "..." : gptReply;
    }

    private string PredictiveInsight()
    {
        if (graphUpdater.lastVibrationLogs.Count < 2) return "";

        float avg = graphUpdater.lastVibrationLogs.Average();
        if (avg > 1.5f)
            return "진동 수치가 높습니다. 베어링 마모 가능성이 있습니다.";
        else if (avg > 1.0f)
            return "주의: 진동이 상승 추세입니다. 정기 점검을 권장합니다.";
        else
            return "정상 범위입니다. 안정적으로 운영 중입니다.";
    }

    private void HandleAction(string action)
    {
        if (!HasPermission(action) && action != "수동" && action != "자동" && action != "적재" && action != "하역")
        {
            AddBotBubble($"[{currentRole}] 역할로는 '{action}' 수행 불가");
            return;
        }

        switch (action)
        {
            case "정지":
                amr.enabled = false;
                AddBotBubble("AMR을 정지했습니다.");
                break;

            case "시작":
                amr.enabled = true;
                AddBotBubble("AMR을 다시 시작했습니다.");
                break;

            case "진단":
                StartCoroutine(SendChatRequest("진단 요청", ComposePrompt("진단 요청", false)));
                break;

            case "수동":
                amr.enabled = false;
                var manual = amr.GetComponent<ARMMovement>();
                if (manual != null) manual.enabled = true;
                AddBotBubble("수동 모드로 전환했습니다.");
                modeStatusText.text = "모드 상태: 수동";
                break;

            case "자동":
                var manual2 = amr.GetComponent<ARMMovement>();
                if (manual2 != null) manual2.enabled = false;
                amr.enabled = true;
                AddBotBubble("자동 모드로 전환했습니다.");
                modeStatusText.text = "모드 상태: 자동";
                break;

            case "하역":
                if (amr.hasPallet)
                {
                    amr.ForceUnload();
                    AddBotBubble("수동으로 하역을 수행했습니다.");
                }
                else
                {
                    AddBotBubble("하역 실패: 현재 적재된 팔레트가 없습니다.");
                }
                break;

            case "적재":
                if (amr.hasPallet)
                {
                    AddBotBubble("이미 팔레트를 적재하고 있습니다.");
                }
                else
                {
                    var pallet = GameObject.FindGameObjectWithTag("Pallet");
                    if(pallet != null)
                    {
                        amr.ForceLoad(pallet);
                        AddBotBubble("수동으로 적재를 수행했습니다.");
                    }
                    else
                    {
                        AddBotBubble("적재 실패: 주변에 팔레트가 없습니다.");
                    }
                }
                break;
        }
    }

    public void ReceiveAutoAlert(string msg)
    {
        AddBotBubble(msg);
        awaitingResponse = true;
    }

    private string ComposePrompt(string userMessage, bool isAlert = false)
    {
        string vib = mesReader.currentVib == 0 ? "불러오지 못함" : mesReader.currentVib.ToString();
        string status = amr.GetStatusSummary();
        return systemPrompt + "\n" +
               $"현재 사용자는 {currentRole}입니다.\n" +
               $"상태: {status}, 진동: {vib}, 온도: {mesReader.currentTemp}\n" +
               (isAlert ? $"경고 응답: {userMessage}" : "");
    }

    [System.Serializable] public class ChatMessage { public string role; public string content; public ChatMessage(string role, string content) { this.role = role; this.content = content; } }
    [System.Serializable] public class ChatRequest { public string model = "gpt-3.5-turbo"; public List<ChatMessage> messages; public ChatRequest(string prompt, string userMsg) { messages = new List<ChatMessage> { new ChatMessage("system", prompt), new ChatMessage("user", userMsg) }; } }
    [System.Serializable] public class GPTResponse { public Choice[] choices; }
    [System.Serializable] public class Choice { public Message message; }
    [System.Serializable] public class Message { public string role, content; }
    [System.Serializable] public class ApiKeyData { public string openai_api_key; }
}
