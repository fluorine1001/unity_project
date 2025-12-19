using System.Collections.Generic;
using UnityEngine;

public class TilePaletteUI : MonoBehaviour
{
    public Transform contentRoot;      // Vertical Layout Group 아래 Content
    public PaletteItemUI itemPrefab;   // 아이템 프리팹

    readonly List<GameObject> spawned = new();

    public void Build(StageLoadout loadout)
    {
        Clear();
        if (loadout == null) return;

        foreach (var e in loadout.entries)
        {
            if (e.tile == null || e.count <= 0) continue;
            var item = Instantiate(itemPrefab, contentRoot);
            item.Bind(e.tile, e.count);
            spawned.Add(item.gameObject);
        }
    }

    void Clear()
    {
        foreach (var go in spawned)
            if (go) Destroy(go);
        spawned.Clear();
    }
}
