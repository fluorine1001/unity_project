using UnityEngine;

public class SpeedTile : FunctionalTile
{
    [Header("Speed Change Settings")]
    [Tooltip("논리 단위 속도 변화량 (예: +1 → 1단위 가속, -1 → 1단위 감속)")]
    public float speedDelta = 1f;

    protected override void Awake()
    {
        base.Awake();
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    protected override void OnBulletHit(BulletFire bullet)
    {
        if (bullet == null) return;

        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        Vector2 dir = rb.linearVelocity.normalized;

        // ✅ 현재 속도를 "논리 단위"로 환산
        float logicalSpeed = rb.linearVelocity.magnitude / GameConfig.SpeedScale;

        // ✅ 논리 단위로 증감
        float newLogicalSpeed = Mathf.Max(0.1f, logicalSpeed + speedDelta);

        // ✅ 다시 물리 단위로 환산 (한 번만 곱)
        rb.linearVelocity = dir * (newLogicalSpeed * GameConfig.SpeedScale);

        Debug.Log($"[SpeedTile] 속도 변경: {logicalSpeed:F2} → {newLogicalSpeed:F2} (실제 {rb.linearVelocity.magnitude:F2})");
    }

    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
