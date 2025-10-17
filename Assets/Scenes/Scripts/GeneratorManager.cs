using UnityEngine;
using UnityEngine.Tilemaps;

public class GeneratorManager : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap generatorTilemap;

    [Header("Prefabs")]
    public GameObject boxPrefab;
    public GameObject enemyPrefab;

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
            print(tileName + " at " + worldPos);

            switch (tileName)
            {
                case "txture_wood_0":
                    Instantiate(boxPrefab, worldPos, Quaternion.identity, spawnParent);
                    break;

                case "EnemyGenerator":
                    Instantiate(enemyPrefab, worldPos, Quaternion.identity, spawnParent);
                    break;

                case "txture_spawn_0":
                    // ?�리???�???�치 ?�??(StageManager?�서 참조?????�도�?
                    StageManager.Instance.RegisterClearTile(worldPos);
                    break;

                default:
                    break;
            }
        }

        // 게임 중에??Generator ?�?�맵 ?��?
        generatorTilemap.gameObject.SetActive(false);
    }
}

