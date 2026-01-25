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
    public List<ManualEntrySO> allEntries; 

    [Header("Auto Load Settings")]
    public string resourcePath = "Manuals"; 
    public string nameFilter = ""; 

    [Header("Left Panel UI")]
    public Transform listContainer;       
    public GameObject listButtonPrefab;   
    public TMP_InputField searchInput;    
    public Transform tagContainer;        
    public GameObject tagTogglePrefab;    

    [Header("Localization Keys")]
    public string searchPlaceholderKey = "UI_SEARCH_PLACEHOLDER";

    // ✅ [추가] 폰트와 비율을 묶는 구조체
    [System.Serializable]
    public struct SearchFontData
    {
        public TMP_FontAsset fontAsset;
        [Tooltip("이 폰트의 고유 스케일 비율 (기본값 1.0). 1.2면 20% 커짐.")]
        public float scaleRatio; 
    }

    [Header("Font Settings")]
    [Tooltip("검색창에서 사용할 폰트들과 각각의 비율을 설정하세요.")]
    // ✅ [변경] 단순 리스트 -> 구조체 리스트
    public List<SearchFontData> searchSupportFonts;

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

    private string currentSearchText = "";
    private HashSet<string> selectedTags = new HashSet<string>(); 
    private ManualEntrySO currentSelectedEntry = null;
    
    private float originalPlaceholderSize = 0f;
    private float originalInputTextSize = 0f;

    void Awake()
    {
        LoadEntriesFromResources();

        if (searchInput != null && searchInput.placeholder != null)
        {
            var placeholderText = searchInput.placeholder as TMP_Text;
            if (placeholderText != null)
                originalPlaceholderSize = placeholderText.fontSize;
        }

        if (searchInput != null && searchInput.textComponent != null)
        {
            originalInputTextSize = searchInput.textComponent.fontSize;
        }
    }

    void OnEnable()
    {
        if (allEntries == null || allEntries.Count == 0)
            LoadEntriesFromResources();

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        ResetUI();
    }

    void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }

        if (searchInput != null)
        {
            searchInput.text = "";
            searchInput.DeactivateInputField();
        }
    }

    void OnLanguageChanged()
    {
        InitializeTags(); 
        RefreshList();    
        UpdateSearchInputStyle();

        if (currentSelectedEntry != null)
        {
            DisplayEntry(currentSelectedEntry);
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
            searchInput.DeactivateInputField();
        }
        
        UpdateSearchInputStyle();

        InitializeTags(); 
        RefreshList();    
        ClearViewer();    
    }

    // ✅ [수정] 폰트별 스케일 비율(r)을 적용하여 동적 폰트 생성
    // ✅ [수정] var 키워드를 사용하여 타입 충돌 방지
    private void UpdateSearchInputStyle()
    {
        if (searchInput == null) return;
        if (LocalizationManager.Instance == null) return;

        // 1. 현재 언어의 메인 폰트 에셋 찾기
        var currentLangData = LocalizationManager.Instance.GetCurrentLanguageData();
        TMP_FontAsset originalMainFont = currentLangData.fontAsset;
        
        float globalRatio = (currentLangData.fontRatio <= 0) ? 1.0f : currentLangData.fontRatio;

        // 2. 검색창 지원 폰트 리스트에서 메인 폰트 비율 찾기
        float mainFontSelfScale = 1.0f;
        if (searchSupportFonts != null)
        {
            foreach (var data in searchSupportFonts)
            {
                if (data.fontAsset == originalMainFont)
                {
                    mainFontSelfScale = (data.scaleRatio <= 0) ? 1.0f : data.scaleRatio;
                    break;
                }
            }
        }

        // 3. 메인 폰트 복제 및 스케일 적용
        TMP_FontAsset dynamicMainFont = Instantiate(originalMainFont);
        
        // 🔥 [수정] FaceInfo -> var 로 변경
        var mainFaceInfo = dynamicMainFont.faceInfo; 
        mainFaceInfo.scale *= mainFontSelfScale;
        dynamicMainFont.faceInfo = mainFaceInfo;

        // 4. Fallback 리스트 구성
        List<TMP_FontAsset> fallbackList = new List<TMP_FontAsset>();
        
        if (searchSupportFonts != null)
        {
            foreach (var data in searchSupportFonts)
            {
                if (data.fontAsset != null && data.fontAsset != originalMainFont) 
                {
                    TMP_FontAsset dynamicFallback = Instantiate(data.fontAsset);
                    
                    float fallbackScale = (data.scaleRatio <= 0) ? 1.0f : data.scaleRatio;

                    // 🔥 [수정] FaceInfo -> var 로 변경
                    var faceInfo = dynamicFallback.faceInfo;
                    faceInfo.scale *= fallbackScale; 
                    dynamicFallback.faceInfo = faceInfo;

                    fallbackList.Add(dynamicFallback);
                }
            }
        }
        
        dynamicMainFont.fallbackFontAssetTable = fallbackList;

        // 5. Placeholder 업데이트
        if (searchInput.placeholder != null)
        {
            var placeholderText = searchInput.placeholder as TMP_Text;
            if (placeholderText != null)
            {
                placeholderText.text = GetLocalizedText(searchPlaceholderKey);
                placeholderText.font = dynamicMainFont; 
                placeholderText.fontSize = Mathf.Round(originalPlaceholderSize * globalRatio);
            }
        }

        // 6. 입력 텍스트(Text Component) 업데이트
        if (searchInput.textComponent != null)
        {
            searchInput.textComponent.font = dynamicMainFont;
            searchInput.textComponent.fontSize = Mathf.Round(originalInputTextSize * globalRatio);
        }
    }

    // ... (이하 기존 코드 동일: Start, InitializeTags, RefreshList 등) ...
    // 생략된 부분은 수정할 필요 없이 그대로 두시면 됩니다.

    void Start()
    {
        if (searchInput != null)
        {
            searchInput.onSubmit.AddListener(OnSearchSubmit);
            searchInput.onEndEdit.AddListener((text) => currentSearchText = text);
        }
    }

    void InitializeTags()
    {
        foreach (Transform child in tagContainer) Destroy(child.gameObject);
        HashSet<string> allTagKeys = new HashSet<string>();
        foreach (var entry in allEntries)
        {
            if (entry.tagKeys != null)
                foreach (var tagKey in entry.tagKeys) allTagKeys.Add(tagKey);
        }

        foreach (var tagKey in allTagKeys)
        {
            GameObject go = Instantiate(tagTogglePrefab, tagContainer);
            TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp) 
            {
                tmp.text = GetLocalizedText(tagKey);
                ApplyFontWithRatio(tmp); 
            }
            Toggle toggle = go.GetComponent<Toggle>();
            toggle.isOn = selectedTags.Contains(tagKey);
            toggle.group = null; 
            Image bgImage = toggle.targetGraphic as Image;
            if (bgImage != null) bgImage.color = toggle.isOn ? tagSelectedColor : tagNormalColor;
            toggle.onValueChanged.AddListener((isOn) => 
            {
                OnTagToggled(tagKey, isOn);
                if (bgImage != null) bgImage.color = isOn ? tagSelectedColor : tagNormalColor;
            });
        }
    }

    void OnTagToggled(string tagKey, bool isOn)
    {
        if (isOn) selectedTags.Add(tagKey);
        else selectedTags.Remove(tagKey);
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
            string translatedTitle = GetLocalizedText(entry.titleKey);
            bool searchMatch = isSearchEmpty || translatedTitle.ToLower().Contains(currentSearchText.ToLower());
            bool isTagEmpty = selectedTags.Count == 0;
            bool tagMatch = isTagEmpty || selectedTags.All(t => entry.tagKeys.Contains(t));
            return searchMatch && tagMatch;
        }).ToList();

        foreach (var entry in filteredList)
        {
            GameObject btn = Instantiate(listButtonPrefab, listContainer);
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if(btnText)
            {
                btnText.text = GetLocalizedText(entry.titleKey);
                ApplyFontWithRatio(btnText);
            }
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
        foreach (var block in entry.blocks) CreateBlock(block);
        Canvas.ForceUpdateCanvases();
    }

    void CreateBlock(ManualBlock block)
    {
        GameObject go = null;
        float ratio = 1.0f;
        if (LocalizationManager.Instance != null)
        {
            ratio = LocalizationManager.Instance.GetCurrentLanguageData().fontRatio;
            if (ratio <= 0) ratio = 1.0f;
        }

        switch (block.type)
        {
            case ManualBlockType.Heading1:
            case ManualBlockType.Heading2:
            case ManualBlockType.BodyText:
                go = Instantiate(textBlockPrefab, contentContainer);
                TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                tmp.text = GetLocalizedText(block.textKey);
                ApplyFontOnly(tmp); 
                float targetSize = bodySize;
                if (block.type == ManualBlockType.Heading1) { targetSize = h1Size; tmp.fontStyle = FontStyles.Normal; }
                else if (block.type == ManualBlockType.Heading2) { targetSize = h2Size; tmp.fontStyle = FontStyles.Normal; }
                else { targetSize = bodySize; tmp.fontStyle = FontStyles.Normal; }
                tmp.fontSize = Mathf.Round(targetSize * ratio);
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

    private string GetLocalizedText(string key)
    {
        if (LocalizationManager.Instance != null) return LocalizationManager.Instance.GetText(key);
        return key; 
    }

    // 헬퍼 함수들 (TMP_Text로 타입 통일)
    private void ApplyFontOnly(TMP_Text tmp)
    {
        if (tmp == null) return;
        if (LocalizationManager.Instance != null)
        {
            var data = LocalizationManager.Instance.GetCurrentLanguageData();
            if (data.fontAsset != null) tmp.font = data.fontAsset;
        }
    }

    private void ApplyFontWithRatio(TMP_Text tmp)
    {
        if (tmp == null) return;
        if (LocalizationManager.Instance != null)
        {
            var data = LocalizationManager.Instance.GetCurrentLanguageData();
            if (data.fontAsset != null) tmp.font = data.fontAsset;

            float ratio = (data.fontRatio <= 0) ? 1.0f : data.fontRatio;
            tmp.fontSize = Mathf.Round(tmp.fontSize * ratio);
        }
    }

    private void ApplyFontWithSpecificSize(TMP_Text tmp, float baseSize)
    {
        if (tmp == null) return;
        if (LocalizationManager.Instance != null)
        {
            var data = LocalizationManager.Instance.GetCurrentLanguageData();
            if (data.fontAsset != null) tmp.font = data.fontAsset;

            float ratio = (data.fontRatio <= 0) ? 1.0f : data.fontRatio;
            tmp.fontSize = Mathf.Round(baseSize * ratio);
        }
    }

    private void LoadEntriesFromResources()
    {
        ManualEntrySO[] loadedData = Resources.LoadAll<ManualEntrySO>(resourcePath);
        allEntries = loadedData
            .Where(entry => string.IsNullOrEmpty(nameFilter) || entry.name.Contains(nameFilter))
            .OrderBy(entry => entry.name)
            .ToList();
    }
}