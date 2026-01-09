using UnityEngine;
using System.Collections.Generic;

// 레이저 발사 방향
public enum LaserDirection { Up, Down, Left, Right }

// 거울의 종류
public enum MirrorType
{
    Triangle,   // 90도 굴절 (삼각거울)
    Square,     // 180도 반사 (사각거울)
    Half        // 투과 + 90도 굴절 (반거울)
}

// 📐 거울 방향 정의 (빛이 나가는 방향 기준)
// 예: UpLeft (◢) = 빛을 위(Up)와 왼쪽(Left)으로 보냄
public enum TriangleOrientation
{
    UpLeft,    // ◢  (입력: ↓,→ / 출력: ←,↑)
    UpRight,   // ◣  (입력: ↓,← / 출력: →,↑)
    RightDown, // ◤  (입력: ↑,← / 출력: →,↓)
    DownLeft   // ◥  (입력: ↑,→ / 출력: ←,↓)
}

// 🚦 레이저 반응 타입
public enum LaserAction
{
    Block,      // 막힘 (벽, 플레이어, 거울 뒷면)
    Pass,       // 통과 (투명 벽, 바닥 트리거)
    Reflect     // 반사/분산 (거울 앞면)
}

// 🏷️ 레이저 상호작용 명찰 (인터페이스)
public interface ILaserInteractable
{
    /// <summary>
    /// 레이저가 맞았을 때 반응과 나갈 방향을 반환
    /// </summary>
    LaserAction OnLaserHit(Vector2 inDir, out List<Vector2> outDirs);
}