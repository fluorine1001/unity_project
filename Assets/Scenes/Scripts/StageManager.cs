using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Control")]
    public int currentStage = 0;
    public int maxClearedStage = 10;
    
    [Tooltip("스테이지별 카메라 위치 리스트.")]
    public List<Vector3> cameraPositions;
    
    public float cameraMoveSpeed = 2f; 

    [Header("Player & Reset")]
    public Transform player;
    public Vector3 startPosition = new Vector3(-17.92f, 2.56f, 0f);

    [Header("Grid Settings")]
    public Vector3 gridCellSize = Vector3.one;

    [Header("Stage UI")]
    public TilePaletteUI paletteUI; 
    public List<StageLoadout> stageLoadouts; 

    // 타일 상태 저장소
    private Dictionary<int, StageLoadout> runtimeLoadoutCache = new Dictionary<int, StageLoadout>();

    private List<Vector3> clearTilePositions = new List<Vector3>();
    private List<Vector3> spawnTilePositions = new List<Vector3>();

    private bool pendingPaletteRefresh = false;
    private bool cameraMoving = false;
    private Camera _mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _mainCamera = Camera.main;

        if (cameraPositions == null || cameraPositions.Count == 0)
        {
            Debug.Log("[StageManager] 기본 카메라 좌표 생성");
            cameraPositions = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f), new Vector3(104.96f, 0f, 0f), new Vector3(212.48f, 0f, 0f),
                new Vector3(319.99f, 0f, 0f), new Vector3(427.52f, 0f, 0f), new Vector3(535.04f, 0f, 0f),
                new Vector3(642.56f, 0f, 0f), new Vector3(750.08f, 0f, 0f), new Vector3(857.60f, 5.12f, 0f),
                new Vector3(0f, 0f, 0f)
            };
        }

        InitializeStageLoadouts();
    }

    private void Start()
    {
        SortTilesAndRefresh();
    }

    private void Update()
    {
        MoveCameraToTarget();
    }

    private void InitializeStageLoadouts()
    {
        // 1. Resources/StageLoadOut 폴더에서 모든 StageLoadout 에셋 로드
        // (대소문자 구분: 요청하신 대로 "StageLoadOut"으로 작성했습니다)
        StageLoadout[] loadedLoadouts = Resources.LoadAll<StageLoadout>("StageLoadouts");

        if (loadedLoadouts != null && loadedLoadouts.Length > 0)
        {
            // 2. Stage_{a}-{b} 형식에 맞춘 이중 정렬 (a 우선, b 차선)
            stageLoadouts = loadedLoadouts
                .OrderBy(x => {
                    // 파일명에서 숫자 부분 추출 (Stage_0-1 -> "0-1")
                    string name = x.name.Replace("Stage_", "");
                    string[] parts = name.Split('-');
                    
                    // a 값 추출
                    return parts.Length > 0 && int.TryParse(parts[0], out int a) ? a : 0;
                })
                .ThenBy(x => {
                    // b 값 추출
                    string name = x.name.Replace("Stage_", "");
                    string[] parts = name.Split('-');
                    
                    return parts.Length > 1 && int.TryParse(parts[1], out int b) ? b : 0;
                })
                .ToList();

            Debug.Log($"<color=green>[StageManager]</color> {stageLoadouts.Count}개의 로드아웃을 정렬하여 로드했습니다.");
            
            // 정렬 결과 확인용 로그 (필요 시 주석 해제)
            // foreach(var s in stageLoadouts) Debug.Log($"로드된 순서: {s.name}");
        }
        else
        {
            Debug.LogError("<color=red>[StageManager]</color> 'Resources/StageLoadOut' 폴더에서 에셋을 찾을 수 없습니다.");
        }
    }

    public void SetGridSize(Vector3 size) => gridCellSize = size;

    private void SortTilesAndRefresh()
    {
        clearTilePositions = clearTilePositions.OrderBy(pos => pos.x).ToList();
        Debug.Log($"[StageManager] 초기화 완료. ClearTile: {clearTilePositions.Count}");
        RefreshStagePalette();
    }

    // === ✅ [수정됨] 스테이지 이동 로직 (점프 버그 수정) ===
    public void CheckStageTransitionOnExit(Vector3 playerPos, Vector2 moveDir)
    {
        // 상하 이동은 무시
        if (Mathf.Abs(moveDir.y) > 0.01f) return;

        // 현재 플레이어가 ClearTile 위에 있는지 확인
        if (!IsClearTile(playerPos)) return;

        int nextStage = currentStage;

        // 오른쪽(→)으로 이동: 다음 스테이지 (Current + 1)
        if (moveDir.x > 0.01f)
        {
            nextStage = currentStage + 1;
            Debug.Log($"[이동 시도] 현재({currentStage}) -> 다음({nextStage})");
        }
        // 왼쪽(←)으로 이동: 이전 스테이지 (Current - 1)
        else if (moveDir.x < -0.01f)
        {
            nextStage = currentStage - 1;
            Debug.Log($"[이동 시도] 현재({currentStage}) -> 이전({nextStage})");
        }
        else return;

        ChangeStage(nextStage);
    }

    private void ChangeStage(int targetStage)
    {
        // 1. 0보다 작으면 무시
        if (targetStage < 0) return;

        // 2. ✅ [수정됨] 데이터가 없을 때 초기화(Reset)하지 않고 에러만 출력 후 중단
        if (targetStage >= cameraPositions.Count)
        {
            Debug.LogError($"[이동 불가] 목표 스테이지 {targetStage}에 대한 카메라 좌표가 없습니다. (현재 데이터 개수: {cameraPositions.Count})");
            // ResetStageSystem(); // <-- 이 줄을 삭제하여 0번으로 튕기는 현상 방지
            return;
        }

        // 3. 정상 이동
        if (targetStage > maxClearedStage) maxClearedStage = targetStage;

        if (currentStage != targetStage)
        {
            Debug.Log($"🎥 스테이지 변경 실행: {currentStage} -> {targetStage}");
            currentStage = targetStage;
            cameraMoving = true;
            pendingPaletteRefresh = true;
        }
    }

    private void MoveCameraToTarget()
    {
        if (_mainCamera == null || currentStage >= cameraPositions.Count) return;

        Vector3 targetPos = cameraPositions[currentStage];
        targetPos.z = _mainCamera.transform.position.z;

        if (Vector3.Distance(_mainCamera.transform.position, targetPos) > 0.05f)
            _mainCamera.transform.position = Vector3.Lerp(_mainCamera.transform.position, targetPos, Time.deltaTime * cameraMoveSpeed);
        else
        {
            _mainCamera.transform.position = targetPos;
            cameraMoving = false;
        }

        if (pendingPaletteRefresh)
        {
            pendingPaletteRefresh = false;
            RefreshStagePalette();
        }
    }

    // === 타일 상태 관리 ===
    private void RefreshStagePalette()
    {
        if (paletteUI == null) return;
        StageLoadout loadoutToUse = null;

        if (stageLoadouts != null && currentStage >= 0 && currentStage < stageLoadouts.Count)
        {
            if (runtimeLoadoutCache.ContainsKey(currentStage))
                loadoutToUse = runtimeLoadoutCache[currentStage];
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
            StageLoadout currentLoadout = runtimeLoadoutCache[currentStage];
            if (currentLoadout.entries != null && tileIndexInLoadout < currentLoadout.entries.Count)
            {
                var entry = currentLoadout.entries[tileIndexInLoadout];
                if (entry.count > 0)
                {
                    entry.count--; 
                    currentLoadout.entries[tileIndexInLoadout] = entry; 
                }
            }
        }
    }

    public void RegisterClearTile(Vector3 pos) { if (!clearTilePositions.Contains(pos)) clearTilePositions.Add(pos); }
    public void RegisterSpawnTile(Vector3 pos) { if (!spawnTilePositions.Contains(pos)) spawnTilePositions.Add(pos); }
    
    // 현재 위치가 ClearTile인지 확인
    public bool IsClearTile(Vector3 worldPos)
    {
        Vector2 checkPos = new Vector2(worldPos.x, worldPos.y);
        float threshold = gridCellSize.x * 0.45f; 

        foreach (var pos in clearTilePositions)
        {
            if (Vector2.Distance(checkPos, pos) < threshold) return true;
        }
        return false;
    }

    public bool IsSpawnTile(Vector3 playerPos)
    {
        Vector2 pPos = new Vector2(playerPos.x, playerPos.y);
        float threshold = gridCellSize.x * 0.6f;
        foreach (var pos in spawnTilePositions) if (Vector2.Distance(pPos, pos) < threshold) return true;
        return false;
    }

    public void OnPlayerStepOnSpawnTile() { }

    // public 호출용 (PlayerController 등에서 사용)
    public void OnPlayerStepOnClearTile() { }

    private void ResetStageSystem()
    {
        Debug.Log("🔄 게임 완전 초기화");
        currentStage = 0;
        cameraMoving = true;
        pendingPaletteRefresh = true;
        runtimeLoadoutCache.Clear();

        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) 
            {
                rb.linearVelocity = Vector2.zero;
                rb.position = startPosition;
            }
            else player.position = startPosition;
        }
        
        if (_mainCamera != null && cameraPositions.Count > 0)
        {
            Vector3 resetPos = cameraPositions[0];
            resetPos.z = _mainCamera.transform.position.z;
            _mainCamera.transform.position = resetPos;
        }
        RefreshStagePalette();
    }
}