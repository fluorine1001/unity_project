using UnityEngine;
using UnityEngine.SceneManagement; 

public class StageExit : MonoBehaviour
{
    [SerializeField] private string nextSceneName; // 기본값은 인스펙터에서 설정 가능

    // ✅ 수정된 부분: 로직을 Start() 함수 내부로 이동
    private void Start()
    {
        // StageManager가 존재하는지 확인 후 로직 실행
        if (StageManager.Instance != null)
        {
            int currentStage = StageManager.Instance.sceneIndex;

            if (currentStage == 1) 
                nextSceneName = "GameScene_2";
            else if (currentStage == 2) 
                nextSceneName = "MainMenu";
            
            // 만약 3, 4 스테이지 등이 있다면 여기에 추가하거나,
            // "GameScene_" + (currentStage + 1) 처럼 자동화할 수도 있습니다.
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌한 물체의 태그가 "Player"인지 확인
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"플레이어 도착! {nextSceneName} 씬으로 이동합니다.");
            
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