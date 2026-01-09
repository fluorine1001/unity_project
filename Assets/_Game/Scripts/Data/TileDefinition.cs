using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Tile Definition")]
public class TileDefinition : ScriptableObject
{
    public string id;

    [Serializable]
    public class Cell
    {
        public Vector2Int offset; // (0,0), (1,0) ...
        public TileKind kind;     // Speed / DeSpeed
    }

    public List<Cell> cells = new();
}
