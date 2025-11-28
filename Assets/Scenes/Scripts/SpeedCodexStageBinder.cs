// SpeedCodexStageBinder.cs
using UnityEngine;

public class SpeedCodexStageBinder : MonoBehaviour
{
    [Header("UI")]
    public SpeedCodexUI codexUI;

    [Header("스테이지별 패턴")]
    public SpeedCodexEntry[] stageEntries;

    private int lastStage = -1;

    private void Update()
    {
        if (StageManager.Instance == null || codexUI == null)
            return;

        int stage = StageManager.Instance.currentStage; // 현재 스테이지 인덱스
        if (stage == lastStage) return; // 변화 없으면 패스

        lastStage = stage;

        // 배열 범위 체크
        if (stage >= 0 && stage < stageEntries.Length)
        {
            codexUI.SetEntry(stageEntries[stage]);
        }
        else
        {
            // 범위 밖이면 패턴 없애기
            codexUI.SetEntry(null);
        }
    }
}
