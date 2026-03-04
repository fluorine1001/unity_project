using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI slotInfoText;
    public Button slotButton;    
    public Button deleteButton;  

    private int _slotIndex;
    private SaveMenuUI _menuUI;
    private bool _hasData;

    public void Init(SaveMenuUI menuUI, int index)
    {
        _menuUI = menuUI;
        _slotIndex = index;
        
        slotButton.onClick.AddListener(OnSlotClicked);

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }
    }

    public void Refresh()
    {
        SaveData data = SaveSystem.Instance.Load(_slotIndex);

        if (data != null)
        {
            _hasData = true;
            
            // ⏰ [수정] StageManager 의존성을 없애고, 언제나 동일하게 "00:00:00"으로 표시
            float t = data.playTime;
            int hours = (int)(t / 3600);
            int minutes = (int)((t % 3600) / 60);
            int seconds = (int)(t % 60);

            string timeStr = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);

            // 텍스트에 시간 추가 (줄바꿈이 제대로 들어가 있는지 확인하세요)
            // SaveData에 sceneIndex가 저장되어 있다고 가정합니다.
            slotInfoText.text = $"Slot {_slotIndex + 1}\nStage {data.sceneIndex}-{data.highestReachedStage + 1}\n{timeStr}";
            
            if(deleteButton != null) deleteButton.gameObject.SetActive(true);
        }
        else
        {
            _hasData = false;
            slotInfoText.text = $"Slot {_slotIndex + 1}\n[Empty]";
            
            if(deleteButton != null) deleteButton.gameObject.SetActive(false);
        }
    }

    private void OnSlotClicked()
    {
        _menuUI.OnSlotClicked(_slotIndex, _hasData);
    }

    private void OnDeleteClicked()
    {
        _menuUI.OnDeleteClicked(_slotIndex);
    }
}