using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Pages")]
    public GameObject mainPage;      // MainPage 오브젝트
    public GameObject settingsPage;  // SettingsPage 오브젝트

    [Header("Controls")]
    public Slider masterVolumeSlider;
    public Toggle fullscreenToggle;

    const string MasterVolumeKey = "MasterVolume";
    const string FullscreenKey = "Fullscreen";

    void Start()
    {
        LoadSettings();
        ShowMainPage();
    }

    void ShowMainPage()
    {
        if (mainPage != null) mainPage.SetActive(true);
        if (settingsPage != null) settingsPage.SetActive(false);
    }

    void ShowSettingsPage()
    {
        if (mainPage != null) mainPage.SetActive(false);
        if (settingsPage != null) settingsPage.SetActive(true);
    }

    void LoadSettings()
    {
        float volume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        bool isFullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;

        ApplyMasterVolume(volume, false);
        ApplyFullscreen(isFullscreen, false);

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = volume;
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = isFullscreen;
    }

    void ApplyMasterVolume(float value, bool save)
    {
        AudioListener.volume = value;

        if (save)
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
    }

    void ApplyFullscreen(bool on, bool save)
    {
        Screen.fullScreen = on;

        if (save)
            PlayerPrefs.SetInt(FullscreenKey, on ? 1 : 0);
    }

    // === UI 이벤트용 ===

    public void OnMasterVolumeChanged(float value)
    {
        ApplyMasterVolume(value, true);
    }

    public void OnFullscreenChanged(bool on)
    {
        ApplyFullscreen(on, true);
    }

    public void OpenSettings()
    {
        ShowSettingsPage();
    }

    public void CloseSettings()
    {
        ShowMainPage();
    }
}
