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

    private void GenerateObjectsFromTilemap()
    {
        if (generatorTilemap == null) return;

        foreach (var pos in generatorTilemap.cellBounds.allPositionsWithin)
        {
            TileBase tileBase = generatorTilemap.GetTile(pos);
            if (tileBase == null) continue;

            string tileName = tileBase.name;
            Vector3 worldPos = generatorTilemap.GetCellCenterWorld(pos);

            if (spawnTileNames.Contains(tileName))
            {
                if (StageManager.Instance != null) StageManager.Instance.RegisterSpawnTile(worldPos);
                continue;
            }

            if (clearTileNames.Contains(tileName))
            {
                if (StageManager.Instance != null) StageManager.Instance.RegisterClearTile(worldPos);

                var go = Instantiate(prefab, worldPos, Quaternion.identity, spawnParent);
                
                // [중요] 원본 코드대로 주석 처리 (Sorting Order 강제 적용 해제)
                // ApplyOrderInLayer(go, spawnOrderInLayer); 
            }
        }
        generatorTilemap.gameObject.SetActive(false);
    }

    private void ApplyOrderInLayer(GameObject go, int order)
    {
        if (go == null) return;
        var renderers = go.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        foreach (var r in renderers) r.sortingOrder = order;
    }
}