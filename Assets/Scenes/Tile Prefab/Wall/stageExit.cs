using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수

public class StageExit : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "GameScene_2"; // 이동할 다음 씬 이름

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌한 물체의 태그가 "Player"인지 확인
        if (collision.CompareTag("Player"))
        {
            Debug.Log("플레이어 도착! 다음 씬으로 이동합니다.");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}