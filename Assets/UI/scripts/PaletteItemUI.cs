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
    [Tooltip("UI에 표시될 타일 하나당 크기 (픽셀 단위). 너무 크면 줄이세요.")]
    public float uiCellSize = 6f; 

    // 내부 변수
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
    // ✨ 드래그 앤 드롭 구현부
    // =========================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (myDef == null || myCount <= 0) return;

        if (TilePlacementManager.Instance != null)
        {
            TilePlacementManager.Instance.StartDrag(myDef, this);
        }

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (myDef == null || myCount <= 0) return;

        if (TilePlacementManager.Instance != null)
        {
            TilePlacementManager.Instance.UpdateDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (myDef == null || myCount <= 0) return;

        if (TilePlacementManager.Instance != null)
        {
            TilePlacementManager.Instance.EndDrag(eventData.position);
        }

        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = true;
    }

    // =========================================================
    // 🖼️ 아이콘 생성 로직 (크기 조절 반영)
    // =========================================================

    private Sprite KindToSprite(TileKind kind)
        => kind == TileKind.Speed ? speedSprite : deSpeedSprite;

    private void BuildCompositeIcon(TileDefinition def)
    {
        ClearCells();
        if (def == null || def.cells == null || def.cells.Count == 0) return;
        if (iconRoot == null || cellTemplate == null) return;

        // 1) 범위 계산
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var c in def.cells)
        {
            if (c.offset.x < minX) minX = c.offset.x;
            if (c.offset.x > maxX) maxX = c.offset.x;
            if (c.offset.y < minY) minY = c.offset.y;
            if (c.offset.y > maxY) maxY = c.offset.y;
        }

        int w = (maxX - minX) + 1;
        int h = (maxY - minY) + 1;

        // Root 크기를 설정값(uiCellSize)에 맞춰 조절
        iconRoot.sizeDelta = new Vector2(uiCellSize * w, uiCellSize * h);

        float cx = (minX + maxX) * 0.5f;
        float cy = (minY + maxY) * 0.5f;

        // 2) 셀 이미지 생성
        foreach (var c in def.cells)
        {
            Sprite spr = KindToSprite(c.kind);
            if (spr == null) continue;

            var img = Instantiate(cellTemplate, iconRoot);
            img.gameObject.SetActive(true);
            
            img.sprite = spr;           
            img.color = Color.white;
            img.preserveAspect = true; // 비율 유지

            var rt = (RectTransform)img.transform;
            rt.sizeDelta = new Vector2(uiCellSize, uiCellSize); // 설정된 크기 적용

            // 중심점 기준으로 위치 정렬
            float ax = (c.offset.x - cx) * uiCellSize;
            float ay = (c.offset.y - cy) * uiCellSize;
            rt.anchoredPosition = new Vector2(ax, ay);

            spawned.Add(img);
        }
    }

    private void ClearCells()
    {
        foreach (var img in spawned)
        {
            if (img) Destroy(img.gameObject);
        }
        spawned.Clear();
    }
}