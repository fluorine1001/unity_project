using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("References")]
    // 로드 메뉴 UI (SavePage 프리팹) 연결
    public SaveMenuUI loadMenuUI; 

    // ✅ 키보드 커서 로직 삭제됨 (Update 제거)

    private void Start()
    {
        // 시작 시 로드 메뉴가 켜져있다면 끔
        if (loadMenuUI != null) loadMenuUI.Close();
    }

    // === 버튼 연결 함수들 ===

    // 1. Play 버튼 (새 게임 시작)
    public void PlayGame()
    {
        // 새 게임은 그냥 1번 씬(GameScene)으로 이동
        // (PendingLoadData가 null이므로 StageManager가 알아서 초기화함)
        SceneManager.LoadScene("GameScene_1");
    }

    // 2. Load 버튼 (이어하기)
    public void OpenLoadMenu()
    {
        if (loadMenuUI != null)
        {
            // true = 로드 모드로 열기
            loadMenuUI.Open(true);
        }
        else
        {
            Debug.LogError("Load Menu UI가 연결되지 않았습니다!");
        }
    }

    // 3. Manual 버튼 (새로 추가)
    public void OpenManual()
    {
        UIManager ui = FindObjectOfType<UIManager>();
        if (ui != null)
        {
            ui.ShowManualMenu();
        }
    }

    // 3. Quit 버튼
    public void QuitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }

    // 4. Credit 버튼
    public void OpenCredit()
    {
        // 크레딧 패널 로직 (필요 시 구현)
        Debug.Log("크레딧 열기");
    }
}