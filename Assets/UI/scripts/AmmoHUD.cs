using UnityEngine;
using TMPro;

public class AmmoHUD : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text ammoText;

    void Start()
    {
        // ✅ [수정됨] 텍스트가 마우스 클릭을 가로채지 않도록 설정
        if (ammoText != null)
        {
            ammoText.raycastTarget = false;
        }

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
            if (!ammoText.gameObject.activeSelf) 
            {
                ammoText.gameObject.SetActive(true);
            }

            ammoText.text = $"Bullets: {currentAmmo}";
            ammoText.color = currentAmmo > 0 ? Color.white : Color.red;
        }
    }
}