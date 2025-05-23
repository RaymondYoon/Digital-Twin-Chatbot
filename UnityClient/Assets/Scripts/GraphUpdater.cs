using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using UnityEngine.UI;

[System.Serializable]
public class SensorLog
{
    public string timestamp;
    public float vibration;
    public float temperature;
}

public class GraphUpdater : MonoBehaviour
{
    public GraphUIRenderer graphUIRenderer;
    public Text explanationText;

    private string apiUrl = "http://localhost:3000/logs";

    void Start()
    {
    }
    public void UpdateGraph()
    {
        StartCoroutine(FetchAndDraw());
    }

    public List<float> lastVibrationLogs = new();

    public IEnumerator FetchAndDraw()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string wrappedJson = "{\"logs\":" + request.downloadHandler.text + "}";
            SensorLogWrapper wrapper = JsonUtility.FromJson<SensorLogWrapper>(wrappedJson);

            if (wrapper == null || wrapper.logs == null || wrapper.logs.Count == 0)
            {
                explanationText.text = "데이터 없음";
                yield break;
            }

            List<float> vibValues = wrapper.logs.Select(l => l.vibration).ToList();
            lastVibrationLogs = vibValues.TakeLast(4).ToList(); // 최근 4개 저장

            graphUIRenderer.DrawGraph(vibValues);
            explanationText.text = $"최근 진동 평균: {vibValues.Average():F2}, 최대: {vibValues.Max():F2}";
        }
        else
        {
            explanationText.text = "로그 요청 실패: " + request.error;
        }
    }


    [System.Serializable]
    public class SensorLogWrapper
    {
        public List<SensorLog> logs;
    }
}
