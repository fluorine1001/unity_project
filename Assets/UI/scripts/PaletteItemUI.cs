using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// [중요] 부모 객체에 투명한 Image 컴포넌트가 있어야 Raycast를 받을 수 있습니다.
// (색상 알파값을 0으로 하되, Raycast Target은 체크되어 있어야 합니다.)
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

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        
        // [설정] 부모(나 자신)의 이미지는 투명해야 하지만 RaycastTarget은 켜져 있어야 함
        // 그래야 ICanvasRaycastFilter가 동작함
        Image myImage = GetComponent<Image>();
        if (myImage != null)
        {
            myImage.raycastTarget = true;
        }
    }

    public void Bind(TileDefinition def, int count)
    {
        myDef = def;
        myCount = count;
        if (countText) countText.text = count.ToString();
        BuildCompositeIcon(def);
    }

    // =========================================================
    // 🎯 [핵심 해결책] 정밀 클릭 판정 (Raycast Filter)
    // =========================================================
    // Unity UI 시스템이 "이 물체가 클릭 가능한가?"를 물어볼 때 호출됩니다.
    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        // 생성된 타일 아이콘들 중 하나라도 마우스 위치에 있다면 true (클릭 성공)
        // 없다면 false를 반환하여 클릭을 투과시킴 -> 뒤에 있는 다른 타일이 클릭됨!
        foreach (var img in spawned)
        {
            if (img == null || !img.gameObject.activeInHierarchy) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                img.rectTransform, 
                sp, 
                eventCamera))
            {
                return true; // 아이콘 위다! 클릭 인정.
            }
        }

        return false; // 빈 공간이다! 뒤로 통과시켜라.
    }

    // =========================================================
    // 🖱️ 드래그 이벤트 (매니저에게 전달)
    // =========================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 이미 IsRaycastLocationValid를 통과했으므로 여기선 별도 검사가 필요 없음
        if (myDef == null || myCount <= 0) return;

        if (TilePlacementManager.Instance != null)
        {
            TilePlacementManager.Instance.StartDrag(myDef, this, eventData.position);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (myDef == null || myCount <= 0) return;
        
        // 드래그 시작 안됐으면 무시
        if (canvasGroup != null && canvasGroup.alpha > 0.9f) return;

        if (TilePlacementManager.Instance != null)
        {
            TilePlacementManager.Instance.UpdateDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (myDef == null || myCount <= 0) return;

        if (canvasGroup != null && canvasGroup.alpha > 0.9f) return;

        if (TilePlacementManager.Instance != null)
        {
            TilePlacementManager.Instance.EndDrag(eventData.position);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1.0f;
            canvasGroup.blocksRaycasts = true;
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

        // 좌표 계산
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

            // [중요] 자식 이미지는 RaycastTarget을 끕니다.
            // 부모(PaletteItemUI)가 좌표 계산으로 직접 처리하므로 중복 방지.
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