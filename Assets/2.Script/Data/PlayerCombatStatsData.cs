using UnityEngine;

/// <summary>
/// 플레이어 기본 체력·공격력·공격 속도와 레벨당 성장치를 담는 ScriptableObject 데이터입니다.
/// <see cref="PlayerProgression"/>이 레벨에 맞게 전투 스탯을 계산할 때 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "PlayerCombatStatsData", menuName = "IdleRPG/Data/Player Combat Stats")]
public class PlayerCombatStatsData : ScriptableObject
{
    [Header("Base Stats")]
    [SerializeField] private float baseMaxHealth = 10f;
    [SerializeField] private float baseDamage = 2f;
    [SerializeField] private float baseAttacksPerSecond = 1.6667f;

    [Header("Per Level Growth")]
    [SerializeField] private float healthPerLevel = 1f;
    [SerializeField] private float damagePerLevel = 0.3f;
    [SerializeField] private float attacksPerSecondPerLevel = 0.05f;

    public float BaseMaxHealth => baseMaxHealth;
    public float BaseDamage => baseDamage;
    public float BaseAttacksPerSecond => baseAttacksPerSecond;
    public float HealthPerLevel => healthPerLevel;
    public float DamagePerLevel => damagePerLevel;
    public float AttacksPerSecondPerLevel => attacksPerSecondPerLevel;
}

