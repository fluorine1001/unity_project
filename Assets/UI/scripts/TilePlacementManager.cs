using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.InputSystem; // 🔥 [필수] New Input System 네임스페이스 추가

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

    private void Awake() { Instance = this; }

    // =========================================================
    // 🖱️ Unity Update (New Input System 적용)
    // =========================================================
    private void Update()
    {
        // 드래그 중이 아니면 무시
        if (!isDragging) return;

        bool rotateInput = false;

        // 1. 키보드 'R' 키 입력 확인
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            rotateInput = true;
        }

        // 2. 마우스 우클릭 확인
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            rotateInput = true;
        }

        // 회전 입력이 들어왔다면
        if (rotateInput)
        {
            RotateWorkingCellsClockwise();    // 데이터 회전
            CreateGhost();                    // 고스트 다시 생성
            UpdateGhostVisual(lastScreenPos); // 위치 즉시 갱신
        }
    }

    // =========================================================
    // 👆 드래그 핸들러 (PaletteItemUI에서 호출)
    // =========================================================

    public void StartDrag(TileDefinition def, PaletteItemUI ui, Vector3 startScreenPos)
    {
        isDragging = true;
        currentUI = ui;
        lastScreenPos = startScreenPos;

        workingCells.Clear();
        if (def != null && def.cells != null)
        {
            foreach (var cell in def.cells)
            {
                workingCells.Add(new TileCell { offset = cell.offset, kind = cell.kind });
            }
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
        isDragging = false;

        Vector3Int originCell = GetCellPosFromScreen(screenPos);

        if (IsPositionValid(originCell))
        {
            SpawnObjects(originCell);
            if (currentUI) Destroy(currentUI.gameObject); 
        }
        
        if (ghostRoot) Destroy(ghostRoot);
        currentUI = null;
        workingCells.Clear();
    }

    // =========================================================
    // 🔄 회전 로직 (시계 방향 90도)
    // =========================================================
    private void RotateWorkingCellsClockwise()
    {
        for (int i = 0; i < workingCells.Count; i++)
        {
            TileCell cell = workingCells[i];
            
            // 시계 방향: (x, y) -> (y, -x)
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
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = 0; 
        return targetTilemap.WorldToCell(worldPos);
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

        // 그리드 정보가 없으면 기본값 1
        Vector3 gridCellSize = Vector3.one;
        if (targetTilemap != null && targetTilemap.layoutGrid != null)
             gridCellSize = targetTilemap.layoutGrid.cellSize;

        foreach (var cell in workingCells)
        {
            GameObject go = new GameObject("GhostCell");
            go.transform.SetParent(ghostRoot.transform);
            
            go.transform.localPosition = new Vector3(cell.offset.x * gridCellSize.x, cell.offset.y * gridCellSize.y, 0);
            
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            GameObject prefab = (cell.kind == TileKind.Speed) ? speedPrefab : deSpeedPrefab;
            
            if (prefab != null)
            {
                var prefabSr = prefab.GetComponent<SpriteRenderer>();
                if (prefabSr) 
                {
                    sr.sprite = prefabSr.sprite;
                    if (sr.sprite != null)
                    {
                        Vector3 spriteSize = sr.sprite.bounds.size;
                        if (spriteSize.x != 0 && spriteSize.y != 0)
                        {
                            go.transform.localScale = new Vector3(
                                gridCellSize.x / spriteSize.x, 
                                gridCellSize.y / spriteSize.y, 
                                1f);
                        }
                    }
                }
            }
            sr.sortingOrder = 100;
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
        Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.3f);
        
        if (hit == null) return true; 

        GameObject hitObj = hit.gameObject;

        if (groundTilemap != null && hitObj == groundTilemap.gameObject) return true;
        if (playerTransform != null && hit.transform == playerTransform) return true;
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
}

// 데이터 정의
[System.Serializable]
public struct TileCell
{
    public Vector2Int offset;
    public TileKind kind;
}
