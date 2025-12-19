using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaletteItemUI : MonoBehaviour
{
    [Header("Composite Icon")]
    public RectTransform iconRoot;
    public Image cellTemplate;

    [Header("Kind Sprites (set in prefab once)")]
    public Sprite speedSprite;
    public Sprite deSpeedSprite;

    [Header("Count")]
    public TMP_Text countText;

    private readonly List<Image> spawned = new();

    public void Bind(TileDefinition def, int count)

    {
        Debug.Log($"[PaletteItemUI] speed={(speedSprite ? speedSprite.name : "NULL")} de={(deSpeedSprite ? deSpeedSprite.name : "NULL")}");
        Debug.Log($"[PaletteItemUI] def={(def ? def.name : "NULL")} cells={(def?.cells==null ? -1 : def.cells.Count)}");
        
        if (countText) countText.text = count.ToString();
        BuildCompositeIcon(def);
    }

    private Sprite KindToSprite(TileKind kind)
        => kind == TileKind.Speed ? speedSprite : deSpeedSprite;

    private void BuildCompositeIcon(TileDefinition def)
    {
        ClearCells();
        if (def == null || def.cells == null || def.cells.Count == 0) return;
        if (iconRoot == null || cellTemplate == null) return;

        // 1) bounds
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var c in def.cells)
        {
            var p = c.offset;
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        int w = maxX - minX + 1;
        int h = maxY - minY + 1;

        float rootW = iconRoot.rect.width;
        float rootH = iconRoot.rect.height;
        float cellSize = 5f; // 원하는 "타일 한 칸" 픽셀 크기 (예: 32)
        iconRoot.sizeDelta = new Vector2(cellSize * w, cellSize * h);

        //float cellSize = Mathf.Min(rootW / w, rootH / h);

        float cx = (minX + maxX) * 0.5f;
        float cy = (minY + maxY) * 0.5f;

        // 2) spawn
        foreach (var c in def.cells)
        {
            Sprite spr = KindToSprite(c.kind);
            if (spr == null) continue;

            var img = Instantiate(cellTemplate, iconRoot);
            img.gameObject.SetActive(true);
            img.overrideSprite = spr;   // ✅ 이게 중요
            img.sprite = spr;           // 같이 넣어도 됨
            img.color = Color.white;    // 알파/색상 혹시 몰라 고정
            img.material = null;        // 이상한 머티리얼 방지
            img.preserveAspect = true;


            var rt = (RectTransform)img.transform;
            rt.sizeDelta = new Vector2(cellSize, cellSize);

            float ax = (c.offset.x - cx) * cellSize;
            float ay = (c.offset.y - cy) * cellSize;
            rt.anchoredPosition = new Vector2(ax, ay);

            spawned.Add(img);
        }

        cellTemplate.gameObject.SetActive(false);
    }

    private void ClearCells()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) Destroy(spawned[i].gameObject);
        spawned.Clear();

        if (cellTemplate) cellTemplate.gameObject.SetActive(false);
    }
}
