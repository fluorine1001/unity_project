using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
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
    [SerializeField] private Animator animator;     // 모션 전환용 (비우면 자동 탐색)

    // 내부 상태
    private Vector2 movementInput;
    private Rigidbody2D rb;
    private readonly List<RaycastHit2D> castColisitions = new List<RaycastHit2D>();
    private bool isMoving = false;
    private bool prevInputWasZero = true;        // "누름 에지" 검출용

    // 바라보는 방향/마지막 이동 방향
    private Vector2 lastMoveDir = Vector2.down;  // 시작 기본 바라봄(아래)

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // 다른 스크립트가 초기 바라보는 방향을 참조할 수 있도록 기본값 보장
        lastMoveDir = lastMoveDir == Vector2.zero ? Vector2.down : lastMoveDir;

        // Grid가 있으면 cellSize 자동 설정
        if (useGridCellSize && grid != null)
            cellSize = grid.cellSize;

        // 시작 위치를 격자에 스냅
        SnapToGrid();

        // 초기 애니메이터 상태 세팅
        ApplyLook(lastMoveDir, isMoving: false);
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
                // 이동 시작 전에 시선/모션 갱신
                BeginMoveLook(dir);
                StartCoroutine(MoveOneCell(dir));
            }
        }

        // 다음 에지 검출을 위한 기록
        prevInputWasZero = (movementInput == Vector2.zero);
    }

    IEnumerator MoveOneCell(Vector2 dir)
    {
        isMoving = true;

        Vector2 step = new Vector2(dir.x * cellSize.x, dir.y * cellSize.y);

        // 1) 내 앞이 막혔는지 캐스트
        castColisitions.Clear();
        float castDistance = step.magnitude + collisitionOffset;

        int hitCount = rb.Cast(dir.normalized, movementFilter, castColisitions, castDistance);

        // 2) 막혔다면, 밀 수 있는 벽이 있는지 확인해서 먼저 밀기
        if (hitCount > 0)
        {
            bool pushed = false;
            PushableWall2D pushable = null;

            // 여러 충돌 중 "가장 가까운" 대상부터 확인
            float nearest = float.MaxValue;
            foreach (var h in castColisitions)
            {
                if (h.collider == null) continue;
                float d = h.distance;
                var p = h.collider.GetComponentInParent<PushableWall2D>();
                if (p != null && d < nearest)
                {
                    nearest = d;
                    pushable = p;
                }
            }

            if (pushable != null)
            {
                // 벽을 내 이동 방향으로 한 칸 밀어보기
                pushed = pushable.TryPush(dir, moveDuration); // 같은 속도로 밀기
                if (pushed)
                {
                    // 벽이 움직임이 끝날 때까지 대기
                    yield return new WaitUntil(() => !pushable.IsMoving);

                    // 벽이 빠졌으니 다시 한 번 캐스트로 길이 비었는지 확인
                    castColisitions.Clear();
                    hitCount = rb.Cast(dir.normalized, movementFilter, castColisitions, castDistance);
                }
            }

            // 아직도 막혀 있으면 이동 포기
            if (hitCount > 0)
            {
                EndMoveLook(); // 애니메이션/시선 복구
                isMoving = false;
                yield break;
            }
        }

        // 3) 이제 길이 뚫렸으니 내가 한 칸 이동
        AudioManager.instance.PlayOneShot(FMODEvents.instance.PlayerDash, this.transform.position);
        
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

        rb.MovePosition(end);
        EndMoveLook();
        isMoving = false;

        CheckClearTile();
        CheckSpawnTile();
    }

    void CheckClearTile()
    {
        if (StageManager.Instance == null) return;

        if (StageManager.Instance.IsClearTile(transform.position))
        {
            StageManager.Instance.OnPlayerStepOnClearTile();
        }
    }

    void CheckSpawnTile()
    {
        if (StageManager.Instance == null) return;

        if (StageManager.Instance.IsSpawnTile(transform.position))
        {
            StageManager.Instance.OnPlayerStepOnSpawnTile();
        }
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

    // === 애니메이션 & 바라보는 방향 처리 ===

    // 이동 시작 시: 애니메이션을 "이동"으로, 시선 갱신
    void BeginMoveLook(Vector2 dir)
    {
        lastMoveDir = dir;                 // 마지막 이동 방향 저장
        ApplyLook(lastMoveDir, true);      // 이동 중
    }

    // 이동 종료 시: 애니메이션을 "대기"로, 마지막 방향 유지
    void EndMoveLook()
    {
        ApplyLook(lastMoveDir, false);     // 대기(Idle)지만 lastMoveDir을 바라봄
    }

    // 스프라이트 반전 + 애니메이터 파라미터 세팅
    void ApplyLook(Vector2 lookDir, bool isMoving)
    {
        // Animator 파라미터 반영
        if (animator != null)
        {
            animator.SetBool("isWalk", isMoving);
            animator.SetFloat("DirectionX", lookDir.x);
            animator.SetFloat("DirectionY", lookDir.y);
            // 필요하다면 Speed/Blend 파라미터도 추가 가능:
            // animator.SetFloat("Speed", isMoving ? 1f : 0f);
        }

        // 좌우 반전(수평 입력 있을 때만 갱신, 수직 이동 중엔 기존 반전 유지)
        if (sprite != null)
        {
            if (lookDir.x > 0.01f) sprite.flipX = false; // 오른쪽
            else if (lookDir.x < -0.01f) sprite.flipX = true; // 왼쪽
            // lookDir.x == 0 이면 flipX 유지 (수직 이동/대기에서 마지막 수평 방향 유지)
        }
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

    // 외부에서 현재 바라보는 방향을 안전하게 조회할 수 있도록 공개
    public Vector2 LastMoveDirection => lastMoveDir;
    
}

