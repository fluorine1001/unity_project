using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityEngine.Video;

public class ManualMenuUI : MonoBehaviour
{
    [Header("System")]
    public UIManager uiManager;
    
    // ✅ Inspector 할당 해제해도 됨 (자동으로 채워짐)
    public List<ManualEntrySO> allEntries; 

    [Header("Auto Load Settings")]
    [Tooltip("Resources 폴더 내의 경로 (예: 'ManualData'). 비워두면 Resources 루트에서 찾습니다.")]
    public string resourcePath = "Manuals"; 
    [Tooltip("파일 이름에 이 문자열이 포함된 것만 로드합니다. (비워두면 모두 로드)")]
    public string nameFilter = ""; 

    [Header("Left Panel UI")]
    public Transform listContainer;       
    public GameObject listButtonPrefab;   
    public TMP_InputField searchInput;    
    public Transform tagContainer;        
    public GameObject tagTogglePrefab;    

    [Header("Tag Style")]
    public Color tagNormalColor = new Color(0.2f, 0.2f, 0.2f, 1f); 
    public Color tagSelectedColor = new Color(0.2f, 0.6f, 1f, 1f); 

    [Header("Right Panel UI")]
    public ScrollRect contentScrollRect;
    public Transform contentContainer;

    [Header("Block Prefabs")]
    public GameObject textBlockPrefab;
    public GameObject imageBlockPrefab;
    public GameObject videoBlockPrefab;
    public GameObject spacerBlockPrefab;

    [Header("Text Settings")]
    public float h1Size = 40f;
    public float h2Size = 32f;
    public float bodySize = 24f;

    // 내부 상태 변수
    private string currentSearchText = "";
    private HashSet<string> selectedTags = new HashSet<string>(); 
    private ManualEntrySO currentSelectedEntry = null;

    // ✅ Awake는 OnEnable보다 먼저 실행되므로 여기서 데이터 로드
    void Awake()
    {
        LoadEntriesFromResources();
    }

    // ✅ 리소스 폴더에서 SO 자동 로드 및 정렬 함수
    private void LoadEntriesFromResources()
    {
        // 1. Resources 폴더에서 로드
        ManualEntrySO[] loadedData = Resources.LoadAll<ManualEntrySO>(resourcePath);

        // 2. 필터링 및 정렬 (LINQ)
        allEntries = loadedData
            .Where(entry => string.IsNullOrEmpty(nameFilter) || entry.name.Contains(nameFilter)) // 이름 필터
            .OrderBy(entry => entry.entryTitle) // 제목 기준 가나다순 정렬
            //.OrderBy(entry => entry.name)     // (옵션) 파일명 기준 정렬 원하면 이걸로 교체
            .ToList();

        Debug.Log($"[ManualMenuUI] {allEntries.Count}개의 매뉴얼 항목을 로드했습니다.");
    }

    void OnEnable()
    {
        // 데이터가 아직 없으면 로드 시도 (안전장치)
        if (allEntries == null || allEntries.Count == 0)
            LoadEntriesFromResources();

        ResetUI();
    }

    // ✅ [추가] 메뉴가 꺼질 때 검색창 포커스 해제 (키보드 입력 먹통 방지)
    void OnDisable()
    {
        if (searchInput != null)
        {
            searchInput.text = ""; // 텍스트 비우기
            searchInput.DeactivateInputField(); // 포커스 해제 (커서 없애기)
        }
    }

    private void ResetUI()
    {
        currentSearchText = "";
        selectedTags.Clear();
        currentSelectedEntry = null;

        if (searchInput != null) 
        {
            searchInput.text = "";
            // ✅ 초기화 시에도 포커스 해제
            searchInput.DeactivateInputField();
        }

        InitializeTags(); 
        RefreshList();    
        ClearViewer();    
    }

    void Start()
    {
        if (searchInput != null)
        {
            searchInput.onSubmit.AddListener(OnSearchSubmit);
            searchInput.onEndEdit.AddListener((text) => currentSearchText = text);
        }
    }

    // ... (이하 기존 코드와 동일) ...
    // InitializeTags, RefreshList, OnEntryClicked 등 나머지 함수들은 그대로 유지하세요.
    
    // =================================================================
    // 1. 태그 관리 (색상 변경 기능 추가)
    // =================================================================
    void InitializeTags()
    {
        foreach (Transform child in tagContainer) Destroy(child.gameObject);

        HashSet<string> allTags = new HashSet<string>();
        foreach (var entry in allEntries)
        {
            if (entry.tags != null)
                foreach (var tag in entry.tags) allTags.Add(tag);
        }

        foreach (var tag in allTags)
        {
            GameObject go = Instantiate(tagTogglePrefab, tagContainer);
            
            TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp) tmp.text = tag;

            Toggle toggle = go.GetComponent<Toggle>();
            toggle.isOn = false; 
            toggle.group = null; 

            Image bgImage = toggle.targetGraphic as Image;
            if (bgImage != null) bgImage.color = tagNormalColor;

            toggle.onValueChanged.AddListener((isOn) => 
            {
                OnTagToggled(tag, isOn);
                if (bgImage != null) bgImage.color = isOn ? tagSelectedColor : tagNormalColor;
            });
        }
    }

    void OnTagToggled(string tag, bool isOn)
    {
        if (isOn) selectedTags.Add(tag);
        else selectedTags.Remove(tag);

        RefreshList(); 
    }

    void OnSearchSubmit(string text)
    {
        currentSearchText = text.Trim();
        RefreshList();
    }

    public void RefreshList()
    {
        foreach (Transform child in listContainer) Destroy(child.gameObject);

        var filteredList = allEntries.Where(entry => 
        {
            bool isSearchEmpty = string.IsNullOrEmpty(currentSearchText);
            bool searchMatch = isSearchEmpty || entry.entryTitle.ToLower().Contains(currentSearchText.ToLower());

            bool isTagEmpty = selectedTags.Count == 0;
            bool tagMatch = isTagEmpty || selectedTags.All(t => entry.tags.Contains(t));

            return searchMatch && tagMatch;
        }).ToList();

        foreach (var entry in filteredList)
        {
            GameObject btn = Instantiate(listButtonPrefab, listContainer);
            
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if(btnText) btnText.text = entry.entryTitle;

            btn.transform.localScale = Vector3.one; 
            
            btn.GetComponent<Button>().onClick.AddListener(() => OnEntryClicked(entry));
        }
    }

    public void OnEntryClicked(ManualEntrySO entry)
    {
        if (currentSelectedEntry == entry)
        {
            ClearViewer();
            currentSelectedEntry = null;
            return;
        }

        currentSelectedEntry = entry;
        DisplayEntry(entry);
    }

    public void DisplayEntry(ManualEntrySO entry)
    {
        ClearViewer();
        contentScrollRect.verticalNormalizedPosition = 1f; 

        foreach (var block in entry.blocks)
        {
            CreateBlock(block);
        }
        
        Canvas.ForceUpdateCanvases();
    }

    void CreateBlock(ManualBlock block)
    {
        GameObject go = null;

        switch (block.type)
        {
            case ManualBlockType.Heading1:
            case ManualBlockType.Heading2:
            case ManualBlockType.BodyText:
                go = Instantiate(textBlockPrefab, contentContainer);
                TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                tmp.text = block.textContent;
                
                if (block.type == ManualBlockType.Heading1) { 
                    tmp.fontSize = h1Size; 
                    tmp.fontStyle = FontStyles.Bold; 
                }
                else if (block.type == ManualBlockType.Heading2) { 
                    tmp.fontSize = h2Size; 
                    tmp.fontStyle = FontStyles.Bold; 
                }
                else { 
                    tmp.fontSize = bodySize; 
                    tmp.fontStyle = FontStyles.Normal; 
                }
                ApplyAlignment(tmp, block.alignment);
                break;

            case ManualBlockType.Image:
                go = Instantiate(imageBlockPrefab, contentContainer);
                Image img = go.GetComponentInChildren<Image>();
                img.sprite = block.imageContent;
                SetLayoutHeight(go, block.sizeValue);
                break;

            case ManualBlockType.Video:
                go = Instantiate(videoBlockPrefab, contentContainer);
                VideoPlayer vp = go.GetComponentInChildren<VideoPlayer>();
                if (vp) vp.clip = block.videoContent;
                SetLayoutHeight(go, block.sizeValue);
                break;

            case ManualBlockType.Spacer:
                go = Instantiate(spacerBlockPrefab, contentContainer);
                SetLayoutHeight(go, block.sizeValue);
                break;
        }

        if (go != null) go.transform.localScale = Vector3.one;
    }

    void SetLayoutHeight(GameObject go, float height)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
    }

    void ApplyAlignment(TextMeshProUGUI tmp, BlockAlignment align)
    {
        switch (align)
        {
            case BlockAlignment.Left: tmp.alignment = TextAlignmentOptions.Left; break;
            case BlockAlignment.Center: tmp.alignment = TextAlignmentOptions.Center; break;
            case BlockAlignment.Right: tmp.alignment = TextAlignmentOptions.Right; break;
        }
    }

    void ClearViewer()
    {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);
    }

    public void OnBackButtonClicked()
    {
        if (uiManager != null) uiManager.ShowMainMenu();
    }
}