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

        // X좌표 순으로 정렬된 스폰 지점들
        var sortedSpawns = allSpawnPositions.OrderBy(p => p.x).ToList();

        for (int i = 0; i < sortedSpawns.Count; i++)
        {
            Vector3Int spawnPos = sortedSpawns[i];
            int minX, maxX;
            bool isMinInclusive = false;

            // ✅ [수정] 내 위치(spawnPos.x)를 기준으로 좌/우 클리어 타일을 분리해서 찾음
            var leftClears = allClearPositions.Where(c => c.x < spawnPos.x).OrderByDescending(c => c.x).ToList();
            var rightClears = allClearPositions.Where(c => c.x > spawnPos.x).OrderBy(c => c.x).ToList();

            if (i == 0) // 0번 스테이지
            {
                minX = generatorTilemap.cellBounds.xMin;
                isMinInclusive = true; 
                // 오른쪽의 첫 번째 클리어 타일이 경계
                maxX = rightClears.Any() ? rightClears[0].x : generatorTilemap.cellBounds.xMax;
            }
            else // 1번 이상의 스테이지
            {
                // 왼쪽에서 가장 가까운 클리어 타일의 x가 시작점
                minX = leftClears.Any() ? leftClears[0].x : generatorTilemap.cellBounds.xMin;
                isMinInclusive = false; // 클리어 타일 "초과" 부터 내 구역

                // 오른쪽에서 가장 가까운 클리어 타일의 x가 끝점
                maxX = rightClears.Any() ? rightClears[0].x : generatorTilemap.cellBounds.xMax;
            }

            Debug.Log($"<color=white>[Stage {i} 분석 범위 설정]</color> {minX} < x <= {maxX} (중심점: {spawnPos.x})");

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

        // 이미 할당된 타일이면 덮어쓰지 않거나, 현재 스테이지가 우선순위가 높다면 갱신
        if (!tileStageMap.ContainsKey(startPos)) 
            tileStageMap.Add(startPos, stageID);
        else 
            tileStageMap[startPos] = stageID;

        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            bool isCurrentClear = allClearPositions.Contains(current);

            foreach (var dir in directions)
            {
                Vector3Int next = current + dir;

                if (visited.Contains(next)) continue;
                if (!IsValidPath(next)) continue;

                // X좌표 범위 체크
                if (next.x > maxX) continue;
                if (isMinInclusive) { if (next.x < minX) continue; }
                else { if (next.x <= minX) continue; }

                // ✅ 핵심: 클리어 타일(경계선)에 도달하면 해당 타일까지만 ID를 부여하고 더 이상 전진하지 않음
                if (isCurrentClear) continue; 

                visited.Add(next);
                queue.Enqueue(next);

                // 데이터 할당
                tileStageMap[next] = stageID; 
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
        
        // 🔍 [디버그] 현재 플레이어가 밟고 있는 좌표 정보 출력
        // Debug.Log($"<color=white>[Pos Check]</color> World: {worldPos} -> Cell: {cellPos}");

        // 2. 해당 좌표에 어떤 타일이 있는지 확인 (어떤 타일맵에서 읽히는지 확인용)
        TileBase genTile = generatorTilemap.GetTile(cellPos);
        TileBase grndTile = (groundTilemap != null) ? groundTilemap.GetTile(cellPos) : null;
        string foundTileName = (genTile != null) ? genTile.name : (grndTile != null ? grndTile.name : "None");

        // 3. tileStageMap에서 데이터 조회
        if (tileStageMap.TryGetValue(cellPos, out int stageIndex))
        {
            // 성공 로그 (ClearTile인 경우 별도 표시)
            if (clearTileNames.Contains(foundTileName))
            {
                Debug.Log($"<color=cyan>[Stage Found]</color> ClearTile({foundTileName}) 위에서 스테이지 {stageIndex} 확인됨! (Cell: {cellPos})");
            }
            return stageIndex;
        }
        else
        {
            // 🔍 [실패 분석 로그] 
            // BFS에서 감지했다면 무조건 맵에 있어야 함. 없다면 좌표가 어긋난 것임.
            Debug.LogWarning($"<color=yellow>[Stage Missing]</color> 좌표 {cellPos}에는 할당된 번호가 없습니다. (밟고 있는 타일: {foundTileName})");
            
            // 주변 1칸을 뒤져서 가장 가까운 스테이지 번호를 찾는 보정 로직 (옵션)
            return FindNearbyStageIndex(cellPos);
        }
    }

    // 만약 좌표 오차로 인해 못 찾는 경우 주변을 검색하는 보조 함수
    private int FindNearbyStageIndex(Vector3Int cellPos)
    {
        Vector3Int[] neighbors = { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down };
        foreach (var offset in neighbors)
        {
            if (tileStageMap.TryGetValue(cellPos + offset, out int idx))
            {
                Debug.Log($"<color=magenta>[Stage Compensated]</color> 본래 좌표 {cellPos}엔 없으나 인접한 {cellPos + offset}에서 스테이지 {idx}를 찾았습니다.");
                return idx;
            }
        }
        return -1;
    }

}