using UnityEngine;

[RequireComponent(typeof(MirrorBlock))]
[RequireComponent(typeof(Rigidbody2D))]
public class MirrorPushable : FunctionalTile
{
    [Header("이동 설정")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 gridOrigin = Vector2.zero;
    [SerializeField] private LayerMask blockingMask;

    [Header("물리 반응 설정")]
    [Tooltip("총알 속도를 그리드 칸 수로 변환할 때 나누는 값입니다. (기본값: GameConfig.SpeedScale)")]
    [SerializeField] private float speedScale = 8f; // GameConfig.SpeedScale과 비슷한 값으로 설정 필요
    [SerializeField] private float decayPerHit = 0f; // 타격 시 속도 감쇠
    
    [Header("옵션")]
    [SerializeField] private bool consumeBullet = true;

    private MirrorBlock _mirrorBlock;
    private Rigidbody2D _rb2d;

    protected override void Awake()
    {
        base.Awake();
        _mirrorBlock = GetComponent<MirrorBlock>();

        // 1. Collider 설정 (Box와 동일하게 Trigger 해제)
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false;

        _rb2d = GetComponent<Rigidbody2D>();
        if (_rb2d == null) _rb2d = gameObject.AddComponent<Rigidbody2D>();

        _rb2d.bodyType = RigidbodyType2D.Kinematic;
        _rb2d.simulated = true;
        _rb2d.useFullKinematicContacts = true;

        // 시작 시 그리드 정렬 (박스와 동일한 로직 적용)
        Vector2 startGridPos = WorldToGrid(transform.position);
        Vector2Int gridCoord = new Vector2Int(Mathf.RoundToInt(startGridPos.x), Mathf.RoundToInt(startGridPos.y));
        transform.position = GridToWorld(gridCoord);
    }

    // ✅ 2. 충돌 감지 함수 추가 (이게 없어서 안 밀렸던 것)
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
        if (bullet == null) return;

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb == null) return;

        // 1. 총알 속도 가져오기
#if UNITY_6000_0_OR_NEWER
        Vector2 velocity = bulletRb.linearVelocity;
#else
        Vector2 velocity = bulletRb.velocity;
#endif

        // 2. 이동 방향 결정 (상하좌우 스냅)
        Vector2 pushDir = SnapToCardinal(velocity);

        // 3. [판정] 거울의 방향 특성상 밀릴 수 있는 각도인지 확인
        if (CheckPushableDirection(pushDir))
        {
            // 4. [계산] 이동할 칸 수 계산
            // Box와 동일하게 magnitude를 scale로 나눔
            float logicalSpeed = velocity.magnitude / speedScale;
            int steps = Mathf.FloorToInt(Mathf.Max(0f, logicalSpeed - decayPerHit));

            // 디버깅용 (속도가 부족한지 확인)
            // Debug.Log($"Mirror Hit! Vel:{velocity.magnitude}, Steps:{steps}, Dir:{pushDir}");

            if (steps > 0)
            {
                ProcessMoveSequence(pushDir, steps, bullet.gameObject);
            }
            else
            {
                // 방향은 맞지만 힘이 부족함
                if (consumeBullet) Destroy(bullet.gameObject);
            }
        }
        else
        {
            // 밀 수 없는 방향(빗면 등)
            if (consumeBullet) Destroy(bullet.gameObject);
        }
    }

    // ... (CheckPushableDirection 로직은 기존 유지) ...
    // ✅ 수정된 방향 판정 로직
    private bool CheckPushableDirection(Vector2 pushDir)
    {
        // 사각/반거울은 모든 방향에서 밀림
        if (_mirrorBlock.mirrorType == MirrorType.Square ||
            _mirrorBlock.mirrorType == MirrorType.Half)
        {
            return true;
        }

        // 삼각거울: 빗면이 아닌 '평평한 뒷면'을 미는 방향이어야 함
        if (_mirrorBlock.mirrorType == MirrorType.Triangle)
        {
            switch (_mirrorBlock.orientation)
            {
                // [CASE 1] ◣ 모양 (Left, Bottom이 평면)
                // -> 왼쪽에서 때리면 Right로 이동, 아래에서 때리면 Up으로 이동
                case TriangleOrientation.UpRight: 
                    return pushDir == Vector2.right || pushDir == Vector2.up;

                // [CASE 2] ◢ 모양 (Right, Bottom이 평면)
                // -> 오른쪽에서 때리면 Left로 이동, 아래에서 때리면 Up으로 이동
                case TriangleOrientation.UpLeft: 
                    return pushDir == Vector2.left || pushDir == Vector2.up;

                // [CASE 3] ◤ 모양 (Left, Top이 평면)
                // -> 왼쪽에서 때리면 Right로 이동, 위에서 때리면 Down으로 이동
                case TriangleOrientation.RightDown: 
                    return pushDir == Vector2.right || pushDir == Vector2.down;

                // [CASE 4] ◥ 모양 (Right, Top이 평면)
                // -> 오른쪽에서 때리면 Left로 이동, 위에서 때리면 Down으로 이동
                case TriangleOrientation.DownLeft: 
                    return pushDir == Vector2.left || pushDir == Vector2.down;
            }
        }
        return false;
    }

    private void ProcessMoveSequence(Vector2 dir, int steps, GameObject bulletGO)
    {
        Vector2 currentGridPos = WorldToGrid(transform.position);
        Vector2Int gridCoord = new Vector2Int(Mathf.RoundToInt(currentGridPos.x), Mathf.RoundToInt(currentGridPos.y));
        Vector2Int dirInt = new Vector2Int(Mathf.RoundToInt(dir.x), Mathf.RoundToInt(dir.y));

        int actualSteps = 0;
        
        // Box와 충돌체 크기 계산 로직 통일
        Vector2 halfExtents = GetHalfExtents(); 

        for (int i = 0; i < steps; i++)
        {
            Vector2Int nextCoord = gridCoord + dirInt;
            Vector3 nextWorldPos = GridToWorld(nextCoord);

            // 1. Blocker 확인
            if (GeneratorManager.Instance != null && GeneratorManager.Instance.IsBlockerTile(nextWorldPos)) break;

            // 2. ClearTile 확인
            if (StageManager.Instance != null && StageManager.Instance.IsClearTile(nextWorldPos)) break;

            // 3. 구멍 확인
            Collider2D holeCol = Physics2D.OverlapPoint(nextWorldPos);
            if (holeCol != null)
            {
                HoleTile hole = holeCol.GetComponent<HoleTile>();
                if (hole != null && hole.IsEmpty())
                {
                    hole.FillHole();
                    MoveTo(nextWorldPos);
                    if (consumeBullet && bulletGO != null) Destroy(bulletGO);
                    Destroy(gameObject, 0.1f);
                    return;
                }
            }

            // 4. 물리적 장애물 확인 (OverlapBox 사용)
            Collider2D hit = Physics2D.OverlapBox(nextWorldPos, halfExtents * 2f, 0f, blockingMask);
            if (hit != null && hit.gameObject != gameObject && hit.gameObject != bulletGO)
            {
                break;
            }

            gridCoord = nextCoord;
            actualSteps++;
        }

        if (actualSteps > 0)
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlayOneShot(FMODEvents.instance.MirrorPushed, transform.position);

            Vector3 finalPos = GridToWorld(gridCoord);
            MoveTo(finalPos);
        }

        if (consumeBullet && bulletGO != null) Destroy(bulletGO);
    }

    private void MoveTo(Vector3 targetPos)
    {
        if (_rb2d != null && _rb2d.bodyType == RigidbodyType2D.Kinematic)
            _rb2d.MovePosition(targetPos);
        else
            transform.position = targetPos;
    }

    // ✅ 3. 유틸리티 함수 통일 (PushableBox2D와 동일하게 변경)
    private Vector2 GetHalfExtents()
    {
        var box = GetComponent<BoxCollider2D>();
        return box ? box.size * 0.5f * AbsVec2(transform.lossyScale) * 0.95f : Vector2.one * (cellSize * 0.45f);
    }
    
    private static Vector2 AbsVec2(Vector3 v) => new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));

    private Vector2 SnapToCardinal(Vector2 v)
    {
        if (v.magnitude < 0.1f) return Vector2.zero;
        return (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            ? new Vector2(Mathf.Sign(v.x), 0)
            : new Vector2(0, Mathf.Sign(v.y));
    }

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