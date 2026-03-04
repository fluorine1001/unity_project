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

        // ✅ [추가] 메뉴가 꺼지면 무조건 BGM 다시 재생
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PauseMusic(false);
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

        // 1. Vertical Layout Group 설정
        VerticalLayoutGroup layoutGroup = contentContainer.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null) layoutGroup = contentContainer.gameObject.AddComponent<VerticalLayoutGroup>();

        // 🔴 중요: 그룹 자체의 Spacing은 0으로 만듭니다. (우리가 수동으로 공간을 넣을 것이므로)
        layoutGroup.spacing = 0f; 
        
        // 필수 설정
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        // 2. 블록 생성 루프
        foreach (var block in entry.blocks) 
        {
            // A. 실제 블록 생성
            CreateBlock(block);

            // B. [핵심] 블록 아래에 투명한 간격(Spacer) 생성
            // 블록에 개별 설정된 값이 있으면(-1보다 크면) 그걸 쓰고, 아니면 페이지 기본값 사용
            float gapSize = (block.spacingAfter >= 0) ? block.spacingAfter : entry.defaultSpacing;
            
            if (gapSize > 0)
            {
                CreateGap(gapSize);
            }
        }
        
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer.GetComponent<RectTransform>());
    }

    // ✅ [신규 함수] 투명한 여백을 만드는 함수
    void CreateGap(float height)
    {
        GameObject gap = new GameObject("Gap_Auto");
        gap.transform.SetParent(contentContainer);
        gap.transform.localScale = Vector3.one;

        // 투명한 공간을 차지하게 하기 위해 LayoutElement 추가
        LayoutElement le = gap.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleHeight = 0;
    }

    void CreateBlock(ManualBlock block)
    {
        GameObject go = null;
        float ratio = 1.0f;
        var currentLangData = new LocalizationManager.LanguageFontData();
        if (LocalizationManager.Instance != null)
        {
            ratio = LocalizationManager.Instance.GetCurrentLanguageData().fontRatio;
            if (ratio <= 0) ratio = 1.0f;
            currentLangData = LocalizationManager.Instance.GetCurrentLanguageData();
        }

        switch (block.type)
        {
            case ManualBlockType.Heading1:
            case ManualBlockType.Heading2:
            case ManualBlockType.BodyText:
                go = Instantiate(textBlockPrefab, contentContainer);
                TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                // 🔴 [수정] 모든 텍스트에 대해 단어 단위 줄바꿈 적용
                // 2. 텍스트 가져오기
                string contentText = GetLocalizedText(block.textKey);

                // ✅ [핵심] 언어 설정에 따라 단어 줄바꿈 적용 여부 결정
                if (LocalizationManager.Instance != null && currentLangData.useWordWrapping)
                {
                    // 체크된 언어라면 <nobr> 태그 함수 적용
                    tmp.text = ApplyWordWrapping(contentText); 
                }
                else
                {
                    // 체크 안 된 언어라면 그냥 원본 텍스트 사용
                    tmp.text = contentText; 
                }
                ApplyFontOnly(tmp); 
                float targetSize = bodySize;
                if (block.type == ManualBlockType.Heading1) { targetSize = h1Size; tmp.fontStyle = FontStyles.Normal; }
                else if (block.type == ManualBlockType.Heading2) { targetSize = h2Size; tmp.fontStyle = FontStyles.Normal; }
                else { targetSize = bodySize; tmp.fontStyle = FontStyles.Normal; }
                tmp.fontSize = Mathf.Round(targetSize * ratio);
                ApplyAlignment(tmp, block.alignment);
                tmp.lineSpacing = block.lineSpacing;           // 줄 간격
                tmp.paragraphSpacing = block.paragraphSpacing; // 문단 간격
                break;
            case ManualBlockType.Image:
                go = Instantiate(imageBlockPrefab, contentContainer);
                
                // 1. 기본 위치 초기화
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                // 2. 컴포넌트 가져오기
                Image img = go.GetComponentInChildren<Image>(); // 자식에 있는 이미지
                LayoutElement le = go.GetComponent<LayoutElement>(); // 부모에 있는 레이아웃 엘리먼트
                if (le == null) le = go.AddComponent<LayoutElement>();

                if (block.imageContent != null)
                {
                    img.sprite = block.imageContent;
                    
                    // 3. 비율 계산 (핵심 로직)
                    float spriteWidth = block.imageContent.rect.width;
                    float spriteHeight = block.imageContent.rect.height;
                    
                    // 부모(Content)의 너비 (혹은 화면 너비)
                    // (RectTransform.rect.width가 0일 경우를 대비해 안전장치 추가)
                    float parentWidth = contentContainer.GetComponent<RectTransform>().rect.width;
                    if (parentWidth <= 0) parentWidth = 1000f; // 임시 기본값
                    
                    // block.sizeValue는 0.0 ~ 1.0 사이의 비율 (예: 1.0이면 가로 꽉 참)
                    // 만약 sizeValue가 100, 200 같은 픽셀 단위라면 로직을 조금 바꿔야 합니다.
                    // 여기서는 "화면 가로 대비 비율"로 가정합니다.
                    float targetWidth = parentWidth * block.sizeValue;

                    // 원본 비율에 맞춘 높이 계산
                    float aspectRatio = (spriteHeight > 0) ? (spriteWidth / spriteHeight) : 1.0f;
                    float targetHeight = targetWidth / aspectRatio;

                    // 4. Layout Element에 값 주입 (강제 적용)
                    le.ignoreLayout = false;
                    le.minWidth = targetWidth;       // 최소 너비 확보
                    le.preferredHeight = targetHeight; // 높이 확보 (중요)
                    le.flexibleHeight = 0;           // 억지로 늘어나지 않게
                    le.flexibleWidth = 0;            // 억지로 늘어나지 않게
                    
                    // 5. 이미지 컴포넌트 설정
                    img.preserveAspect = true; // 이미지 찌그러짐 방지
                }
                else
                {
                    go.SetActive(false); // 이미지 없으면 숨김
                }
                break;
            case ManualBlockType.Video:
                // 1. 프리팹 생성 및 초기화
                go = Instantiate(videoBlockPrefab, contentContainer);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                // 2. 주요 컴포넌트 가져오기
                var rawImg = go.GetComponentInChildren<UnityEngine.UI.RawImage>(); 
                var vp = go.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();
                // 계층 구조 변경에도 안전하게 찾기 위해 true(includeInactive) 사용
                var playButton = go.transform.GetComponentInChildren<UnityEngine.UI.Button>(true); 
                var volumeSlider = go.transform.GetComponentInChildren<UnityEngine.UI.Slider>(true);
                
                // 3. 오디오 소스 설정 (UI 사운드용 2D 설정)
                var audioSource = go.GetComponentInChildren<AudioSource>();
                if (audioSource == null) audioSource = go.AddComponent<AudioSource>();
                
                audioSource.spatialBlend = 0f;  // 3D 효과 제거 (소리가 작게 들리는 원인 방지)
                audioSource.playOnAwake = false; // 자동 재생 방지

                // 4. 레이아웃 엘리먼트 (크기 제어용)
                var leVideo = go.GetComponent<LayoutElement>();
                if (leVideo == null) leVideo = go.AddComponent<LayoutElement>();

                // 5. 비디오 콘텐츠 유효성 검사 및 설정 시작
                if (block.videoContent != null && vp != null)
                {
                    vp.clip = block.videoContent;

                    // --- [A. 크기 및 비율 계산 (Multiplier 방식)] ---
                    float origWidth = block.videoContent.width;
                    float origHeight = block.videoContent.height;
                    
                    // sizeValue가 0이면 1배(원본), 아니면 입력된 값을 배율로 사용
                    float multiplier = (block.sizeValue > 0) ? block.sizeValue : 1.0f;
                    
                    float finalWidth = origWidth * multiplier;
                    float finalHeight = origHeight * multiplier;

                    // LayoutElement에 반영 (리스트 내 공간 확보)
                    leVideo.minWidth = finalWidth;
                    leVideo.preferredWidth = finalWidth;
                    leVideo.minHeight = finalHeight;
                    leVideo.preferredHeight = finalHeight;

                    // 부모 RectTransform 크기 확정 (자식 UI 앵커 기준점)
                    RectTransform rect = go.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(finalWidth, finalHeight);

                    // 재생 버튼 크기도 배율에 맞춰 조정
                    if (playButton != null) 
                    {
                        playButton.transform.localScale = Vector3.one * multiplier;
                    }

                    // --- [B. 화면(RenderTexture) 생성 및 연결] ---
                    // 비디오 해상도에 딱 맞는 텍스처 생성
                    RenderTexture rt = new RenderTexture((int)origWidth, (int)origHeight, 0);
                    vp.targetTexture = rt;
                    rawImg.texture = rt;

                    // --- [C. 오디오 연결 로직 (순서 중요)] ---
                    vp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.AudioSource;
                    vp.SetTargetAudioSource(0, audioSource); 
                    vp.EnableAudioTrack(0, true);            

                    // --- [D. 슬라이더 드래그 상태 감지용 변수] ---
                    // 람다 식(Closure)에서 공유하기 위해 지역 변수로 선언
                    bool isDraggingSlider = false;

                    // --- [E. 볼륨 슬라이더 설정] ---
                    if (block.enableAudio)
                    {
                        audioSource.mute = false;
                        audioSource.volume = block.initialVolume;
                        
                        if (volumeSlider != null)
                        {
                            volumeSlider.gameObject.SetActive(true);
                            volumeSlider.value = block.initialVolume;
                            
                            // 이벤트 리스너 초기화 및 재등록
                            volumeSlider.onValueChanged.RemoveAllListeners();
                            volumeSlider.onValueChanged.AddListener((val) => audioSource.volume = val);

                            // 🔥 [핵심] EventTrigger를 사용해 드래그 중인지 감지
                            var trigger = volumeSlider.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                            if (trigger == null) trigger = volumeSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                            
                            trigger.triggers.Clear(); // 기존 트리거 초기화

                            // 1. 눌렀을 때 (PointerDown) -> 드래그 시작
                            var entryDown = new UnityEngine.EventSystems.EventTrigger.Entry();
                            entryDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
                            entryDown.callback.AddListener((data) => { isDraggingSlider = true; });
                            trigger.triggers.Add(entryDown);

                            // 2. 뗐을 때 (PointerUp) -> 드래그 종료
                            var entryUp = new UnityEngine.EventSystems.EventTrigger.Entry();
                            entryUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
                            entryUp.callback.AddListener((data) => { isDraggingSlider = false; });
                            trigger.triggers.Add(entryUp);
                        }
                    }
                    else
                    {
                        // 오디오 비활성화 시 처리
                        audioSource.mute = true;
                        audioSource.volume = 0f;
                        if (volumeSlider != null) volumeSlider.gameObject.SetActive(false);
                    }

                    // --- [F. 화면 클릭 시 재생/일시정지 (슬라이더 예외 처리 포함)] ---
                    var videoClickBtn = rawImg.GetComponent<UnityEngine.UI.Button>();
                    if (videoClickBtn == null) videoClickBtn = rawImg.gameObject.AddComponent<UnityEngine.UI.Button>();
                    
                    // 버튼 깜빡임 효과 제거
                    videoClickBtn.transition = UnityEngine.UI.Selectable.Transition.None; 

                    videoClickBtn.onClick.RemoveAllListeners();
                    videoClickBtn.onClick.AddListener(() => 
                    {
                        // 🔥 슬라이더를 조작 중이라면 영상 클릭 이벤트 무시!
                        if (isDraggingSlider) return;

                        if (vp.isPlaying)
                        {
                            // 실행 중 -> 일시정지
                            vp.Pause();
                            if (playButton != null) playButton.gameObject.SetActive(true);
                            
                            // 비디오 멈춤 -> FMOD BGM 다시 재생
                            if (AudioManager.instance != null) AudioManager.instance.PauseMusic(false);
                        }
                        else
                        {
                            // 멈춤 -> 재생
                            vp.Play();
                            if (playButton != null) playButton.gameObject.SetActive(false);

                            // 비디오 재생 -> FMOD BGM 일시정지
                            if (AudioManager.instance != null) AudioManager.instance.PauseMusic(true);
                        }
                    });

                    // --- [G. 초기 재생 상태 및 썸네일 처리] ---
                    if (block.autoPlay)
                    {
                        vp.playOnAwake = true;
                        vp.isLooping = true;
                        if (playButton != null) playButton.gameObject.SetActive(false);
                        
                        // 자동 재생 시 BGM 즉시 정지
                        if (AudioManager.instance != null) AudioManager.instance.PauseMusic(true);
                        
                        vp.Play();
                    }
                    else
                    {
                        // 수동 재생 모드
                        vp.playOnAwake = false;
                        vp.isLooping = true;
                        
                        // 🟢 썸네일(첫 프레임) 보여주기 로직
                        vp.prepareCompleted += (source) => 
                        {
                            source.time = 0;
                            source.Play(); 
                            source.Pause(); // 아주 잠깐 재생 후 멈춰서 첫 화면을 그림
                        };

                        // 모든 설정 완료 후 준비 시작
                        vp.Prepare(); 

                        if (playButton != null)
                        {
                            playButton.gameObject.SetActive(true);
                            playButton.onClick.RemoveAllListeners();
                            playButton.onClick.AddListener(() => 
                            {
                                // 버튼 클릭 시 재생 시작
                                if (AudioManager.instance != null) AudioManager.instance.PauseMusic(true);
                                
                                vp.Play();
                                playButton.gameObject.SetActive(false);
                            });
                        }
                    }
                }
                else
                {
                    // 비디오 콘텐츠가 없으면 숨김
                    go.SetActive(false);
                }
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
        // ✅ [추가] 페이지를 넘길 때(기존 비디오가 삭제될 때) BGM 다시 재생
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PauseMusic(false);
        }
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
            
            // 🔴 [변경 전] 이름 순 정렬
            // .OrderBy(entry => entry.name) 
            
            // 🟢 [변경 후] ID 순서대로 정렬 (ID가 같으면 이름 순)
            .OrderBy(entry => entry.entryID) 
            .ThenBy(entry => entry.name) 
            
            .ToList();
    }

    // 모든 언어에 대해 단어 단위(공백 기준) 줄바꿈을 강제하는 함수
    private string ApplyWordWrapping(string sourceText)
    {
        if (string.IsNullOrEmpty(sourceText)) return "";

        // 1. 기존의 줄바꿈(\n)은 유지해야 하므로 줄 단위로 먼저 나눕니다.
        string[] lines = sourceText.Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            // 2. 각 줄을 공백(' ') 기준으로 쪼갭니다.
            string[] words = lines[i].Split(' ');
            
            for (int j = 0; j < words.Length; j++)
            {
                // 3. 빈 칸이 아니라면 단어 앞뒤에 <nobr> 태그를 붙입니다.
                // <nobr> : No Line Break (이 안에서는 줄바꿈 금지)
                if (!string.IsNullOrEmpty(words[j]))
                {
                    words[j] = $"<nobr>{words[j]}</nobr>";
                }
            }
            
            // 4. 단어들을 다시 공백으로 연결합니다.
            lines[i] = string.Join(" ", words);
        }

        // 5. 줄들을 다시 엔터(\n)로 연결합니다.
        return string.Join("\n", lines);
    }
}