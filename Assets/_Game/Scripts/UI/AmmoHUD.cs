using UnityEngine;
using TMPro;

public class AmmoHUD : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text ammoText;

    [Header("Localization")]
    [Tooltip("CSV에 등록된 Key값 (예: HUD_AMMO -> '총알: {0}')")]
    public string localizationKey = "STAT_BULLET_CNT"; 

    // ✅ [추가] 원본 폰트 크기 저장용 변수
    private float originalFontSize;

    void Awake()
    {
        // ✅ [추가] 시작 시점의 폰트 크기를 기준으로 잡음
        if (ammoText != null)
        {
            originalFontSize = ammoText.fontSize;
        }
    }

    void Start()
    {
        // 1. 텍스트가 마우스 클릭을 가로채지 않도록 설정
        if (ammoText != null)
        {
            ammoText.raycastTarget = false;
        }

        // 2. StageManager 이벤트 구독 (총알 개수 변경 감지)
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnAmmoChanged += UpdateAmmoDisplay;
            
            // 초기 표시
            UpdateAmmoDisplay(StageManager.Instance.CurrentAmmo);
        }

        // 3. LocalizationManager 이벤트 구독 (언어 변경 감지)
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            
            // 초기 업데이트 (폰트 및 크기 적용을 위해)
            // 데이터가 로드된 상태라면 즉시 갱신
            OnLanguageChanged();
        }
    }

    void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnAmmoChanged -= UpdateAmmoDisplay;
        }

        // 언어 변경 이벤트 구독 해제
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    // ✅ 언어가 바뀌었을 때 호출되는 함수
    private void OnLanguageChanged()
    {
        // 현재 총알 개수를 가져와서 UI를 강제로 새로고침 (새 언어와 폰트, 크기로)
        if (StageManager.Instance != null)
        {
            UpdateAmmoDisplay(StageManager.Instance.CurrentAmmo);
        }
    }

    // ✅ 총알 개수나 언어가 바뀔 때 UI 갱신
    private void UpdateAmmoDisplay(int currentAmmo)
    {
        if (ammoText != null)
        {
            if (!ammoText.gameObject.activeSelf) 
            {
                ammoText.gameObject.SetActive(true);
            }

            if (LocalizationManager.Instance != null)
            {
                // ✅ [수정] 폰트 데이터(에셋 + 비율) 가져오기
                var langData = LocalizationManager.Instance.GetCurrentLanguageData();

                // 1. 폰트 에셋 교체
                if (langData.fontAsset != null) 
                    ammoText.font = langData.fontAsset;

                // 2. 폰트 크기 비율 적용
                float ratio = (langData.fontRatio <= 0) ? 1.0f : langData.fontRatio;
                // 픽셀 아트 게임이므로 소수점 제거 (Mathf.Round)
                ammoText.fontSize = Mathf.Round(originalFontSize * ratio);

                // 3. 번역된 텍스트 적용 (포맷팅: {0} 자리에 숫자 대입)
                ammoText.text = LocalizationManager.Instance.GetText(localizationKey, currentAmmo);
            }
            else
            {
                // 매니저가 없을 때를 대비한 기본값
                ammoText.text = $"Bullets: {currentAmmo}";
            }

            ammoText.color = currentAmmo > 0 ? Color.white : Color.red;
        }
    }
}