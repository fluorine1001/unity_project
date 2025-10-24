using UnityEngine;
using UnityEngine.Tilemaps;

public class GeneratorManager : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap generatorTilemap;

    [Header("Prefabs")]
    public GameObject boxPrefab;
    public GameObject speedUpTilePrefab;
    public GameObject speedDownTilePrefab;

    [Header("Parent for spawned objects")]
    public Transform spawnParent;

    private void Start()
    {
        GenerateObjectsFromTilemap();
    }

    private void GenerateObjectsFromTilemap()
    {
        foreach (var pos in generatorTilemap.cellBounds.allPositionsWithin)
        {
            Tile tile = generatorTilemap.GetTile(pos) as Tile;
            if (tile == null) continue;

            string tileName = tile.sprite.name;
            Vector3 worldPos = generatorTilemap.GetCellCenterWorld(pos);

            switch (tileName)
            {
                case "txture_wood_0":
                    Instantiate(boxPrefab, worldPos, Quaternion.identity, spawnParent);
                    break;

                case "txture_spawn_0":
                    StageManager.Instance.RegisterClearTile(worldPos);
                    break;

                case "txture_default":
                    StageManager.Instance.RegisterSpawnTile(worldPos);
                    break;

                case "tileTemp_inspeed_0":
                    Instantiate(speedUpTilePrefab, worldPos, Quaternion.identity, spawnParent);
                    break;

                case "tileTemp_despeed_0":
                    Instantiate(speedDownTilePrefab, worldPos, Quaternion.identity, spawnParent);
                    break;


                default:
                    break;
            }
        }

        // 게임 중에는 Generator 타일맵 숨김
        generatorTilemap.gameObject.SetActive(false);
    }
}

