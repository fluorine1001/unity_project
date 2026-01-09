// SpeedCodexEntry.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Speed Codex Entry")]
public class SpeedCodexEntry : ScriptableObject
{
    [Header("ID 또는 이름 (디버그용)")]
    public string codexId;

    [Header("이 패턴에 포함된 칸들")]
    public CodexCell[] cells;

    [Header("이 패턴의 논리 크기 (UI 정렬용)")]
    public Vector2Int size = new Vector2Int(5, 2); // 예: 폭 5, 높이 2
}
