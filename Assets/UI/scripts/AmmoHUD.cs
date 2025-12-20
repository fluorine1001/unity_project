using UnityEngine;
using TMPro;

public class AmmoHUD : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text ammoText;

    void Start()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnAmmoChanged += UpdateAmmoDisplay;
            UpdateAmmoDisplay(StageManager.Instance.CurrentAmmo);
        }
    }

    void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnAmmoChanged -= UpdateAmmoDisplay;
        }
    }

    private void UpdateAmmoDisplay(int currentAmmo)
    {
        if (ammoText != null)
        {
            // 🔥 [수정] 혹시 꺼져있을 수 있으니 강제로 켭니다!
            if (!ammoText.gameObject.activeSelf) 
            {
                ammoText.gameObject.SetActive(true);
            }

            ammoText.text = $"Bullets: {currentAmmo}";
            
            // 색상 변경 (선택사항)
            ammoText.color = currentAmmo > 0 ? Color.white : Color.red;
        }
    }
}