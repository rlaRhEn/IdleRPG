using UnityEngine;
using System;

/// <summary>
/// 몬스터 체력·피격·보상 지급·풀 반환/파괴. 스포너가 보상·풀 콜백을 설정하고 UI 데미지 숫자와 연동합니다.
/// </summary>
public class MonsterHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private bool destroyWhenDead = false;

    private float currentHealth;
    private bool isDead;
    private bool autoFinalizeDeath = true;
    private bool isFinalized;
    private Action<MonsterHealth> returnToPool;
    private PlayerProgression rewardReceiver;
    private int rewardGold;
    private int rewardExperience;

    public bool IsDead => isDead;
    public event Action Died;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void OnEnable()
    {
        // 풀에서 재활성화될 때 상태 초기화
        currentHealth = maxHealth;
        isDead = false;
        isFinalized = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
    }

    public void SetPoolReturnAction(Action<MonsterHealth> onReturnToPool)
    {
        returnToPool = onReturnToPool;
    }

    public void SetAutoFinalizeDeath(bool enabled)
    {
        autoFinalizeDeath = enabled;
    }

    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = maxHealth;
        isDead = false;
    }

    public void SetRewardInfo(PlayerProgression receiver, int gold, int experience)
    {
        rewardReceiver = receiver;
        rewardGold = Mathf.Max(0, gold);
        rewardExperience = Mathf.Max(0, experience);
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
        CombatTextKind kind = isCritical ? CombatTextKind.MonsterHitCritical : CombatTextKind.MonsterHitNormal;
        DamageTextPool.Show(transform.position, appliedDamage, kind);
        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        isDead = true;

        // 죽은 몬스터는 즉시 움직임/충돌을 막음.
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Died?.Invoke();
        if (rewardReceiver != null)
            rewardReceiver.AddRewards(rewardGold, rewardExperience);

        if (autoFinalizeDeath)
            FinalizeDeath();
    }

    public void FinalizeDeath()
    {
        if (isFinalized) return;
        isFinalized = true;

        if (!destroyWhenDead && returnToPool != null)
        {
            returnToPool.Invoke(this);
            return;
        }

        Destroy(gameObject);
    }
}

