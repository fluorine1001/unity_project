using UnityEngine;

public class DynamicYDepthSort : MonoBehaviour
{
    private const float Y_AXIS_MULTIPLIER = 50f; 
    
    [Tooltip("Order 제한(3만) 내에서 최대한 높은 값으로 설정하여 음수를 방지하는 기준 오프셋.")]
    public int baseSortingOrder = 29999; 

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
        int calculatedOrder = (int)(-transform.position.y * Y_AXIS_MULTIPLIER) + baseSortingOrder;

        int finalOrder = Mathf.Max(calculatedOrder, 1); 

        foreach (var r in renderers)
        {
            r.sortingOrder = finalOrder;
        }
    }
}