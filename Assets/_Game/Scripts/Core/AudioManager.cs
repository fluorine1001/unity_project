using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance { get; private set; }

    [Header("Volume Settings")]
    [SerializeField] private string masterVcaPath = "vca:/Master"; // FMOD에서 만든 Master VCA 경로
    [SerializeField] private string masterSaveKey = "Vol_Master";

    private VCA masterVCA;
    private Dictionary<string, VCA> vcaDictionary = new Dictionary<string, VCA>();

    // 기존 변수들
    private List<EventInstance> eventInstances;
    private EventInstance musicEventInstance;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Found more than one Audio Manager in the scene.");
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // 볼륨 설정 유지를 위해 파괴 방지 추천

        eventInstances = new List<EventInstance>();
    }

    private void Start()
    {
        // 1. VCA 초기화 및 저장된 볼륨 적용
        InitializeMasterVolume();

        // 2. 배경음악 재생 (기존 로직)
        // StageManager가 있는지 확인 (없을 경우 에러 방지)
        if (StageManager.Instance != null)
        {
            if (StageManager.Instance.sceneIndex == 1) InitializeMusic(FMODEvents.instance.Scene1Music);
            else if (StageManager.Instance.sceneIndex == 2) InitializeMusic(FMODEvents.instance.Scene2Music);
        }
    }

    // --- Volume Control Logic ---

    private void InitializeMasterVolume()
    {
        masterVCA = RuntimeManager.GetVCA(masterVcaPath);
        float savedVol = PlayerPrefs.GetFloat(masterSaveKey, 1.0f); // 기본값 1.0 (100%)
        SetMasterVolume(savedVol);
    }

    // UI가 생성될 때 호출하여 해당 카테고리 VCA를 로드하고 초기화
    public void InitializeCategoryVolume(VolumeCategorySO category)
    {
        if (!vcaDictionary.ContainsKey(category.saveKey))
        {
            VCA vca = RuntimeManager.GetVCA(category.vcaPath);
            vcaDictionary.Add(category.saveKey, vca);
        }
        
        float savedVol = PlayerPrefs.GetFloat(category.saveKey, 1.0f);
        SetCategoryVolume(category, savedVol);
    }

    public void SetMasterVolume(float sliderValue)
    {
        // 값 저장
        PlayerPrefs.SetFloat(masterSaveKey, sliderValue);
        PlayerPrefs.Save();

        // 실제 볼륨 적용
        ApplyVolumeToVCA(masterVCA, sliderValue);
    }

    public void SetCategoryVolume(VolumeCategorySO category, float sliderValue)
    {
        // 값 저장
        PlayerPrefs.SetFloat(category.saveKey, sliderValue);
        PlayerPrefs.Save();

        // 캐싱된 VCA 찾아서 적용
        if (vcaDictionary.TryGetValue(category.saveKey, out VCA vca))
        {
            ApplyVolumeToVCA(vca, sliderValue);
        }
    }

    public float GetMasterVolume() => PlayerPrefs.GetFloat(masterSaveKey, 1.0f);
    public float GetCategoryVolume(VolumeCategorySO category) => PlayerPrefs.GetFloat(category.saveKey, 1.0f);

    // [핵심] 슬라이더(0~1) -> dB 기반 Logarithmic Volume 변환
    private void ApplyVolumeToVCA(VCA vca, float sliderValue)
    {
        // dB 변환 공식을 제거하고 값을 그대로 넣습니다.
        // 슬라이더 0.5(50%) -> FMOD 볼륨 0.5 (절반 크기)
        vca.setVolume(sliderValue);
    }


    // --- Existing FMOD Logic (유지) ---

    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public EventInstance CreateInstance(EventReference EventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(EventReference);
        eventInstances.Add(eventInstance);
        return eventInstance;
    }

    private void InitializeMusic(EventReference musicEventReference)
    {
        musicEventInstance = CreateInstance(musicEventReference);
        musicEventInstance.start();
    }

    private void CleanUp()
    {
        if (eventInstances != null)
        {
            foreach (EventInstance eventInstance in eventInstances)
            {
                eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                eventInstance.release();
            }
            eventInstances.Clear();
        }
    }

    private void OnDestroy()
    {
        CleanUp();
    }
}