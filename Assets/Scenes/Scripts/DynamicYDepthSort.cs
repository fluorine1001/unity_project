using UnityEngine;

public class DynamicYDepthSort : MonoBehaviour
{
    private const float Y_AXIS_MULTIPLIER = 50f; 
    
    [Tooltip("전체적인 베이스 오더 값")]
    public int baseSortingOrder = 29999; 

    [Tooltip("개별 물체의 높이 보정값. (거울: -100, 박스: 0, 플레이어: 0)")]
    public int sortOffset = 0; // ✅ 새로 추가된 부분

    private SpriteRenderer[] renderers;

    void Start()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        UpdateSortingOrder();
    }

    void LateUpdate()
    {
        UpdateSortingOrder();
    }

    private void UpdateSortingOrder()
    {
        // Y좌표에 따른 기본 순서 + 개별 오프셋 적용
        int calculatedOrder = (int)(-transform.position.y * Y_AXIS_MULTIPLIER) + baseSortingOrder + sortOffset;

        // 최소 1 이상 유지 (배경보다 앞)
        int finalOrder = Mathf.Max(calculatedOrder, 1); 

        foreach (var r in renderers)
        {
            r.sortingOrder = finalOrder;
        }
    }
}