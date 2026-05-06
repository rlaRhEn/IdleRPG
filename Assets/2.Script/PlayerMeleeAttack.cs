using UnityEngine;
using System.Collections.Generic;

public class PlayerMeleeAttack : MonoBehaviour
{
    [SerializeField] private float damage = 2f;
    [SerializeField] private float attacksPerSecond = 1.6667f; // 0.6초마다 1회
    [SerializeField] private float baseAttackAnimationDuration = 0.6f; // 기본 애니메이션(속도 1배) 길이
    [SerializeField] private float minAnimationSpeed = 0.5f;
    [SerializeField] private float maxAnimationSpeed = 3.0f;
    [SerializeField] private float multiAttackRadius = 0.8f;
    [SerializeField] private int maxMultiAttackTargets = 6;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] [Range(0f, 1f)] private float criticalHitChance = 0.12f;
    [SerializeField] private float criticalDamageMultiplier = 2f;

    private float lastAttackTime = -Mathf.Infinity;
    private float defaultAnimatorSpeed = 1f;
    private bool cachedDefaultSpeed;
    private Transform currentAttackTarget;
    private readonly HashSet<MonsterHealth> hitMonsters = new HashSet<MonsterHealth>();

    float AttackInterval => 1f / Mathf.Max(attacksPerSecond, 0.01f);
    public float CurrentDamage => damage;
    public float CurrentAttacksPerSecond => attacksPerSecond;
    public float CurrentCriticalChance => criticalHitChance;

    void ApplyDamageToMonster(MonsterHealth monster)
    {
        if (monster == null) return;
        bool isCrit = Random.value < criticalHitChance;
        float finalDamage = isCrit ? damage * criticalDamageMultiplier : damage;
        monster.TakeDamage(finalDamage, isCrit);
    }

    void Awake()
    {
        if (enemyLayerMask.value == 0)
            enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    public bool BeginAttack(Transform target)
    {
        if (target == null) return false;

        if (Time.time - lastAttackTime < AttackInterval)
            return false;

        MonsterHealth monster = target.GetComponent<MonsterHealth>();
        if (monster == null || monster.IsDead) return false;

        currentAttackTarget = target;
        lastAttackTime = Time.time;
        return true;
    }

    // 공격 애니메이션 이벤트에서 호출: 실제 타격 프레임에만 데미지 적용
    public void OnAttackHitFrame()
    {
        if (currentAttackTarget == null) return;

        MonsterHealth mainTarget = currentAttackTarget.GetComponent<MonsterHealth>();
        if (mainTarget == null || mainTarget.IsDead)
        {
            currentAttackTarget = null;
            return;
        }

        hitMonsters.Clear();
        Vector2 center = currentAttackTarget.position;
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(center, multiAttackRadius, enemyLayerMask);

        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            if (hitMonsters.Count >= maxMultiAttackTargets) break;

            Collider2D collider = nearbyColliders[i];
            if (collider == null) continue;

            MonsterHealth monster = collider.GetComponent<MonsterHealth>();
            if (monster == null || monster.IsDead) continue;
            if (!hitMonsters.Add(monster)) continue;

            ApplyDamageToMonster(monster);
        }

        if (hitMonsters.Count == 0)
            ApplyDamageToMonster(mainTarget);

        currentAttackTarget = null;
    }

    public void ApplyAttackAnimationSpeed(Animator animator)
    {
        if (animator == null) return;

        if (!cachedDefaultSpeed)
        {
            defaultAnimatorSpeed = animator.speed;
            cachedDefaultSpeed = true;
        }

        // 공격 간격이 짧아질수록(공속 증가) 공격 애니메이션이 같은 비율로 짧아지게 배속 적용
        float attackAnimSpeed = baseAttackAnimationDuration / AttackInterval;
        animator.speed = Mathf.Clamp(attackAnimSpeed, minAnimationSpeed, maxAnimationSpeed);
    }

    public void ResetAnimationSpeed(Animator animator)
    {
        if (animator == null) return;
        animator.speed = cachedDefaultSpeed ? defaultAnimatorSpeed : 1f;
        currentAttackTarget = null;
    }

    public void ConfigureStats(float newDamage, float newAttacksPerSecond)
    {
        damage = Mathf.Max(0f, newDamage);
        attacksPerSecond = Mathf.Max(0.01f, newAttacksPerSecond);
    }

    public void AddDamage(float amount)
    {
        damage = Mathf.Max(0f, damage + amount);
    }

    public void AddAttackSpeed(float amount)
    {
        attacksPerSecond = Mathf.Max(0.01f, attacksPerSecond + amount);
    }

    public void AddCriticalHitChance(float amount)
    {
        criticalHitChance = Mathf.Clamp01(criticalHitChance + amount);
    }
}

