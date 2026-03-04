using UnityEngine;
using TMPro;

public class PlayTimeHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;

    void Update()
    {
        // StageManager가 없으면 아무것도 안 함 (메인메뉴 등 안전장치)
        if (StageManager.Instance != null && timeText != null)
        {
            float t = StageManager.Instance.currentPlayTime;
            timeText.text = StageManager.Instance.GetFormattedTime(t);
        }
    }
}