using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveDuration = 0.08f;          // 한 칸 이동에 걸리는 시간(초)

    [Header("Grid / Cell Size")]
    public Grid grid;                            // 씬에 Grid가 있으면 할당
    public bool useGridCellSize = true;          // Grid의 cellSize 자동 사용 여부
    public Vector2 cellSize = new Vector2(0.16f, 0.16f); // Grid 없을 때 직접 지정

    [Header("Collision")]
    public float collisitionOffset = 0.05f;      // 충돌 감지 offset
    public ContactFilter2D movementFilter;       // 충돌에서 제외/포함 레이어 설정

    // 기존 변수들
    Vector2 movementInput;
    Rigidbody2D rb;
    List<RaycastHit2D> castColisitions = new List<RaycastHit2D>();

    // 추가 상태
    bool isMoving = false;
    bool prevInputWasZero = true;                // "누름 에지" 검출용

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Grid가 있으면 cellSize 자동 설정
        if (useGridCellSize && grid != null)
        {
            cellSize = grid.cellSize;
        }

        // 시작 위치를 그리드에 스냅(선택)
        SnapToGrid();
    }

    // 연속 물리 이동 로직은 제거하고(또는 미사용),
    // 한 번 입력 → 코루틴 이동으로 전환
    void FixedUpdate()
    {
        // 아무 것도 하지 않음 (의도적으로 비움)
    }

    // 새 Input System 메시지 핸들러
    void OnMove(InputValue movementValue)
    {
        movementInput = movementValue.Get<Vector2>();

        // "0 → 비영(非0)"으로 바뀌는 순간만 한 칸 이동 시작 (눌렀을 때 한 번)
        if (!isMoving && movementInput != Vector2.zero && prevInputWasZero)
        {
            Vector2 dir = QuantizeToCardinal(movementInput); // 대각 입력을 가로나 세로로 정규화
            if (dir != Vector2.zero)
            {
                StartCoroutine(MoveOneCell(dir));
            }
        }

        // 다음 에지 검출을 위해 현재 입력이 0인지 기록
        prevInputWasZero = (movementInput == Vector2.zero);
    }

    IEnumerator MoveOneCell(Vector2 dir)
    {
        isMoving = true;

        // 이동 거리(정확히 한 칸)
        Vector2 step = new Vector2(
            dir.x * cellSize.x,
            dir.y * cellSize.y
        );

        // 충돌 체크: 이동 방향으로 셀 거리 + 오프셋 만큼 캐스트
        castColisitions.Clear();
        float castDistance = step.magnitude + collisitionOffset;

        // Rigidbody2D.Cast는 방향 벡터를 정규화해서 쓰는 것이 안전
        int count = rb.Cast(
            dir.normalized,
            movementFilter,
            castColisitions,
            castDistance
        );

        if (count > 0)
        {
            // 막혀 있으면 이동 안 함
            isMoving = false;
            yield break;
        }

        // 부드럽게 보간 이동
        Vector2 start = rb.position;
        Vector2 end = start + step;

        float t = 0f;
        float dur = Mathf.Max(0.0001f, moveDuration);

        // 고정 업데이트 타이밍에 맞춰 이동(물리 일관성)
        while (t < 1f)
        {
            t += Time.fixedDeltaTime / dur;
            Vector2 pos = Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            rb.MovePosition(pos);
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(end); // 최종 스냅
        isMoving = false;
    }

    // 대각 입력이 들어와도 가로나 세로 중 큰 축으로만 1칸 이동
    Vector2 QuantizeToCardinal(Vector2 v)
    {
        if (Mathf.Approximately(v.x, 0f) && Mathf.Approximately(v.y, 0f))
            return Vector2.zero;

        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            return new Vector2(Mathf.Sign(v.x), 0f);
        else
            return new Vector2(0f, Mathf.Sign(v.y));
    }

    void SnapToGrid()
    {
        if (grid != null)
        {
            Vector3Int cell = grid.WorldToCell(transform.position);
            Vector3 center = grid.GetCellCenterWorld(cell);
            rb.position = new Vector2(center.x, center.y);
        }
        else
        {
            Vector3 p = transform.position;
            float x = Mathf.Round(p.x / cellSize.x) * cellSize.x;
            float y = Mathf.Round(p.y / cellSize.y) * cellSize.y;
            rb.position = new Vector2(x, y);
        }
    }
}
