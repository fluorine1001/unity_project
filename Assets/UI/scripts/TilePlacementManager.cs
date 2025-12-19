using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TilePlacementManager : MonoBehaviour
{
    public static TilePlacementManager Instance { get; private set; }

    [Header("Settings")]
    public Tilemap targetTilemap;       // 좌표 계산용
    public Tilemap groundTilemap;       // 바닥 확인용
    public Transform objectRoot;        // 오브젝트 정리용

    [Header("Interaction")]
    public Transform playerTransform;   // 플레이어 (발 밑 확인용)

    [Header("Prefabs")]
    public GameObject speedPrefab;      
    public GameObject deSpeedPrefab;    

    private TileDefinition currentDef;
    private PaletteItemUI currentUI;
    private GameObject ghostRoot;
    private List<SpriteRenderer> ghostRenderers = new List<SpriteRenderer>();

    private void Awake() { Instance = this; }

    public void StartDrag(TileDefinition def, PaletteItemUI ui)
    {
        currentDef = def;
        currentUI = ui;
        CreateGhost(def);
    }

    public void UpdateDrag(Vector3 screenPos)
    {
        if (ghostRoot == null) return;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;
        
        Vector3Int cellPos = targetTilemap.WorldToCell(worldPos);
        ghostRoot.transform.position = targetTilemap.GetCellCenterWorld(cellPos);

        bool isValid = IsPositionValid(cellPos, currentDef);
        SetGhostColor(isValid ? new Color(1, 1, 1, 0.5f) : new Color(1, 0, 0, 0.5f));
    }

    public void EndDrag(Vector3 screenPos)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        Vector3Int originCell = targetTilemap.WorldToCell(worldPos);

        if (IsPositionValid(originCell, currentDef))
        {
            SpawnObjects(originCell, currentDef);
            if (currentUI) Destroy(currentUI.gameObject); 
        }
        
        if (ghostRoot) Destroy(ghostRoot);
        currentDef = null;
        currentUI = null;
    }

    private void SpawnObjects(Vector3Int originCell, TileDefinition def)
    {
        foreach (var cell in def.cells)
        {
            Vector3Int placePos = originCell + (Vector3Int)cell.offset;
            Vector3 spawnPos = targetTilemap.GetCellCenterWorld(placePos);
            GameObject prefabToUse = (cell.kind == TileKind.Speed) ? speedPrefab : deSpeedPrefab;

            if (prefabToUse != null)
                Instantiate(prefabToUse, spawnPos, Quaternion.identity, objectRoot);
        }
    }

    // =========================================================
    // 🔥 [수정됨] BFS 제거, 단순 위치/레이어 조건 검사
    // =========================================================
    private bool IsPositionValid(Vector3Int originCell, TileDefinition def)
    {
        if (def == null) return false;

        foreach (var cell in def.cells)
        {
            Vector3Int checkPos = originCell + (Vector3Int)cell.offset;

            // 해당 칸에 설치해도 되는지 검사 (단일 함수로 통합)
            if (!CheckTileCondition(checkPos)) return false;
        }
        return true;
    }

    private bool CheckTileCondition(Vector3Int pos)
    {
        // 1. 바닥(Ground)이 없으면 허공이므로 설치 불가
        if (groundTilemap != null && !groundTilemap.HasTile(pos)) return false;

        // 2. 해당 위치에 무엇이 있는지 확인 (반경 0.3f)
        Vector2 worldPos = targetTilemap.GetCellCenterWorld(pos);
        Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.3f);
        
        // 아무것도 없으면? -> 바닥은 있으니 설치 가능 (OK)
        if (hit == null) return true;

        // 무언가 있다면, 그게 무엇인지 판단
        GameObject hitObj = hit.gameObject;

        // A. 감지된 게 '바닥 타일맵' 자체라면 -> 무시하고 설치 가능
        if (groundTilemap != null && hitObj == groundTilemap.gameObject) return true;

        // B. 감지된 게 '플레이어'라면 -> 플레이어 발 밑 설치 허용 (선택 사항)
        if (playerTransform != null && hit.transform == playerTransform) return true;

        // C. 🔥 [핵심] 감지된 물체의 레이어가 'PlayerPass'인가?
        // PlayerPass 레이어라면 (예: 이미 설치된 스피드 타일, 아이템 등) -> 겹쳐 설치 허용
        if (hitObj.layer == LayerMask.NameToLayer("PlayerPass"))
        {
            return true; 
        }

        // D. 그 외 (Wall, Obstacle 등 PlayerPass가 아닌 레이어) -> 설치 불가
        return false;
    }

    private void CreateGhost(TileDefinition def)
    {
        ghostRoot = new GameObject("GhostRoot");
        ghostRenderers.Clear();
        Vector3 gridCellSize = Vector3.one;
        if (targetTilemap != null && targetTilemap.layoutGrid != null)
            gridCellSize = targetTilemap.layoutGrid.cellSize;

        foreach (var cell in def.cells)
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
                        float scaleX = gridCellSize.x / spriteSize.x;
                        float scaleY = gridCellSize.y / spriteSize.y;
                        go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                    }
                }
                else go.transform.localScale = prefab.transform.localScale;
            }
            sr.sortingOrder = 100;
            ghostRenderers.Add(sr);
        }
    }
    private void SetGhostColor(Color c) { foreach (var sr in ghostRenderers) sr.color = c; }
}