using UnityEngine;
using System.Collections.Generic;

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
    [SerializeField] private float speedScale = 8f; 
    [SerializeField] private float decayPerHit = 0f; 
    
    [Header("옵션")]
    [SerializeField] private bool consumeBullet = true;

    private MirrorBlock _mirrorBlock;
    private Rigidbody2D _rb2d;

    protected override void Awake()
    {
        base.Awake();
        _mirrorBlock = GetComponent<MirrorBlock>();

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false;

        _rb2d = GetComponent<Rigidbody2D>();
        if (_rb2d == null) _rb2d = gameObject.AddComponent<Rigidbody2D>();

        _rb2d.bodyType = RigidbodyType2D.Kinematic;
        _rb2d.simulated = true;
        _rb2d.useFullKinematicContacts = true;

        Vector2 startGridPos = WorldToGrid(transform.position);
        Vector2Int gridCoord = new Vector2Int(Mathf.RoundToInt(startGridPos.x), Mathf.RoundToInt(startGridPos.y));
        transform.position = GridToWorld(gridCoord);
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
        if (bullet == null) return;

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb == null) return;

#if UNITY_6000_0_OR_NEWER
        Vector2 velocity = bulletRb.linearVelocity;
#else
        Vector2 velocity = bulletRb.velocity;
#endif

        Vector2 pushDir = SnapToCardinal(velocity);

        if (CheckPushableDirection(pushDir))
        {
            float logicalSpeed = velocity.magnitude / speedScale;
            int steps = Mathf.FloorToInt(Mathf.Max(0f, logicalSpeed - decayPerHit));

            if (steps > 0)
            {
                ProcessMoveSequence(pushDir, steps, bullet.gameObject);
            }
            else
            {
                if (consumeBullet) Destroy(bullet.gameObject);
            }
        }
        else
        {
            if (consumeBullet) Destroy(bullet.gameObject);
        }
    }

    private bool CheckPushableDirection(Vector2 pushDir)
    {
        if (_mirrorBlock.mirrorType == MirrorType.Square ||
            _mirrorBlock.mirrorType == MirrorType.Half)
        {
            return true;
        }

        if (_mirrorBlock.mirrorType == MirrorType.Triangle)
        {
            switch (_mirrorBlock.orientation)
            {
                case TriangleOrientation.UpRight: 
                    return pushDir == Vector2.right || pushDir == Vector2.up;
                case TriangleOrientation.UpLeft: 
                    return pushDir == Vector2.left || pushDir == Vector2.up;
                case TriangleOrientation.RightDown: 
                    return pushDir == Vector2.right || pushDir == Vector2.down;
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
        
        Vector2 halfExtents = GetHalfExtents(); 
        HoleTile targetHole = null; // 🕳️ 이동 도중 발견한 구멍 저장용

        // 1. 이동 시뮬레이션
        for (int i = 0; i < steps; i++)
        {
            Vector2Int nextCoord = gridCoord + dirInt;
            Vector3 nextWorldPos = GridToWorld(nextCoord);

            // (A) Blocker 확인
            if (GeneratorManager.Instance != null && GeneratorManager.Instance.IsBlockerTile(nextWorldPos)) break;

            // (B) ClearTile 확인
            if (StageManager.Instance != null && StageManager.Instance.IsClearTile(nextWorldPos)) break;

            // (C) 물리적 장애물 확인 (OverlapBox)
            Collider2D hit = Physics2D.OverlapBox(nextWorldPos, halfExtents * 2f, 0f, blockingMask);
            
            if (hit != null && hit.gameObject != gameObject && hit.gameObject != bulletGO)
            {
                HoleTile hole = hit.GetComponent<HoleTile>();

                // ✅ [수정됨] 구멍 처리 로직 변경
                if (hole != null)
                {
                    if (hole.IsEmpty())
                    {
                        // 1. 비어있는 구멍 발견 -> 이동 목표를 여기로 설정하고 멈춤
                        targetHole = hole;
                        gridCoord = nextCoord;
                        actualSteps++;
                        break; // 루프 종료 (더 이상 못 감)
                    }
                    // 2. 채워진 구멍 -> 그냥 바닥이므로 통과 (continue)
                }
                else
                {
                    // 3. 구멍이 아닌 진짜 벽/장애물 -> 멈춤
                    break;
                }
            }
            else
            {
                // 장애물 없음 -> 이동 확정
                gridCoord = nextCoord;
                actualSteps++;
            }
        }

        // 2. 실제 이동 및 최종 위치 처리
        if (actualSteps > 0)
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlayOneShot(FMODEvents.instance.MirrorPushed, transform.position);

            Vector3 finalPos = GridToWorld(gridCoord);
            MoveTo(finalPos);

            // ✅ [수정됨] 이동 후 구멍 처리
            // 루프 안에서 비어있는 구멍을 만났다면 여기서 처리
            if (targetHole != null)
            {
                targetHole.FillHole(); // 구멍 채우기
                Destroy(gameObject);   // 거울 파괴

                // 총알도 즉시 제거
                if (consumeBullet && bulletGO != null) 
                    Destroy(bulletGO);

                return; // 함수 종료
            }
        }

        // 구멍에 빠지지 않고 이동만 마쳤을 경우 총알 제거
        if (consumeBullet && bulletGO != null) Destroy(bulletGO);
    }

    // [NEW] 최종 위치에 구멍이 있는지 확인하고 처리하는 함수
    private void CheckFinalPositionForHole(Vector3 targetPos, GameObject bulletGO)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(targetPos);
        
        foreach (var hit in hits)
        {
            HoleTile hole = hit.GetComponent<HoleTile>();
            if (hole != null && hole.IsEmpty())
            {
                hole.FillHole();     // 구멍 채우기
                Destroy(gameObject); // 거울 파괴

                // 총알 즉시 제거
                if (consumeBullet && bulletGO != null)
                    Destroy(bulletGO);

                return; 
            }
        }
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