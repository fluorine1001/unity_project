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
    public Tilemap generatorTilemap;

    [Header("Prefab Mappings (TileName → Prefab)")]
    [Tooltip("타일 이름과 대응되는 프리팹 리스트")] 
    public List<TilePrefabMapping> prefabMappings = new();

    [Header("Spawn / Clear Tile Names")]
    [Tooltip("StageManager.RegisterSpawnTile() 에 등록될 타일 sprite 이름 목록")]
    public List<string> spawnTileNames = new();

    [Header("Spawn / Clear Tile Names")]
    [Tooltip("StageManager.RegisterClearTile() 에 등록될 타일 sprite 이름 목록")]
    public List<string> clearTileNames = new();

    [Header("Parent for spawned objects")]
    public Transform spawnParent;

    [Header("Rendering")]
    [Tooltip("DynamicYDepthSort의 Base Sorting Order와 동일하게 설정해야 합니다. (3만 제한 내에서 최대)")]
    [SerializeField] public int spawnOrderInLayer = 29999; // <--- 2. public으로 변경, 29999로 설정

    private Dictionary<string, GameObject> prefabDict;

    private void Start()
    {
        BuildPrefabDictionary();
        GenerateObjectsFromTilemap();
    }

    private void BuildPrefabDictionary()
    {
        prefabDict = new Dictionary<string, GameObject>();

        foreach (var mapping in prefabMappings)
        {
            if (mapping == null || mapping.prefab == null || string.IsNullOrEmpty(mapping.tileName))
            {
                Debug.LogWarning("[Generator] 잘못된 TilePrefabMapping이 있습니다.");
                continue;
            }

            if (!prefabDict.ContainsKey(mapping.tileName))
                prefabDict.Add(mapping.tileName, mapping.prefab);
            else
                Debug.LogWarning($"[Generator] 중복된 tileName 감지: {mapping.tileName}");
        }
    }

    private void GenerateObjectsFromTilemap()
{
    foreach (var pos in generatorTilemap.cellBounds.allPositionsWithin)
    {
        Tile tile = generatorTilemap.GetTile(pos) as Tile;
        if (tile == null) continue;

        string tileName = tile.name;
        Vector3 worldPos = generatorTilemap.GetCellCenterWorld(pos);

        // --- 수정된 부분 시작 ---

        // 1. Spawn 타일인지 확인 후 등록
        if (spawnTileNames.Contains(tileName))
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.RegisterSpawnTile(worldPos);
            }
            continue; // 프리팹 생성 안 함
        }

        // 2. Clear 타일인지 확인 후 등록
        if (clearTileNames.Contains(tileName))
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.RegisterClearTile(worldPos);
            }
            continue; // 프리팹 생성 안 함
        }

        // --- 수정된 부분 끝 ---

        if (prefabDict.TryGetValue(tileName, out GameObject prefab))
        {
            var go = Instantiate(prefab, worldPos, Quaternion.identity, spawnParent);
            // ApplyOrderInLayer(go, spawnOrderInLayer); // 필요시 활성화
        }
    }
    
    // 모든 타일 스캔이 끝난 후 타일맵 비활성화
    generatorTilemap.gameObject.SetActive(false);
}
    private void ApplyOrderInLayer(GameObject go, int order)
    {
        if (go == null) return;

        var renderers = go.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        foreach (var r in renderers)
        {
            r.sortingOrder = order;
        }
    }
}