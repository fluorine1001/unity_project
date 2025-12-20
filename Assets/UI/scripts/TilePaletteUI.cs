using System.Collections.Generic;
using UnityEngine;

public class TilePaletteUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentRoot;      // Scroll View 안의 Content (Vertical Layout Group)
    public PaletteItemUI itemPrefab;   // 생성할 개별 아이템 프리팹 (PaletteItemUI 스크립트 포함)

    // 생성된 아이템들을 관리하기 위한 리스트
    private readonly List<GameObject> spawned = new();

    /// <summary>
    /// 외부(StageManager 등)에서 호출하여 타일 목록을 UI에 표시합니다.
    /// </summary>
    /// <param name="loadout">표시할 타일 정보들이 담긴 데이터</param>
    public void Build(StageLoadout loadout)
    {
        // 1. 기존에 떠있던 아이템들을 모두 지웁니다.
        Clear();

        if (loadout == null || loadout.entries == null) return;
        if (itemPrefab == null || contentRoot == null)
        {
            Debug.LogWarning("[TilePaletteUI] itemPrefab 혹은 contentRoot가 연결되지 않았습니다.");
            return;
        }

        // 2. 로드아웃에 있는 타일 정보대로 아이템 생성
        // 🔥 [수정] 인덱스를 전달하기 위해 for문 사용
        for (int i = 0; i < loadout.entries.Count; i++)
        {
            var e = loadout.entries[i];

            // 타일 데이터가 없거나 개수가 0개 이하라면 표시하지 않음
            if (e.tile == null || e.count <= 0) continue;

            // 프리팹 생성
            PaletteItemUI item = Instantiate(itemPrefab, contentRoot);
            
            // 🔥 [수정] Bind 호출 시 인덱스(i)를 함께 전달
            item.Bind(e.tile, e.count, i);

            // 리스트에 추가하여 추후 관리
            spawned.Add(item.gameObject);
        }
    }

    /// <summary>
    /// 현재 표시된 모든 타일 UI를 제거합니다.
    /// </summary>
    public void Clear()
    {
        foreach (var go in spawned)
        {
            if (go != null) Destroy(go);
        }
        spawned.Clear();
    }
}