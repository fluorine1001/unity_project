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

            Vector3 worldPos = generatorTilemap.CellToWorld(pos) + generatorTilemap.tileAnchor;
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
                    // 클리어 타일 위치 저장 (StageManager에서 참조할 수 있도록)
                    StageManager.Instance.RegisterClearTile(worldPos);
                    break;

                default:
                    break;
            }
        }

        // 게임 중에는 Generator 타일맵 숨김
        generatorTilemap.gameObject.SetActive(false);
    }
}
