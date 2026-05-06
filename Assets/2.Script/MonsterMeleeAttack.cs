using UnityEngine;

public class MonsterMeleeAttack : MonoBehaviour
{
    [SerializeField] private float damage = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float criticalHitChance = 0.08f;
    [SerializeField] private float criticalDamageMultiplier = 1.5f;
    [SerializeField] private float attacksPerSecond = 1f;
    [SerializeField] private float baseAttackAnimationDuration = 1f;
    [SerializeField] private float minAnimationSpeed = 0.5f;
    [SerializeField] private float maxAnimationSpeed = 3f;

    private float lastAttackTime = -Mathf.Infinity;
    private float defaultAnimatorSpeed = 1f;
    private bool cachedDefaultSpeed;
    private PlayerHealth currentTarget;

    private float AttackInterval => 1f / Mathf.Max(attacksPerSecond, 0.01f);

    public bool BeginAttack(PlayerHealth target)
    {
        if (target == null || target.IsDead) return false;
        if (Time.time - lastAttackTime < AttackInterval) return false;

        currentTarget = target;
        lastAttackTime = Time.time;
        return true;
    }

    public void OnAttackHitFrame()
    {
        if (currentTarget == null || currentTarget.IsDead)
        {
            currentTarget = null;
            return;
        }

        bool isCrit = Random.value < criticalHitChance;
        float finalDamage = isCrit ? damage * criticalDamageMultiplier : damage;
        currentTarget.TakeDamage(finalDamage, isCrit);
        currentTarget = null;
    }

    public void ApplyAttackAnimationSpeed(Animator animator)
    {
        if (animator == null) return;

        if (!cachedDefaultSpeed)
        {
            defaultAnimatorSpeed = animator.speed;
            cachedDefaultSpeed = true;
        }

        float speed = baseAttackAnimationDuration / AttackInterval;
        animator.speed = Mathf.Clamp(speed, minAnimationSpeed, maxAnimationSpeed);
    }

    public void ResetAnimationSpeed(Animator animator)
    {
        if (animator == null) return;
        animator.speed = cachedDefaultSpeed ? defaultAnimatorSpeed : 1f;
        currentTarget = null;
    }

    public void SetDamage(float newDamage)
    {
        damage = Mathf.Max(0f, newDamage);
    }

    public void SetAttacksPerSecond(float newAttacksPerSecond)
    {
        attacksPerSecond = Mathf.Max(0.01f, newAttacksPerSecond);
    }
}

