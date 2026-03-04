using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // 텍스트 제어용

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Pages")]
    public SaveMenuUI loadMenuUI;       // 로드 메뉴
    public LanguagePage languagePage;   // 언어 메뉴

    [Header("UI Elements")]
    public TextMeshProUGUI bestTimeText; // 최고 기록 텍스트

    // ✅ [추가] AmmoHUD처럼 원본 폰트 크기를 저장할 변수
    private float originalFontSize;

    private void Awake()
    {
        // ✅ [추가] 시작 시점의 폰트 크기 저장
        if (bestTimeText != null)
        {
            originalFontSize = bestTimeText.fontSize;
        }
    }

    private void Start()
    {
        // 1. 모든 메뉴 닫기
        CloseAllMenus();

        // 2. 이벤트 구독 및 초기화 (AmmoHUD 방식 적용)
        if (LocalizationManager.Instance != null)
        {
            // 언어 변경 시 ShowBestTime이 호출되도록 연결
            LocalizationManager.Instance.OnLanguageChanged += ShowBestTime;
            
            // ✅ [핵심] 시작하자마자 현재 언어에 맞는 폰트와 텍스트를 적용
            ShowBestTime();
        }
        else
        {
            // 매니저가 없을 경우 기본 텍스트 표시
            ShowBestTime();
        }
    }

    private void OnDestroy()
    {
        // ✅ [필수] 이벤트 연결 해제 (메모리 누수 방지)
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= ShowBestTime;
        }
    }

    private void CloseAllMenus()
    {
        if (loadMenuUI != null) loadMenuUI.Close();
        if (languagePage != null) languagePage.gameObject.SetActive(false);
    }

    // ✅ [수정] AmmoHUD의 로직(폰트 교체 + 번역)을 적용한 함수
    private void ShowBestTime()
    {
        if (bestTimeText == null) return;

        // ---------------------------------------------------------------
        // 1. 폰트 및 사이즈 설정 (AmmoHUD.cs 로직 이식)
        // ---------------------------------------------------------------
        if (LocalizationManager.Instance != null)
        {
            var langData = LocalizationManager.Instance.GetCurrentLanguageData();

            // (1) 폰트 에셋 교체 (이게 없어서 깨졌던 것!)
            if (langData.fontAsset != null)
            {
                bestTimeText.font = langData.fontAsset;
            }

            // (2) 폰트 크기 비율 적용
            float ratio = (langData.fontRatio <= 0) ? 1.0f : langData.fontRatio;
            bestTimeText.fontSize = Mathf.Round(originalFontSize * ratio);
        }

        // ---------------------------------------------------------------
        // 2. 텍스트 내용 설정 (번역 적용)
        // ---------------------------------------------------------------
        string key = "GlobalBestClearTime";

        if (PlayerPrefs.HasKey(key))
        {
            float time = PlayerPrefs.GetFloat(key);
            
            int hours = (int)(time / 3600);
            int minutes = (int)((time % 3600) / 60);
            int seconds = (int)(time % 60);

            if (LocalizationManager.Instance != null)
            {
                // CSV 포맷 사용 (예: "최고 기록: {0}:{1}:{2}")
                bestTimeText.text = LocalizationManager.Instance.GetText("UI_BEST_TIME", hours, minutes, seconds);
            }
            else
            {
                // 매니저 없을 때 기본값
                bestTimeText.text = string.Format("Best Time: {0:00}:{1:00}:{2:00}", hours, minutes, seconds);
            }
        }
        else
        {
            // 기록 없음
            if (LocalizationManager.Instance != null)
            {
                bestTimeText.text = LocalizationManager.Instance.GetText("UI_BEST_TIME_NONE");
            }
            else
            {
                bestTimeText.text = "Best Time: --:--:--";
            }
        }
    }

    // === 버튼 연결 함수들 (기존 유지) ===

    public void PlayGame()
    {
        StageManager.SharedPlayTime = 0f;
        SceneManager.LoadScene("GameScene_1");
    }

    public void OpenLoadMenu()
    {
        CloseAllMenus(); 
        if (loadMenuUI != null) loadMenuUI.Open(true);
        else Debug.LogError("Load Menu UI가 연결되지 않았습니다!");
    }

    public void OpenLanguageMenu()
    {
        CloseAllMenus();
        if (languagePage != null) languagePage.gameObject.SetActive(true);
        else Debug.LogError("Language Page가 연결되지 않았습니다!");
    }

    // 🔥 [수정] 크레딧 버튼을 누르면 크레딧 씬으로 이동
    public void OpenCredit()
    {
        SceneManager.LoadScene("_Game/Scenes/CreditScene"); // 아까 만든 씬 이름
    }

    // 언어 메뉴나 크레딧의 [X] 버튼, [Back] 버튼에 연결할 공용 닫기 함수
    public void CloseCurrentMenu()
    {
        CloseAllMenus();
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}