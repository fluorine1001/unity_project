using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

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

    [Header("Prefab Mappings (TileName → Prefab)")]
    [Tooltip("타일 이름과 생성될 프리팹을 연결하는 리스트입니다. (Spawn, Clear 타일도 여기에 시각적 프리팹이 등록되어 있어야 합니다)")] 
    public List<TilePrefabMapping> prefabMappings = new();

    [Header("Spawn / Clear Tile Names")]
    [Tooltip("StageManager에 체크포인트(Spawn)로 등록할 타일 이름들")]
    public List<string> spawnTileNames = new();

    [Tooltip("StageManager에 클리어 지점(Clear)으로 등록할 타일 이름들")]
    public List<string> clearTileNames = new();

    [Header("Blocker Settings")]
    [Tooltip("TileBlocker(설치 불가, 통과 가능)로 변환할 타일 이름들")]
    public List<string> blockerTileNames = new(); 

    [Tooltip("생성될 Blocker 프리팹 (투명, IsTrigger 권장)")]
    public GameObject blockerPrefab;              

    [Header("Parent for spawned objects")]
    [Tooltip("생성된 오브젝트들이 들어갈 부모. 비워두면 자동으로 'MapEnvironment'를 생성합니다.")]
    public Transform spawnParent;

    // 빠른 조회를 위한 딕셔너리
    private Dictionary<string, GameObject> prefabDict;

    private void Start()
    {
        // [중요] StageManager가 맵을 백업할 수 있도록 부모 오브젝트 설정
        // 사용자가 Inspector에 할당하지 않았다면 자동으로 "MapEnvironment"를 생성해서 할당
        if (spawnParent == null)
        {
            GameObject env = GameObject.Find("MapEnvironment");
            if (env == null) env = new GameObject("MapEnvironment");
            spawnParent = env.transform;
        }

        // 1. 프리팹 매핑 딕셔너리 구축
        BuildPrefabDictionary();
        
        // 2. 타일맵을 읽어 오브젝트 생성 및 데이터 등록
        GenerateObjectsFromTilemap();
    }

    private void BuildPrefabDictionary()
    {
        prefabDict = new Dictionary<string, GameObject>();

        foreach (var mapping in prefabMappings)
        {
            if (mapping == null || mapping.prefab == null || string.IsNullOrEmpty(mapping.tileName))
            {
                // Debug.LogWarning("[Generator] 유효하지 않은 TilePrefabMapping이 발견되었습니다.");
                continue;
            }

            if (!prefabDict.ContainsKey(mapping.tileName))
            {
                prefabDict.Add(mapping.tileName, mapping.prefab);
            }
            else
            {
                Debug.LogWarning($"[Generator] 중복된 타일 이름이 감지되었습니다: {mapping.tileName}. 첫 번째 매핑만 사용됩니다.");
            }
        }
    }

    private void GenerateObjectsFromTilemap()
    {
        if (generatorTilemap == null)
        {
            Debug.LogError("[Generator] GeneratorTilemap이 연결되지 않았습니다!");
            return;
        }

        // 타일맵의 모든 위치를 순회
        foreach (var pos in generatorTilemap.cellBounds.allPositionsWithin)
        {
            Tile tile = generatorTilemap.GetTile(pos) as Tile;
            if (tile == null) continue; // 빈 칸은 건너뜀

            string tileName = tile.name;
            Vector3 worldPos = generatorTilemap.GetCellCenterWorld(pos);

            // ==========================================================
            // 1. 데이터 등록 단계 (StageManager에 위치 알림)
            // ==========================================================

            // A. Spawn 타일 (체크포인트)
            if (spawnTileNames.Contains(tileName))
            {
                if (StageManager.Instance != null)
                {
                    StageManager.Instance.RegisterSpawnTile(worldPos);
                }
                // Continue 하지 않음 (시각적 프리팹 생성)
            }

            // B. Clear 타일 (클리어 지점)
            if (clearTileNames.Contains(tileName))
            {
                if (StageManager.Instance != null)
                {
                    StageManager.Instance.RegisterClearTile(worldPos);
                }
                // Continue 하지 않음 (시각적 프리팹 생성)
            }

            // ==========================================================
            // 2. 오브젝트 생성 단계 (Instantiate)
            // ==========================================================

            // C. Blocker 타일 (설치 금지 구역)
            if (blockerTileNames.Contains(tileName))
            {
                if (blockerPrefab != null)
                {
                    Instantiate(blockerPrefab, worldPos, Quaternion.identity, spawnParent);
                }
                else
                {
                    Debug.LogWarning($"[Generator] {tileName}은 Blocker로 설정되었으나, blockerPrefab이 비어있습니다.");
                }
                continue; // Blocker는 프리팹 매핑을 거치지 않으므로 여기서 끝
            }

            // D. 일반/Spawn/Clear 프리팹 생성
            if (prefabDict.TryGetValue(tileName, out GameObject prefab))
            {
                Instantiate(prefab, worldPos, Quaternion.identity, spawnParent);
            }
        }
        
        // 3. 최적화를 위해 원본 타일맵 비활성화
        generatorTilemap.gameObject.SetActive(false);

        // ==========================================================
        // 3. 데이터 정렬 및 초기화 요청 (필수)
        // ==========================================================
        if (StageManager.Instance != null)
        {
            Debug.Log("<color=cyan>[Generator]</color> 맵 생성 및 등록 완료 -> StageManager 초기화 요청");
            StageManager.Instance.InitializeStageData();
        }
    }
}