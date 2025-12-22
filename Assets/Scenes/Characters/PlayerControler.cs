using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Move (one step per input)")]
    public float moveDuration = 0.08f;

    [Header("Grid / Cell Size")]
    public Grid grid;                                
    public bool useGridCellSize = true;          
    public Vector2 cellSize = new Vector2(0.16f, 0.16f); 

    [Header("Collision")]
    public float collisitionOffset = 0.05f;      
    public ContactFilter2D movementFilter;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer sprite; 
    [SerializeField] private Animator animator;     

    private Vector2 movementInput;
    private Rigidbody2D rb;
    private readonly List<RaycastHit2D> castColisitions = new List<RaycastHit2D>();
    private bool isMoving = false;
    private bool prevInputWasZero = true;        
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

    // PlayerController.cs 내 MoveOneCell 수정

    // PlayerController.cs 내 MoveOneCell 코루틴 수정

    IEnumerator MoveOneCell(Vector2 dir)
    {
        isMoving = true;
        Vector2 start = rb.position;
        Vector2 step = new Vector2(dir.x * cellSize.x, dir.y * cellSize.y);
        Vector2 end = start + step; // 목적지 좌표

        // 1. 충돌 검사
        castColisitions.Clear();
        float castDistance = step.magnitude + collisitionOffset;
        int hitCount = rb.Cast(dir.normalized, movementFilter, castColisitions, castDistance);

        if (hitCount > 0)
        {
            EndMoveLook();
            isMoving = false;
            yield break;
        }

        // 🔍 [이동 시작 로그] 목적지의 스테이지 번호를 미리 확인
        if (GeneratorManager.Instance != null)
        {
            int targetStageIdx = GeneratorManager.Instance.GetStageIndexFromWorldPos(end);
            Debug.Log($"<color=white>🏃 [Move Start]</color> 목적지: {end} | 예상 스테이지: <color=yellow>{targetStageIdx}</color>");
        }

        // ✅ 스테이지 전환 체크
        if (StageManager.Instance != null)
        {
            StageManager.Instance.CheckStageTransition(end);
        }

        // 2. 실제 이동 처리
        if (AudioManager.instance != null)
            AudioManager.instance.PlayOneShot(FMODEvents.instance.PlayerDash, transform.position);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.fixedDeltaTime / moveDuration;
            rb.MovePosition(Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t)));
            yield return new WaitForFixedUpdate();
        }
        rb.MovePosition(end);

        // 🔍 [이동 종료 로그] 최종 도착지 스테이지 확인
        if (GeneratorManager.Instance != null)
        {
            int finalStageIdx = GeneratorManager.Instance.GetStageIndexFromWorldPos(transform.position);
            Debug.Log($"<color=lime>📍 [Move End]</color> 현재 위치: {transform.position} | 확정 스테이지: <color=cyan>{finalStageIdx}</color>");
        }

        EndMoveLook();
        isMoving = false;
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