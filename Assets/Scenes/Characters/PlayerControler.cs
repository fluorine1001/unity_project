using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Move (one step per input)")]
    [Tooltip("한 칸 이동에 걸리는 시간(초)")]
    public float moveDuration = 0.08f;

    [Header("Grid / Cell Size")]
    public Grid grid;                            // 씬에 Grid가 있으면 할당
    public bool useGridCellSize = true;          // Grid의 cellSize 자동 사용
    public Vector2 cellSize = new Vector2(0.16f, 0.16f); // Grid 없을 때 직접 지정

    [Header("Collision")]
    [Tooltip("충돌 감지 offset")]
    public float collisitionOffset = 0.05f;      // (기존 변수명 유지)
    [Tooltip("충돌에서 제외/포함할 레이어 설정")]
    public ContactFilter2D movementFilter;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer sprite; // 좌우 반전용 (비우면 자동 탐색)

    // 내부 상태
    private Vector2 movementInput;
    private Rigidbody2D rb;
    private readonly List<RaycastHit2D> castColisitions = new List<RaycastHit2D>();
    private bool isMoving = false;
    private bool prevInputWasZero = true;        // "누름 에지" 검출용

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null)
            sprite = GetComponentInChildren<SpriteRenderer>();

        // Grid가 있으면 cellSize 자동 설정
        if (useGridCellSize && grid != null)
            cellSize = grid.cellSize;

        // 시작 위치를 격자에 스냅
        SnapToGrid();
    }

    // 연속 이동은 사용하지 않음 (고의로 비워둠)
    void FixedUpdate() { }

    // 새 Input System - Move 액션
    void OnMove(InputValue movementValue)
    {
        movementInput = movementValue.Get<Vector2>();

        // "0 → 비0"으로 바뀌는 순간만 한 칸 이동 (키 다운 에지)
        if (!isMoving && movementInput != Vector2.zero && prevInputWasZero)
        {
            Vector2 dir = QuantizeToCardinal(movementInput); // 대각 → 가로나 세로
            if (dir != Vector2.zero)
            {
                UpdateFacing(dir); // 좌우 반전 갱신
                StartCoroutine(MoveOneCell(dir));
            }
        }

        // 다음 에지 검출을 위한 기록
        prevInputWasZero = (movementInput == Vector2.zero);
    }

    IEnumerator MoveOneCell(Vector2 dir)
    {
        isMoving = true;

        // 정확히 한 칸
        Vector2 step = new Vector2(dir.x * cellSize.x, dir.y * cellSize.y);

        // 충돌 체크: 셀 거리 + 오프셋만큼 캐스트
        castColisitions.Clear();
        float castDistance = step.magnitude + collisitionOffset;

        int hitCount = rb.Cast(
            dir.normalized,          // 이동 방향(정규화)
            movementFilter,          // 필터
            castColisitions,         // 결과 저장
            castDistance             // 거리
        );

        if (hitCount > 0)
        {
            // 막혀 있으면 이동하지 않음
            isMoving = false;
            yield break;
        }

        // 부드럽게 보간 이동 (물리 타이밍에 맞춤)
        Vector2 start = rb.position;
        Vector2 end = start + step;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, moveDuration);

        while (t < 1f)
        {
            t += Time.fixedDeltaTime / dur;
            float s = Mathf.SmoothStep(0f, 1f, t);
            rb.MovePosition(Vector2.Lerp(start, end, s));
            yield return new WaitForFixedUpdate();
        }

        // 정확히 스냅
        rb.MovePosition(end);
        isMoving = false;
    }

    // 대각 입력이 들어와도 가로나 세로 중 더 큰 축으로만 1칸 이동
    Vector2 QuantizeToCardinal(Vector2 v)
    {
        if (Mathf.Approximately(v.x, 0f) && Mathf.Approximately(v.y, 0f))
            return Vector2.zero;

        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            return new Vector2(Mathf.Sign(v.x), 0f);
        else
            return new Vector2(0f, Mathf.Sign(v.y));
    }

    // 좌우 이동 시 스프라이트 반전
    void UpdateFacing(Vector2 dir)
    {
        if (sprite == null) return;

        if (dir.x > 0.01f)       sprite.flipX = false; // 오른쪽
        else if (dir.x < -0.01f) sprite.flipX = true;  // 왼쪽
        // 수직 이동만 있을 땐 유지
    }

    // 시작/배치 시 그리드 중앙에 정렬
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
