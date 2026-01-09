using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using System;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Control")]
    // ✅ 현재 씬의 번호를 저장하는 변수
    [Tooltip("현재 스테이지가 속한 씬의 인덱스 번호입니다.")]
    public int sceneIndex = 0;

    [Header("Stage Control")]
    public int currentStage = 0;
    
    // ✅ 최고 도달 스테이지 (체크포인트 역할)
    public int highestReachedStage = 0; 
    public int maxClearedStage = 10;
    
    [Tooltip("스테이지별 카메라 위치 리스트.")]
    public List<Vector3> cameraPositions;
    
    [Tooltip("카메라 이동 속도")]
    public float cameraMoveSpeed = 5f; 

    [Header("Player & Reset")]
    public Transform player;
    public Transform objectRoot; 
    public Vector3 startPosition = new Vector3(-17.92f, 2.56f, 0f);

    [Header("Map Environment (New)")]
    [Tooltip("자동 생성된 맵의 부모. GeneratorManager가 채워줍니다.")]
    public Transform mapEnvironmentRoot;
    
    // 🛠️ 리셋을 위한 맵 원본 백업본
    private GameObject mapBackup; 

    [Header("Grid Settings")]
    public Vector3 gridCellSize = Vector3.one;

    [Header("Stage UI")]
    public TilePaletteUI paletteUI; 
    public List<StageLoadout> stageLoadouts; 

    // 타일 상태 저장소
    private Dictionary<int, StageLoadout> runtimeLoadoutCache = new Dictionary<int, StageLoadout>();

    // 타일 좌표들
    private List<Vector3> clearTilePositions = new List<Vector3>();
    private List<Vector3> spawnTilePositions = new List<Vector3>(); 

    // =================================================================
    // 🧩 [NEW] 퍼즐 시스템 데이터 (타겟 블록, 문)
    // =================================================================
    private Dictionary<int, List<LaserTargetBlock>> stagePuzzleBlocks = new Dictionary<int, List<LaserTargetBlock>>();
    private Dictionary<int, List<DoorController>> stageDoors = new Dictionary<int, List<DoorController>>();
    // =================================================================

    private bool pendingPaletteRefresh = false;
    private Camera _mainCamera;

    public event Action<int> OnAmmoChanged;

    private Dictionary<(int, int), int> stageAmmoSettings = new Dictionary<(int, int), int>()
    {
        // --- Scene 0 설정 ---
        { (1, 0), 20 }, 
        { (1, 1), 1 }, 
        { (1, 2), 1 }, 
        { (1, 3), 2 },
        { (1, 4), 2 },
        { (1, 5), 4 },
        { (1, 6), 3 },
        { (1, 7), 10 },
        { (1, 8), 12 },
        { (2, 0), 4 },
        { (2, 1), 4 },
        { (2, 2), 3 },
        { (2, 3), 4 },
        { (2, 4), 4 },
        { (2, 5), 5 },
        { (2, 6), 3 },
        { (2, 7), 5 },

    };

    public int CurrentAmmo { get; private set; }
    private HashSet<int> visitedStages = new HashSet<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _mainCamera = Camera.main;

        highestReachedStage = currentStage;

        // ✅ [복구 완료] 카메라 좌표가 없으면 하드코딩된 값 사용
        if (cameraPositions == null || cameraPositions.Count == 0)
        {
            Debug.Log("<color=yellow>[StageManager]</color> 카메라 좌표가 비어있어 기본값을 로드합니다.");
            cameraPositions = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f), 
                new Vector3(104.96f, 0f, 0f), 
                new Vector3(212.48f, 0f, 0f),
                new Vector3(319.99f, 0f, 0f), 
                new Vector3(427.52f, 0f, 0f), 
                new Vector3(535.04f, 0f, 0f),
                new Vector3(642.56f, 0f, 0f), 
                new Vector3(750.08f, 0f, 0f), 
                new Vector3(857.60f, 5.12f, 0f),
                new Vector3(965.12f, 5.12f, 0f),
                new Vector3(0f, 0f, 0f) 
            };
        }

        if (objectRoot == null)
        {
            GameObject obj = GameObject.Find("ObjectRoot");
            if (obj != null) objectRoot = obj.transform;
        }

        InitializePaletteUI();
        InitializeStageLoadouts();
        
        ReloadAmmo(currentStage);
    }

    // 🛠️ [추가됨] GeneratorManager가 맵 생성을 끝내면 호출
    public void InitializeStageData()
    {
        // 1. 맵 환경(상자, 벽 등) 연결
        if (mapEnvironmentRoot == null)
        {
            GameObject envObj = GameObject.Find("MapEnvironment");
            if (envObj != null) mapEnvironmentRoot = envObj.transform;
        }

        // 2. 맵 백업 (최초 1회만 수행)
        if (mapEnvironmentRoot != null && mapBackup == null)
        {
            mapBackup = Instantiate(mapEnvironmentRoot.gameObject);
            mapBackup.name = "MapEnvironment_Backup";
            mapBackup.SetActive(false); 
            DontDestroyOnLoad(mapBackup); 
            Debug.Log("<color=green>[StageManager]</color> 맵 백업 완료.");
        }

        SortTilesAndRefresh();
        ResetGamePartial();
    }

    private void Update()
    {
        // ✅ [복구 완료] 카메라 이동 로직
        MoveCameraToTarget();

        if (pendingPaletteRefresh)
        {
            pendingPaletteRefresh = false;
            RefreshStagePalette();
        }
    }

    // =========================================================
    // ✅ 카메라 이동 (사용자 코드 복원)
    // =========================================================
    private void MoveCameraToTarget()
    {
        if (_mainCamera == null) return;
        if (cameraPositions == null || cameraPositions.Count == 0) return;

        int targetIndex = Mathf.Clamp(currentStage, 0, cameraPositions.Count - 1);
        
        Vector3 targetPos = cameraPositions[targetIndex];
        targetPos.z = -10f; 

        float distance = Vector3.Distance(_mainCamera.transform.position, targetPos);
        if (distance > 0.01f)
        {
            _mainCamera.transform.position = Vector3.Lerp(_mainCamera.transform.position, targetPos, Time.deltaTime * cameraMoveSpeed);
        }
        else
        {
            _mainCamera.transform.position = targetPos;
        }
    }

    // =========================================================
    // 데이터 로드 및 UI
    // =========================================================
    private void InitializePaletteUI()
    {
        if (paletteUI != null) return;
        GameObject leftBarObj = GameObject.Find("LeftBar");
        if (leftBarObj == null) leftBarObj = GameObject.Find("Canvas/LeftBar");
        if (leftBarObj != null) paletteUI = leftBarObj.GetComponent<TilePaletteUI>();
    }

    private void InitializeStageLoadouts()
    {
        Debug.Log($"<color=cyan>[Init] Loadout 초기화 시작. 현재 Scene Index 설정값: {this.sceneIndex}</color>");

        StageLoadout[] loadedLoadouts = Resources.LoadAll<StageLoadout>("StageLoadouts");

        if (loadedLoadouts != null && loadedLoadouts.Length > 0)
        {
            Debug.Log($"[Init] Resources 폴더에서 총 {loadedLoadouts.Length}개의 파일을 찾았습니다.");

            stageLoadouts = loadedLoadouts
                .Select(x =>
                {
                    string name = x.name.Replace("Stage_", "");
                    string[] parts = name.Split('-');
                    
                    int sIndex = -1;
                    int stIndex = -1;

                    if (parts.Length >= 2)
                    {
                        int.TryParse(parts[0], out sIndex);
                        int.TryParse(parts[1], out stIndex);
                    }
                    
                    // 🔍 파싱 로그 (너무 많으면 주석 처리)
                    // Debug.Log($"[Parsing] 파일명: {x.name} -> Scene: {sIndex}, Stage: {stIndex}");

                    return new { Asset = x, SceneIdx = sIndex, StageIdx = stIndex };
                })
                .Where(item => 
                {
                    bool match = item.SceneIdx == this.sceneIndex;
                    if (!match && item.SceneIdx == 2) 
                    {
                        // 혹시 Scene 2 데이터인데 걸러지는지 확인용 로그
                        Debug.LogWarning($"[Filter] {item.Asset.name} (Scene {item.SceneIdx})이 현재 설정된 Scene Index ({this.sceneIndex})와 달라 제외됨.");
                    }
                    return match;
                })
                .OrderBy(item => item.StageIdx)
                .Select(item => item.Asset)
                .ToList();
            
            Debug.Log($"<color=green>[Init] 최종 로드된 리스트 개수: {stageLoadouts.Count}개 (Target Scene: {this.sceneIndex})</color>");
            for(int i=0; i<stageLoadouts.Count; i++)
            {
                Debug.Log($"   [{i}번 인덱스] : {stageLoadouts[i].name}");
            }
        }
        else
        {
            stageLoadouts = new List<StageLoadout>();
            Debug.LogError("[Init] Resources/StageLoadouts 폴더에서 파일을 찾을 수 없습니다!");
        }
    }

    // =========================================================
    // 탄약 및 스테이지 관리
    // =========================================================
    bool forceFlag = false;

    private void ReloadAmmo(int stageIndex)
    {
        // visitedStages 체크는 현재 씬 내에서의 중복 방문 방지용으로 유지하거나, 
        // 씬까지 포함하여 체크하려면 HashSet<(int, int)>로 바꿔야 할 수도 있습니다.
        // 일단 기존 로직(현재 씬 기준 stageIndex)을 유지합니다.
        if (!forceFlag && visitedStages.Contains(stageIndex)) return; 
        if (!forceFlag) visitedStages.Add(stageIndex);
        if (forceFlag) forceFlag = false;

        // ✅ 수정된 부분: (현재 씬 번호, 현재 스테이지 번호)로 딕셔너리 조회
        if (stageAmmoSettings.TryGetValue((sceneIndex, stageIndex), out int maxAmmo)) 
        {
            CurrentAmmo = maxAmmo;
        }
        else 
        {
            // 설정값이 없으면 기본값 0 (혹은 원하는 기본값)
            CurrentAmmo = 0; 
        }
        
        OnAmmoChanged?.Invoke(CurrentAmmo);
    }

    // StageManager.cs

    // 기존의 복잡한 체크 로직 대신, GeneratorManager의 데이터를 직접 활용합니다.
    // StageManager.cs

    public void CheckStageTransition(Vector3 targetWorldPos)
    {
        if (GeneratorManager.Instance == null) 
        {
            Debug.LogError("<color=red>[StageManager]</color> GeneratorManager 인스턴스를 찾을 수 없습니다!");
            return;
        }

        int targetStageIndex = GeneratorManager.Instance.GetStageIndexFromWorldPos(targetWorldPos);

        // 🔍 로그 4: 전환 체크 시점 로그
        if (targetStageIndex != -1)
        {
            if (targetStageIndex != currentStage)
            {
                Debug.Log($"<color=lime>🎬 [Transition Success]</color> 목적지 스테이지({targetStageIndex})가 현재({currentStage})와 달라 카메라를 이동합니다.");
                ChangeStage(targetStageIndex);
            }
        }
        else
        {
            Debug.Log($"<color=gray>[Transition Ignore]</color> 목적지 {targetWorldPos}는 어떤 스테이지에도 속해있지 않습니다.");
        }
    }

    private void ChangeStage(int targetStage)
    {
        if (targetStage < 0 || targetStage >= cameraPositions.Count) 
        {
            Debug.LogError($"<color=red>[Stage Error]</color> 스테이지 {targetStage}에 해당하는 카메라 좌표가 없습니다!");
            return;
        }
        
        int previousStage = currentStage;
        currentStage = targetStage;

        // 🔍 로그 추가: 목적지 좌표를 직접 확인하세요.
        Debug.Log($"<color=yellow>📸 [Camera Destination]</color> 스테이지 변경: {previousStage} -> {currentStage} | 이동 목표: {cameraPositions[currentStage]}");

        if (currentStage > highestReachedStage) highestReachedStage = currentStage;

        ReloadAmmo(currentStage);
        pendingPaletteRefresh = true;
    }

    // =========================================================
    // 🧩 [NEW] 퍼즐 및 문 관리 로직 (새로 추가된 기능)
    // =========================================================
    
    // 타겟/논타겟 블록 등록
    public void RegisterPuzzleBlock(int stageID, LaserTargetBlock block)
    {
        if (!stagePuzzleBlocks.ContainsKey(stageID))
            stagePuzzleBlocks[stageID] = new List<LaserTargetBlock>();
        
        if (!stagePuzzleBlocks[stageID].Contains(block))
            stagePuzzleBlocks[stageID].Add(block);
    }

    // 문 등록
    public void RegisterDoor(int stageID, DoorController door)
    {
        if (!stageDoors.ContainsKey(stageID))
            stageDoors[stageID] = new List<DoorController>();
        
        if (!stageDoors[stageID].Contains(door))
            stageDoors[stageID].Add(door);
    }

    public void CheckDoorState(int stageID)
{
    // 데이터가 없는 경우 방지
    if (!stagePuzzleBlocks.ContainsKey(stageID) || !stageDoors.ContainsKey(stageID)) return;

    List<LaserTargetBlock> blocks = stagePuzzleBlocks[stageID];
    List<DoorController> doors = stageDoors[stageID];

    // 1. 퍼즐 조건 계산
    bool allTargetsOn = true;
    bool allNonTargetsOff = true;

    foreach (var block in blocks)
    {
        if (block.isTarget)
        {
            if (!block.IsActive) allTargetsOn = false;
        }
        else
        {
            if (block.IsActive) allNonTargetsOff = false;
        }
    }

    // 최종적으로 문이 열려야 하는 상태
    bool shouldBeOpen = allTargetsOn && allNonTargetsOff;
    bool f1 = false, f2 = false;
    // 2. 상태 변화 감지 및 적용
    foreach (var door in doors)
    {
        if (door == null) continue;

        // 문 컨트롤러에서 현재 열림 여부를 가져옴 (DoorController에 isOpen public getter 필요)
        bool currentlyOpen = door.IsDoorOpen; 

        // [상태 전환 시점 포착]
        if (!currentlyOpen && shouldBeOpen)
        {
            // 🔓 닫혀있다가 열리는 순간
            Debug.Log($"<color=lime>🔓 [Door Event]</color> Stage {stageID}: 모든 조건 충족! 문이 열립니다.");
            door.SetDoorState(true);
            f1 = true;
            // 여기에 문 열리는 사운드 재생 등을 추가할 수 있습니다.
        }
        else if (currentlyOpen && !shouldBeOpen)
        {
            // 🔒 열려있다가 다시 닫히는 순간
            Debug.Log($"<color=orange>🔒 [Door Event]</color> Stage {stageID}: 조건 불충분! 문이 다시 닫힙니다.");
            door.SetDoorState(false);
            f2 = true;
            AudioManager.instance.PlayOneShot(FMODEvents.instance.DoorClosed, transform.position);
            // 여기에 문 닫히는 사운드 재생 등을 추가할 수 있습니다.
        }
    }
    if(f1) AudioManager.instance.PlayOneShot(FMODEvents.instance.DoorOpened, transform.position);
    if(f2) AudioManager.instance.PlayOneShot(FMODEvents.instance.DoorClosed, transform.position);
}

    // =========================================================
    // 타일 관리
    // =========================================================
    public void RegisterClearTile(Vector3 pos) { if (!clearTilePositions.Contains(pos)) clearTilePositions.Add(pos); }
    public void RegisterSpawnTile(Vector3 pos) { if (!spawnTilePositions.Contains(pos)) spawnTilePositions.Add(pos); }

    private void SortTilesAndRefresh()
    {
        if (spawnTilePositions.Count > 0) spawnTilePositions = spawnTilePositions.OrderBy(pos => pos.x).ToList();
        if (clearTilePositions.Count > 0) clearTilePositions = clearTilePositions.OrderBy(pos => pos.x).ToList();
        RefreshStagePalette();
    }

    private void RefreshStagePalette()
    {
        if (paletteUI == null)
        {
            Debug.LogError("[Palette] PaletteUI가 연결되지 않았습니다.");
            return;
        }

        StageLoadout loadoutToUse = null;

        // 🔍 상태 확인 로그
        Debug.Log($"<color=yellow>[Refresh] 팔레트 갱신 요청 - currentStage: {currentStage}, List Count: {stageLoadouts?.Count ?? 0}</color>");

        if (stageLoadouts != null && currentStage >= 0 && currentStage < stageLoadouts.Count)
        {
            if (runtimeLoadoutCache.ContainsKey(currentStage))
            {
                Debug.Log($"[Refresh] 캐시된 데이터 사용 (Stage {currentStage})");
                loadoutToUse = runtimeLoadoutCache[currentStage];
            }
            else
            {
                if (stageLoadouts[currentStage] != null)
                {
                    Debug.Log($"[Refresh] 원본 에셋 복제하여 신규 생성 (Index {currentStage}: {stageLoadouts[currentStage].name})");
                    loadoutToUse = Instantiate(stageLoadouts[currentStage]);
                    runtimeLoadoutCache.Add(currentStage, loadoutToUse);
                }
                else
                {
                    Debug.LogError($"[Error] 리스트의 {currentStage}번 인덱스 요소가 null입니다!");
                }
            }
        }
        else
        {
            // 🚨 여기가 실행된다면 조건 불일치
            if (stageLoadouts == null) Debug.LogError("[Fail] stageLoadouts 리스트가 null입니다.");
            else if (currentStage < 0) Debug.LogError($"[Fail] currentStage({currentStage})가 음수입니다.");
            else if (currentStage >= stageLoadouts.Count) 
            {
                Debug.LogError($"<color=red>[Fail] 인덱스 초과! currentStage({currentStage})가 리스트 크기({stageLoadouts.Count})보다 크거나 같습니다.</color>");
                Debug.LogError("힌트: 씬 인덱스 설정이 잘못되었거나, 스테이지 번호가 0부터 시작하지 않을 수 있습니다.");
            }
        }

        paletteUI.Build(loadoutToUse);
    }
    
    public void ConsumeTile(int tileIndexInLoadout)
    {
        if (runtimeLoadoutCache.ContainsKey(currentStage))
        {
            var loadout = runtimeLoadoutCache[currentStage];
            if (loadout.entries != null && tileIndexInLoadout < loadout.entries.Count)
            {
                var entry = loadout.entries[tileIndexInLoadout];
                if (entry.count > 0)
                {
                    entry.count--;
                    loadout.entries[tileIndexInLoadout] = entry; 
                }
            }
        }
    }

    // =========================================================
    // 유틸리티
    // =========================================================
    public Vector3 GetCurrentStageCheckpoint()
    {
        // 리스트가 비어있는지 확인
        if (spawnTilePositions == null || spawnTilePositions.Count == 0) 
        {
            Debug.LogError("스폰 타일 리스트가 비어있습니다!");
            return startPosition;
        }

        // 인덱스 범위 초과 방지
        int targetIndex = Mathf.Clamp(currentStage, 0, spawnTilePositions.Count - 1);
        
        Vector3 targetPos = spawnTilePositions[targetIndex];
        
        // Z값 보정 (2D 게임이라면 보통 0)
        targetPos.z = 0f; 
        
        return targetPos;
    }

    public bool IsClearTile(Vector3 worldPos)
    {
        Vector2 checkPos = new Vector2(worldPos.x, worldPos.y);
        float threshold = gridCellSize.x * 0.45f; 
        foreach (var pos in clearTilePositions) if (Vector2.Distance(checkPos, pos) < threshold) return true;
        return false;
    }

    public bool IsSpawnTile(Vector3 worldPos)
    {
        Vector2 checkPos = new Vector2(worldPos.x, worldPos.y);
        float threshold = gridCellSize.x * 0.45f; 
        foreach (var pos in spawnTilePositions) if (Vector2.Distance(checkPos, pos) < threshold) return true;
        return false;
    }

    public bool HasAmmo() => CurrentAmmo > 0;
    
    public void UseAmmo()
    {
        if (CurrentAmmo > 0)
        {
            CurrentAmmo--;
            OnAmmoChanged?.Invoke(CurrentAmmo);
        }
    }

    public void SetGridSize(Vector3 size) => gridCellSize = size;
    public void OnPlayerStepOnSpawnTile() { }
    public void OnPlayerStepOnClearTile() { }

    // =========================================================
    // ✅ 리셋 기능 (맵 복구 기능 + [NEW] 퍼즐 리셋 추가)
    // =========================================================
    public void ResetGamePartial()
    {
        Debug.Log($"🔄 [Reset] 체크포인트(Stage {highestReachedStage})로 복귀 및 전체 초기화");

        // 1. 플레이어 설치물 제거 (ObjectRoot)
        if (objectRoot != null) foreach (Transform child in objectRoot) Destroy(child.gameObject);
        
        // 🧩 [NEW] 퍼즐 및 맵 데이터 초기화 (재등록을 위해 리스트 비움)
        stagePuzzleBlocks.Clear();
        stageDoors.Clear();
        

        // 🛠️ 2. 맵 환경(MapEnvironment) 초기화 및 복구
        if (mapEnvironmentRoot != null && mapBackup != null)
        {
            // 현재 망가진/변경된 맵 삭제
            Destroy(mapEnvironmentRoot.gameObject);
            
            // 백업본에서 새 맵 생성
            GameObject newMap = Instantiate(mapBackup);
            newMap.name = mapBackup.name.Replace("_Backup", ""); 
            newMap.SetActive(true);
            
            // 참조 갱신
            mapEnvironmentRoot = newMap.transform; 
            
            // GeneratorManager의 참조도 갱신해줘야 혹시 모를 오류 방지 (선택사항)
            var gen = FindObjectOfType<GeneratorManager>();
            if (gen != null) gen.spawnParent = mapEnvironmentRoot;
            // 여기서 GeneratorManager의 Start() 등이 실행되며 블록/스폰 타일이 다시 등록됩니다.
        }

        // 3. 스테이지 및 데이터 리셋
        currentStage = highestReachedStage;

        foreach(var loadout in runtimeLoadoutCache.Values)
        {
            if(loadout != null) Destroy(loadout); 
        }
        runtimeLoadoutCache.Clear();
        
        forceFlag = true;
        ReloadAmmo(currentStage);

        RefreshStagePalette();

        // 4. 플레이어 위치 이동
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        if (player != null)
        {
            player.position = GetCurrentStageCheckpoint();
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector2.zero; 
#else
                rb.velocity = Vector2.zero;
#endif
                rb.angularVelocity = 0f;
                rb.Sleep(); 
                rb.WakeUp();
            }
        }
        // 카메라는 Update()에서 currentStage를 따라 자동으로 이동함
    }
    
}