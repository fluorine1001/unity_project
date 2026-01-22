using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // 씬 이동을 위해 필요
using TMPro;

public class SaveMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject savePageRoot;
    public GameObject confirmPopup;
    public TextMeshProUGUI popupMessageText;

    [Header("Slots")]
    public Transform slotContainer;
    public GameObject slotPrefab;
    public int slotCount = 3;

    private List<SaveSlotUI> _uiSlots = new List<SaveSlotUI>();
    
    // ✅ 모드 구분 변수
    private bool _isLoadMode = false; 
    private int _targetSlotIndex = -1;
    private bool _isDeleteMode = false; 

    void Awake()
    {
        // 슬롯 생성 (최초 1회)
        for (int i = 0; i < slotCount; i++)
        {
            GameObject go = Instantiate(slotPrefab, slotContainer);
            SaveSlotUI ui = go.GetComponent<SaveSlotUI>();
            ui.Init(this, i);
            _uiSlots.Add(ui);
        }

        savePageRoot.SetActive(false);
        confirmPopup.SetActive(false);
    }

    // ✅ [중요] 외부에서 열 때 모드를 설정함 (true: 로드모드, false: 저장모드)
    public void Open(bool isLoadMode)
    {

        Debug.Log("Open 함수 호출됨! 이제 패널을 켭니다."); // 이 로그가 뜨는지 확인
        
        _isLoadMode = isLoadMode;
        savePageRoot.SetActive(true);
        confirmPopup.SetActive(false);
        
        RefreshAllSlots();
    }

    // 기본 Open은 저장 모드로 동작 (기존 코드 호환용)
    public void Open() { Open(false); }

    public void Close()
    {
        savePageRoot.SetActive(false);
        confirmPopup.SetActive(false);
    }

    private void RefreshAllSlots()
    {
        // 리스트 인덱스를 사용하기 위해 for문으로 변경
        for (int i = 0; i < _uiSlots.Count; i++)
        {
            SaveSlotUI slot = _uiSlots[i];
            slot.Refresh(); // 슬롯 UI 갱신

            // 해당 슬롯에 데이터가 있는지 확인
            bool hasData = SaveSystem.Instance.Load(i) != null;

            if (slot.deleteButton != null)
            {
                // 조건: (로드 모드가 아님) AND (데이터가 있음)
                bool showDeleteButton = (!_isLoadMode) && hasData;
                slot.deleteButton.gameObject.SetActive(showDeleteButton);
            }
        }
    }

    // 슬롯 클릭 시 동작 (핵심 로직)
    public void OnSlotClicked(int index, bool hasData)
    {
        _targetSlotIndex = index;
        _isDeleteMode = false;

        if (_isLoadMode)
        {
            // === [로드 모드] ===
            if (!hasData)
            {
                // 빈 슬롯 클릭 -> 아무 일도 안 함
                return;
            }
            
            // 데이터 있음 -> 즉시 로드 실행
            LoadGameAndStart(index);
        }
        else
        {
            // === [저장 모드] ===
            if (hasData) ShowPopup("Save file already exists.\nWould you like to overwrite it?");
            else ProcessAction(); // 빈 슬롯은 즉시 저장
        }
    }

    // 삭제 버튼 클릭 (로드 모드에선 버튼이 숨겨지므로 호출될 일 없음)
    public void OnDeleteClicked(int index)
    {
        _targetSlotIndex = index;
        _isDeleteMode = true;
        ShowPopup("Are you sure you want to delete it?\nIt cannot be recovered.");
    }

    private void ShowPopup(string message)
    {
        if (popupMessageText != null) popupMessageText.text = message;
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

        if (_isDeleteMode) SaveSystem.Instance.DeleteSave(_targetSlotIndex);
        else SaveSystem.Instance.Save(_targetSlotIndex);
        
        RefreshAllSlots();
    }

    // ✅ 데이터를 불러와서 게임 시작
    private void LoadGameAndStart(int slotIndex)
    {
        // 1. 데이터 불러오기
        SaveData data = SaveSystem.Instance.Load(slotIndex);
        if (data == null) 
        {
            Debug.LogError("로드할 데이터가 없습니다!");
            return;
        }

        // 2. 다음 씬으로 데이터 넘겨주기 (StageManager가 있다고 가정)
        StageManager.PendingLoadData = data;

        // 3. 씬 이름 조합하기 (sceneIndex가 k면 -> "GameScene_k")
        // 예: data.sceneIndex가 1이면 -> "GameScene_1"
        string sceneName = $"_Game/Scenes/GameScene_{data.sceneIndex}";

        Debug.Log($"씬 로드 시작: {sceneName}"); // 확인용 로그

        // 4. 해당 이름의 씬 로드
        SceneManager.LoadScene(sceneName);
    }
}