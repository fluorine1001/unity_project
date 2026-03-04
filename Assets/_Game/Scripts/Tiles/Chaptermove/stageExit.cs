using UnityEngine;
using UnityEngine.SceneManagement; 

public class StageExit : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("마지막 스테이지의 번호를 입력하세요. (예: 2 스테이지가 끝이면 2)")]
    [SerializeField] private int lastStageIndex = 2; 

    private string nextSceneName; 
    private bool doRecordUpdate = false;

    private void Start()
    {
        // StageManager가 존재하는지 확인 후 로직 실행
        if (StageManager.Instance != null)
        {
            int currentStage = StageManager.Instance.sceneIndex;

            // ✅ 현재 스테이지가 설정한 '마지막 스테이지' 번호와 같다면 -> 메인 메뉴로
            if (currentStage >= lastStageIndex)
            {
                doRecordUpdate = true;
                nextSceneName = "MainMenu";
            }
            // ✅ 아니라면 -> 다음 번호의 스테이지로 자동 설정 (예: GameScene_1 -> GameScene_2)
            else
            {
                doRecordUpdate = false;
                nextSceneName = "GameScene_" + (currentStage + 1);
            }
            
            Debug.Log($"[StageExit] 현재: {currentStage} / 목표: {lastStageIndex} / 다음 씬: {nextSceneName}");
        }
        else
        {
            // 테스트용: 매니저가 없을 땐 그냥 인스펙터 값을 따르거나 경고
            Debug.LogWarning("StageManager가 없습니다. nextSceneName이 설정되지 않을 수 있습니다.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌한 물체의 태그가 "Player"인지 확인
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"플레이어 도착! {nextSceneName} 씬으로 이동합니다.");

            if (doRecordUpdate)
            {
                StageManager.Instance.CheckAndSaveBestTime();
            }
            
            // 씬 이름이 비어있지 않을 때만 이동
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogError("이동할 다음 씬 이름(nextSceneName)이 설정되지 않았습니다!");
            }
        }
    }
}   