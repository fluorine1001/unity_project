using UnityEngine;

namespace LaserSystem
{
    /// <summary>
    /// 실제 화면에 그릴 레이저 선분
    /// (렌더링 전용 데이터)
    /// </summary>
    public struct LaserSegment
    {
        public Vector2 worldStart;
        public Vector2 worldEnd;

        public LaserSegment(Vector2 worldStart, Vector2 worldEnd)
        {
            this.worldStart = worldStart;
            this.worldEnd = worldEnd;
        }
    }
}
