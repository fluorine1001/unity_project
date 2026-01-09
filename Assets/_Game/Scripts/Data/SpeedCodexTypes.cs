// SpeedCodexTypes.cs
using UnityEngine;
public enum SpeedTileKind
{
    None,
    SpeedUp,    // 초록
    SpeedDown   // 빨강
}


[System.Serializable]
public struct CodexCell
{
    [Tooltip("패턴 기준 (0,0)에서의 오프셋 좌표")]
    public Vector2Int offset;

    [Tooltip("가속/감속/빈칸")]
    public SpeedTileKind kind;
}
