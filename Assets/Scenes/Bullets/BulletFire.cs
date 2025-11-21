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
    [SerializeField] private float projectileSpeed = 6f;          // ⚙️ 논리 단위 속도 (GameConfig.SpeedScale이 곱해짐)
    [SerializeField] private float projectileLifetime = 4f;
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private string wallTag = "Wall";

    [Header("Interaction")]
    [SerializeField] private bool destroyPushableWalls = true;

    [Header("Visual")]
    [SerializeField] private float spriteAngleOffset = 0f;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isProjectile;
    private float lifeTimer;
    private Vector2 currentDirection = Vector2.down;
    private Transform playerTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (col != null)
        {
            col.isTrigger = true;
        }
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
        if (isProjectile)
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= projectileLifetime)
            {
                Destroy(gameObject);
            }
            return;
        }

        EnsurePlayerReference();
        if (playerTransform == null) return;

        Vector2 lookDir = ResolveLookDirection();
        currentDirection = lookDir;
        UpdateOrientation(currentDirection);

        Vector3 targetPos = playerTransform.position + CalculateFollowOffset(currentDirection);
        if (followLerpSpeed <= 0f)
        {
            transform.position = targetPos;
        }
        else
        {
            float lerpT = 1f - Mathf.Exp(-followLerpSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPos, lerpT);
        }

        if (lookDir != Vector2.zero && IsFirePressed())
        {
            SpawnProjectile(lookDir.normalized);
        }
    }

    private void SpawnProjectile(Vector2 direction)
    {
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
            {
                // ✅ 전역 속도 스케일 적용
                cloneRb.linearVelocity = direction * projectileSpeed * GameConfig.SpeedScale;
            }
        }
    }

    private bool IsFirePressed()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private void ConfigureProjectile(Vector2 direction)
    {
        isProjectile = true;
        currentDirection = direction.normalized;
        lifeTimer = 0f;
        transform.SetParent(null);
        UpdateOrientation(currentDirection);
        PrepareProjectileBody();
    }

    private void PrepareProjectileBody()
    {
        if (rb == null) return;

        rb.isKinematic = false;
        rb.simulated = true;

        // ✅ 전역 스케일 적용
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
        {
            col.enabled = false;
        }
    }

    private void EnsurePlayerReference()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
        }

        if (player != null && playerTransform == null)
        {
            playerTransform = player.transform;
        }
    }

    private Vector2 ResolveLookDirection()
    {
        if (player == null) return currentDirection == Vector2.zero ? Vector2.down : currentDirection;

        Vector2 lookDir = player.LastMoveDirection;
        if (lookDir == Vector2.zero)
        {
            lookDir = currentDirection == Vector2.zero ? Vector2.down : currentDirection;
        }

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
        projectileLifetime = source.projectileLifetime;
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
        {
            return followOffset;
        }

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

        // FunctionalTile 계열이면 Destroy하지 않음
        if (collider.GetComponent<FunctionalTile>() != null)
        {
            return;
        }

        if (player != null && collider.transform.IsChildOf(player.transform))
            return;

        PushableWall2D pushable = collider.GetComponentInParent<PushableWall2D>();
        if (pushable != null && destroyPushableWalls)
        {
            Destroy(pushable.gameObject);
            Destroy(gameObject);
            return;
        }

        if (IsWall(collider.gameObject))
        {
            Destroy(gameObject);
        }
    }

    private bool IsWall(GameObject target)
    {
        if (target == null) return false;

        if (!string.IsNullOrEmpty(wallTag) && target.CompareTag(wallTag))
        {
            return true;
        }

        if (wallLayers.value != 0 && (wallLayers.value & (1 << target.layer)) != 0)
        {
            return true;
        }

        return false;
    }
}
