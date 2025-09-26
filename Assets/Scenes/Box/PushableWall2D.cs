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

    public bool IsMoving { get; private set; }

    Rigidbody2D rb;
    Collider2D col;
    readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (useGridCellSize && grid != null) cellSize = grid.cellSize;
    }

    public bool TryPush(Vector2 dir, float? durationOverride = null)
    {
        if (IsMoving) return false;
        if (dir == Vector2.zero) return false;

        Vector2 step = new Vector2(
            dir.x * Mathf.Abs(cellSize.x),
            dir.y * Mathf.Abs(cellSize.y)
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
        );

        // 이동 목적지 충돌 검사
        hits.Clear();
        float dist = step.magnitude + castOffset;
        int count = rb.Cast(dir.normalized, blockFilter, hits, dist);
        if (count > 0) return false;

        StartCoroutine(MoveRoutine(step, durationOverride ?? moveDuration));
        return true;
    }

    IEnumerator MoveRoutine(Vector2 step, float duration)
    {
        IsMoving = true;

        Vector2 start = rb.position;
        Vector2 end   = start + step;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, duration);

        while (t < 1f)
        {
            t += Time.fixedDeltaTime / dur;
            float s = Mathf.SmoothStep(0f, 1f, t);
            rb.MovePosition(Vector2.Lerp(start, end, s));
            yield return new WaitForFixedUpdate();
        }
        rb.MovePosition(end);
        IsMoving = false;
    }
}
