using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.InputSystem; 

public class TilePlacementManager : MonoBehaviour
{
    public static TilePlacementManager Instance { get; private set; }

    [Header("Settings")]
    public Tilemap targetTilemap;       
    public Tilemap groundTilemap;       
    public Transform objectRoot;        

    [Header("Interaction")]
    public Transform playerTransform;   

    [Header("Prefabs")]
    public GameObject speedPrefab;      
    public GameObject deSpeedPrefab;    

    // 상태 변수
    private PaletteItemUI currentUI;
    private bool isDragging = false;
    private Vector3 lastScreenPos;      

    // 작업용 데이터
    private List<TileCell> workingCells = new List<TileCell>(); 

    // 고스트(미리보기) 관련
    private GameObject ghostRoot;
    private List<SpriteRenderer> ghostRenderers = new List<SpriteRenderer>();

    private void Awake() 
    { 
        Instance = this; 
        AutoAssignSettings();
    }

    private void Reset()
    {
        AutoAssignSettings();
    }

    // 필수 컴포넌트 자동 할당
    private void AutoAssignSettings()
    {
        if (targetTilemap == null || groundTilemap == null)
        {
            GameObject grid = GameObject.Find("Grid");
            if (grid != null)
            {
                Transform t = grid.transform.Find("tile_temp_ground");
                if (t != null)
                {
                    Tilemap tm = t.GetComponent<Tilemap>();
                    if (targetTilemap == null) targetTilemap = tm;
                    if (groundTilemap == null) groundTilemap = tm;
                }
            }
        }

        if (objectRoot == null)
        {
            GameObject objRoot = GameObject.Find("ObjectRoot");
            if (objRoot != null) objectRoot = objRoot.transform;
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.Find("player_0");
            if (player != null) playerTransform = player.transform;
        }

#if UNITY_EDITOR
        if (speedPrefab == null)
            speedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenes/Tile Prefab/Speed/Speed_Up.prefab");
        
        if (deSpeedPrefab == null)
            deSpeedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenes/Tile Prefab/Speed/Speed_Down.prefab");
#endif
    }

    private void Update()
    {
        if (!isDragging) return;

        bool rotateInput = false;

        // R키 또는 우클릭으로 회전
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            rotateInput = true;

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            rotateInput = true;

        if (rotateInput)
        {
            RotateWorkingCellsClockwise();    
            CreateGhost();                    
            UpdateGhostVisual(lastScreenPos); 
        }
    }

    // =========================================================
    // 🖱️ 드래그 시작 / 진행 / 종료
    // =========================================================

    public void StartDrag(TileDefinition def, PaletteItemUI ui, Vector3 startScreenPos)
    {
        // 사운드 재생
        if (AudioManager.instance != null && FMODEvents.instance != null)
            AudioManager.instance.PlayOneShot(FMODEvents.instance.TilesSelected, this.transform.position);

        if (def == null || def.cells == null || def.cells.Count == 0) return;

        isDragging = true;
        currentUI = ui;
        lastScreenPos = startScreenPos;

        // 드래그할 셀 데이터 복사
        workingCells.Clear();
        foreach (var cell in def.cells)
        {
            workingCells.Add(new TileCell { offset = cell.offset, kind = cell.kind });
        }

        CreateGhost();
        UpdateGhostVisual(lastScreenPos);
    }

    public void UpdateDrag(Vector3 screenPos)
    {
        if (!isDragging) return;
        lastScreenPos = screenPos;
        UpdateGhostVisual(screenPos);
    }

    public void EndDrag(Vector3 screenPos)
    {
        if (!isDragging) return;
        isDragging = false;

        Vector3Int originCell = GetCellPosFromScreen(screenPos);

        if (IsPositionValid(originCell))
        {
            // 성공 사운드
            if (AudioManager.instance != null && FMODEvents.instance != null)
                AudioManager.instance.PlayOneShot(FMODEvents.instance.TilesDropped, this.transform.position);

            SpawnObjects(originCell);
            
            // 탄약 소비
            if (StageManager.Instance != null && currentUI != null)
            {
                StageManager.Instance.ConsumeTile(currentUI.LoadoutIndex);
            }

            // UI 아이콘 제거
            if (currentUI) Destroy(currentUI.gameObject); 
        }
        else
        {
            // 실패 사운드
            if (AudioManager.instance != null && FMODEvents.instance != null)
                AudioManager.instance.PlayOneShot(FMODEvents.instance.TilesBlocked, this.transform.position);

            Debug.Log("[TilePlacementManager] 설치 실패: 유효하지 않은 위치");
        }
        
        // 고스트 정리
        if (ghostRoot) Destroy(ghostRoot);
        currentUI = null;
        workingCells.Clear();
    }

    // =========================================================
    // 🔄 회전 및 좌표 변환
    // =========================================================

    private void RotateWorkingCellsClockwise()
    {
        for (int i = 0; i < workingCells.Count; i++)
        {
            TileCell cell = workingCells[i];
            int newX = cell.offset.y;
            int newY = -cell.offset.x;
            cell.offset = new Vector2Int(newX, newY);
            workingCells[i] = cell;
        }
    }
    
    private Vector3Int GetCellPosFromScreen(Vector3 screenPos)
    {
        if (Camera.main == null || targetTilemap == null) return Vector3Int.zero;

        Plane zPlane = new Plane(Vector3.back, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (zPlane.Raycast(ray, out float enter))
        {
            Vector3 worldHitPos = ray.GetPoint(enter);
            return targetTilemap.WorldToCell(worldHitPos);
        }

        Vector3 simpleWorld = Camera.main.ScreenToWorldPoint(screenPos);
        simpleWorld.z = 0;
        return targetTilemap.WorldToCell(simpleWorld);
    }

    // =========================================================
    // 👻 고스트(미리보기) 처리 - 시인성 개선됨
    // =========================================================

    private void UpdateGhostVisual(Vector3 screenPos)
    {
        if (ghostRoot == null) return;
        Vector3Int cellPos = GetCellPosFromScreen(screenPos);
        
        // 🔥 [개선] Z축을 -5f로 설정하여 모든 오브젝트보다 앞에 표시
        Vector3 worldPos = targetTilemap.GetCellCenterWorld(cellPos);
        worldPos.z = -5f; 
        ghostRoot.transform.position = worldPos;

        bool isValid = IsPositionValid(cellPos);
        
        // 🔥 [개선] Alpha값을 0.8로 올려서 진하게 표시 + 색상 구분 명확화
        Color validColor = new Color(0.6f, 1f, 0.6f, 0.5f);   // 연두색
        Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.5f); // 진한 빨강

        SetGhostColor(isValid ? validColor : invalidColor);
    }

    private void CreateGhost()
    {
        if (ghostRoot != null) Destroy(ghostRoot);
        ghostRoot = new GameObject("GhostRoot");
        ghostRenderers.Clear();

        Vector3 gridCellSize = Vector3.one;
        if (targetTilemap != null && targetTilemap.layoutGrid != null)
             gridCellSize = targetTilemap.layoutGrid.cellSize;

        foreach (var cell in workingCells)
        {
            GameObject go = new GameObject("GhostCell");
            go.transform.SetParent(ghostRoot.transform);
            
            // 로컬 위치 설정 (부모인 GhostRoot가 움직임)
            go.transform.localPosition = new Vector3(cell.offset.x * gridCellSize.x, cell.offset.y * gridCellSize.y, 0);
            go.transform.localScale = Vector3.one; 

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            GameObject prefab = (cell.kind == TileKind.Speed) ? speedPrefab : deSpeedPrefab;
            if (prefab != null)
            {
                var prefabSr = prefab.GetComponent<SpriteRenderer>();
                if (prefabSr) 
                {
                    sr.sprite = prefabSr.sprite;
                    // 프리팹 원래 색상 적용 (투명도는 나중에 SetGhostColor로 덮어씀)
                    sr.color = prefabSr.color; 
                }
            }
            
            // 🔥 [개선] Sorting Order를 최대로 설정하여 가려짐 방지
            sr.sortingOrder = 32767; 
            
            ghostRenderers.Add(sr);
        }
    }
    
    private void SetGhostColor(Color c) { foreach (var sr in ghostRenderers) sr.color = c; }

    // =========================================================
    // ✅ 유효성 검사 (StageManager 연동)
    // =========================================================

    private bool IsPositionValid(Vector3Int originCell)
    {
        if (workingCells.Count == 0) return false;

        foreach (var cell in workingCells)
        {
            Vector3Int checkPos = originCell + (Vector3Int)cell.offset;
            if (!CheckTileCondition(checkPos)) return false;
        }
        return true;
    }

    private bool CheckTileCondition(Vector3Int pos)
    {
        // 1. 바닥(Ground) 타일 존재 여부 확인 (허공 설치 불가)
        if (groundTilemap != null)
        {
            if (!groundTilemap.HasTile(pos)) return false;
        }

        Vector3 worldPos = targetTilemap.GetCellCenterWorld(pos);

        // 2. 🔥 StageManager에게 ClearTile인지 확인
        if (StageManager.Instance != null)
        {
            // StageManager가 인식한 ClearTile 범위 내라면 설치 금지
            if (StageManager.Instance.IsClearTile(worldPos))
            {
                // Debug.Log($"[TilePlacement] ClearTile 영역입니다. ({worldPos})");
                return false; 
            }
        }

        // 3. 물리적 장애물(Collider) 확인
        // (반경 0.2f로 체크하여 벽, 기존 타일, 장애물 등 감지)
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.2f); 

        foreach (var hit in hits)
        {
            GameObject hitObj = hit.gameObject;

            // 바닥 타일맵은 장애물 아님
            if (groundTilemap != null && hitObj == groundTilemap.gameObject) continue;
            
            // 통과 가능한 레이어(PlayerPass)는 무시 (원하면 설치 가능하게)
            if (hitObj.layer == LayerMask.NameToLayer("PlayerPass")) continue;

            // 플레이어 위에는 설치 불가
            if (playerTransform != null && hit.transform == playerTransform) return false;

            // 그 외(벽, 다른 타일 등) 충돌체가 있으면 설치 불가
            return false; 
        }

        return true;
    }

    private void SpawnObjects(Vector3Int originCell)
    {
        foreach (var cell in workingCells)
        {
            Vector3Int placePos = originCell + (Vector3Int)cell.offset;
            Vector3 spawnPos = targetTilemap.GetCellCenterWorld(placePos);
            GameObject prefabToUse = (cell.kind == TileKind.Speed) ? speedPrefab : deSpeedPrefab;

            if (prefabToUse != null)
            {
                Instantiate(prefabToUse, spawnPos, Quaternion.identity, objectRoot);
            }
        }
    }

    [System.Serializable]
    public struct TileCell
    {
        public Vector2Int offset;
        public TileKind kind;
    }
}