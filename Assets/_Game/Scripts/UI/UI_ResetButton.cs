using UnityEngine;

public class UI_ResetButton : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("비상용: UIManager가 없을 때 닫을 패널")]
    public GameObject panelToHide;

    public void OnClickReset()
    {
        // 1. StageManager 리셋
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ResetGamePartial();
        }

        // 2. 메뉴 패널 닫기 (UIManager.Instance 사용)
        // ✅ [수정] FindObjectOfType 대신 Instance로 즉시 접근
        if (UIManager.Instance != null)
        {
            // (닫기: false, 소리 끄기: false)
            UIManager.Instance.bookPanel(false, false);
        }
        else
        {
            // 만약 UIManager가 없다면(예외 상황) 직접 끔
            if (panelToHide != null)
            {
                panelToHide.SetActive(false);
            }
        }
    }
}