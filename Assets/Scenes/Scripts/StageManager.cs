using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using System;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

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

    private Dictionary<int, int> stageAmmoSettings = new Dictionary<int, int>()
    {
        { 0, 99 }, { 1, 5 }, { 2, 3 }, { 3, 4 },
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
        StageLoadout[] loadedLoadouts = Resources.LoadAll<StageLoadout>("StageLoadouts");
        if (loadedLoadouts != null && loadedLoadouts.Length > 0)
        {
            stageLoadouts = loadedLoadouts
                .OrderBy(x => {
                    string name = x.name.Replace("Stage_", "");
                    string[] parts = name.Split('-');
                    return parts.Length > 0 && int.TryParse(parts[0], out int a) ? a : 0;
                }).ToList();
        }
    }

    // =========================================================
    // 탄약 및 스테이지 관리
    // =========================================================
    private void ReloadAmmo(int stageIndex)
    {
        if (visitedStages.Contains(stageIndex)) return; 
        visitedStages.Add(stageIndex);

        if (stageAmmoSettings.TryGetValue(stageIndex, out int maxAmmo)) CurrentAmmo = maxAmmo;
        else CurrentAmmo = 0;
        
        OnAmmoChanged?.Invoke(CurrentAmmo);
    }

    public void CheckStageTransitionOnExit(Vector3 playerPos, Vector2 moveDir)
    {
        if (Mathf.Abs(moveDir.y) > 0.01f) return;
        if (!IsClearTile(playerPos)) return;

        int nextStage = currentStage;
        if (moveDir.x > 0.01f) nextStage = currentStage + 1;
        else if (moveDir.x < -0.01f) nextStage = currentStage - 1;
        else return;

        ChangeStage(nextStage);
    }

    private void ChangeStage(int targetStage)
    {
        if (targetStage < 0 || targetStage >= cameraPositions.Count) return;
        
        if (targetStage > maxClearedStage) maxClearedStage = targetStage;
        if (targetStage > highestReachedStage) highestReachedStage = targetStage;

        if (currentStage != targetStage)
        {
            currentStage = targetStage;
            ReloadAmmo(targetStage);
            pendingPaletteRefresh = true;
        }
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
        if (paletteUI == null) return;
        StageLoadout loadoutToUse = null;

        if (stageLoadouts != null && currentStage >= 0 && currentStage < stageLoadouts.Count)
        {
            if (runtimeLoadoutCache.ContainsKey(currentStage))
            {
                loadoutToUse = runtimeLoadoutCache[currentStage];
            }
            else
            {
                if (stageLoadouts[currentStage] != null)
                {
                    loadoutToUse = Instantiate(stageLoadouts[currentStage]);
                    runtimeLoadoutCache.Add(currentStage, loadoutToUse);
                }
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
        if (spawnTilePositions == null || spawnTilePositions.Count == 0) return startPosition;
        if (currentStage >= 0 && currentStage < spawnTilePositions.Count) return spawnTilePositions[currentStage];
        return spawnTilePositions.Last(); 
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
        spawnTilePositions.Clear(); 
        clearTilePositions.Clear();

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
        
        visitedStages.Clear();
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