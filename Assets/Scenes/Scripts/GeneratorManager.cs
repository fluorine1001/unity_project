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

    [Header("Rendering")]
    [Tooltip("생성된 프리팹들의 SpriteRenderer Order in Layer 값")]
    [SerializeField] private int spawnOrderInLayer = -2;

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
                {
                    var go = Instantiate(boxPrefab, worldPos, Quaternion.identity, spawnParent);
                    ApplyOrderInLayer(go, spawnOrderInLayer);
                    break;
                }

                case "txture_spawn_0":
                    StageManager.Instance.RegisterClearTile(worldPos);
                    break;

                case "txture_default":
                    StageManager.Instance.RegisterSpawnTile(worldPos);
                    break;

                case "tileTemp_inspeed_0":
                {
                    var go = Instantiate(speedUpTilePrefab, worldPos, Quaternion.identity, spawnParent);
                    ApplyOrderInLayer(go, spawnOrderInLayer);
                    break;
                }

                case "tileTemp_despeed_0":
                {
                    var go = Instantiate(speedDownTilePrefab, worldPos, Quaternion.identity, spawnParent);
                    ApplyOrderInLayer(go, spawnOrderInLayer);
                    break;
                }

                default:
                    break;
            }
        }

        // 게임 중에는 Generator 타일맵 숨김
        generatorTilemap.gameObject.SetActive(false);
    }

    /// <summary>
    /// 전달한 GameObject 및 모든 자식의 SpriteRenderer.order를 지정값으로 세팅
    /// </summary>
    private void ApplyOrderInLayer(GameObject go, int order)
    {
        if (go == null) return;

        // 본인 포함 전체 계층
        var renderers = go.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        foreach (var r in renderers)
        {
            r.sortingOrder = order;
        }
    }
}
