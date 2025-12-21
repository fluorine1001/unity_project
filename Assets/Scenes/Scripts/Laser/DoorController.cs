using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class DoorController : MonoBehaviour
{
    public int StageID { get; private set; } = -1;

    private BoxCollider2D col;
    private SpriteRenderer rend;
    private bool isOpen = false;

    // ✅ [추가] 외부(StageManager)에서 문이 열려있는지 확인할 수 있게 해주는 프로퍼티
    public bool IsDoorOpen => isOpen; 

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        rend = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        var generator = FindObjectOfType<GeneratorManager>();
        if (generator != null)
        {
            StageID = generator.GetStageIndexFromWorldPos(transform.position);
        }

        if (StageID != -1 && StageManager.Instance != null)
        {
            StageManager.Instance.RegisterDoor(StageID, this);
        }
        
        // 초기 상태: 닫힘 (SetDoorState 내부에서 isOpen을 체크하므로 안전함)
        SetDoorState(false);
    }

    public void SetDoorState(bool open)
    {
        // ✅ 이미 원하는 상태라면 중복 실행 방지
        if (isOpen == open) return;

        isOpen = open;

        if (col != null) col.enabled = !isOpen;
        if (rend != null) rend.enabled = !isOpen;

        Debug.Log($"<color=cyan>[Door]</color> Stage {StageID} 문 {(isOpen ? "열림" : "닫힘")}");
    }
}