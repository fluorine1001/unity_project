// Assets/Scripts/FMODEvents.cs
using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    [Header("BGM")]
    [EventRef] [SerializeField] private string scene1MusicPath = Defaults.Scene1Music;
    [EventRef] [SerializeField] private string scene2MusicPath = Defaults.Scene2Music;

    [Header("Player")]
    [EventRef] [SerializeField] private string bulletLaunchedPath = Defaults.BulletLaunched;
    [EventRef] [SerializeField] private string bulletAcceleratedPath = Defaults.BulletAccelerated;
    [EventRef] [SerializeField] private string bulletDeceleratedPath = Defaults.BulletDecelerated;
    [EventRef] [SerializeField] private string playerDashPath = Defaults.PlayerDash;

    [Header("Objects")]
    [EventRef] [SerializeField] private string boxPushedPath  = Defaults.BoxPushed;
    [EventRef] [SerializeField] private string boxBrokenPath  = Defaults.BoxBroken;
    [EventRef] [SerializeField] private string holeFilledPath = Defaults.HoleFilled;

    [Header("UI")]
    [EventRef] [SerializeField] private string menuPressedPath = Defaults.MenuPressed;
    [EventRef] [SerializeField] private string menuClosedPath  = Defaults.MenuClosed;

    // EventReference 프로퍼티들...
    public EventReference Scene1Music { get; private set; }
    public EventReference Scene2Music { get; private set; }
    public EventReference BulletLaunched { get; private set; }
    public EventReference BulletAccelerated { get; private set; }
    public EventReference BulletDecelerated { get; private set; }
    public EventReference BoxPushed { get; private set; }
    public EventReference BoxBroken { get; private set; }
    public EventReference PlayerDash { get; private set; }
    public EventReference MenuPressed { get; private set; }
    public EventReference MenuClosed { get; private set; }
    public EventReference HoleFilled { get; private set; }

    public static FMODEvents instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("Found more than one FMOD Events instance in the scene.");
            return;
        }

        instance = this;
        InitEventReferences();
    }

    private void InitEventReferences()
    {
        Scene1Music = RuntimeManager.PathToEventReference(scene1MusicPath);
        Scene2Music = RuntimeManager.PathToEventReference(scene2MusicPath);
        BulletLaunched = RuntimeManager.PathToEventReference(bulletLaunchedPath);
        BulletAccelerated = RuntimeManager.PathToEventReference(bulletAcceleratedPath);
        BulletDecelerated = RuntimeManager.PathToEventReference(bulletDeceleratedPath);
        BoxPushed = RuntimeManager.PathToEventReference(boxPushedPath);
        BoxBroken = RuntimeManager.PathToEventReference(boxBrokenPath);
        PlayerDash = RuntimeManager.PathToEventReference(playerDashPath);
        MenuPressed = RuntimeManager.PathToEventReference(menuPressedPath);
        MenuClosed = RuntimeManager.PathToEventReference(menuClosedPath);
        HoleFilled = RuntimeManager.PathToEventReference(holeFilledPath);
    }

    // 중앙 기본값: 코드에서 이 값만 수정하면 에디터 자동 동기화가 그 값을 반영함
    public static class Defaults
    {
        public const string Scene1Music = "event:/BGM/Scene1BGM";
        public const string Scene2Music = "event:/BGM/Scene2BGM";

        public const string BulletLaunched = "event:/SFX/Player/BulletLaunched";
        public const string BulletAccelerated = "event:/SFX/Player/BulletAccelerated";
        public const string BulletDecelerated = "event:/SFX/Player/BulletDecelerated";
        public const string PlayerDash = "event:/SFX/Player/Dash";

        public const string BoxPushed  = "event:/SFX/Objects/BoxPushed";
        public const string BoxBroken  = "event:/SFX/Objects/BoxBroken";
        public const string HoleFilled = "event:/SFX/Objects/HoleFilled";

        public const string MenuPressed = "event:/SFX/UI/MenuPressed";
        public const string MenuClosed  = "event:/SFX/UI/MenuClosed";
    }
}
