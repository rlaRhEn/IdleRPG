using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 주기적으로 주변 적에게 광역 피해를 주는 패시브형 스킬. 플레이어 근접 데미지를 배율로 스케일합니다.
/// </summary>
public class AutoAoeSkillItem : MonoBehaviour
{
    [SerializeField] private bool unlocked = true;
    [SerializeField] private float castInterval = 5f;
    [SerializeField] private float attackRadius = 2.4f;
    [SerializeField] private int maxTargets = 8;
    [SerializeField] private float damageMultiplier = 1.6f;
    [SerializeField] private float fallbackDamage = 3f;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] [Range(0f, 1f)] private float criticalChance = 0.15f;
    [SerializeField] private float criticalDamageMultiplier = 2f;

    private float castTimer;
    private PlayerHealth playerHealth;
    private PlayerMeleeAttack playerMeleeAttack;
    private readonly HashSet<MonsterHealth> hitMonsters = new HashSet<MonsterHealth>();

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerMeleeAttack = GetComponent<PlayerMeleeAttack>();

        if (enemyLayerMask.value == 0)
            enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    void Update()
    {
        if (!unlocked) return;
        if (playerHealth != null && playerHealth.IsDead) return;

        castTimer += Time.deltaTime;
        if (castTimer < Mathf.Max(0.1f, castInterval)) return;

        castTimer = 0f;
        CastAutoAoe();
    }

    void CastAutoAoe()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, attackRadius, enemyLayerMask);
        if (nearbyColliders == null || nearbyColliders.Length == 0) return;

        float baseDamage = playerMeleeAttack != null ? playerMeleeAttack.CurrentDamage : fallbackDamage;
        float scaledDamage = Mathf.Max(0.1f, baseDamage * damageMultiplier);

        hitMonsters.Clear();
        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            if (hitMonsters.Count >= Mathf.Max(1, maxTargets)) break;

            Collider2D collider = nearbyColliders[i];
            if (collider == null) continue;

            MonsterHealth monster = collider.GetComponent<MonsterHealth>();
            if (monster == null || monster.IsDead) continue;
            if (!hitMonsters.Add(monster)) continue;

            bool isCritical = Random.value < criticalChance;
            float finalDamage = isCritical ? scaledDamage * criticalDamageMultiplier : scaledDamage;
            monster.TakeDamage(finalDamage, isCritical);
        }
    }
}
