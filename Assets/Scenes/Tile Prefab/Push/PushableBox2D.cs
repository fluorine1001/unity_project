using UnityEngine;

public class PushableBox2D : FunctionalTile
{
    [Header("물리 반응 설정")]
    [SerializeField] private float breakSpeedThreshold = 8f; // 파괴 속도 임계값
    [SerializeField] private float decayPerHit = 0.5f;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 gridOrigin = Vector2.zero;
    [SerializeField] private LayerMask blockingMask;

    [Header("세부 옵션")]
    [SerializeField] private bool consumeBulletOnPush = true;
    [SerializeField] private bool destroyBulletOnBreak = true;
    [SerializeField] private bool axisAlignedPush = true;
    [SerializeField] private float hitCooldown = 0.05f;

    private float _lastHitTime = -999f;
    private Rigidbody2D _rb2d;

    protected override void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false;

        _rb2d = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
        _rb2d.bodyType = RigidbodyType2D.Kinematic;
        _rb2d.simulated = true;
        _rb2d.useFullKinematicContacts = true;
    }

    protected override void OnBulletHit(BulletFire bullet)
    {
        if (!CanAcceptHit() || bullet == null) return;

        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // ✅ 총알의 "진행방향"을 그대로 사용
        Vector2 dir = rb.linearVelocity.normalized;
        float logicalSpeed = rb.linearVelocity.magnitude / GameConfig.SpeedScale;

        HandleHit(dir, logicalSpeed, bullet.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var bullet = collision.collider.GetComponent<BulletFire>();
        if (bullet != null) OnBulletHit(bullet);
    }

    private bool CanAcceptHit()
    {
        if (Time.time - _lastHitTime < hitCooldown) return false;
        _lastHitTime = Time.time;
        return true;
    }

    private void HandleHit(Vector2 dir, float speed, GameObject bulletGO)
    {
        // 1️⃣ [복구됨] 속도가 임계값 이상이면 상자 파괴 (이동 로직보다 먼저 수행)
        if (speed >= breakSpeedThreshold)
        {
            AudioManager.instance.PlayOneShot(FMODEvents.instance.BoxBroken, this.transform.position);
            
            // 총알 제거 옵션 확인
            if (destroyBulletOnBreak && bulletGO != null) 
                Destroy(bulletGO);
            
            // 상자 파괴
            Destroy(gameObject);
            return; // 여기서 함수 종료 (이동 안 함)
        }

        // 2️⃣ 이동 칸 수 계산
        int steps = Mathf.FloorToInt(Mathf.Max(0f, speed - decayPerHit));
        if (steps <= 0)
        {
            if (consumeBulletOnPush && bulletGO != null) Destroy(bulletGO);
            return;
        }

        // 3️⃣ 축 정렬 (이동 방향 보정)
        if (axisAlignedPush)
        {
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                dir = new Vector2(Mathf.Sign(dir.x), 0f);
            else
                dir = new Vector2(0f, Mathf.Sign(dir.y));
        }

        Vector2 startGridPos = WorldToGrid(transform.position);
        Vector2Int gridCoord = new Vector2Int(Mathf.RoundToInt(startGridPos.x), Mathf.RoundToInt(startGridPos.y));
        Vector2 halfExtents = GetHalfExtents();

        int actualSteps = 0;

        // 4️⃣ 이동 시뮬레이션
        for (int i = 0; i < steps; i++)
        {
            Vector2Int nextCoord = gridCoord + new Vector2Int((int)Mathf.Round(dir.x), (int)Mathf.Round(dir.y));
            Vector3 nextWorld = GridToWorld(nextCoord);

            // ✅ [추가됨] 다음 위치가 ClearTile(클리어 구역)이면 이동 막기
            if (StageManager.Instance != null && StageManager.Instance.IsClearTile(nextWorld))
            {
                break; // 루프 탈출 (이동 불가)
            }

            // ✔ (1) 구멍(Hole) 검사
            Collider2D holeCol = Physics2D.OverlapPoint(nextWorld);
            if (holeCol != null)
            {
                HoleTile hole = holeCol.GetComponent<HoleTile>();
                if (hole != null && hole.IsEmpty())
                {
                    hole.FillHole();            // 구멍 채우기
                    Destroy(gameObject);        // 박스 삭제

                    if (consumeBulletOnPush && bulletGO != null)
                        Destroy(bulletGO);

                    return; // 상자가 구멍에 빠졌으므로 종료
                }
            }

            // ✔ (2) 일반 장애물(벽, 다른 상자 등) 검사
            Collider2D hit = Physics2D.OverlapBox(nextWorld, halfExtents * 2f, 0f, blockingMask);
            if (hit != null && hit.gameObject != this.gameObject)
            {
                break; // 장애물 만남 -> 이동 중단
            }

            // ✔ (3) 이동 가능 -> 좌표 갱신
            gridCoord = nextCoord;
            actualSteps++;
        }

        // 5️⃣ 실제 이동 처리
        if (actualSteps != 0)
        {
            AudioManager.instance.PlayOneShot(FMODEvents.instance.BoxPushed, this.transform.position);
        }

        Vector3 snappedPos = GridToWorld(gridCoord);
        MoveTo(snappedPos);

        if (consumeBulletOnPush && bulletGO != null) Destroy(bulletGO);
    }

    private void MoveTo(Vector3 targetPos)
    {
        if (_rb2d != null && _rb2d.bodyType == RigidbodyType2D.Kinematic)
            _rb2d.MovePosition(targetPos);
        else
            transform.position = targetPos;
    }

    private Vector2 GetHalfExtents()
    {
        var box = GetComponent<BoxCollider2D>();
        return box ? box.size * 0.5f * AbsVec2(transform.lossyScale) : Vector2.one * (cellSize * 0.49f);
    }

    private static Vector2 AbsVec2(Vector3 v) => new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));

    private Vector2 WorldToGrid(Vector2 worldPos)
    {
        Vector2 offset = worldPos - gridOrigin - Vector2.one * (cellSize / 2f);
        return offset / cellSize;
    }

    private Vector3 GridToWorld(Vector2Int gridCoord)
    {
        return new Vector3(
            gridOrigin.x + (gridCoord.x + 0.5f) * cellSize,
            gridOrigin.y + (gridCoord.y + 0.5f) * cellSize,
            transform.position.z
        );
    }
}