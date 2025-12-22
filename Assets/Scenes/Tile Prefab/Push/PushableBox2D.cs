using UnityEngine;

public class PushableBox2D : FunctionalTile
{
    [Header("물리 반응 설정")]
    [SerializeField] private float breakSpeedThreshold = 8f; // 파괴 속도 임계값
    [SerializeField] private float decayPerHit = 0.5f;       // 타격 시 속도 감소량
    [SerializeField] private float cellSize = 1f;            // 그리드 크기
    [SerializeField] private Vector2 gridOrigin = Vector2.zero;
    [SerializeField] private LayerMask blockingMask;         // 이동을 막는 레이어

    [Header("세부 옵션")]
    [SerializeField] private bool consumeBulletOnPush = true;
    [SerializeField] private bool destroyBulletOnBreak = true;
    [SerializeField] private bool axisAlignedPush = true;
    [SerializeField] private float hitCooldown = 0.05f;

    private float _lastHitTime = -999f;
    private Rigidbody2D _rb2d;

    protected override void Awake()
    {
        base.Awake(); // FunctionalTile에 Awake가 있다면 호출

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false; // 충돌 감지를 위해 Trigger 끄기

        _rb2d = GetComponent<Rigidbody2D>();
        if (_rb2d == null) _rb2d = gameObject.AddComponent<Rigidbody2D>();

        _rb2d.bodyType = RigidbodyType2D.Kinematic;
        _rb2d.simulated = true;
        _rb2d.useFullKinematicContacts = true;
    }

    // 충돌 발생 시 호출
    private void OnCollisionEnter2D(Collision2D collision)
    {
        var bullet = collision.collider.GetComponent<BulletFire>();
        if (bullet != null)
        {
            OnBulletHit(bullet);
        }
    }

    public override void OnBulletHit(BulletFire bullet)
    {
        if (!CanAcceptHit() || bullet == null) return;

        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // [주의] Unity 6 이전 버전(2022 등)이라면 rb.linearVelocity 대신 rb.velocity를 사용해야 합니다.
        // 여기서는 주신 코드대로 linearVelocity를 사용합니다. 에러나면 .velocity로 바꾸세요.
#if UNITY_6000_0_OR_NEWER
        Vector2 velocity = rb.linearVelocity;
#else
        Vector2 velocity = rb.velocity;
#endif

        Vector2 dir = velocity.normalized;
        
        // 속도 계산 로직
        // GameConfig.SpeedScale이 너무 크면 logicalSpeed가 작아져서 안 움직일 수 있습니다.
        float logicalSpeed = velocity.magnitude / GameConfig.SpeedScale;

        // 디버깅: 속도 확인
        // Debug.Log($"[Box] Hit! BulletVel: {velocity.magnitude}, LogicalSpeed: {logicalSpeed}");

        HandleHit(dir, logicalSpeed, bullet.gameObject);
    }

    private bool CanAcceptHit()
    {
        if (Time.time - _lastHitTime < hitCooldown) return false;
        _lastHitTime = Time.time;
        return true;
    }

    private void HandleHit(Vector2 dir, float speed, GameObject bulletGO)
    {
        // 1️⃣ 속도가 임계값 이상이면 상자 파괴
        if (speed >= breakSpeedThreshold)
        {
            AudioManager.instance.PlayOneShot(FMODEvents.instance.BoxBroken, this.transform.position);
            
            if (destroyBulletOnBreak && bulletGO != null) 
                Destroy(bulletGO);
            
            Destroy(gameObject);
            return; 
        }

        // 2️⃣ 이동 칸 수 계산
        // 여기서 steps가 0이 나오면 "아예 안 밀리는" 현상이 발생합니다.
        int steps = Mathf.FloorToInt(Mathf.Max(0f, speed - decayPerHit));
        
        if (steps <= 0)
        {
            Debug.LogWarning($"[Box] 힘이 부족함. Speed({speed}) - Decay({decayPerHit}) < 1. Steps: 0");
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

            // (A) GeneratorManager Blocker 체크
            if (IsNextStepBlocker(nextWorld)) 
            {
                Debug.Log("[Box] GeneratorManager Blocker에 막힘");
                break;
            }

            // (B) StageManager ClearTile 체크
            if (StageManager.Instance != null && StageManager.Instance.IsClearTile(nextWorld))
            {
                Debug.Log("[Box] ClearTile 구역이라 이동 불가");
                break; 
            }

            // (C) 구멍(Hole) 체크
            Collider2D holeCol = Physics2D.OverlapPoint(nextWorld);
            if (holeCol != null)
            {
                HoleTile hole = holeCol.GetComponent<HoleTile>();
                if (hole != null && hole.IsEmpty())
                {
                    hole.FillHole();            
                    Destroy(gameObject);        

                    if (consumeBulletOnPush && bulletGO != null)
                        Destroy(bulletGO);

                    return; // 구멍에 빠지면 즉시 종료
                }
            }

            // (D) 일반 장애물(벽, 다른 상자) 체크
            Collider2D hit = Physics2D.OverlapBox(nextWorld, halfExtents * 2f, 0f, blockingMask);
            if (hit != null && hit.gameObject != this.gameObject)
            {
                Debug.Log($"[Box] 장애물에 막힘: {hit.gameObject.name}");
                break; 
            }

            // 이동 가능 확정
            gridCoord = nextCoord;
            actualSteps++;
        }

        // 5️⃣ 실제 이동 처리
        if (actualSteps > 0)
        {
            AudioManager.instance.PlayOneShot(FMODEvents.instance.BoxPushed, this.transform.position);
            Vector3 snappedPos = GridToWorld(gridCoord);
            MoveTo(snappedPos);
            Debug.Log($"[Box] {actualSteps}칸 이동 완료");
        }
        else
        {
             Debug.Log($"[Box] 이동 시도했으나 모든 경로가 막힘 (Steps: {steps}, Actual: 0)");
        }

        if (consumeBulletOnPush && bulletGO != null) Destroy(bulletGO);
    }

    private bool IsNextStepBlocker(Vector3 worldPos)
    {
        if (GeneratorManager.Instance == null) return false;
        return GeneratorManager.Instance.IsBlockerTile(worldPos); 
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
        // 박스 크기를 약간 줄여서(0.95배) 옆 타일과 겹치는 문제 방지
        return box ? box.size * 0.5f * AbsVec2(transform.lossyScale) * 0.95f : Vector2.one * (cellSize * 0.45f);
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