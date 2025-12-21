using UnityEngine;

namespace LaserSystem
{
    /// <summary>
    /// 논리적 레이저 한 줄의 상태
    /// (시간, 속도 개념 없음)
    /// </summary>
    public struct LaserRay
    {
        public Vector2Int originCell;
        public Direction direction;

        public LaserRay(Vector2Int originCell, Direction direction)
        {
            this.originCell = originCell;
            this.direction = direction;
        }

        public override string ToString()
        {
            return $"LaserRay(cell={originCell}, dir={direction})";
        }
    }
}
