using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject Panel;           
    
    [Header("Pages")]
    public GameObject MainMenuPage;    
    public SaveMenuUI saveMenuUI;      
    
    // ✅ [추가] 매뉴얼 메뉴 UI 연결
    public GameObject ManualMenuPage;  
    public ManualMenuUI manualMenuUI; // (필수는 아니지만 닫을 때 초기화 용도 등으로 추천)
    // ✅ [추가 1] 언어 설정 페이지 연결 변수
    public GameObject LanguageMenuPage;

    public bool IsPanelOpen { get; private set; }

    void Start()
    {
        if (Panel != null) Panel.SetActive(false);
        IsPanelOpen = false;
        
        ShowMainMenu(); // 초기화 로직 통합
    }

    public void bookPanel(bool newState)
    {
        if (Panel != null)
        {
            if (!IsPanelOpen) AudioManager.instance.PlayOneShot(FMODEvents.instance.MenuPressed, this.transform.position);
            else AudioManager.instance.PlayOneShot(FMODEvents.instance.MenuClosed, this.transform.position);

            Panel.SetActive(newState);

            if (newState)
            {
                ShowMainMenu();
            }
        }

        IsPanelOpen = newState;
        if (newState == false)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void ShowMainMenu()
    {
        if (MainMenuPage != null) MainMenuPage.SetActive(true);
        if (saveMenuUI != null) saveMenuUI.Close();
        // ✅ [추가] 매뉴얼 끄기
        if (ManualMenuPage != null) ManualMenuPage.SetActive(false);

        // ✅ [추가 2] 메인 메뉴로 올 때 언어 페이지 끄기
        if (LanguageMenuPage != null) LanguageMenuPage.SetActive(false);
    }

    // ... ShowSaveMenu 등 기존 코드 ...

    // ✅ [추가] 매뉴얼 메뉴 보여주기 함수
    public void ShowManualMenu()
    {
        if (MainMenuPage != null) MainMenuPage.SetActive(false);
        if (saveMenuUI != null) saveMenuUI.Close();
        if (ManualMenuPage != null) 
        {
            ManualMenuPage.SetActive(true);
            // 켤 때 리스트를 한번 갱신해주고 싶다면
            if(manualMenuUI != null) manualMenuUI.RefreshList();
        }
    }

    public void ShowSaveMenu()
    {
        if (MainMenuPage != null) MainMenuPage.SetActive(false); // 기존 버튼 숨김
        if (saveMenuUI != null) saveMenuUI.Open();               // 세이브 화면 켬
    }

    // ✅ [추가 3] 언어 설정 페이지 보여주기 함수 (Language Button에 연결)
    public void ShowLanguageMenu()
    {
        if (MainMenuPage != null) MainMenuPage.SetActive(false); // 메인 숨김
        if (saveMenuUI != null) saveMenuUI.Close();
        if (ManualMenuPage != null) ManualMenuPage.SetActive(false);

        if (LanguageMenuPage != null)
        {
            LanguageMenuPage.SetActive(true); // 언어 페이지 켬
        }
    }

    // UIManager.cs 안에 추가
    public bool IsUIActive 
    {
        get 
        {
            // 1. [핵심 수정] 책 패널 전체가 닫혀있다면 -> 내부 페이지 상태와 상관없이 false 반환
            if (!IsPanelOpen) return false;

            // 2. 패널이 열려있다면 -> 매뉴얼(또는 다른 메뉴)이 켜져있는지 확인
            bool isManualOpen = manualMenuUI != null && manualMenuUI.gameObject.activeSelf;

            // ✅ [추가 4] 언어 페이지가 열려있는 경우도 UI Active 상태로 인식
            bool isLanguageOpen = LanguageMenuPage != null && LanguageMenuPage.activeSelf;
            
            return isManualOpen || isLanguageOpen; 
        }
    }
}