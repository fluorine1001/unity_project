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

    private GameObject ghostRoot;
    private List<SpriteRenderer> ghostRenderers = new List<SpriteRenderer>();

    private void Awake() 
    { 
        Instance = this; 
        
        // 게임 시작 시 자동 할당 실행
        AutoAssignSettings();
    }

    // 컴포넌트를 추가하거나 Inspector에서 Reset을 눌렀을 때도 실행
    private void Reset()
    {
        AutoAssignSettings();
    }

    /// <summary>
    /// 요청된 오브젝트와 프리팹을 자동으로 찾아 할당합니다.
    /// </summary>
    private void AutoAssignSettings()
    {
        // 1. Grid 하위의 tile_temp_ground 찾기
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

        // 2. ObjectRoot 오브젝트 찾기
        if (objectRoot == null)
        {
            GameObject objRoot = GameObject.Find("ObjectRoot");
            if (objRoot != null) objectRoot = objRoot.transform;
        }

        // 3. player_0 오브젝트 찾기
        if (playerTransform == null)
        {
            GameObject player = GameObject.Find("player_0");
            if (player != null) playerTransform = player.transform;
        }

#if UNITY_EDITOR
        // 4. 프리팹 에셋 로드 (에디터 환경에서만 동작)
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
    // 👆 드래그 핸들러
    // =========================================================

    public void StartDrag(TileDefinition def, PaletteItemUI ui, Vector3 startScreenPos)
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.TilesSelected, this.transform.position);

        if (def == null || def.cells == null || def.cells.Count == 0)
        {
            Debug.LogError("[TilePlacementManager] 타일 정의(Definition)가 비어있습니다!");
            return;
        }

        isDragging = true;
        currentUI = ui;
        lastScreenPos = startScreenPos;

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
            AudioManager.instance.PlayOneShot(FMODEvents.instance.TilesDropped, this.transform.position);
            SpawnObjects(originCell);
            
            if (StageManager.Instance != null && currentUI != null)
            {
                StageManager.Instance.ConsumeTile(currentUI.LoadoutIndex);
            }

            if (currentUI) Destroy(currentUI.gameObject); 
        }
        else
        {
            AudioManager.instance.PlayOneShot(FMODEvents.instance.TilesBlocked, this.transform.position);
            Debug.Log("[TilePlacementManager] 설치 실패: 유효하지 않은 위치");
        }
        
        if (ghostRoot) Destroy(ghostRoot);
        currentUI = null;
        workingCells.Clear();
    }

    // =========================================================
    // 🔄 회전 로직
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

    // =========================================================
    // 👻 Ghost 및 좌표 처리
    // =========================================================
    
    private Vector3Int GetCellPosFromScreen(Vector3 screenPos)
    {
        if (Camera.main == null || targetTilemap == null) return Vector3Int.zero;

        // Plane Raycast를 사용하여 정확한 Z=0 지점 찾기
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

    private void UpdateGhostVisual(Vector3 screenPos)
    {
        if (ghostRoot == null) return;

        Vector3Int cellPos = GetCellPosFromScreen(screenPos);
        ghostRoot.transform.position = targetTilemap.GetCellCenterWorld(cellPos);

        bool isValid = IsPositionValid(cellPos);
        SetGhostColor(isValid ? new Color(1, 1, 1, 0.5f) : new Color(1, 0, 0, 0.5f));
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
            
            go.transform.localPosition = new Vector3(cell.offset.x * gridCellSize.x, cell.offset.y * gridCellSize.y, 0);
            go.transform.localScale = Vector3.one; 

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            GameObject prefab = (cell.kind == TileKind.Speed) ? speedPrefab : deSpeedPrefab;
            
            if (prefab != null)
            {
                var prefabSr = prefab.GetComponent<SpriteRenderer>();
                if (prefabSr) sr.sprite = prefabSr.sprite;
            }
            sr.sortingOrder = 999; 
            ghostRenderers.Add(sr);
        }
    }
    
    private void SetGhostColor(Color c) { foreach (var sr in ghostRenderers) sr.color = c; }

    // =========================================================
    // ✅ 유효성 검사
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
        if (groundTilemap != null && !groundTilemap.HasTile(pos)) return false;

        Vector2 worldPos = targetTilemap.GetCellCenterWorld(pos);
        Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.2f); 
        
        if (hit == null) return true; 

        GameObject hitObj = hit.gameObject;

        if (groundTilemap != null && hitObj == groundTilemap.gameObject) return true;
        if (playerTransform != null && hit.transform == playerTransform) return false; // 플레이어 위치 설치 불가
        if (hitObj.layer == LayerMask.NameToLayer("PlayerPass")) return true;

        return false;
    }

    // =========================================================
    // 🏗️ 오브젝트 생성
    // =========================================================
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