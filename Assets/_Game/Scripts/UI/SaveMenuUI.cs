using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;

public class SaveMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject savePageRoot;
    public GameObject confirmPopup;

    // 🔥 [수정] 단순 텍스트 대신 번역 컴포넌트 사용
    // 인스펙터에서 팝업창의 텍스트 오브젝트에 LocalizedText 컴포넌트를 붙이고 여기에 연결하세요.
    public LocalizedText popupMessageText; 

    [Header("Slots")]
    public Transform slotContainer;
    public GameObject slotPrefab;
    public int slotCount = 3;

    private List<SaveSlotUI> _uiSlots = new List<SaveSlotUI>();
    
    // 모드 구분 변수
    private bool _isLoadMode = false; 
    private int _targetSlotIndex = -1;
    private bool _isDeleteMode = false; 

    void Awake()
    {
        // 슬롯 초기화 (중복 방지 로직 추가)
        foreach (Transform child in slotContainer) Destroy(child.gameObject);
        _uiSlots.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            GameObject go = Instantiate(slotPrefab, slotContainer);
            SaveSlotUI ui = go.GetComponent<SaveSlotUI>();
            if (ui != null)
            {
                ui.Init(this, i);
                _uiSlots.Add(ui);
            }
        }

        savePageRoot.SetActive(false);
        confirmPopup.SetActive(false);
    }

    public void Open(bool isLoadMode)
    {
        Debug.Log($"SaveMenu Open: LoadMode = {isLoadMode}");
        
        _isLoadMode = isLoadMode;
        savePageRoot.SetActive(true);
        confirmPopup.SetActive(false);
        
        RefreshAllSlots();
    }

    public void Open() { Open(false); }

    public void Close()
    {
        savePageRoot.SetActive(false);
        confirmPopup.SetActive(false);
    }

    // SaveMenuUI.cs

    private void RefreshAllSlots()
    {
        if (SaveSystem.Instance == null) return;

        for (int i = 0; i < _uiSlots.Count; i++)
        {
            SaveSlotUI slot = _uiSlots[i];
            bool hasData = SaveSystem.Instance.Load(i) != null;
            
            slot.Refresh(); 

            if (slot.deleteButton != null)
            {
                // 🔴 기존 코드 (문제 원인): 로드 모드가 아닐 때만 삭제 버튼 표시
                // bool showDeleteButton = (!_isLoadMode) && hasData;

                // 🟢 수정 코드: 모드와 상관없이 데이터가 있으면 무조건 표시
                bool showDeleteButton = hasData;
                
                slot.deleteButton.gameObject.SetActive(showDeleteButton);
            }
        }
    }

    public void OnSlotClicked(int index, bool hasData)
    {
        _targetSlotIndex = index;
        _isDeleteMode = false;

        if (_isLoadMode)
        {
            // === [로드 모드] ===
            if (!hasData) return;
            LoadGameAndStart(index);
        }
        else
        {
            // === [저장 모드] ===
            if (hasData) 
            {
                // 🔥 [수정] 하드코딩 문자열 대신 CSV의 키(Key)를 전달
                ShowPopup("MSG_OVERWRITE_CONFIRM"); 
            }
            else 
            {
                ProcessAction(); // 빈 슬롯은 즉시 저장
            }
        }
    }

    public void OnDeleteClicked(int index)
    {
        _targetSlotIndex = index;
        _isDeleteMode = true;
        // 🔥 [수정] 하드코딩 문자열 대신 CSV의 키(Key)를 전달
        ShowPopup("MSG_DELETE_CONFIRM");
    }

    // 🔥 [수정] 텍스트가 아닌 키를 받아서 처리하도록 변경
    private void ShowPopup(string localizationKey)
    {
        if (popupMessageText != null) 
        {
            // LocalizedText의 SetKey 기능을 사용하여 언어/폰트 자동 적용
            popupMessageText.SetKey(localizationKey);
        }
        confirmPopup.SetActive(true);
    }

    public void OnConfirmOverwrite()
    {
        ProcessAction();
        confirmPopup.SetActive(false);
    }

    public void OnCancelOverwrite()
    {
        _targetSlotIndex = -1;
        confirmPopup.SetActive(false);
    }

    private void ProcessAction()
    {
        if (_targetSlotIndex == -1) return;

        if (_isDeleteMode) 
        {
            SaveSystem.Instance.DeleteSave(_targetSlotIndex);
            
            // ✅ [추가] 현재 플레이 중인 슬롯을 삭제했다면, 연결 끊기
            if (StageManager.Instance.CurrentSlotIndex == _targetSlotIndex)
            {
                StageManager.Instance.CurrentSlotIndex = -1;
            }
        }
        else 
        {
            SaveSystem.Instance.Save(_targetSlotIndex);
            
            // ✅ [추가] 저장 성공 시, 현재 슬롯 인덱스 갱신
            StageManager.Instance.CurrentSlotIndex = _targetSlotIndex;
        }
        
        RefreshAllSlots();
    }

    private void LoadGameAndStart(int slotIndex)
    {
        SaveData data = SaveSystem.Instance.Load(slotIndex);
        if (data == null) 
        {
            Debug.LogError("로드할 데이터가 없습니다!");
            return;
        }

        StageManager.PendingLoadData = data;
        
        // ✅ [추가] 로드할 슬롯 번호 전달
        StageManager.PendingSlotIndex = slotIndex;

        string sceneName = $"GameScene_{data.sceneIndex}";
        Debug.Log($"씬 로드 시작: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}