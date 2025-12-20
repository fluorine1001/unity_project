using UnityEngine;

namespace LaserSystem
{
    public interface ILaserInteractable
    {
        /// <summary>
        /// 레이저가 이 셀에 도달했을 때 호출됨
        /// </summary>
        /// <param name="ray">들어온 레이저 상태</param>
        /// <param name="hitCell">충돌한 셀 좌표</param>
        /// <returns>레이저 처리 결과</returns>
        LaserHitResult OnLaserHit(LaserRay ray, Vector2Int hitCell);
    }
}
