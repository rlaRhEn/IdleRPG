using UnityEngine;

public class MonsterMeleeDamageOnCollision : MonoBehaviour
{
    [SerializeField] private float damage = 0.1f;
    [SerializeField] private float hitCooldown = 0.4f;

    private float lastHitTime = -Mathf.Infinity;

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDealDamageFromCollider(collision.collider);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryDealDamageFromCollider(collision.collider);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryDealDamageFromCollider(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryDealDamageFromCollider(other);
    }

    void TryDealDamageFromCollider(Collider2D other)
    {
        if (Time.time - lastHitTime < hitCooldown) return;

        var playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead) return;

        // 몬스터가 죽어있으면(컴포넌트 제거/파괴 전) 추가 데미지 방지
        var monsterHealth = GetComponent<MonsterHealth>();
        if (monsterHealth != null && monsterHealth.IsDead) return;

        playerHealth.TakeDamage(damage);
        lastHitTime = Time.time;
    }

    public void SetDamage(float newDamage)
    {
        damage = Mathf.Max(0f, newDamage);
    }
}

