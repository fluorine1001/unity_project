using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BulletFire : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private UIManager uIManager;
    [SerializeField] private PlayerController player;
    [SerializeField] private Vector3 followOffset = new Vector3(0.18f, 0f, 0f);
    [SerializeField] private float followLerpSpeed = 20f;

    [Header("Projectile Settings")]
    [SerializeField] private GameObject bulletProjectilePrefab;
    [SerializeField] private float projectileSpeed = 6f;          
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private string wallTag = "Wall";

    [Header("Interaction")]
    [SerializeField] private bool destroyPushableWalls = true;

    [Header("Visual")]
    [SerializeField] private float spriteAngleOffset = 0f;

    [Header("Fire Cooldown")]
    [SerializeField] private float fireCooldown = 0.5f; // ⏳ 발사 후 재장전 시간
    private float fireCooldownTimer = 0f;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isProjectile;
    private Vector2 currentDirection = Vector2.down;
    private Transform playerTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (col != null)
            col.isTrigger = true;
    }

    void Start()
    {
        if (!isProjectile)
        {
            EnsurePlayerReference();
            SetupFollowerBody();
            SnapToPlayer();
        }
        else
        {
            PrepareProjectileBody();
        }
    }

    void Update()
    {
        // === [수정됨] 발사체(총알) 상태일 때 처리 ===
        if (isProjectile)
        {
            float logicalSpeed = rb.linearVelocity.magnitude / GameConfig.SpeedScale;
            if(logicalSpeed <= 0.5f){
                Destroy(gameObject);
                return;
            }

            // 2. 🔥 [신규] 매 프레임마다 현재 위치가 ClearTile 위인지 검사
            // 물리 충돌(Collider) 없이 좌표만으로 체크합니다.
            if (StageManager.Instance != null && StageManager.Instance.IsClearTile(transform.position))
            {
                // 필요하다면 소멸 이펙트 추가 가능
                Destroy(gameObject);
                return;
            }

            return; // 발사체는 여기서 Update 종료
        }

        // === 플레이어 따라다니는 상태 (발사 전) ===

        // 쿨다운 감소
        if (fireCooldownTimer > 0f)
            fireCooldownTimer -= Time.deltaTime;

        EnsurePlayerReference();
        if (playerTransform == null) return;

        Vector2 lookDir = ResolveLookDirection();
        currentDirection = lookDir;
        UpdateOrientation(currentDirection);

        Vector3 targetPos = playerTransform.position + CalculateFollowOffset(currentDirection);
        float lerpT = 1f - Mathf.Exp(-followLerpSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, lerpT);

        // 발사 입력 체크
        if (lookDir != Vector2.zero && IsFirePressed() && fireCooldownTimer <= 0f)
        {
            SpawnProjectile(lookDir.normalized);
            fireCooldownTimer = fireCooldown; 
        }
    }

    private void SpawnProjectile(Vector2 direction)
    {
        // 🔥 [추가] 발사 시 탄약 차감
        if (StageManager.Instance != null)
        {
            StageManager.Instance.UseAmmo();
        }
        
        AudioManager.instance.PlayOneShot(FMODEvents.instance.BulletLaunched, this.transform.position);

        GameObject prefab = bulletProjectilePrefab != null ? bulletProjectilePrefab : gameObject;
        GameObject clone = Instantiate(prefab, transform.position, Quaternion.identity);

        BulletFire bullet = clone.GetComponent<BulletFire>();
        if (bullet != null)
        {
            bullet.CopyProjectileSettingsFrom(this);
            bullet.ConfigureProjectile(direction);
        }
        else
        {
            Rigidbody2D cloneRb = clone.GetComponent<Rigidbody2D>();
            if (cloneRb != null)
                cloneRb.linearVelocity = direction * projectileSpeed * GameConfig.SpeedScale;
        }
    }

    private bool IsFirePressed()
    {
        // 1. 탄약이 없으면 발사 불가
        if (StageManager.Instance != null && !StageManager.Instance.HasAmmo())
        {
            return false; 
        }

        // 2. 플레이어가 Blocker 타일 위에 있으면 발사 불가
        if (playerTransform != null && GeneratorManager.Instance != null)
        {
            if (GeneratorManager.Instance.IsBlockerTile(playerTransform.position))
            {
                return false;
            }
        }

        // ❌ [삭제됨] UI 위 마우스 오버 체크
        // 스페이스바는 마우스 위치와 상관없이 발사되어야 하므로 이 체크는 제거합니다.
        // if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) ...

#if ENABLE_INPUT_SYSTEM
        // ✅ [수정] New Input System: 오직 스페이스바만 체크
        bool isSpacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        return isSpacePressed;
#else
        // ✅ [수정] Legacy Input Manager: 오직 스페이스바만 체크
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private void ConfigureProjectile(Vector2 direction)
    {
        isProjectile = true;
        currentDirection = direction.normalized;
        
        transform.SetParent(null);
        UpdateOrientation(currentDirection);
        PrepareProjectileBody();
    }

    private void PrepareProjectileBody()
    {
        if (rb == null) return;

        rb.isKinematic = false;
        rb.simulated = true;
        rb.linearVelocity = currentDirection * projectileSpeed * GameConfig.SpeedScale;

        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = true;
        }
    }

    private void SetupFollowerBody()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
            rb.simulated = false;
        }

        if (col != null)
            col.enabled = false;
    }

    private void EnsurePlayerReference()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        if (player != null && playerTransform == null)
            playerTransform = player.transform;
    }

    private Vector2 ResolveLookDirection()
    {
        if (player == null)
            return currentDirection == Vector2.zero ? Vector2.down : currentDirection;

        Vector2 lookDir = player.LastMoveDirection;
        if (lookDir == Vector2.zero)
            lookDir = currentDirection == Vector2.zero ? Vector2.down : currentDirection;

        return lookDir;
    }

    private void UpdateOrientation(Vector2 lookDir)
    {
        if (lookDir == Vector2.zero) return;

        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);
    }

    private void SnapToPlayer()
    {
        if (playerTransform == null) return;

        Vector2 lookDir = ResolveLookDirection();
        UpdateOrientation(lookDir);
        transform.position = playerTransform.position + CalculateFollowOffset(lookDir);
    }

    private void CopyProjectileSettingsFrom(BulletFire source)
    {
        projectileSpeed = source.projectileSpeed;
        
        wallLayers = source.wallLayers;
        wallTag = source.wallTag;
        destroyPushableWalls = source.destroyPushableWalls;
        followOffset = source.followOffset;
        followLerpSpeed = source.followLerpSpeed;
        player = source.player;
        playerTransform = source.playerTransform;
        bulletProjectilePrefab = source.bulletProjectilePrefab;
    }

    private Vector3 CalculateFollowOffset(Vector2 lookDir)
    {
        if (lookDir == Vector2.zero)
            return followOffset;

        Vector2 forward = lookDir.normalized;
        Vector2 perpendicular = new Vector2(-forward.y, forward.x);

        Vector3 offset = Vector3.zero;
        offset.x = forward.x * followOffset.x + perpendicular.x * followOffset.y;
        offset.y = forward.y * followOffset.x + perpendicular.y * followOffset.y;
        offset.z = followOffset.z;
        return offset;
    }

    void OnEnable()
    {
        if (!isProjectile)
        {
            EnsurePlayerReference();
            SnapToPlayer();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider2D collider)
    {
        if (!isProjectile || collider == null) return;

        // 🔥 [추가] ClearTile 위를 지나갈 수 없음 (즉시 소멸)
        if (StageManager.Instance != null && StageManager.Instance.IsClearTile(transform.position))
        {
            Destroy(gameObject);
            return;
        }

        if (collider.GetComponent<FunctionalTile>() != null)
            return;

        if (player != null && collider.transform.IsChildOf(player.transform))
            return;

        if (IsWall(collider.gameObject))
            Destroy(gameObject);
    }

    private bool IsWall(GameObject target)
    {
        if (target == null) return false;

        if (!string.IsNullOrEmpty(wallTag) && target.CompareTag(wallTag))
            return true;

        if (wallLayers.value != 0 && (wallLayers.value & (1 << target.layer)) != 0)
            return true;

        return false;
    }
}
