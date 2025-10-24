using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PushableWall2D : MonoBehaviour
{
    [Header("Grid / Cell Size")]
    public Grid grid;
    public bool useGridCellSize = true;
    public Vector2 cellSize = new Vector2(0.16f, 0.16f);

    [Header("Move")]
    public float moveDuration = 0.08f;

    [Header("Collision")]
    public float castOffset = 0.05f;
    public ContactFilter2D blockFilter; // 벽/장애물 레이어(플레이어는 포함 X)

    [Header("Bullet Interaction")]
    [Tooltip("이 속도 이상이면 상자를 파괴")]
    [SerializeField] private float breakSpeed = 10f;
    [Tooltip("이 속도 이상이면 상자를 민다")]
    [SerializeField] private float pushSpeed = 4f;
    [Tooltip("pushSpeed에서 이동할 '셀' 거리. 속도에 비례해 증가")]
    [SerializeField] private float baseCellsAtPushSpeed = 1f;

    [Tooltip("BulletFire가 없어도 Rigidbody2D 속도로 판단")]
    [SerializeField] private bool allowAnyRigidbodyAsBullet = true;

    public bool IsMoving { get; private set; }

    Rigidbody2D rb;
    Collider2D col;
    readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (useGridCellSize && grid != null) cellSize = grid.cellSize;

        // 플레이어와 물리 충돌되도록 설정
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        col.isTrigger = false; // 반드시 충돌
    }

    // 총알 충돌만 처리. 플레이어와의 상호작용 없음.
    void OnTriggerEnter2D(Collider2D other) => HandleImpact(other.attachedRigidbody, other);
    void OnCollisionEnter2D(Collision2D other) => HandleImpact(other.rigidbody, other.collider);

    void HandleImpact(Rigidbody2D otherRb, Collider2D otherCol)
    {
        if (otherCol == null || otherRb == null) return;

        // 총알 판정
        var bullet = otherCol.GetComponentInParent<BulletFire>();
        if (bullet == null && !allowAnyRigidbodyAsBullet) return;

        float speed = otherRb.linearVelocity.magnitude;
        if (speed <= 0f) return;

        // 총알 진행 축에 맞춰 밀기
        Vector2 dir = AxisAligned(otherRb.linearVelocity.normalized);
        if (dir == Vector2.zero) return;

        // 파괴 우선
        if (speed >= breakSpeed)
        {
            Destroy(gameObject);
            return;
        }

        // 밀기: 속도 비례 셀 수
        if (speed >= pushSpeed)
        {
            float ratio = speed / Mathf.Max(0.0001f, pushSpeed);
            int cells = Mathf.Max(1, Mathf.RoundToInt(baseCellsAtPushSpeed * ratio));
            TryPush(dir, cells);
        }
    }

    static Vector2 AxisAligned(Vector2 v)
    {
        float ax = Mathf.Abs(v.x);
        float ay = Mathf.Abs(v.y);
        if (ax < 1e-4f && ay < 1e-4f) return Vector2.zero;
        return ax >= ay ? new Vector2(v.x >= 0 ? 1 : -1, 0) : new Vector2(0, v.y >= 0 ? 1 : -1);
    }

    // 외부에서 호출해도 플레이어 로직 없음
    public bool TryPush(Vector2 dir, float? durationOverride = null) => TryPush(dir, 1, durationOverride);

    public bool TryPush(Vector2 dir, int cells, float? durationOverride = null)
    {
        if (IsMoving) return false;
        if (dir == Vector2.zero) return false;
        cells = Mathf.Max(1, cells);

        Vector2 oneStep = new Vector2(
            dir.x * Mathf.Abs(cellSize.x),
            dir.y * Mathf.Abs(cellSize.y)
        );
        Vector2 step = oneStep * cells;

        // 목적지 충돌 검사(플레이어는 blockFilter에서 제외)
        hits.Clear();
        float dist = step.magnitude + castOffset;
        int count = rb.Cast(dir.normalized, blockFilter, hits, dist);
        if (count > 0) return false;

        StartCoroutine(MoveRoutine(step, durationOverride ?? moveDuration, cells));
        return true;
    }

    IEnumerator MoveRoutine(Vector2 step, float durationPerCell, int cells)
    {
        IsMoving = true;

        Vector2 start = rb.position;
        Vector2 end = start + step;

        float totalDuration = Mathf.Max(0.0001f, durationPerCell) * Mathf.Max(1, cells);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.fixedDeltaTime / totalDuration;
            float s = Mathf.SmoothStep(0f, 1f, t);
            rb.MovePosition(Vector2.Lerp(start, end, s));
            yield return new WaitForFixedUpdate();
        }
        rb.MovePosition(end);
        IsMoving = false;
    }
}
