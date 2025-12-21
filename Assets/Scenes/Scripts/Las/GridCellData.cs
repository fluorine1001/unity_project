namespace LaserSystem
{
    /// <summary>
    /// 레이저 관점에서의 셀 정보
    /// (물리/레이어와 완전히 분리됨)
    /// </summary>
    public class GridCellData
    {
        /// <summary>
        /// true면 레이저는 이 셀에서 막힘(소멸)
        /// 예: 벽, 박스, 플레이어
        /// </summary>
        public bool blocksLaser;

        /// <summary>
        /// 레이저가 닿았을 때 반응하는 객체
        /// 예: 거울, 타겟, 논타겟, 먹지
        /// </summary>
        public ILaserInteractable laserResponder;

        public GridCellData(bool blocksLaser = false, ILaserInteractable responder = null)
        {
            this.blocksLaser = blocksLaser;
            this.laserResponder = responder;
        }
    }
}
