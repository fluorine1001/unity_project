using UnityEngine;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [Header("Localization Setting")]
    public string localizationKey; 

    [System.Serializable]
    public struct LanguageScaleOverride
    {
        public string languageCode; // 예: "EN"
        public float scaleRatio;    // 예: 0.9 (이 텍스트만 영어에서 좀 작게)
    }

    [Header("Optional Settings")]
    [Tooltip("특정 언어에서만 매니저 설정(전역 비율)을 무시하고 개별 비율을 적용하고 싶을 때 추가하세요.")]
    public List<LanguageScaleOverride> customScaleRatios; 

    private TMP_Text targetText;
    private float originalFontSize;

    private void Awake()
    {
        targetText = GetComponent<TMP_Text>();
        
        if (targetText != null)
        {
            originalFontSize = targetText.fontSize;
        }
    }

    private void Start()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            UpdateText(); 
        }
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
    }

    public void UpdateText()
    {
        if (LocalizationManager.Instance == null || targetText == null) return;

        // 1. 매니저 데이터 가져오기 (폰트 에셋용)
        var langData = LocalizationManager.Instance.GetCurrentLanguageData();

        // 폰트 변경
        if (langData.fontAsset != null) 
        {
            targetText.font = langData.fontAsset;
        }

        // 2. 비율 결정 로직 (개별 설정 우선 -> 없으면 매니저 설정)
        float finalRatio = GetFinalScaleRatio(langData);

        // 크기 적용 (원본 * 비율)
        targetText.fontSize = Mathf.Round(originalFontSize * finalRatio);

        // 3. 텍스트 내용 변경
        if (!string.IsNullOrEmpty(localizationKey))
        {
            targetText.text = LocalizationManager.Instance.GetText(localizationKey);
        }
    }

    // ✅ 우선순위에 따라 최종 비율을 계산하는 함수
    private float GetFinalScaleRatio(LocalizationManager.LanguageFontData globalData)
    {
        string currentLang = LocalizationManager.Instance.currentLanguage;

        // 1. 개별 오버라이드 리스트 확인
        if (customScaleRatios != null)
        {
            foreach (var overrideData in customScaleRatios)
            {
                // 현재 언어에 대한 설정이 있다면 그 값을 사용
                if (overrideData.languageCode == currentLang)
                {
                    // 0 이하는 1.0으로 안전처리
                    return (overrideData.scaleRatio <= 0) ? 1.0f : overrideData.scaleRatio;
                }
            }
        }

        // 2. 없으면 매니저(전역) 설정 사용
        return (globalData.fontRatio <= 0) ? 1.0f : globalData.fontRatio;
    }

    public void SetKey(string newKey)
    {
        localizationKey = newKey;
        UpdateText();
    }

    public void SetTextArgs(params object[] args)
    {
        if (LocalizationManager.Instance == null || targetText == null) return;
        
        var langData = LocalizationManager.Instance.GetCurrentLanguageData();

        // 폰트 업데이트
        if (langData.fontAsset != null) targetText.font = langData.fontAsset;
        
        // 비율 업데이트 (함수 재사용)
        float finalRatio = GetFinalScaleRatio(langData);
        targetText.fontSize = Mathf.Round(originalFontSize * finalRatio);

        // 텍스트 업데이트
        targetText.text = LocalizationManager.Instance.GetText(localizationKey, args);
    }
}