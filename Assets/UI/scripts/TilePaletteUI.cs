using System.Collections.Generic;
using UnityEngine;

public class TilePaletteUI : MonoBehaviour
{
    public Transform contentRoot;      
    public PaletteItemUI itemPrefab;   

    readonly List<GameObject> spawned = new();

    public void Build(StageLoadout loadout)
    {
        Clear();
        if (loadout == null) return;

        // ✅ [수정] foreach -> for문으로 변경하여 인덱스 추적
        for (int i = 0; i < loadout.entries.Count; i++)
        {
            var e = loadout.entries[i];

            if (e.tile == null || e.count <= 0) continue;

            var item = Instantiate(itemPrefab, contentRoot);
            
            // ✅ [수정] 타일 정보와 함께 인덱스(i)도 전달
            item.Bind(e.tile, e.count, i);
            
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