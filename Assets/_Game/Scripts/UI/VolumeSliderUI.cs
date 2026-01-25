using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VolumeSliderUI : MonoBehaviour
{
    [Header("Components")]
    // ✅ [수정 1] TextMeshProUGUI 대신 LocalizedText를 연결하여 번역 기능을 사용합니다.
    [SerializeField] private LocalizedText categoryNameLabel;
    
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueText;

    private VolumeCategorySO targetCategory;
    private bool isMaster = false;

    // 마스터 볼륨용 초기화
    public void SetupMaster()
    {
        isMaster = true;

        // ✅ [수정 2] "Master Volume" 텍스트를 직접 넣는 대신, 번역 키를 설정합니다.
        if (categoryNameLabel != null)
        {
            // CSV에 "Label_Master" 키를 추가해주셔야 합니다. (없으면 키가 그대로 나옵니다)
            categoryNameLabel.SetKey("VOLUME_LABEL_MASTER");
        }

        float currentVal = AudioManager.instance.GetMasterVolume();
        InitSlider(currentVal);
    }

    // 리스트 아이템(카테고리)용 초기화
    public void SetupCategory(VolumeCategorySO category)
    {
        isMaster = false;
        targetCategory = category;

        // ✅ [수정 3] SO에 저장된 번역 키(localizationKey)를 사용합니다.
        if (categoryNameLabel != null)
        {
            if (!string.IsNullOrEmpty(category.localizationKey))
            {
                categoryNameLabel.SetKey(category.localizationKey);
            }
            else
            {
                // 만약 데이터에 키가 없다면 비상용으로 기존 이름(English)을 표시합니다.
                var tmp = categoryNameLabel.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = category.categoryName;
            }
        }

        // 매니저에 VCA 로드 요청 (기존 로직 유지)
        AudioManager.instance.InitializeCategoryVolume(category);

        float currentVal = AudioManager.instance.GetCategoryVolume(category);
        InitSlider(currentVal);
    }

    // --- 아래는 기존 로직과 100% 동일합니다 ---

    private void InitSlider(float value)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
        
        UpdateValueText(value);

        // 이벤트 재연결
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        UpdateValueText(value);

        if (isMaster)
        {
            AudioManager.instance.SetMasterVolume(value);
        }
        else if (targetCategory != null)
        {
            AudioManager.instance.SetCategoryVolume(targetCategory, value);
        }
    }

    private void UpdateValueText(float value)
    {
        // 퍼센트 변환 (0.0% ~ 100.0%)
        float percent = value * 100f;
        valueText.text = $"{percent:F1}%";
    }
}