using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Stage Loadout")]
public class StageLoadout : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public TileDefinition tile;
        public int count = 1;
    }

    public List<Entry> entries = new();
}
