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

    private List<SaveSlotUI> _uiSlots = new List<SaveSlotUI>();
    
    // 모드 구분 변수
    private bool _isLoadMode = false; 
    private int _targetSlotIndex = -1;
    private bool _isDeleteMode = false; 

    public bool isOpened = false;

    void Awake()
    {
        // 슬롯 초기화 (중복 방지 로직 추가)
        foreach (Transform child in slotContainer) Destroy(child.gameObject);
        _uiSlots.Clear();

        for (int i = 0; i < SaveSystem.SlotCount; i++)
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

    private void Update()
    {
        if(isOpened) RefreshAllSlots();
    }

    public void Open(bool isLoadMode)
    {
        Debug.Log($"SaveMenu Open: LoadMode = {isLoadMode}");
        
        isOpened = true;
        _isLoadMode = isLoadMode;
        savePageRoot.SetActive(true);
        confirmPopup.SetActive(false);
        
        RefreshAllSlots();
    }

    public void Open() { Open(false); }

    public void Close()
    {
        isOpened = false;
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
            // 1. 실제 파일 삭제 (SaveSystem은 DontDestroyOnLoad라 메인 메뉴에도 존재함)
            SaveSystem.Instance.DeleteSave(_targetSlotIndex);
            
            // 2. [수정] StageManager가 존재하는 경우에만 현재 슬롯 정보 갱신
            // (메인 메뉴에서는 StageManager가 없을 수 있으므로 null 체크 필수)
            if (StageManager.Instance != null)
            {
                if (StageManager.Instance.CurrentSlotIndex == _targetSlotIndex)
                {
                    StageManager.Instance.CurrentSlotIndex = -1;
                }
            }
        }
        else 
        {
            // 저장 로직 (메인 메뉴에서는 저장할 일이 없으므로 보통 이쪽으로는 안 옴)
            // 하지만 안전을 위해 여기도 null 체크를 해두는 것이 좋습니다.
            
            // 주의: SaveSystem.Save() 내부에서도 StageManager를 쓰기 때문에
            // 메인 메뉴에서 저장을 시도하면 SaveSystem 안에서 에러가 날 수 있음.
            // (현재 로직상 메인 메뉴는 Load Mode로 열리므로 괜찮음)
            
            if (StageManager.Instance != null)
            {
                SaveSystem.Instance.Save(_targetSlotIndex);
                StageManager.Instance.CurrentSlotIndex = _targetSlotIndex;
            }
            else
            {
                Debug.LogWarning("메인 메뉴(StageManager 없음)에서는 게임을 저장할 수 없습니다.");
            }
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