using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI slotInfoText;
    public Button slotButton;    // 슬롯 전체 클릭 (저장/덮어쓰기)
    public Button deleteButton;  // 🗑️ 삭제 버튼 (새로 추가!)

    private int _slotIndex;
    private SaveMenuUI _menuUI;
    private bool _hasData;

    public void Init(SaveMenuUI menuUI, int index)
    {
        _menuUI = menuUI;
        _slotIndex = index;
        
        slotButton.onClick.AddListener(OnSlotClicked);

        // 삭제 버튼 이벤트 연결
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
            slotInfoText.text = $"Slot {_slotIndex + 1}\nStage {data.sceneIndex}-{data.currentStage + 1}\n{data.saveTime}";
            
            // 데이터가 있으면 삭제 버튼 보이기
            if(deleteButton != null) deleteButton.gameObject.SetActive(true);
        }
        else
        {
            _hasData = false;
            slotInfoText.text = $"Slot {_slotIndex + 1}\n[Empty]";
            
            // 빈 슬롯이면 삭제 버튼 숨기기
            if(deleteButton != null) deleteButton.gameObject.SetActive(false);
        }
    }

    private void OnSlotClicked()
    {
        _menuUI.OnSlotClicked(_slotIndex, _hasData);
    }

    // 삭제 버튼 클릭 시
    private void OnDeleteClicked()
    {
        _menuUI.OnDeleteClicked(_slotIndex);
    }
}