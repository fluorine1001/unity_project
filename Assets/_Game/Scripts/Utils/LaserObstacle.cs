using UnityEngine;
using System.Collections.Generic;

// 플레이어, Wall, 일반 PushableBox에 이 컴포넌트를 추가하세요.
public class LaserObstacle : MonoBehaviour, ILaserInteractable
{
    [Header("Settings")]
    [Tooltip("체크하면 레이저가 그냥 통과합니다. (예: 유리벽)")]
    public bool isTransparent = false;

    public LaserAction OnLaserHit(Vector2 inDir, out List<Vector2> outDirs)
    {
        outDirs = null; // 반사하지 않으므로 null

        if (isTransparent)
        {
            return LaserAction.Pass; // 통과
        }
        else
        {
            return LaserAction.Block; // 막힘
        }
    }
}