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
    [EventRef] [SerializeField] private string laserBuzzingPath = Defaults.LaserBuzzing;
    [EventRef] [SerializeField] private string mirrorPushedPath = Defaults.MirrorPushed;
    [EventRef] [SerializeField] private string paperBurntPath = Defaults.PaperBurnt;
    [EventRef] [SerializeField] private string doorOpenedPath = Defaults.DoorOpened;
    [EventRef] [SerializeField] private string doorClosedPath = Defaults.DoorClosed;
    [EventRef] [SerializeField] private string targetActivatedPath = Defaults.TargetActivated;
    [EventRef] [SerializeField] private string nontargetActivatedPath = Defaults.NonTargetActivated;

    [Header("UI")]
    [EventRef] [SerializeField] private string menuPressedPath = Defaults.MenuPressed;
    [EventRef] [SerializeField] private string menuClosedPath  = Defaults.MenuClosed;
    [EventRef] [SerializeField] private string tilesSelectedPath  = Defaults.TilesSelected;
    [EventRef] [SerializeField] private string tilesDroppedPath  = Defaults.TilesDropped;
    [EventRef] [SerializeField] private string tilesBlockedPath  = Defaults.TilesBlocked;

    // EventReference 프로퍼티들...
    public EventReference Scene1Music { get; private set; }
    public EventReference Scene2Music { get; private set; }
    public EventReference BulletLaunched { get; private set; }
    public EventReference BulletAccelerated { get; private set; }
    public EventReference BulletDecelerated { get; private set; }
    public EventReference BoxPushed { get; private set; }
    public EventReference BoxBroken { get; private set; }
    public EventReference LaserBuzzing { get; private set; }
    public EventReference MirrorPushed { get; private set; }
    public EventReference PaperBurnt { get; private set; }
    public EventReference DoorOpened { get; private set; }
    public EventReference DoorClosed { get; private set; }
    public EventReference TargetActivated { get; private set; }
    public EventReference NonTargetActivated { get; private set; }
    public EventReference PlayerDash { get; private set; }
    public EventReference MenuPressed { get; private set; }
    public EventReference MenuClosed { get; private set; }
    public EventReference HoleFilled { get; private set; }
    public EventReference TilesSelected { get; private set; }
    public EventReference TilesDropped { get; private set; }
    public EventReference TilesBlocked { get; private set; }

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
        LaserBuzzing = RuntimeManager.PathToEventReference(laserBuzzingPath);
        MirrorPushed = RuntimeManager.PathToEventReference(mirrorPushedPath);
        PaperBurnt = RuntimeManager.PathToEventReference(paperBurntPath);
        DoorOpened = RuntimeManager.PathToEventReference(doorOpenedPath);
        DoorClosed = RuntimeManager.PathToEventReference(doorClosedPath);
        TargetActivated = RuntimeManager.PathToEventReference(targetActivatedPath);
        NonTargetActivated = RuntimeManager.PathToEventReference(nontargetActivatedPath);
        PlayerDash = RuntimeManager.PathToEventReference(playerDashPath);
        MenuPressed = RuntimeManager.PathToEventReference(menuPressedPath);
        MenuClosed = RuntimeManager.PathToEventReference(menuClosedPath);
        HoleFilled = RuntimeManager.PathToEventReference(holeFilledPath);
        TilesSelected = RuntimeManager.PathToEventReference(tilesSelectedPath);
        TilesDropped = RuntimeManager.PathToEventReference(tilesDroppedPath);
        TilesBlocked = RuntimeManager.PathToEventReference(tilesBlockedPath);
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
        public const string LaserBuzzing = "event:/SFX/Objects/LaserBuzzing";
        public const string MirrorPushed = "event:/SFX/Objects/MirrorPushed";
        public const string PaperBurnt = "event:/SFX/Objects/PaperBurnt";
        public const string DoorClosed = "event:/SFX/Objects/DoorClosed";
        public const string DoorOpened = "event:/SFX/Objects/DoorOpened";
        public const string TargetActivated = "event:/SFX/Objects/TargetActivated";
        public const string NonTargetActivated = "event:/SFX/Objects/NonTargetActivated";

        public const string MenuPressed = "event:/SFX/UI/MenuPressed";
        public const string MenuClosed  = "event:/SFX/UI/MenuClosed";
        public const string TilesSelected  = "event:/SFX/UI/TilesSelected";
        public const string TilesDropped  = "event:/SFX/UI/TilesDropped";
        public const string TilesBlocked  = "event:/SFX/UI/TilesBlocked";
    }
}
