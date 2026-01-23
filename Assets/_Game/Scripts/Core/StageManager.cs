using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using System;
using UnityEngine.EventSystems; // 👈 [필수] 이거 꼭 추가해야 합니다!

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    public static SaveData PendingLoadData = null;

    [Header("Stage Control")]
    // ✅ 현재 씬의 번호를 저장하는 변수
    [Tooltip("현재 스테이지가 속한 씬의 인덱스 번호입니다.")]
    public int sceneIndex = 0;

    [Header("Stage Control")]
    public int currentStage = 0;
    
    // ✅ 최고 도달 스테이지 (체크포인트 역할)
    public int highestReachedStage = 0; 
    public int maxClearedStage = 10;

    
    [Tooltip("카메라 이동 속도")]
    public float cameraMoveSpeed = 5f; 

    // =================================================================
    // 🛠️ [FIX] 누락된 카메라 변수 추가 (여기부터 복사하세요)
    // =================================================================
    [Header("Camera Settings")]
    [Tooltip("기본 카메라 줌 사이즈 (앵커가 없을 때 사용)")]
    public float defaultOrthoSize = 5f;       // 기본값 5 추천

    [Tooltip("카메라 이동 부드러움 정도 (작을수록 빠름, 0.1 ~ 0.3 추천)")]
    public float cameraSmoothTime = 0.25f;    
    
    [Tooltip("카메라 줌 변경 속도")]
    public float cameraZoomSpeed = 3f;        

    // SmoothDamp 함수가 내부적으로 사용하는 속도 참조 변수 (private이어야 함)
    private Vector3 currentCameraVelocity;    
    // =================================================================

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

    // ✅ [NEW] 카메라 앵커 관리용 딕셔너리 (Key: StageIndex, Value: Anchors List)
    private Dictionary<int, List<CameraAnchor>> stageAnchors = new Dictionary<int, List<CameraAnchor>>();

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

        // ✅ [NEW] 씬에 있는 모든 앵커 수집
        CollectCameraAnchors();

        if (objectRoot == null)
        {
            GameObject obj = GameObject.Find("ObjectRoot");
            if (obj != null) objectRoot = obj.transform;
        }

        InitializePaletteUI();
        InitializeStageLoadouts();
        
        ReloadAmmo(currentStage);
    }

    public void CollectCameraAnchors()
    {
        stageAnchors.Clear();
        CameraAnchor[] anchors = FindObjectsOfType<CameraAnchor>();
        
        foreach (var anchor in anchors)
        {
            if (!stageAnchors.ContainsKey(anchor.stageIndex))
            {
                stageAnchors[anchor.stageIndex] = new List<CameraAnchor>();
            }
            stageAnchors[anchor.stageIndex].Add(anchor);
        }
        
        // Debug.Log($"<color=cyan>[Camera] {anchors.Length}개의 카메라 앵커를 찾았습니다.</color>");
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

        // 맵이 재생성되었을 수 있으므로 앵커 다시 수집 (앵커가 MapEnvironment 자식일 경우 대비)
        CollectCameraAnchors();

        SortTilesAndRefresh();
        ResetGamePartial();

        if (PendingLoadData != null)
        {
            ApplyLoadedData(PendingLoadData);
            PendingLoadData = null; // 사용 후 비우기
        }
    }

    // ✅ [추가] 저장된 데이터를 게임에 적용하는 함수
    private void ApplyLoadedData(SaveData data)
    {
        Debug.Log($"<color=green>[Load] 저장된 데이터 적용 중... (Stage {data.currentStage})</color>");

        // 1. 데이터 덮어쓰기
        this.currentStage = data.currentStage;
        this.highestReachedStage = data.highestReachedStage;
        // (sceneIndex는 이미 SceneManager를 통해 맞춰져 있음)

        // 2. 탄약 및 UI 갱신 (ResetGamePartial이 엉뚱한 스테이지로 설정했을 수 있으므로 다시 실행)
        forceFlag = true; // 강제 갱신 플래그
        ReloadAmmo(currentStage);
        RefreshStagePalette();

        // 3. 플레이어 및 카메라 위치 강제 이동
        if (player != null)
        {
            // 저장된 스테이지의 체크포인트로 이동
            player.position = GetCurrentStageCheckpoint();
            
            // 물리 속도 초기화 (안전장치)
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector2.zero; 
#else
                rb.velocity = Vector2.zero;
#endif
                rb.angularVelocity = 0f;
            }
        }
        
        // ✅ [수정] 즉시 이동: 타겟 위치 계산 후 이동
        if (_mainCamera != null)
        {
            (Vector3 targetPos, float targetSize) = CalculateCameraTarget();
            _mainCamera.transform.position = targetPos;
            _mainCamera.orthographicSize = targetSize;
        }
    }

    private (Vector3 position, float size) CalculateCameraTarget()
    {
        Vector3 targetPos = _mainCamera.transform.position;
        float targetSize = defaultOrthoSize;

        // 1. 플레이어 위치 확인
        if (player == null) return (targetPos, targetSize);

        // 2. 현재 플레이어가 위치한 스테이지 인덱스 확인 (GeneratorManager 이용)
        int playerStageIndex = -1;
        if (GeneratorManager.Instance != null)
        {
            playerStageIndex = GeneratorManager.Instance.GetStageIndexFromWorldPos(player.transform.position);
        }

        // 3. 조건 처리
        // 플레이어가 스테이지 위에 없거나(-1), 해당 스테이지에 등록된 앵커가 없는 경우
        if (playerStageIndex == -1 || !stageAnchors.ContainsKey(playerStageIndex))
        {
            // -> 플레이어 위로 이동, 줌 1.0 (default)
            targetPos = player.position;
            targetSize = defaultOrthoSize;
        }
        else
        {
            // -> 해당 스테이지의 앵커들 중 플레이어와 가장 가까운 것 찾기
            List<CameraAnchor> anchors = stageAnchors[playerStageIndex];
            CameraAnchor closestAnchor = null;
            float minDistance = float.MaxValue;

            foreach (var anchor in anchors)
            {
                if (anchor == null) continue;
                float dist = Vector3.Distance(player.position, anchor.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestAnchor = anchor;
                }
            }

            if (closestAnchor != null)
            {
                targetPos = closestAnchor.transform.position;
                
                // 줌 계산: 1보다 크면 확대(Size 작아짐), 1보다 작으면 축소(Size 커짐)
                // 예: Scale 2.0 -> Size = Default / 2.0 (절반 크기 = 2배 확대)
                float scale = Mathf.Max(0.01f, closestAnchor.zoomScale); // 0 나누기 방지
                targetSize = defaultOrthoSize / scale;
            }
        }

        // Z축 고정
        targetPos.z = -10f;
        return (targetPos, targetSize);
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
        if (_mainCamera == null || player == null) return;

        // 목표 위치 및 사이즈 계산
        (Vector3 targetPos, float targetSize) = CalculateCameraTarget();

        // 1. 위치 이동 (SmoothDamp로 부드럽게 자연스러운 이동)
        _mainCamera.transform.position = Vector3.SmoothDamp(
            _mainCamera.transform.position, 
            targetPos, 
            ref currentCameraVelocity, 
            cameraSmoothTime
        );

        // 2. 줌 변경 (Lerp)
        if (Mathf.Abs(_mainCamera.orthographicSize - targetSize) > 0.01f)
        {
            _mainCamera.orthographicSize = Mathf.Lerp(
                _mainCamera.orthographicSize, 
                targetSize, 
                Time.deltaTime * cameraZoomSpeed
            );
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
        if (GeneratorManager.Instance == null) return;

        // 현재 밟고 있는 땅의 스테이지 번호 가져오기
        int targetStageIndex = GeneratorManager.Instance.GetStageIndexFromWorldPos(targetWorldPos);

        // ✅ [수정] 스테이지 번호가 바뀌었다면 무조건 처리 ( -1 포함 )
        if (targetStageIndex != currentStage)
        {
            // 1. 스테이지 밖(-1)으로 나간 경우
            if (targetStageIndex == -1)
            {
                currentStage = -1; // 현재 스테이지 없음으로 설정
                RefreshStagePalette(); // 팔레트 비우기 호출
                
                Debug.Log("<color=gray>[Stage] 스테이지 영역을 벗어났습니다. (UI 숨김)</color>");
            }
            // 2. 새로운 스테이지로 진입한 경우
            else
            {
                ChangeStage(targetStageIndex);
            }
        }
    }

    private void ChangeStage(int targetStage)
    {
        // ❌ 기존 인덱스 범위 체크 제거 (카메라 리스트가 없어졌으므로)
        // 대신 앵커가 존재하는지만 체크할 수도 있으나, 앵커가 없어도 플레이어 팔로우로 동작하므로 허용
        
        int previousStage = currentStage;
        currentStage = targetStage;

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

        // ✅ [추가] 현재 스테이지가 -1(없음)이거나 범위를 벗어나면 팔레트를 비움
        if (currentStage == -1 || stageLoadouts == null || currentStage < 0 || currentStage >= stageLoadouts.Count)
        {
            paletteUI.Build(null); // UI 초기화 (아이콘 삭제)
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
    // =========================================================
    // ✅ 리셋 기능 (앵커 재수집 포함)
    // =========================================================
    public void ResetGamePartial()
    {
        // UI 포커스 및 패널 닫기 (이전 요청사항 유지)
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        UIManager ui = FindObjectOfType<UIManager>(); 
        if (ui != null) ui.bookPanel(false); 

        if (objectRoot != null) foreach (Transform child in objectRoot) Destroy(child.gameObject);
        
        stagePuzzleBlocks.Clear();
        stageDoors.Clear();

        if (mapEnvironmentRoot != null && mapBackup != null)
        {
            Destroy(mapEnvironmentRoot.gameObject);
            GameObject newMap = Instantiate(mapBackup);
            newMap.name = mapBackup.name.Replace("_Backup", ""); 
            newMap.SetActive(true);
            mapEnvironmentRoot = newMap.transform; 
            
            var gen = FindObjectOfType<GeneratorManager>();
            if (gen != null) gen.spawnParent = mapEnvironmentRoot;

            // ✅ 맵이 새로 생겼으므로, 맵 안에 앵커가 있다면 다시 수집해야 함
            CollectCameraAnchors();
        }

        currentStage = highestReachedStage;

        foreach(var loadout in runtimeLoadoutCache.Values) if(loadout != null) Destroy(loadout); 
        runtimeLoadoutCache.Clear();
        
        forceFlag = true;
        ReloadAmmo(currentStage);
        RefreshStagePalette();

        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.ForceStop();

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
            }
        }
    }
    
}