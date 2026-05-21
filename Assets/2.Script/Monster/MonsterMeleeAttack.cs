using UnityEngine;

/// <summary>
/// 몬스터 근접 공격: 공격 간격·크리·애니 속도, 플레이어 <see cref="PlayerHealth"/>에게 데미지. 애니 이벤트 없을 때를 대비한 지연 타격도 포함.
/// </summary>
public class MonsterMeleeAttack : MonoBehaviour
{
    [SerializeField] private float damage = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float criticalHitChance = 0.08f;
    [SerializeField] private float criticalDamageMultiplier = 1.5f;
    [SerializeField] private float attacksPerSecond = 1f;
    [SerializeField] private float baseAttackAnimationDuration = 1f;
    [SerializeField] private float minAnimationSpeed = 0.5f;
    [SerializeField] private float maxAnimationSpeed = 3f;
    [SerializeField] private float fallbackHitDelay = 0.18f;

    private float lastAttackTime = -Mathf.Infinity;
    private float defaultAnimatorSpeed = 1f;
    private bool cachedDefaultSpeed;
    private PlayerHealth currentTarget;
    private bool pendingHit;
    private float pendingHitTimer;

    private float AttackInterval => 1f / Mathf.Max(attacksPerSecond, 0.01f);

    public bool BeginAttack(PlayerHealth target)
    {
        if (target == null || target.IsDead) return false;
        if (Time.time - lastAttackTime < AttackInterval) return false;

        currentTarget = target;
        lastAttackTime = Time.time;
        pendingHit = true;
        pendingHitTimer = 0f;
        return true;
    }

    void Update()
    {
        if (!pendingHit) return;
        pendingHitTimer += Time.deltaTime;
        if (pendingHitTimer < Mathf.Max(0.02f, fallbackHitDelay)) return;

        // 모바일 빌드에서 애니메이션 이벤트가 누락돼도 데미지가 들어가도록 보장합니다.
        ApplyPendingHit();
    }

    public void OnAttackHitFrame()
    {
        ApplyPendingHit();
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
        pendingHit = false;
        pendingHitTimer = 0f;
    }

    public void SetDamage(float newDamage)
    {
        damage = Mathf.Max(0f, newDamage);
    }

    public void SetAttacksPerSecond(float newAttacksPerSecond)
    {
        attacksPerSecond = Mathf.Max(0.01f, newAttacksPerSecond);
    }

    void ApplyPendingHit()
    {
        pendingHit = false;
        pendingHitTimer = 0f;

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
}

