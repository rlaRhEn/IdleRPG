using System;
using UnityEngine;

/// <summary>
/// 플레이어 체력(현재/최대), 피격·치유·사망 처리. 피격 시 떠오르는 데미지 텍스트와 연동하고 사망 시 조작·충돌을 끕니다.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private bool destroyOnDeath = true;

    private float currentHealth;
    private bool isDead;

    public bool IsDead => isDead;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public event Action<float, float> HealthChanged;

    void Awake()
    {
        currentHealth = maxHealth;
        NotifyHealthChanged();
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, false);
    }

    public void TakeDamage(float damage, bool isCritical)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        float appliedDamage = Mathf.Min(damage, currentHealth);
        currentHealth -= damage;
        CombatTextKind kind = isCritical ? CombatTextKind.PlayerHitCritical : CombatTextKind.PlayerHitNormal;
        DamageTextPool.Show(transform.position, appliedDamage, kind);
        NotifyHealthChanged();
        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        float appliedHeal = Mathf.Min(amount, maxHealth - currentHealth);
        if (appliedHeal <= 0f) return;

        currentHealth += appliedHeal;
        DamageTextPool.Show(transform.position, appliedHeal, CombatTextKind.Heal);
        NotifyHealthChanged();
    }

    public void SetMaxHealth(float newMaxHealth, bool healToFull)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        if (healToFull)
        {
            currentHealth = maxHealth;
            isDead = false;
            NotifyHealthChanged();
            return;
        }

        currentHealth = Mathf.Min(currentHealth, maxHealth);
        NotifyHealthChanged();
    }

    void Die()
    {
        isDead = true;

        // 플레이어 로직/충돌을 정지시킴.
        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (destroyOnDeath)
            Destroy(gameObject);
    }

    void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }
}

