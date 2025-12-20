using UnityEngine;

namespace LaserSystem
{
    public interface IGridQuery
    {
        /// <summary>
        /// 셀이 맵 내부인지
        /// </summary>
        bool IsInside(Vector2Int cell);

        /// <summary>
        /// 해당 셀의 레이저 관점 데이터
        /// </summary>
        GridCellData GetCell(Vector2Int cell);

        /// <summary>
        /// 셀 중심의 월드 좌표 (렌더링용)
        /// </summary>
        Vector2 CellCenterWorld(Vector2Int cell);
    }
}
