using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MESStatusReader : MonoBehaviour
{
    public Text resultText;
    public GPTChatbot chatbot;

    private string apiUrl = "http://localhost:3000/status";
    private float timer = 0f;

    public float currentTemp;
    public float currentVib;

    private int abnormalCount = 0;
    private const int warningThreshold = 3;
    private bool hasAlertedOnce = false; // 1회 경고 여부

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 3f)
        {
            timer = 0f;
            StartCoroutine(RequestStatus());
        }
    }

    public void GetStatus()
    {
        StartCoroutine(RequestStatus());
    }

    private float previousVib = -1f;

    IEnumerator RequestStatus()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            MESStatus status = JsonUtility.FromJson<MESStatus>(request.downloadHandler.text);
            currentTemp = status.temperature;
            currentVib = status.vibration;

            string tempStr = $"온도: {currentTemp}°C";
            string vibStr = $"진동 수치: {currentVib} ({(currentVib > 1.0f ? "비정상" : "정상")})";
            resultText.text = tempStr + "\n" + vibStr;

            // 동일한 값이면 중복으로 판단 안함
            if (currentVib > 1.0f && currentVib != previousVib)
            {
                abnormalCount++;
                previousVib = currentVib;

                if (!hasAlertedOnce && chatbot != null)
                {
                    chatbot.ReceiveAutoAlert("비정상 진동이 감지되었습니다. 정지할까요?");
                    hasAlertedOnce = true;
                }

                if (abnormalCount >= warningThreshold && chatbot != null)
                {
                    chatbot.ReceiveAutoAlert("진동이 3회 연속 비정상입니다. 예지보전 점검이 필요합니다. 점검할까요?");
                    abnormalCount = 0;
                    hasAlertedOnce = false;
                }
            }
            else if (currentVib <= 1.0f)
            {
                abnormalCount = 0;
                hasAlertedOnce = false;
                previousVib = currentVib;
            }
        }
        else
        {
            resultText.text = "요청 실패: " + request.error;
        }
    }
}

[System.Serializable]
public class MESStatus
{
    public float temperature;
    public float vibration;
}
