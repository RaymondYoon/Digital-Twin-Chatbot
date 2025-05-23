using UnityEngine;

public class ChatbotUIController : MonoBehaviour
{
    // 에디터에서 드래그할 MainPanel (Left+Right+Bottom 모두 묶어 놓은 오브젝트)
    public GameObject mainPanel;
    // 버튼 텍스트를 바꾸고 싶다면 추가
    public UnityEngine.UI.Text buttonText;

    // 버튼 OnClick 에 이 함수를 연결
    public void ToggleChatbot()
    {
        bool isActive = mainPanel.activeSelf;
        mainPanel.SetActive(!isActive);
        if (buttonText != null)
            buttonText.text = isActive ? "챗봇 On" : "챗봇 Off";
    }
}
