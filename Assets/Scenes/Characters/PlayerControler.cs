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
    public Grid grid;                            
    public bool useGridCellSize = true;          
    public Vector2 cellSize = new Vector2(0.16f, 0.16f); 

    [Header("Collision")]
    [Tooltip("충돌 감지 offset")]
    public float collisitionOffset = 0.05f;      
    [Tooltip("충돌에서 제외/포함할 레이어 설정")]
    public ContactFilter2D movementFilter;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer sprite; 
    [SerializeField] private Animator animator;     

    // 내부 상태
    private Vector2 movementInput;
    private Rigidbody2D rb;
    private readonly List<RaycastHit2D> castColisitions = new List<RaycastHit2D>();
    private bool isMoving = false;
    private bool prevInputWasZero = true;        

    // 바라보는 방향/마지막 이동 방향
    private Vector2 lastMoveDir = Vector2.down;  

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        lastMoveDir = lastMoveDir == Vector2.zero ? Vector2.down : lastMoveDir;

        if (useGridCellSize && grid != null)
            cellSize = grid.cellSize;

        SnapToGrid();
        ApplyLook(lastMoveDir, isMoving: false);

        // ✅ [복구] 예전 코드의 필터 설정 적용
        movementFilter.useLayerMask = true;
        movementFilter.useTriggers = true;
    }

    void OnMove(InputValue movementValue)
    {
        movementInput = movementValue.Get<Vector2>();

        if (!isMoving && movementInput != Vector2.zero && prevInputWasZero)
        {
            Vector2 dir = QuantizeToCardinal(movementInput); 
            if (dir != Vector2.zero)
            {
                BeginMoveLook(dir);
                StartCoroutine(MoveOneCell(dir));
            }
        }
        prevInputWasZero = (movementInput == Vector2.zero);
    }

    IEnumerator MoveOneCell(Vector2 dir)
    {
        isMoving = true;

        // 1. [신규 기능] 이동 시작 전, ClearTile 위에서 나가는 방향인지 체크 (스테이지 이동)
        if (StageManager.Instance != null)
        {
            StageManager.Instance.CheckStageTransitionOnExit(transform.position, dir);
        }

        // 2. [복구] 예전 코드의 강력한 충돌 감지 로직 (rb.Cast)
        Vector2 step = new Vector2(dir.x * cellSize.x, dir.y * cellSize.y);
        
        castColisitions.Clear();
        // 이동 거리 + 오프셋만큼 레이를 쏨
        float castDistance = step.magnitude + collisitionOffset;
        
        int hitCount = rb.Cast(dir.normalized, movementFilter, castColisitions, castDistance);

        if (hitCount > 0)
        {
            // 벽에 막힘 -> 이동 취소
            EndMoveLook(); 
            isMoving = false;
            yield break;
        }

        // 3. 이동 실행 (길이 뚫렸을 때만)
        if (AudioManager.instance != null && FMODEvents.instance != null)
            AudioManager.instance.PlayOneShot(FMODEvents.instance.PlayerDash, transform.position);
        
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

        // 4. 도착 후 체크 (SpawnTile 등)
        CheckSpawnTile();
    }

    void CheckSpawnTile()
    {
        if (StageManager.Instance == null) return;
        if (StageManager.Instance.IsSpawnTile(transform.position))
        {
            StageManager.Instance.OnPlayerStepOnSpawnTile();
        }
    }

    Vector2 QuantizeToCardinal(Vector2 v)
    {
        if (Mathf.Approximately(v.x, 0f) && Mathf.Approximately(v.y, 0f)) return Vector2.zero;
        return (Mathf.Abs(v.x) >= Mathf.Abs(v.y)) ? new Vector2(Mathf.Sign(v.x), 0f) : new Vector2(0f, Mathf.Sign(v.y));
    }
    
    void BeginMoveLook(Vector2 dir)
    {
        lastMoveDir = dir;
        ApplyLook(lastMoveDir, true);
    }
    
    void EndMoveLook()
    {
        ApplyLook(lastMoveDir, false);
    }
    
    void ApplyLook(Vector2 lookDir, bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool("isWalk", isMoving);
            animator.SetFloat("DirectionX", lookDir.x);
            animator.SetFloat("DirectionY", lookDir.y);
        }

        if (sprite != null)
        {
            if (lookDir.x > 0.01f) sprite.flipX = false; 
            else if (lookDir.x < -0.01f) sprite.flipX = true; 
        }
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

    public Vector2 LastMoveDirection => lastMoveDir;
}