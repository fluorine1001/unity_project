using System.Collections.Generic;
using UnityEngine;

namespace LaserSystem
{
    public class LaserSystem : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("IGridQuery를 구현한 컴포넌트")]
        public MonoBehaviour gridQueryProvider;

        [Header("Rendering")]
        public LaserRenderer laserRenderer;

        private IGridQuery grid;

        // 레이저 계산 결과 (렌더링용)
        private readonly List<LaserSegment> segments = new List<LaserSegment>();

        // A1: 방문 체크 (cell, direction)
        private readonly HashSet<(Vector2Int, Direction)> visited
            = new HashSet<(Vector2Int, Direction)>();

        private void Awake()
        {
            grid = gridQueryProvider as IGridQuery;
            if (grid == null)
            {
                Debug.LogError(
                    "[LaserSystem] gridQueryProvider does not implement IGridQuery"
                );
            }
        }

        /// <summary>
        /// 외부 이벤트(플레이어 이동, 박스 이동 등)에서 호출
        /// </summary>
        public void RecalculateAllLasers(IEnumerable<LaserRay> initialRays)
        {
            segments.Clear();
            visited.Clear();

            Queue<LaserRay> queue = new Queue<LaserRay>();

            foreach (var ray in initialRays)
                queue.Enqueue(ray);

            while (queue.Count > 0)
            {
                LaserRay ray = queue.Dequeue();

                // 방문 체크 (A1)
                if (visited.Contains((ray.originCell, ray.direction)))
                    continue;

                visited.Add((ray.originCell, ray.direction));

                TraceSingleRay(ray, queue);
            }

            // 🔎 현재 단계에서는 로그로만 확인
            Debug.Log($"[LaserSystem] segments count = {segments.Count}");

            laserRenderer.Render(segments);
        }

        /// <summary>
        /// 레이저 하나를 다음 상호작용 지점까지 추적
        /// </summary>
        private void TraceSingleRay(LaserRay ray, Queue<LaserRay> queue)
        {
            Vector2Int dirVec = ray.direction.ToVector2Int();

            Vector2Int currentCell = ray.originCell;
            Vector2 worldStart = grid.CellCenterWorld(currentCell);

            while (true)
            {
                Vector2Int nextCell = currentCell + dirVec;

                // 맵 밖이면 종료
                if (!grid.IsInside(nextCell))
                {
                    Vector2 worldEnd = grid.CellCenterWorld(currentCell);
                    segments.Add(new LaserSegment(worldStart, worldEnd));
                    return;
                }

                GridCellData cellData = grid.GetCell(nextCell);
                Debug.Log($"[Trace] check nextCell={nextCell} blocks={cellData.blocksLaser} responder={(cellData.laserResponder!=null)}");

                // 레이저가 막히는 경우
                if (cellData.blocksLaser)
                {
                    Vector2 worldEnd = grid.CellCenterWorld(nextCell);
                    segments.Add(new LaserSegment(worldStart, worldEnd));
                    return;
                }

                // 레이저 상호작용 타일
                if (cellData.laserResponder != null)
                {
                    Vector2 worldEnd = grid.CellCenterWorld(nextCell);
                    segments.Add(new LaserSegment(worldStart, worldEnd));

                    LaserHitResult result =
                        cellData.laserResponder.OnLaserHit(ray, nextCell);

                    if (result.terminate)
                        return;

                    if (result.spawnedRays != null)
                    {
                        foreach (var spawned in result.spawnedRays)
                            queue.Enqueue(spawned);
                    }
                    return;
                }

                // 아무것도 없으면 계속 진행
                currentCell = nextCell;
            }
        }

        /// <summary>
        /// (다음 단계에서 Renderer가 사용)
        /// </summary>
        public IReadOnlyList<LaserSegment> GetSegments()
        {
            return segments;
        }

        private void Start()
        {
            // ⚠️ 테스트용 (다음 단계에서 삭제)
            var testRays = new List<LaserRay>
            {
                new LaserRay(new Vector2Int(0, 0), Direction.Right)
            };

            RecalculateAllLasers(testRays);
        }
    }
}
