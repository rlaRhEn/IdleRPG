using UnityEngine;

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

