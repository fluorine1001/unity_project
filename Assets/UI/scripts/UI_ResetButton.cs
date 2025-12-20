using UnityEngine;

public class UI_ResetButton : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("리셋 버튼 클릭 시 닫아야 할 패널 (예: Panel 또는 MainPage)")]
    public GameObject panelToHide;

    // 버튼 클릭 시 호출될 함수 (Inspector 연결용)
    public void OnClickReset()
    {
        // 1. StageManager를 통해 게임 상태 리셋
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ResetGamePartial();
        }
        else
        {
            Debug.LogError("[UI_ResetButton] StageManager 인스턴스를 찾을 수 없습니다.");
        }

        // 2. 메뉴 패널 닫기 (게임 화면으로 복귀)
        if (panelToHide != null)
        {
            panelToHide.SetActive(false);
        }
    }
}