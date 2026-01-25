using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용

public class LanguagePage : MonoBehaviour
{
    [Header("System")]
    public UIManager uiManager;

    [Header("UI Components")]
    [Tooltip("스크롤 뷰의 Content 오브젝트를 연결하세요.")]
    public Transform listContent; 
    
    [Tooltip("리스트에 추가될 버튼 프리팹입니다.")]
    public GameObject languageButtonPrefab; 

    [Header("Navigation")]
    public Button btnBack; // 뒤로가기 버튼

    void Start()
    {
        // 1. 언어 목록 동적 생성
        GenerateLanguageList();

        // 2. 뒤로가기 버튼 연결
        if (btnBack != null)
            btnBack.onClick.AddListener(OnBackButtonClicked);
    }

    private void GenerateLanguageList()
    {
        // 기존에 생성된 버튼이 있다면 모두 삭제 (초기화)
        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }

        if (LocalizationManager.Instance == null) return;

        // 매니저에 등록된 모든 언어 데이터 가져오기 (폰트 정보 포함)
        var langList = LocalizationManager.Instance.fontList;

        foreach (var langData in langList)
        {
            // 프리팹 생성
            GameObject btnObj = Instantiate(languageButtonPrefab, listContent);
            
            // 버튼 텍스트 설정
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                // 1. 언어 이름 설정 (예: "한국어", "English")
                btnText.text = langData.displayName; 
                
                // 🔥 [수정] 해당 언어 데이터에 할당된 전용 폰트 적용
                // 현재 선택된 언어와 상관없이, 이 버튼은 '자신만의 폰트'를 사용합니다.
                if (langData.fontAsset != null) 
                {
                    btnText.font = langData.fontAsset;
                }
            }

            // 클릭 이벤트 연결
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                string code = langData.languageCode; 
                btn.onClick.AddListener(() => OnLanguageSelected(code));
            }
        }
    }

    private void OnLanguageSelected(string langCode)
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.ChangeLanguage(langCode);
        }
    }

    private void OnBackButtonClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowMainMenu();
        }
    }
}