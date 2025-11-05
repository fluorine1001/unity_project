using UnityEngine;

/// <summary>
/// 총알의 충돌에만 반응하여 Grid 칸 단위로 밀리거나 파괴되는 상자.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class PushableBox2D : MonoBehaviour
{
    [Header("Behavior Settings")]
    [SerializeField] private float breakSpeedThreshold = 8f; // 파괴 기준 속도
    [SerializeField] private float decayPerHit = 0.5f;       // 감속 상수
    [SerializeField] private float cellSize = 1f;            // Grid 한 칸 크기
    [SerializeField] private Vector2 gridOrigin = Vector2.zero; // 그리드 원점 오프셋
    [SerializeField] private LayerMask blockingMask;

    [Header("Options")]
    [SerializeField] private bool consumeBulletOnPush = true;
    [SerializeField] private bool destroyBulletOnBreak = true;
    [SerializeField] private bool axisAlignedPush = true;
    [SerializeField] private float hitCooldown = 0.05f;

    private float _lastHitTime = -999f;

    public void OnBulletHit(BulletFire bullet)
    {
        if (!CanAcceptHit()) return;

        var rb = bullet != null ? bullet.GetComponent<Rigidbody2D>() : null;
        if (rb == null) return;

        Vector2 v = rb.linearVelocity;
        float speed = v.magnitude;
        Vector2 dir = v.sqrMagnitude > 1e-6f ? v.normalized : Vector2.zero;

        HandleHit(dir, speed, bullet != null ? bullet.gameObject : null);
    }

    private bool CanAcceptHit()
    {
        if (Time.time - _lastHitTime < hitCooldown) return false;
        _lastHitTime = Time.time;
        return true;
    }

    private void HandleHit(Vector2 dir, float speed, GameObject bulletGO)
    {
        if (speed >= breakSpeedThreshold)
        {
            if (destroyBulletOnBreak && bulletGO != null) Destroy(bulletGO);
            Destroy(gameObject);
            return;
        }

        // 밀릴 칸 수 계산
        int steps = Mathf.FloorToInt(Mathf.Max(0f, speed - decayPerHit));
        if (steps <= 0)
        {
            if (consumeBulletOnPush && bulletGO != null) Destroy(bulletGO);
            return;
        }

        // 축 정렬 방향
        if (axisAlignedPush)
        {
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                dir = new Vector2(Mathf.Sign(dir.x), 0f);
            else
                dir = new Vector2(0f, Mathf.Sign(dir.y));
        }

        // 현재 그리드 기준 위치 계산
        Vector2 startGridPos = WorldToGrid(transform.position);
        Vector2Int gridCoord = new Vector2Int(Mathf.RoundToInt(startGridPos.x), Mathf.RoundToInt(startGridPos.y));

        Vector2 halfExtents = GetHalfExtents();

        // 칸 단위 이동
        for (int i = 0; i < steps; i++)
        {
            Vector2Int nextCoord = gridCoord + new Vector2Int((int)Mathf.Round(dir.x), (int)Mathf.Round(dir.y));
            Vector3 nextWorld = GridToWorld(nextCoord);

            // 충돌 체크
            Collider2D hit = Physics2D.OverlapBox(nextWorld, halfExtents * 2f, 0f, blockingMask);
            if (hit != null)
            {
                break; // 막히면 중단
            }

            gridCoord = nextCoord;
        }

        Vector3 snappedPos = GridToWorld(gridCoord);
        MoveTo(snappedPos);

        if (consumeBulletOnPush && bulletGO != null) Destroy(bulletGO);
    }

    private void MoveTo(Vector3 targetPos)
    {
        var rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null && rb2d.bodyType == RigidbodyType2D.Kinematic)
            rb2d.MovePosition(targetPos);
        else
            transform.position = targetPos;
    }

    private Vector2 GetHalfExtents()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
            return box.size * 0.5f * AbsVec2(transform.lossyScale);
        return Vector2.one * (cellSize * 0.49f);
    }

    private static Vector2 AbsVec2(Vector3 v) => new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));

    // --- Grid 변환 함수들 ---
    private Vector2 WorldToGrid(Vector2 worldPos)
    {
        Vector2 offsetPos = worldPos - gridOrigin;
        return offsetPos / cellSize;
    }

    private Vector3 GridToWorld(Vector2Int gridCoord)
    {
        return new Vector3(
            gridOrigin.x + gridCoord.x * cellSize,
            gridOrigin.y + gridCoord.y * cellSize,
            transform.position.z
        );
    }

    // Inspector에서 파라미터 세팅 함수 (선택)
    public void SetParams(float breakThreshold, float decay, float tileSize, Vector2 origin)
    {
        breakSpeedThreshold = breakThreshold;
        decayPerHit = decay;
        cellSize = tileSize;
        gridOrigin = origin;
    }
}
