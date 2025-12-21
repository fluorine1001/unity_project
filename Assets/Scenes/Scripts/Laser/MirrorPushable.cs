using UnityEngine;

[RequireComponent(typeof(MirrorBlock))]
[RequireComponent(typeof(Rigidbody2D))]
public class MirrorPushable : FunctionalTile
{
    [Header("이동 설정")]
    [SerializeField] private float cellSize = 1f;  
    [SerializeField] private Vector2 gridOrigin = Vector2.zero; 
    [SerializeField] private LayerMask blockingMask;

    [Header("옵션")]
    [SerializeField] private bool consumeBullet = true; 

    private MirrorBlock _mirrorBlock;
    private Rigidbody2D _rb2d;

    protected override void Awake()
    {
        base.Awake(); 
        _mirrorBlock = GetComponent<MirrorBlock>();
        
        _rb2d = GetComponent<Rigidbody2D>();
        if (_rb2d == null) _rb2d = gameObject.AddComponent<Rigidbody2D>();
        
        _rb2d.bodyType = RigidbodyType2D.Kinematic;
        _rb2d.simulated = true;
        _rb2d.useFullKinematicContacts = true;
        
        transform.position = GridToWorld(WorldToGrid(transform.position));
    }

    protected override void OnBulletHit(BulletFire bullet)
    {
        if (bullet == null) return;

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb == null) return;

#if UNITY_6000_0_OR_NEWER
        Vector2 velocity = bulletRb.linearVelocity;
#else
        Vector2 velocity = bulletRb.velocity;
#endif
        Vector2 pushDir = SnapToCardinal(velocity);

        // 방향 판정 호출
        if (CheckPushableDirection(pushDir))
        {
            TryMoveInstant(pushDir, bullet.gameObject);
        }
        else
        {
            // 빗면 등을 때려서 못 미는 경우 -> 총알만 삭제
            if (consumeBullet) Destroy(bullet.gameObject);
        }
    }

    // =========================================================
    // ✅ 1. 판정 로직 수정 (삼각거울 로직 복구 + 반거울 로직 추가)
    // =========================================================
    private bool CheckPushableDirection(Vector2 pushDir)
    {
        // 1. 사각거울(Square) OR 반거울(Half)은 방향 상관없이 무조건 밀림
        // ※ 주의: MirrorType.Half 부분은 실제 사용하시는 Enum 이름(예: Semi, HalfMirror 등)으로 바꾸세요.
        if (_mirrorBlock.mirrorType == MirrorType.Square || 
            _mirrorBlock.mirrorType == MirrorType.Half) 
        {
            return true;
        }

        // 2. 삼각거울(Triangle)은 원래대로 방향을 탐 (빗면은 안 밀림)
        if (_mirrorBlock.mirrorType == MirrorType.Triangle)
        {
            switch (_mirrorBlock.orientation)
            {
                // ◣ UpRight (위, 오른쪽이 막힘) -> 왼쪽이나 아래를 때려야 밀림 (진행방향 Right, Up)
                case TriangleOrientation.UpRight:
                    return pushDir == Vector2.right || pushDir == Vector2.up;

                // ◢ UpLeft (위, 왼쪽이 막힘) -> 오른쪽이나 아래를 때려야 밀림 (진행방향 Left, Up)
                case TriangleOrientation.UpLeft:
                    return pushDir == Vector2.left || pushDir == Vector2.up;

                // ◤ RightDown (아래, 오른쪽이 막힘) -> 왼쪽이나 위를 때려야 밀림 (진행방향 Right, Down)
                case TriangleOrientation.RightDown:
                    return pushDir == Vector2.right || pushDir == Vector2.down;

                // ◥ DownLeft (아래, 왼쪽이 막힘) -> 오른쪽이나 위를 때려야 밀림 (진행방향 Left, Down)
                case TriangleOrientation.DownLeft:
                    return pushDir == Vector2.left || pushDir == Vector2.down;
            }
        }

        return false;
    }

    // =========================================================
    // ✅ 2. 이동 로직 (즉시 이동)
    // =========================================================
    private void TryMoveInstant(Vector2 dir, GameObject bulletGO)
    {
        Vector2 currentGridPos = WorldToGrid(transform.position);
        
        Vector2Int nextGridPos = new Vector2Int(
            Mathf.RoundToInt(currentGridPos.x + dir.x), 
            Mathf.RoundToInt(currentGridPos.y + dir.y)
        );
        
        Vector3 nextWorldPos = GridToWorld(nextGridPos);

        // A. 장애물 체크
        Vector2 halfSize = Vector2.one * (cellSize * 0.4f);
        Collider2D hit = Physics2D.OverlapBox(nextWorldPos, halfSize, 0f, blockingMask);
        
        if (hit != null && hit.gameObject != gameObject && hit.gameObject != bulletGO)
        {
            if (consumeBullet && bulletGO != null) Destroy(bulletGO);
            return;
        }

        // B. 클리어 타일 체크
        if (StageManager.Instance != null && StageManager.Instance.IsClearTile(nextWorldPos))
        {
            if (consumeBullet && bulletGO != null) Destroy(bulletGO);
            return;
        }

        // C. 이동 실행
        if (consumeBullet && bulletGO != null) Destroy(bulletGO);

        if (AudioManager.instance != null)
            AudioManager.instance.PlayOneShot(FMODEvents.instance.BoxPushed, transform.position);

        transform.position = nextWorldPos;
        
        CheckHole(nextWorldPos);
    }

    private void CheckHole(Vector3 pos)
    {
        Collider2D holeCol = Physics2D.OverlapPoint(pos);
        if (holeCol != null)
        {
            HoleTile hole = holeCol.GetComponent<HoleTile>();
            if (hole != null && hole.IsEmpty())
            {
                hole.FillHole();
                Destroy(gameObject);
            }
        }
    }

    private Vector2 SnapToCardinal(Vector2 v)
    {
        if (v.magnitude < 0.1f) return Vector2.zero;
        return (Mathf.Abs(v.x) > Mathf.Abs(v.y)) 
            ? new Vector2(Mathf.Sign(v.x), 0) 
            : new Vector2(0, Mathf.Sign(v.y));
    }

    private Vector2 WorldToGrid(Vector2 worldPos)
    {
        return (worldPos - gridOrigin - Vector2.one * (cellSize * 0.5f)) / cellSize;
    }

    private Vector3 GridToWorld(Vector2 gridPos)
    {
        return new Vector3(
            gridOrigin.x + (Mathf.Round(gridPos.x) + 0.5f) * cellSize,
            gridOrigin.y + (Mathf.Round(gridPos.y) + 0.5f) * cellSize,
            transform.position.z
        );
    }
}