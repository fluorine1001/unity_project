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

    [Header("Logic Blocking Settings")]
    [Tooltip("에디터에서 직접 만든 로직용 타일맵을 여기에 할당하세요.")]
    public Tilemap logicTilemap; 

    [Tooltip("BFS 탐색을 막을 타일 에셋을 여기에 할당하세요. (이 타일이 로직 타일맵에 찍혀 있으면 탐색을 멈춥니다)")]
    public TileBase logicBlockerTile;           

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

    // ✅ 1. 싱글톤 인스턴스 정의 (추가)
    public static GeneratorManager Instance { get; private set; }

    private void Awake()
    {
        // ✅ 2. 싱글톤 초기화 (추가)
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

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

        if (logicTilemap != null)
        {
            var renderer = logicTilemap.GetComponent<TilemapRenderer>();
            if (renderer != null) renderer.enabled = false;
        }

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

    // GeneratorManager.cs 내부에 추가
    public bool IsBlockerTile(Vector3 worldPos)
    {
        if (generatorTilemap == null) return false;
        Vector3Int cellPos = generatorTilemap.WorldToCell(worldPos);
        
        // 분석 단계에서 수집한 블로커 좌표 리스트에 포함되어 있는지 확인
        return allBlockerPositions.Contains(cellPos);
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

                // ✅ [출력 추가] 타일 이름, 타일맵 좌표, 실제 월드 좌표 출력
                Debug.Log($"<color=green>[SpawnTile 발견]</color> 이름: {tileName} | 타일좌표: {pos} | 월드좌표: {worldPos}");
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

        // X좌표 순으로 정렬 (스테이지 번호 부여 순서 보장용)
        var sortedSpawns = allSpawnPositions.OrderBy(p => p.x).ToList();

        // 기존의 범위 계산 로직(minX, maxX)을 모두 삭제하고 단순화합니다.
        for (int i = 0; i < sortedSpawns.Count; i++)
        {
            Vector3Int spawnPos = sortedSpawns[i];
            
            // 좌표 제한 없이 시작점과 스테이지 ID만 넘깁니다.
            RunBFS(spawnPos, i);
        }

        // 분석이 끝났으므로 생성용 타일맵은 숨김 처리
        if (generatorTilemap != null) generatorTilemap.gameObject.SetActive(false);
    }

    // GeneratorManager.cs 내부

    private void RunBFS(Vector3Int startPos, int stageID)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        queue.Enqueue(startPos);

        HashSet<Vector3Int> visitedInThisPass = new HashSet<Vector3Int>();
        visitedInThisPass.Add(startPos);

        if (!tileStageMap.ContainsKey(startPos)) tileStageMap.Add(startPos, stageID);
        else tileStageMap[startPos] = stageID;

        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();

            foreach (var dir in directions)
            {
                Vector3Int next = current + dir;

                if (visitedInThisPass.Contains(next)) continue;
                if (!IsValidPath(next)) continue; 

                // 🛑 1. [기존] 물리적 벽(Blocker) 체크
                if (allBlockerPositions.Contains(next)) continue; 

                // 🛑 2. [NEW] 로직 타일맵 체크 (투명 벽)
                // 플레이어는 지나갈 수 있지만, 스테이지 구역 확장은 여기서 멈춥니다.
                if (logicTilemap != null && logicBlockerTile != null)
                {
                    // 해당 좌표에 있는 타일 가져오기
                    TileBase tileOnLogic = logicTilemap.GetTile(next);
                    
                    // 우리가 지정한 '차단 타일'과 똑같은 타일이면 탐색 중단
                    if (tileOnLogic == logicBlockerTile) 
                    {
                        continue; 
                    }
                }

                // 🛑 3. [다른 스테이지 체크]
                if (!tileStageMap.ContainsKey(next))
                {
                    tileStageMap.Add(next, stageID);
                }
                
                // 🛑 4. [ClearTile 체크]
                if (allClearPositions.Contains(next))
                {
                    visitedInThisPass.Add(next);
                    continue; 
                }

                visitedInThisPass.Add(next);
                queue.Enqueue(next);
            }
        }
    }
    // GeneratorManager.cs

    public int GetStageIndexFromWorldPos(Vector3 worldPos)
    {
        if (generatorTilemap == null) 
        {
            Debug.LogError("<color=red>[Generator]</color> generatorTilemap이 연결되지 않았습니다!");
            return -1;
        }

        // 1. 월드 좌표를 셀 좌표로 변환
        Vector3Int cellPos = generatorTilemap.WorldToCell(worldPos);

        // 2. 해당 좌표에 어떤 타일이 있는지 확인 (디버깅용)
        TileBase genTile = generatorTilemap.GetTile(cellPos);
        TileBase grndTile = (groundTilemap != null) ? groundTilemap.GetTile(cellPos) : null;
        string foundTileName = (genTile != null) ? genTile.name : (grndTile != null ? grndTile.name : "None");

        // 3. tileStageMap에서 데이터 조회
        if (tileStageMap.TryGetValue(cellPos, out int stageIndex))
        {
            if (clearTileNames.Contains(foundTileName))
            {
                // Debug.Log($"<color=cyan>[Stage Found]</color> ClearTile({foundTileName}) 위에서 스테이지 {stageIndex} 확인됨! (Cell: {cellPos})");
            }
            return stageIndex;
        }
        else
        {
            // 🛑 [수정됨] 인접한 스테이지를 찾는 로직(FindNearbyStageIndex)을 제거했습니다.
            // 정확히 타일 위에 있지 않다면 -1을 반환합니다.
            return -1; 
        }
    }

}