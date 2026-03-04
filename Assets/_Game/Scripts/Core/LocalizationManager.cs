using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions; // ✅ 정규식 사용 필수

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [System.Serializable]
    public struct LanguageFontData
    {
        public string languageCode;
        public string displayName;
        public TMP_FontAsset fontAsset;
        public float fontRatio;

        // ✅ [추가] 이 언어에서 단어 단위(공백 기준) 줄바꿈을 강제할지 여부
        [Tooltip("체크하면 띄어쓰기(공백) 기준으로만 줄바꿈이 일어납니다. (영어, 한국어 추천 / 일본어, 중국어 비추천)")]
        public bool useWordWrapping;
    }

    [Header("Settings")]
    public List<LanguageFontData> fontList;
    public string currentLanguage = "EN";

    private Dictionary<string, Dictionary<string, string>> localizedText = new Dictionary<string, Dictionary<string, string>>();
    private bool isReady = false;

    public delegate void LanguageChangeHandler();
    public event LanguageChangeHandler OnLanguageChanged;

    private void Awake()
    {
        // 싱글톤 패턴 및 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 저장된 언어 불러오기
            currentLanguage = PlayerPrefs.GetString("SelectedLanguage", "EN");
            
            LoadCSV(); // CSV 로드 실행
        }
        else
        {
            // 씬 이동 시 중복 생성된 매니저가 있다면, 폰트 정보만 갱신해주고 파괴
            Instance.fontList = this.fontList;
            Destroy(gameObject);
        }
    }

    private void LoadCSV()
    {
        // 🚨 [수정됨] 사용자분의 원래 경로("Localization/LocalizationData")로 복구!
        TextAsset textAsset = Resources.Load<TextAsset>("Localization/LocalizationData");
        
        if (textAsset == null)
        {
            Debug.LogError("❌ [Localization] 파일을 찾을 수 없습니다! Resources/Localization/LocalizationData.csv 파일이 있는지 확인하세요.");
            return;
        }

        // 1. BOM 제거 (엑셀 저장 시 생기는 특수문자 제거)
        string csvText = textAsset.text.Trim('\uFEFF', '\u200B');

        // 2. 줄바꿈 통일 (윈도우/맥 호환)
        string[] lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length <= 1) return;

        // 3. 정규식 패턴 (따옴표 안의 콤마는 무시하고 쪼개기)
        string pattern = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";

        // 헤더 파싱
        string[] headers = Regex.Split(lines[0], pattern);
        // 헤더 공백 제거 (Key, EN, KR...)
        for (int i = 0; i < headers.Length; i++) headers[i] = headers[i].Trim();

        localizedText.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // 정규식으로 열 분리
            string[] columns = Regex.Split(lines[i], pattern);

            // Key가 없거나 헤더보다 짧으면 패스
            if (columns.Length < headers.Length) continue;

            string key = columns[0].Trim();
            if (string.IsNullOrEmpty(key)) continue;

            if (!localizedText.ContainsKey(key))
            {
                localizedText[key] = new Dictionary<string, string>();
            }

            for (int j = 1; j < headers.Length; j++)
            {
                if (j < columns.Length)
                {
                    string langCode = headers[j].Trim();
                    string content = columns[j].Trim();

                    // ✅ CSV 따옴표 처리 로직
                    // 1. 양끝 따옴표 제거 ("내용" -> 내용)
                    if (content.StartsWith("\"") && content.EndsWith("\""))
                    {
                        content = content.Substring(1, content.Length - 2);
                    }
                    // 2. 이중 따옴표("" -> ") 및 줄바꿈(\n) 처리
                    content = content.Replace("\"\"", "\"").Replace("\\n", "\n");

                    localizedText[key][langCode] = content;
                }
            }
        }

        isReady = true;
        Debug.Log($"✅ [Localization] 로드 성공! 총 {localizedText.Count}개의 데이터가 로드되었습니다.");
    }

    public string GetText(string key)
    {
        if (!isReady) return key;

        if (localizedText.ContainsKey(key))
        {
            if (localizedText[key].ContainsKey(currentLanguage))
            {
                return localizedText[key][currentLanguage];
            }
        }
        return key;
    }

    public string GetText(string key, params object[] args)
    {
        string text = GetText(key);
        return string.Format(text, args);
    }

    public LanguageFontData GetCurrentLanguageData()
    {
        foreach (var data in fontList)
        {
            if (data.languageCode == currentLanguage) return data;
        }
        return new LanguageFontData { fontRatio = 1.0f };
    }

    public void ChangeLanguage(string langCode)
    {
        currentLanguage = langCode;
        PlayerPrefs.SetString("SelectedLanguage", langCode);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }
}