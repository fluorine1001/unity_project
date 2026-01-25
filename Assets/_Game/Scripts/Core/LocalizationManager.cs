using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [System.Serializable]
    public struct LanguageFontData
    {
        public string languageCode; // 예: "KR", "EN"
        public string displayName;  // UI 표시용 이름 (예: "한국어", "English") ✅
        public TMP_FontAsset fontAsset; // 해당 언어용 폰트

        public float fontRatio;
    }

    [Header("Settings")]
    public List<LanguageFontData> fontList; // 인스펙터에서 할당
    public string currentLanguage = "EN";

    // 데이터 저장소: <Key, <LanguageCode, Value>>
    private Dictionary<string, Dictionary<string, string>> localizedText = new Dictionary<string, Dictionary<string, string>>();
    private bool isReady = false;

    // 언어가 바뀔 때 발동하는 이벤트
    public delegate void LanguageChangeHandler();
    public event LanguageChangeHandler OnLanguageChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCSV();
        }
        else
        {
            // 🔥 [수정됨] 컴파일 에러 해결 및 로직 수정
            // 이미 살아있는 매니저(Instance)에게 현재 씬의 최신 폰트 리스트를 덮어씌웁니다.
            Instance.fontList = this.fontList;
            
            // 현재 씬의 중복된 매니저는 파괴합니다.
            Destroy(gameObject);
        }
    }

    private void LoadCSV()
    {
        // Resources 폴더의 LocalizationData.csv 로드
        TextAsset csvData = Resources.Load<TextAsset>("Localization/LocalizationData");
        if (csvData == null)
        {
            Debug.LogError("LocalizationData.csv를 찾을 수 없습니다!");
            return;
        }

        string[] lines = csvData.text.Split('\n');
        if (lines.Length <= 1) return;

        // 첫 줄(헤더) 파싱: Key, KR, EN, JP ...
        string[] headers = lines[0].Trim().Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // 쉼표로 분리 (따옴표 안 쉼표 처리 등 복잡한 로직은 생략, 간단한 파싱)
            string[] columns = line.Split(',');

            string key = columns[0];
            if (string.IsNullOrEmpty(key)) continue;

            if (!localizedText.ContainsKey(key))
                localizedText.Add(key, new Dictionary<string, string>());

            // 각 언어별 데이터 저장
            for (int j = 1; j < headers.Length; j++)
            {
                if (j < columns.Length)
                {
                    string langCode = headers[j].Trim(); // KR, EN...
                    string content = columns[j].Replace("{c}", ","); // 쉼표 이스케이프 필요시 사용
                    localizedText[key].Add(langCode, content);
                }
            }
        }
        isReady = true;
    }

    // 1. 단순 텍스트 가져오기
    public string GetText(string key)
    {
        if (!isReady) return key;
        if (localizedText.ContainsKey(key) && localizedText[key].ContainsKey(currentLanguage))
        {
            return localizedText[key][currentLanguage];
        }
        return key; // 번역 없으면 키 그대로 반환
    }

    // 2. 포맷팅 텍스트 가져오기 (예: "총알: {0}개")
    public string GetText(string key, params object[] args)
    {
        string text = GetText(key);
        return string.Format(text, args);
    }

    // ✅ 현재 언어 설정을 통째로 가져오는 함수 (폰트 + 비율)
    public LanguageFontData GetCurrentLanguageData()
    {
        foreach (var data in fontList)
        {
            if (data.languageCode == currentLanguage) return data;
        }
        // 못 찾으면 기본값 반환 (비율 1.0)
        return new LanguageFontData { fontRatio = 1.0f };
    }

    // 언어 변경 함수
    public void ChangeLanguage(string newLangCode)
    {
        currentLanguage = newLangCode;
        OnLanguageChanged?.Invoke(); // 구독자들에게 알림
    }
}