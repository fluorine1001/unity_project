using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Pages")]
    public SaveMenuUI loadMenuUI;       // 로드 메뉴
    public LanguagePage languagePage;   // 언어 메뉴
    public GameObject creditPage;       // 크레딧 페이지 (예시로 추가함)

    private void Start()
    {
        // 게임 시작 시 모든 팝업 닫고 깔끔하게 시작
        CloseAllMenus();
    }

    // 🔥 핵심 기능: 모든 메뉴를 일단 다 닫는 함수
    private void CloseAllMenus()
    {
        if (loadMenuUI != null) loadMenuUI.Close();
        if (languagePage != null) languagePage.gameObject.SetActive(false);
        if (creditPage != null) creditPage.SetActive(false);
    }

    // === 버튼 연결 함수들 ===

    public void PlayGame()
    {
        SceneManager.LoadScene("GameScene_1");
    }

    public void OpenLoadMenu()
    {
        // 1. 다 닫고
        CloseAllMenus(); 
        
        // 2. 얘만 켬
        if (loadMenuUI != null) loadMenuUI.Open(true);
        else Debug.LogError("Load Menu UI가 연결되지 않았습니다!");
    }

    public void OpenLanguageMenu()
    {
        // 1. 다 닫고
        CloseAllMenus();

        // 2. 얘만 켬
        if (languagePage != null) languagePage.gameObject.SetActive(true);
    }

    public void OpenCredit()
    {
        // 1. 다 닫고
        CloseAllMenus();

        // 2. 얘만 켬 (크레딧 페이지가 있다면)
        if (creditPage != null) creditPage.SetActive(true);
        else Debug.Log("크레딧 페이지가 연결되지 않았습니다.");
    }

    // 언어 메뉴나 크레딧의 [X] 버튼, [Back] 버튼에 연결할 공용 닫기 함수
    public void CloseCurrentMenu()
    {
        CloseAllMenus();
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }
}