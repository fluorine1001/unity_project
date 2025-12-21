using System.Collections.Generic;
using UnityEngine;

namespace LaserSystem
{
    /// <summary>
    /// 레이저용 Grid 조회 임시 구현
    /// (Prefab Grid 기준, 이후 교체/확장 가능)
    /// </summary>

    public class GridQueryImpl : MonoBehaviour, IGridQuery
    {
        [Header("Grid Settings")]
        public Vector2Int gridSize = new Vector2Int(20, 20);
        public float cellSize = 1f;

        // 셀 좌표 → 레이저 관점 데이터
        private Dictionary<Vector2Int, GridCellData> grid
            = new Dictionary<Vector2Int, GridCellData>();

        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 &&
                   cell.x < gridSize.x && cell.y < gridSize.y;
        }

        public GridCellData GetCell(Vector2Int cell)
        {
            if (!grid.TryGetValue(cell, out var data))
            {
                // 기본값: 아무것도 없는 빈 셀
                data = new GridCellData();
                grid[cell] = data;
            }
            return data;
        }

        public Vector2 CellCenterWorld(Vector2Int cell)
        {
            return new Vector2(
                (cell.x + 0.5f) * cellSize,
                (cell.y + 0.5f) * cellSize
            );
        }

        /* ---------- 등록용 API (다음 단계에서 사용) ---------- */

        public void SetBlocking(Vector2Int cell, bool blocks)
        {
            GetCell(cell).blocksLaser = blocks;
        }

        public void SetResponder(Vector2Int cell, ILaserInteractable responder)
        {
            GetCell(cell).laserResponder = responder;
        }

        public void ClearCell(Vector2Int cell)
        {
            grid.Remove(cell);
        }

        private void Start()
        {
            var c = new Vector2Int(5, 0);
            SetBlocking(c, true);
            Debug.Log($"[GridTest] {c} blocksLaser={GetCell(c).blocksLaser}");
        }
    }
}
