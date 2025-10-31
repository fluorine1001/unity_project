using UnityEngine;

public class SpeedTile : FunctionalTile
{
    [Header("Speed Change Settings")]
    public float speedDelta = 1f;  // +면 가속, -면 감속
    protected override void OnBulletHit(BulletFire bullet)
    {
        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir = rb.linearVelocity.normalized;
            float oldSpeed = rb.linearVelocity.magnitude;
            float newSpeed = Mathf.Max(0.1f, oldSpeed + speedDelta);

            rb.linearVelocity = dir * newSpeed;

        }
    }
}
