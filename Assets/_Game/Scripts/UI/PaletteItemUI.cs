using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))] 
public class PaletteItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, ICanvasRaycastFilter
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

    public int LoadoutIndex { get; private set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        
        Image myImage = GetComponent<Image>();
        if (myImage != null)
        {
            myImage.raycastTarget = true;
        }
    }

    public void Bind(TileDefinition def, int count, int index)
    {
        myDef = def;
        myCount = count;
        LoadoutIndex = index; 

        if (countText) countText.text = count.ToString();
        BuildCompositeIcon(def);
    }

    // =========================================================
    // 🎯 정밀 클릭 판정 (Raycast Filter)
    // =========================================================
    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        // 비활성화 상태면 클릭 불가
        if (!isActiveAndEnabled) return false;

        foreach (var img in spawned)
        {
            if (img == null || !img.gameObject.activeInHierarchy) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                img.rectTransform, 
                sp, 
                eventCamera))
            {
                return true; 
            }
        }
        return false; 
    }

    // =========================================================
    // 🖱️ 드래그 이벤트
    // =========================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (myDef == null || myCount <= 0) return;

        if (TilePlacementManager.Instance != null)
        {
            TilePlacementManager.Instance.StartDrag(myDef, this, eventData.position);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            // 🔥 [수정] blocksRaycasts = false 삭제
            // 스크롤 뷰가 이벤트를 뺏어가는 것을 방지하기 위해 true 유지 (기본값)
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (myDef == null || myCount <= 0) return;
        
        // 드래그가 시작되지 않았으면(alpha가 1.0f 근처면) 무시
        if (canvasGroup != null && canvasGroup.alpha > 0.9f) return;

        if (TilePlacementManager.Instance != null)
        {
            TilePlacementManager.Instance.UpdateDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (myDef == null || myCount <= 0) return;
        
        // 드래그가 유효하지 않았으면 무시
        if (canvasGroup != null && canvasGroup.alpha > 0.9f) return;

        if (TilePlacementManager.Instance != null)
        {
            TilePlacementManager.Instance.EndDrag(eventData.position);
        }

        // UI 원상복구 (객체가 파괴되지 않았다면)
        if (this != null && canvasGroup != null)
        {
            canvasGroup.alpha = 1.0f;
        }
    }

    // =========================================================
    // 🖼️ 아이콘 생성
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
            img.raycastTarget = false; 

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