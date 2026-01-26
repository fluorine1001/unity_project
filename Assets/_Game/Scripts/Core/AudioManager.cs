using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.SceneManagement; // 👈 필수 추가!

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance { get; private set; }

    [Header("Volume Settings")]
    [SerializeField] private string masterVcaPath = "vca:/Master";
    [SerializeField] private string masterSaveKey = "Vol_Master";

    private VCA masterVCA;
    private Dictionary<string, VCA> vcaDictionary = new Dictionary<string, VCA>();

    private List<EventInstance> eventInstances;
    private EventInstance musicEventInstance;

    private void Awake()
    {
        if (instance != null)
        {
            // 씬 이동 시 중복 생성되는 매니저 삭제
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        eventInstances = new List<EventInstance>();
    }

    // ✅ 이벤트 등록 (씬이 로드될 때마다 알림을 받음)
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // ✅ 이벤트 해제 (메모리 누수 방지)
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        InitializeMasterVolume();
        // Start에서는 음악 재생을 호출하지 않음 (OnSceneLoaded가 대신 함)
    }

    // 🔥 [핵심] 씬 로드가 완료될 때마다 자동으로 실행되는 함수
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. 기존 음악 정지 (메인 메뉴 -> 게임, 혹은 게임 -> 게임 이동 시 겹침 방지)
        StopMusic();

        // 2. 씬 이름에 따라 음악 재생
        // (StageManager가 아직 초기화 전일 수도 있으므로 씬 이름으로 체크하는 게 안전합니다)
        if (scene.name == "GameScene_1")
        {
            InitializeMusic(FMODEvents.instance.Scene1Music);
        }
        else if (scene.name == "GameScene_2")
        {
            InitializeMusic(FMODEvents.instance.Scene2Music);
        }
        // 메인 메뉴(MainMenuScene)인 경우 위 조건에 안 걸리므로 음악이 안 나옴 (의도한 대로)
    }

    public void StopMusic()
    {
        if (musicEventInstance.isValid())
        {
            musicEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicEventInstance.release();
        }
    }

    // ... (이 아래는 기존 코드와 동일) ...

    private void InitializeMasterVolume()
    {
        masterVCA = RuntimeManager.GetVCA(masterVcaPath);
        float savedVol = PlayerPrefs.GetFloat(masterSaveKey, 1.0f);
        SetMasterVolume(savedVol);
    }

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
        PlayerPrefs.SetFloat(masterSaveKey, sliderValue);
        PlayerPrefs.Save();
        ApplyVolumeToVCA(masterVCA, sliderValue);
    }

    public void SetCategoryVolume(VolumeCategorySO category, float sliderValue)
    {
        PlayerPrefs.SetFloat(category.saveKey, sliderValue);
        PlayerPrefs.Save();

        if (vcaDictionary.TryGetValue(category.saveKey, out VCA vca))
        {
            ApplyVolumeToVCA(vca, sliderValue);
        }
    }

    public float GetMasterVolume() => PlayerPrefs.GetFloat(masterSaveKey, 1.0f);
    public float GetCategoryVolume(VolumeCategorySO category) => PlayerPrefs.GetFloat(category.saveKey, 1.0f);

    private void ApplyVolumeToVCA(VCA vca, float sliderValue)
    {
        vca.setVolume(sliderValue);
    }

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
        StopMusic(); // 안전장치
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