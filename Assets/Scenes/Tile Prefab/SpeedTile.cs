using UnityEngine;

public class SpeedTile : FunctionalTile
{
    [Header("Speed Change Settings")]
    public float speedDelta = 1f;  // +면 가속, -면 감속

    protected override void OnBulletHit(BulletFire bullet)
    {
        // 기본 동작: 총알의 속도를 speedDelta만큼 변경
        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir = rb.linearVelocity.normalized;
            float newSpeed = Mathf.Max(0.1f, rb.linearVelocity.magnitude + speedDelta);
            rb.linearVelocity = dir * newSpeed;
        }
    }
}
