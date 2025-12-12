using UnityEngine;

public enum CellType { Speed, DeSpeed }

[System.Serializable]
public struct BlockCell
{
    public Vector2Int offset;
    public CellType type;
}

[CreateAssetMenu(fileName = "BlockDefinition", menuName = "Blocks/BlockDefinition")]
public class BlockDefinition : ScriptableObject
{
    public BlockCell[] cells;
}