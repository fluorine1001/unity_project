using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class PaletteItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Composite Icon")]
    public RectTransform iconRoot;
    public Image cellTemplate;

    [Header("Kind Sprites")]
    public Sprite speedSprite;
    public Sprite deSpeedSprite;

    [Header("Count")]
    public TMP_Text countText;

    [Header("UI Settings")]
    public float uiCellSize = 30f; 

    // 내부 데이터
    private TileDefinition myDef;
    private int myCount;
    private CanvasGroup canvasGroup;
    private readonly List<Image> spawned = new();

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Bind(TileDefinition def, int count)
    {
        myDef = def;
        myCount = count;
        if (countText) countText.text = count.ToString();
        BuildCompositeIcon(def);
    }

    // =========================================================
    // 🖱️ 드래그 이벤트 (매니저에게 전달)
    // =========================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (myDef == null || myCount <= 0) return;

        // 매니저에게 드래그 시작 알림
        if (TilePlacementManager.Instance != null)
        {
            TilePlacementManager.Instance.StartDrag(myDef, this, eventData.position);
        }

        // UI 반투명 처리
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (myDef == null || myCount <= 0) return;

        // 매니저에게 현재 마우스 위치 전달
        if (TilePlacementManager.Instance != null)
        {
            TilePlacementManager.Instance.UpdateDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (myDef == null || myCount <= 0) return;

        // 매니저에게 드래그 종료 알림
        if (TilePlacementManager.Instance != null)
        {
            TilePlacementManager.Instance.EndDrag(eventData.position);
        }

        // UI 원상복구
        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = true;
    }

    // =========================================================
    // 🖼️ 아이콘 생성 (기존 로직 유지)
    // =========================================================
    private Sprite KindToSprite(TileKind kind) => kind == TileKind.Speed ? speedSprite : deSpeedSprite;

    private void BuildCompositeIcon(TileDefinition def)
    {
        ClearCells();
        if (def == null || def.cells == null) return;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var c in def.cells)
        {
            if (c.offset.x < minX) minX = c.offset.x; if (c.offset.x > maxX) maxX = c.offset.x;
            if (c.offset.y < minY) minY = c.offset.y; if (c.offset.y > maxY) maxY = c.offset.y;
        }
        int w = (maxX - minX) + 1;
        int h = (maxY - minY) + 1;

        if (iconRoot) iconRoot.sizeDelta = new Vector2(uiCellSize * w, uiCellSize * h);
        float cx = (minX + maxX) * 0.5f;
        float cy = (minY + maxY) * 0.5f;

        foreach (var c in def.cells)
        {
            Sprite spr = KindToSprite(c.kind);
            if (spr == null) continue;
            var img = Instantiate(cellTemplate, iconRoot);
            img.gameObject.SetActive(true);
            img.sprite = spr;           
            img.preserveAspect = true;
            var rt = (RectTransform)img.transform;
            rt.sizeDelta = new Vector2(uiCellSize, uiCellSize);
            rt.anchoredPosition = new Vector2((c.offset.x - cx) * uiCellSize, (c.offset.y - cy) * uiCellSize);
            spawned.Add(img);
        }
    }

    private void ClearCells()
    {
        foreach (var img in spawned) if (img) Destroy(img.gameObject);
        spawned.Clear();
    }
}