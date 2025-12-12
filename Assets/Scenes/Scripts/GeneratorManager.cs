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

    [Tooltip("StageManager.RegisterClearTile() 에 등록될 타일 sprite 이름 목록")]
    public List<string> clearTileNames = new();

    [Header("Parent for spawned objects")]
    public Transform spawnParent;

    [Header("Rendering")]
    [Tooltip("생성된 모든 프리팹의 SpriteRenderer.sortingOrder")]
    [SerializeField] private int spawnOrderInLayer = -2;

    /// <summary>
    /// tileName → prefab 캐시용 딕셔너리
    /// </summary>
    private Dictionary<string, GameObject> prefabDict;

    private void Start()
    {
        BuildPrefabDictionary();
        GenerateObjectsFromTilemap();
    }

    /// <summary>
    /// Inspector에서 받은 prefabMappings를 딕셔너리(prefabDict)로 변환
    /// </summary>
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

            // 프로젝트 창에 보이는 '타일 파일(Asset)'의 이름을 가져옴 (편함!)
            string tileName = tile.name;
            Vector3 worldPos = generatorTilemap.GetCellCenterWorld(pos);

            // 1) Spawn Tile
            if (spawnTileNames.Contains(tileName))
            {
                StageManager.Instance.RegisterSpawnTile(worldPos);
                continue;
            }

            // 2) Clear Tile
            if (clearTileNames.Contains(tileName))
            {
                StageManager.Instance.RegisterClearTile(worldPos);
                continue;
            }

            // 3) Prefab 매핑 Tile
            if (prefabDict.TryGetValue(tileName, out GameObject prefab))
            {
                var go = Instantiate(prefab, worldPos, Quaternion.identity, spawnParent);
                ApplyOrderInLayer(go, spawnOrderInLayer);
            }
        }

        // 게임 시작 후 generator tilemap 숨김
        generatorTilemap.gameObject.SetActive(false);
    }

    /// <summary>
    /// 전달한 오브젝트 및 자식 오브젝트의 SpriteRenderer.sortingOrder를 통일해 설정
    /// </summary>
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
