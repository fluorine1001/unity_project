using System.Collections.Generic;

namespace LaserSystem
{
    public struct LaserHitResult
    {
        /// <summary>
        /// true면 이 지점에서 레이저 종료
        /// </summary>
        public bool terminate;

        /// <summary>
        /// 반사/분기되어 새로 생성될 레이저들
        /// </summary>
        public List<LaserRay> spawnedRays;

        public static LaserHitResult Terminate()
        {
            return new LaserHitResult
            {
                terminate = true,
                spawnedRays = null
            };
        }

        public static LaserHitResult Continue(params LaserRay[] rays)
        {
            return new LaserHitResult
            {
                terminate = false,
                spawnedRays = new List<LaserRay>(rays)
            };
        }
    }
}
