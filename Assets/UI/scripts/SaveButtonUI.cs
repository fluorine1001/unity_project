using TMPro;
using UnityEngine;

public class SaveButtonUI : MonoBehaviour
{
    public TextMeshProUGUI statusText;

    public void SaveSlot0()
    {
        if (SaveSystem.Instance == null) return;
        SaveSystem.Instance.Save(0);
        if (statusText != null) statusText.text = "Saved complete";
    }
}
