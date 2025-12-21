using System.Collections.Generic;
using UnityEngine;

namespace LaserSystem
{
    public class LaserRenderer : MonoBehaviour
    {
        [Header("Visual Settings")]
        public float laserWidth = 0.15f;
        public Material laserMaterial;

        // LineRenderer 풀
        private readonly List<LineRenderer> linePool = new List<LineRenderer>();

        /// <summary>
        /// LaserSystem이 계산한 세그먼트를 화면에 그림
        /// </summary>
        public void Render(IReadOnlyList<LaserSegment> segments)
        {
            EnsurePoolSize(segments.Count);

            for (int i = 0; i < linePool.Count; i++)
            {
                if (i >= segments.Count)
                {
                    linePool[i].gameObject.SetActive(false);
                    continue;
                }

                var seg = segments[i];
                var lr = linePool[i];

                lr.gameObject.SetActive(true);
                lr.positionCount = 2;
                lr.SetPosition(0, seg.worldStart);
                lr.SetPosition(1, seg.worldEnd);
            }
        }

        private void EnsurePoolSize(int required)
        {
            while (linePool.Count < required)
            {
                var go = new GameObject($"LaserLine_{linePool.Count}");
                go.transform.SetParent(transform);

                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.startWidth = laserWidth;
                lr.endWidth = laserWidth;
                lr.material = laserMaterial;
                lr.numCapVertices = 2;

                linePool.Add(lr);
            }
        }
    }
}
