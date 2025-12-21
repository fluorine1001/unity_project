using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq; // LINQ 사용 (OrderBy, Min, Max)

[System.Serializable]
public class TilePrefabMapping
{
    [Tooltip("타일 sprite.name 과 동일해야 합니다.")]
    public string tileName;
    public GameObject prefab;
}

public class GeneratorManager : MonoBehaviour
{
    [Header("Tilemaps")]
    [Tooltip("탐색할 타일맵입니다.")]
    public Tilemap generatorTilemap;

    // ✅ 추가: 바닥 타일이 실제로 깔려 있는 타일맵 변수
    [Tooltip("실제 바닥 타일(tile_temp_ground 등)이 깔려 있는 타일맵")]
    public Tilemap groundTilemap;

    [Header("Prefab Mappings (TileName → Prefab)")]
    [Tooltip("타일 이름과 생성될 프리팹을 연결하는 리스트입니다.")] 
    public List<TilePrefabMapping> prefabMappings = new();

    [Header("Spawn / Clear Tile Names")]
    public List<string> spawnTileNames = new();
    public List<string> clearTileNames = new();
    public List<string> floorTileNames = new(); // ✅ 추가: 바닥 타일 이름 리스트

    [Header("Blocker Settings")]
    public List<string> blockerTileNames = new(); 
    public GameObject blockerPrefab;              

    [Header("Parent for spawned objects")]
    public Transform spawnParent;

    // 빠른 조회를 위한 딕셔너리
    private Dictionary<string, GameObject> prefabDict;

    // ✅ [추가] 알고리즘을 위한 좌표 데이터 저장소
    private List<Vector3Int> allSpawnPositions = new List<Vector3Int>();
    private List<Vector3Int> allClearPositions = new List<Vector3Int>();
    private List<Vector3Int> allBlockerPositions = new List<Vector3Int>();
    private List<Vector3Int> allFloorPositions = new List<Vector3Int>();

    [Header("Stage Analysis Settings")]
    [Tooltip("BFS 탐색에 포함할 모든 타일 이름들 (바닥, 벽, 프리팹 타일 등 전부 포함)")]
    public List<string> walkableTileNames = new List<string>();

    // ✅ [추가] 결과 저장: (타일 좌표) -> (스테이지 번호)
    // 스테이지에 속하지 않는 타일은 키가 존재하지 않음
    private Dictionary<Vector3Int, int> tileStageMap = new Dictionary<Vector3Int, int>();

    private void Start()
    {
        if (spawnParent == null)
        {
            GameObject env = GameObject.Find("MapEnvironment");
            if (env == null) env = new GameObject("MapEnvironment");
            spawnParent = env.transform;
        }

        BuildPrefabDictionary();
        GenerateObjectsFromTilemap();

        // ✅ [추가] 맵 생성이 끝난 후, 스테이지 구역 분석 실행
        AnalyzeStageAreas();
    }

    private void BuildPrefabDictionary()
    {
        prefabDict = new Dictionary<string, GameObject>();
        foreach (var mapping in prefabMappings)
        {
            if (mapping == null || mapping.prefab == null || string.IsNullOrEmpty(mapping.tileName)) continue;
            if (!prefabDict.ContainsKey(mapping.tileName)) prefabDict.Add(mapping.tileName, mapping.prefab);
        }
    }
    private bool IsWalkableTile(Vector3Int pos)
    {
        // generatorTilemap 혹은 groundTilemap 둘 중 하나에라도 타일 데이터가 있는지 확인
        TileBase genTile = generatorTilemap.GetTile(pos);
        TileBase grndTile = (groundTilemap != null) ? groundTilemap.GetTile(pos) : null;

        if (genTile == null && grndTile == null) return false;

        // 타일이 존재한다면, 그 타일의 이름이 walkable 목록에 있는지 확인
        string tName = (genTile != null) ? genTile.name : grndTile.name;

        return walkableTileNames.Contains(tName) || 
            floorTileNames.Contains(tName) || 
            spawnTileNames.Contains(tName) || 
            clearTileNames.Contains(tName);
    }

    private bool IsValidPath(Vector3Int pos)
    {
        // BFS가 길을 찾을 때 호출됨
        return IsWalkableTile(pos);
    }

    private void GenerateObjectsFromTilemap()
    {
        if (generatorTilemap == null) return;

        // 리스트 초기화
        allSpawnPositions.Clear();
        allClearPositions.Clear();
        allBlockerPositions.Clear();
        allFloorPositions.Clear();
        tileStageMap.Clear();

        // 1. generatorTilemap 순회 (프리팹 생성 및 특수 타일 수집)
        foreach (var pos in generatorTilemap.cellBounds.allPositionsWithin)
        {
            Tile tile = generatorTilemap.GetTile(pos) as Tile;
            if (tile == null) continue;

            string tileName = tile.name;
            Vector3 worldPos = generatorTilemap.GetCellCenterWorld(pos);

            if (!walkableTileNames.Contains(tileName)) walkableTileNames.Add(tileName);

            if (spawnTileNames.Contains(tileName))
            {
                allSpawnPositions.Add(pos);
                if (StageManager.Instance != null) StageManager.Instance.RegisterSpawnTile(worldPos);
            }
            else if (clearTileNames.Contains(tileName))
            {
                allClearPositions.Add(pos);
                if (StageManager.Instance != null) StageManager.Instance.RegisterClearTile(worldPos);
            }
            else if (blockerTileNames.Contains(tileName))
            {
                allBlockerPositions.Add(pos);
                if (blockerPrefab != null) Instantiate(blockerPrefab, worldPos, Quaternion.identity, spawnParent);
                continue; 
            }

            if (prefabDict.TryGetValue(tileName, out GameObject prefab))
            {
                Instantiate(prefab, worldPos, Quaternion.identity, spawnParent);
            }
        }

        if (groundTilemap != null)
        {
            // groundTilemap의 전체 범위를 로그로 찍어보세요.
            Debug.Log($"[Check] GroundTilemap Bounds: {groundTilemap.cellBounds}");

            foreach (var pos in groundTilemap.cellBounds.allPositionsWithin)
            {
                TileBase tile = groundTilemap.GetTile(pos);
                if (tile != null)
                {
                    // 타일이 발견될 때마다 이름과 좌표를 찍습니다.
                    Debug.Log($"[Found] 좌표: {pos}, 타일명: {tile.name}");
                            
                    if (floorTileNames.Contains(tile.name))
                    {
                        allFloorPositions.Add(pos);
                        if (!walkableTileNames.Contains(tile.name)) walkableTileNames.Add(tile.name);
                    }
                }
            }
        }
        else Debug.Log("There's no such thing.");

        // 보고 로그 출력
        Debug.Log($"<color=yellow>📊 [Generation Report]</color> Floor: {allFloorPositions.Count}, Spawn: {allSpawnPositions.Count}, WalkableTypes: {walkableTileNames.Count}");
        
        if (StageManager.Instance != null) StageManager.Instance.InitializeStageData();
    }

    // ==================================================================================
    // ✅ [핵심 기능] 스테이지 구역 분석 및 ID 부여 알고리즘
    // ==================================================================================
    private void AnalyzeStageAreas()
    {
        if (allSpawnPositions.Count == 0) return;

        // X좌표 오름차순 정렬
        var sortedSpawns = allSpawnPositions.OrderBy(p => p.x).ToList();

        for (int i = 0; i < sortedSpawns.Count; i++)
        {
            Vector3Int spawnPos = sortedSpawns[i];
            int minX, maxX;
            bool isMinInclusive = false;

            if (i == 0) // 0번 스테이지 (조건 3)
            {
                minX = generatorTilemap.cellBounds.xMin;
                isMinInclusive = true; // a 이상

                var closestClears = allClearPositions
                    .OrderBy(c => Mathf.Abs(c.x - spawnPos.x))
                    .Take(2);
                maxX = closestClears.Any() ? closestClears.Max(c => c.x) : generatorTilemap.cellBounds.xMax;
            }
            else // 그 외 스테이지 (조건 4)
            {
                var closestBlockers = allBlockerPositions
                    .OrderBy(b => Mathf.Abs(b.x - spawnPos.x))
                    .Take(4);
                minX = closestBlockers.Any() ? closestBlockers.Min(b => b.x) : generatorTilemap.cellBounds.xMin;
                isMinInclusive = false; // a 초과

                var closestClears = allClearPositions
                    .OrderBy(c => Mathf.Abs(c.x - spawnPos.x))
                    .Take(4);
                maxX = closestClears.Any() ? closestClears.Max(c => c.x) : generatorTilemap.cellBounds.xMax;
            }

            // BFS 실행
            RunBFS(spawnPos, i, minX, maxX, isMinInclusive);
        }
        generatorTilemap.gameObject.SetActive(false);
    }

    private void RunBFS(Vector3Int startPos, int stageID, int minX, int maxX, bool isMinInclusive)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        queue.Enqueue(startPos);

        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        visited.Add(startPos);

        if (!tileStageMap.ContainsKey(startPos)) tileStageMap.Add(startPos, stageID);

        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();

            foreach (var dir in directions)
            {
                Vector3Int next = current + dir;

                if (visited.Contains(next)) continue;

                // [조건 6] 타일이 없는 곳(Null)은 갈 수 없음
                // [추가] 하지만 walkableTileNames에 등록된 '벽', '프리팹' 등은 모두 통과 가능
                if (!IsValidPath(next)) continue;

                // [조건 3, 4] X좌표 범위 제약 (b 미만)
                if (next.x >= maxX) continue;

                // [조건 3, 4] a 이상/초과 제약
                if (isMinInclusive) {
                    if (next.x < minX) continue; // 0번 스테이지: a 이상
                } else {
                    if (next.x <= minX) continue; // 그 외: a 초과
                }

                // [조건 5] 위 조건을 만족하면 어떤 제약도 없이 탐색(BFS 확장)
                visited.Add(next);
                queue.Enqueue(next);

                // [조건 7] 탐색된 모든 타일에 스테이지 ID 할당
                if (!tileStageMap.ContainsKey(next)) 
                    tileStageMap.Add(next, stageID);
                else 
                    tileStageMap[next] = stageID; 
            }
        }
    }

    public int GetStageIndexFromWorldPos(Vector3 worldPos)
    {
        if (generatorTilemap == null) return -1;

        Vector3Int cellPos = generatorTilemap.WorldToCell(worldPos);
        
        // generatorTilemap에 없으면 groundTilemap에서 타일을 찾음
        TileBase tile = generatorTilemap.GetTile(cellPos);
        if (tile == null && groundTilemap != null) tile = groundTilemap.GetTile(cellPos);
        
        string tileName = (tile != null) ? tile.name : "None (공백)";

        if (tileStageMap.TryGetValue(cellPos, out int stageIndex))
        {
            
            return stageIndex;
        }
        
        
        return -1;
    }
}