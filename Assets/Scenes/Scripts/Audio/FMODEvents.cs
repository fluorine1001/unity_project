using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    // =======================
    // 1. 이벤트 경로 문자열
    // =======================
    [Header("Music")]
    [EventRef]
    [SerializeField] 
    private string scene1MusicPath = "event:/Music/Scene1BGM";

    [Header("SFX")]

    [Header("Player")]
    
    [EventRef]
    [SerializeField] 
    private string bulletLaunchedPath = "event:/SFX/Player/BulletLaunched";

    [EventRef]
    [SerializeField] 
    private string bulletAcceleratedPath = "event:/SFX/Player/BulletAccelerated";

    [EventRef]
    [SerializeField] 
    private string bulletDeceleratedPath = "event:/SFX/Player/BulletDecelerated";

    [EventRef]
    [SerializeField] 
    private string playerDashPath = "event:/SFX/Player/Dash";

    [Header("Objects")]

    [EventRef]
    [SerializeField] 
    private string boxPushedPath = "event:/SFX/Objects/BoxPushed";

    [EventRef]
    [SerializeField] 
    private string boxBrokenPath = "event:/SFX/Objects/BoxBroken";

    [EventRef]
    [SerializeField] 
    private string holeFilledPath = "event:/SFX/Objects/HoleFilled";


    [Header("UI")]

    [EventRef]
    [SerializeField] 
    private string menuPressedPath = "event:/SFX/UI/MenuPressed";

    [EventRef]
    [SerializeField] 
    private string menuClosedPath = "event:/SFX/UI/MenuClosed";

    // =======================
    // 2. 외부에 공개되는 EventReference
    // =======================
    public EventReference Scene1Music    { get; private set; }
    public EventReference BulletLaunched { get; private set; }
    public EventReference BulletAccelerated     { get; private set; }
    public EventReference BulletDecelerated     { get; private set; }
    public EventReference BoxPushed      { get; private set; }
    public EventReference BoxBroken      { get; private set; }
    public EventReference PlayerDash     { get; private set; }
    public EventReference MenuPressed    { get; private set; }
    public EventReference MenuClosed     { get; private set; }
    public EventReference HoleFilled     { get; private set; }

    // =======================
    // 3. 싱글턴 인스턴스
    // =======================
    public static FMODEvents instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("Found more than one FMOD Events instance in the scene.");
            return;
        }

        instance = this;

        // 경로 문자열 → EventReference 변환
        InitEventReferences();
    }

    private void InitEventReferences()
    {
        if (!string.IsNullOrEmpty(scene1MusicPath))
            Scene1Music = RuntimeManager.PathToEventReference(scene1MusicPath);

        if (!string.IsNullOrEmpty(bulletLaunchedPath))
            BulletLaunched = RuntimeManager.PathToEventReference(bulletLaunchedPath);
        
        if (!string.IsNullOrEmpty(bulletAcceleratedPath))
            BulletAccelerated = RuntimeManager.PathToEventReference(bulletAcceleratedPath);

        if (!string.IsNullOrEmpty(bulletDeceleratedPath))
            BulletDecelerated = RuntimeManager.PathToEventReference(bulletDeceleratedPath);

        if (!string.IsNullOrEmpty(boxPushedPath))
            BoxPushed = RuntimeManager.PathToEventReference(boxPushedPath);
        
        if (!string.IsNullOrEmpty(boxBrokenPath))
            BoxBroken = RuntimeManager.PathToEventReference(boxBrokenPath);

        if (!string.IsNullOrEmpty(playerDashPath))
            PlayerDash = RuntimeManager.PathToEventReference(playerDashPath);

        if (!string.IsNullOrEmpty(menuPressedPath))
            MenuPressed = RuntimeManager.PathToEventReference(menuPressedPath);

        if (!string.IsNullOrEmpty(menuClosedPath))
            MenuClosed = RuntimeManager.PathToEventReference(menuClosedPath);
        
        if (!string.IsNullOrEmpty(holeFilledPath))
            HoleFilled = RuntimeManager.PathToEventReference(holeFilledPath);
    }
}
