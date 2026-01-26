using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement; // 👈 이 줄을 꼭 추가해야 합니다!

public class UIManager : MonoBehaviour
{

    // ✅ [추가] 어디서든 접근 가능한 정적 인스턴스 선언
    public static UIManager Instance { get; private set; }

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

    // ✅ [추가 1] 볼륨(사운드) 페이지 연결
    public GameObject VolumeMenuPage; 
    public VolumePageUI volumePageUI; // 리스트 초기화를 위해 스크립트 참조 필요

    [Header("Popups")]
    // ✅ [추가] 메인 메뉴 이동 확인 팝업
    public GameObject mainMenuConfirmPopup; 
    public LocalizedText mainMenuConfirmText; // 팝업 메시지 내용 ("저장되지 않은 내용이 있습니다...")

    public bool IsPanelOpen { get; private set; }

    // ✅ [추가] Awake에서 인스턴스 초기화
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // UIManager가 씬이 바뀔 때 파괴되지 않기를 원한다면 아래 주석 해제
        // DontDestroyOnLoad(gameObject); 
    }

    void Start()
    {
        if (Panel != null) Panel.SetActive(false);
        IsPanelOpen = false;
        
        ShowMainMenu(); // 초기화 로직 통합
    }

    public void ToggleBookPanel()
    {
        bookPanel(!IsPanelOpen);
    }

    // ✅ [수정] playSound 파라미터 추가 (기본값 true)
    public void bookPanel(bool newState, bool playSound = true)
    {
        // 상태 변화가 없으면 리턴
        if (IsPanelOpen == newState) return;

        if (Panel != null)
        {
            // ✅ [수정] playSound가 true일 때만 소리 재생
            if (playSound)
            {
                if (newState) 
                    AudioManager.instance.PlayOneShot(FMODEvents.instance.MenuPressed, this.transform.position);
                else 
                    AudioManager.instance.PlayOneShot(FMODEvents.instance.MenuClosed, this.transform.position);
            }

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

    // ✅ [추가] Main Menu 버튼 클릭 시 호출
    public void OnMainMenuButtonClicked()
    {
        // 1. 변경 사항(저장되지 않은 진행도) 확인
        if (StageManager.Instance.HasUnsavedChanges())
        {
            // 변경 사항이 있다면 팝업 띄우기
            OpenMainMenuConfirmPopup();
        }
        else
        {
            // 변경 사항이 없다면 바로 메인 메뉴로 이동
            GoToMainMenuScene();
        }
    }

    private void OpenMainMenuConfirmPopup()
    {
        if (mainMenuConfirmPopup != null)
        {
            mainMenuConfirmPopup.SetActive(true);
            
            // 메시지 설정: "저장되지 않은 데이터가 있습니다. 메인으로 이동하시겠습니까?"
            if (mainMenuConfirmText != null)
            {
                // CSV에 "MSG_UNSAVED_EXIT" 키를 추가해서 사용하세요.
                // 내용 예시: "변경 사항이 저장되지 않았습니다. 메인 메뉴로 돌아가시겠습니까?"
                mainMenuConfirmText.SetKey("MSG_UNSAVED_EXIT"); 
            }
        }
        else
        {
            // 팝업이 연결 안 되어있으면 그냥 나감 (안전 장치)
            GoToMainMenuScene();
        }
    }

    // ✅ 팝업에서 [Yes] 버튼 클릭
    public void OnConfirmMainMenuExit()
    {
        // 팝업 닫고 메인 메뉴로
        if (mainMenuConfirmPopup != null) mainMenuConfirmPopup.SetActive(false);
        GoToMainMenuScene();
    }

    // ✅ 팝업에서 [No] 버튼 클릭
    public void OnCancelMainMenuExit()
    {
        // 그냥 팝업만 닫기
        if (mainMenuConfirmPopup != null) mainMenuConfirmPopup.SetActive(false);
    }

    private void GoToMainMenuScene()
    {
        // ✅ [수정] 패널을 닫되, 소리는 내지 않음 (false 전달)
        bookPanel(false, playSound: false); 

        // 메인 메뉴로 갈 때 배경음악 끄기
        if (AudioManager.instance != null)
        {
            AudioManager.instance.StopMusic();
        }
        
        // 메인 메뉴 씬 로드
        SceneManager.LoadScene("_Game/Scenes/MainMenu/MainMenu");
    }

    public void ShowMainMenu()
    {
        if (MainMenuPage != null) MainMenuPage.SetActive(true);
        if (saveMenuUI != null) saveMenuUI.Close();
        // ✅ [추가] 매뉴얼 끄기
        if (ManualMenuPage != null) ManualMenuPage.SetActive(false);

        // ✅ [추가 2] 메인 메뉴로 올 때 언어 페이지 끄기
        if (LanguageMenuPage != null) LanguageMenuPage.SetActive(false);

        // ✅ [추가 2] 볼륨 페이지 끄기
        if (VolumeMenuPage != null) VolumeMenuPage.SetActive(false);
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

    // ✅ [추가 3] 볼륨(사운드) 페이지 보여주기 함수
    public void ShowVolumeMenu()
    {
        CloseAllSubMenus(); // 다른 메뉴 끄기

        if (VolumeMenuPage != null)
        {
            VolumeMenuPage.SetActive(true);
            
            // 페이지가 열릴 때 슬라이더 리스트 생성/갱신
            if (volumePageUI != null) 
            {
                volumePageUI.InitializeUI();
            }
        }
    }

    // (도우미 함수) 메인 메뉴를 제외한 서브 메뉴들을 모두 닫는 로직
    private void CloseAllSubMenus()
    {
        if (MainMenuPage != null) MainMenuPage.SetActive(false);
        if (saveMenuUI != null) saveMenuUI.Close();
        if (ManualMenuPage != null) ManualMenuPage.SetActive(false);
        if (LanguageMenuPage != null) LanguageMenuPage.SetActive(false);
        if (VolumeMenuPage != null) VolumeMenuPage.SetActive(false);
    }

    // UIManager.cs 안에 추가
    public bool IsUIActive 
    {
        get 
        {
            // 1. [핵심 수정] 책 패널 전체가 닫혀있다면 -> 내부 페이지 상태와 상관없이 false 반환
            if (!IsPanelOpen) return false;
            else return true;
        }
    }
}