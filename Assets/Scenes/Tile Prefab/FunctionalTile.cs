using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class FunctionalTile : MonoBehaviour
{
    protected virtual void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // 모든 기능형 타일은 트리거로 동작
    }

    // 🔸 총알이 진입했을 때 호출
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        BulletFire bullet = other.GetComponent<BulletFire>();
        if (bullet != null)
        {
            OnBulletHit(bullet);
        }
    }

    // 🔸 구체 타일이 오버라이드해서 기능 구현
    protected abstract void OnBulletHit(BulletFire bullet);
}
