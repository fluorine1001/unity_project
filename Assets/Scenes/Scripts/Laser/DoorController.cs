using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class DoorController : MonoBehaviour
{
    public int StageID { get; private set; } = -1;

    private BoxCollider2D col;
    private SpriteRenderer rend;
    private bool isOpen = false;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        rend = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // ✅ GeneratorManager를 통해 이 문이 위치한 정확한 스테이지 ID를 가져옵니다.
        var generator = FindObjectOfType<GeneratorManager>();
        if (generator != null)
        {
            StageID = generator.GetStageIndexFromWorldPos(transform.position);
        }

        if (StageID != -1 && StageManager.Instance != null)
        {
            StageManager.Instance.RegisterDoor(StageID, this);
        }
        
        // 초기 상태: 닫힘
        SetDoorState(false);
    }

    public void SetDoorState(bool open)
    {
        // ✅ 이미 원하는 상태라면 중복 실행 방지
        if (isOpen == open) return;

        isOpen = open;

        // 문이 열리면(open=true) 장애물(col)과 모습(rend)을 비활성화
        if (col != null) col.enabled = !isOpen;
        if (rend != null) rend.enabled = !isOpen;

        Debug.Log($"<color=cyan>[Door]</color> Stage {StageID} 문 {(isOpen ? "열림" : "닫힘")}");
    }
}