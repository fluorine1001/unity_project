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
        base.Awake(); 

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false; 

        _rb2d = GetComponent<Rigidbody2D>();
        if (_rb2d == null) _rb2d = gameObject.AddComponent<Rigidbody2D>();

        _rb2d.bodyType = RigidbodyType2D.Kinematic;
        _rb2d.simulated = true;
        _rb2d.useFullKinematicContacts = true;
    }

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

#if UNITY_6000_0_OR_NEWER
        Vector2 velocity = rb.linearVelocity;
#else
        Vector2 velocity = rb.velocity;
#endif

        Vector2 dir = velocity.normalized;
        float logicalSpeed = velocity.magnitude / GameConfig.SpeedScale;

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
        int steps = Mathf.FloorToInt(Mathf.Max(0f, speed - decayPerHit));
        
        if (steps <= 0)
        {
            Debug.LogWarning($"[Box] 힘이 부족함. Steps: 0");
            if (consumeBulletOnPush && bulletGO != null) Destroy(bulletGO);
            return;
        }

        // 3️⃣ 축 정렬
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

        // 4️⃣ 이동 시뮬레이션 (벽 체크만 수행, 구멍은 무시)
        for (int i = 0; i < steps; i++)
        {
            Vector2Int nextCoord = gridCoord + new Vector2Int((int)Mathf.Round(dir.x), (int)Mathf.Round(dir.y));
            Vector3 nextWorld = GridToWorld(nextCoord);

            // (A) GeneratorManager Blocker 체크
            if (IsNextStepBlocker(nextWorld)) 
            {
                Debug.Log("[Box] 벽에 막힘");
                break;
            }

            // (B) StageManager ClearTile 체크
            if (StageManager.Instance != null && StageManager.Instance.IsClearTile(nextWorld))
            {
                Debug.Log("[Box] ClearTile 구역이라 이동 불가");
                break; 
            }

            // (C) 일반 장애물(벽, 다른 상자) 체크
            Collider2D hit = Physics2D.OverlapBox(nextWorld, halfExtents * 2f, 0f, blockingMask);
            if (hit != null && hit.gameObject != this.gameObject)
            {
                // [중요 수정] 장애물이 'HoleTile'이라면, 벽이 아니므로 멈추지 않고 통과합니다.
                if (hit.GetComponent<HoleTile>() != null)
                {
                    // 구멍은 장애물이 아님 -> 통과 (Loop 계속 진행)
                }
                else
                {
                    Debug.Log($"[Box] 장애물에 막힘: {hit.gameObject.name}");
                    break; // 진짜 벽이면 멈춤
                }
            }

            // 이동 가능 확정
            gridCoord = nextCoord;
            actualSteps++;
        }

        // 5️⃣ 실제 이동 처리 및 최종 위치 구멍 체크
        if (actualSteps > 0)
        {
            AudioManager.instance.PlayOneShot(FMODEvents.instance.BoxPushed, this.transform.position);
            Vector3 snappedPos = GridToWorld(gridCoord);
            
            // 이동
            MoveTo(snappedPos);
            Debug.Log($"[Box] {actualSteps}칸 이동 완료");

            // [NEW] 이동 완료 후 최종 위치에서 구멍(Hole) 체크
            CheckFinalPositionForHole(snappedPos, bulletGO);
        }
        else
        {
             Debug.Log($"[Box] 이동 불가 (Steps: {steps}, Actual: 0)");
        }

        // 총알 제거 (구멍에 빠지지 않았을 때만 여기서 제거, 빠졌으면 CheckFinalPositionForHole 내부에서 처리됨)
        // 안전하게 처리하기 위해 bulletGO가 null이 아닌 경우만 파괴
        if (consumeBulletOnPush && bulletGO != null) Destroy(bulletGO);
    }

    // 최종 위치에 구멍이 있는지 확인하고 처리하는 함수
    private void CheckFinalPositionForHole(Vector3 targetPos, GameObject bulletGO)
    {
        // 겹쳐있는 모든 콜라이더 확인 (박스 자신도 포함될 수 있으니 주의)
        Collider2D[] hits = Physics2D.OverlapPointAll(targetPos);
        
        foreach (var hit in hits)
        {
            HoleTile hole = hit.GetComponent<HoleTile>();
            // 구멍이 존재하고 비어있다면
            if (hole != null && hole.IsEmpty())
            {
                hole.FillHole();     // 구멍 채우기
                Destroy(gameObject); // 상자 파괴

                // 총알도 즉시 제거 (상위 함수에서 중복 제거되지 않도록 null 체크 하므로 안전)
                if (consumeBulletOnPush && bulletGO != null)
                    Destroy(bulletGO);

                return; // 처리 끝
            }
        }
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