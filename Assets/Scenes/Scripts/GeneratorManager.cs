using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public class TilePrefabMapping
{
    public string tileName;
    public GameObject prefab;
}

public class GeneratorManager : MonoBehaviour
{
    public Tilemap generatorTilemap;
    public List<TilePrefabMapping> prefabMappings = new();
    public List<string> spawnTileNames = new();
    public List<string> clearTileNames = new();
    public Transform spawnParent;
    public int spawnOrderInLayer = 29999;

    private Dictionary<string, GameObject> prefabDict;

    private void Start()
    {
        if (generatorTilemap != null && generatorTilemap.layoutGrid != null)
        {
            if (StageManager.Instance != null)
                StageManager.Instance.SetGridSize(generatorTilemap.layoutGrid.cellSize);
        }

        BuildPrefabDictionary();
        GenerateObjectsFromTilemap();
    }

    private void BuildPrefabDictionary()
    {
        prefabDict = new Dictionary<string, GameObject>();
        foreach (var mapping in prefabMappings)
        {
            if (mapping != null && mapping.prefab != null && !string.IsNullOrEmpty(mapping.tileName))
            {
                if (!prefabDict.ContainsKey(mapping.tileName))
                    prefabDict.Add(mapping.tileName, mapping.prefab);
            }
        }
    }

    // 🔥 [수정됨] 생성 로직 전면 수정
    private void GenerateObjectsFromTilemap()
    {
        if (generatorTilemap == null) return;

        foreach (var pos in generatorTilemap.cellBounds.allPositionsWithin)
        {
            TileBase tileBase = generatorTilemap.GetTile(pos);
            if (tileBase == null) continue;

            string tileName = tileBase.name;
            Vector3 worldPos = generatorTilemap.GetCellCenterWorld(pos);

            // 1. Spawn 타일 (플레이어 시작 위치)
            // - 로직만 등록하고, 벽은 생성하지 않으므로 continue
            if (spawnTileNames.Contains(tileName))
            {
                if (StageManager.Instance != null) StageManager.Instance.RegisterSpawnTile(worldPos);
                continue; 
            }

            // 2. Clear 타일 (목표 지점)
            // - 로직 등록 후, 시각적 타일도 생성해야 하므로 continue 하지 않음!
            if (clearTileNames.Contains(tileName))
            {
                if (StageManager.Instance != null) StageManager.Instance.RegisterClearTile(worldPos);
            }

            // 3. [공통] 프리팹 생성 (벽, 바닥, 클리어 타일 등 모두 포함)
            // - 딕셔너리에 등록된 이름이라면 무조건 생성합니다.
            if (prefabDict.TryGetValue(tileName, out GameObject prefab))
            {
                var go = Instantiate(prefab, worldPos, Quaternion.identity, spawnParent);
                
                // 필요하다면 주석 해제하여 사용
                // ApplyOrderInLayer(go, spawnOrderInLayer); 
            }
        }
        
        // 생성이 끝났으므로 원본 타일맵은 숨김 처리
        generatorTilemap.gameObject.SetActive(false);
    }

    private void ApplyOrderInLayer(GameObject go, int order)
    {
        if (go == null) return;
        var renderers = go.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        foreach (var r in renderers) r.sortingOrder = order;
    }
}