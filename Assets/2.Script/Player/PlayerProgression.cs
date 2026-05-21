using UnityEngine;
using System;

/// <summary>
/// 레벨·골드·경험치·영구 보너스(공격/체력) 관리, 레벨업 시 <see cref="PlayerCombatStatsData"/> 기반으로 <see cref="PlayerHealth"/>·<see cref="PlayerMeleeAttack"/> 스탯 반영.
/// </summary>
public class PlayerProgression : MonoBehaviour
{
    [SerializeField] private PlayerCombatStatsData statsData;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMeleeAttack playerMeleeAttack;
    [SerializeField] private int startLevel = 1;
    [SerializeField] private int startGold = 100;
    [SerializeField] private int startExperience = 0;
    [SerializeField] private int baseRequiredExperience = 20;
    [SerializeField] private float requiredExperienceGrowth = 1.35f;
    [SerializeField] private bool healToFullOnLevelUp = true;
    [SerializeField] private float bonusDamage;
    [SerializeField] private float bonusMaxHealth;

    private int currentLevel;
    private int currentGold;
    private int currentExperience;

    public int CurrentLevel => currentLevel;
    public int CurrentGold => currentGold;
    public int CurrentExperience => currentExperience;
    public int CurrentRequiredExperience => GetRequiredExperienceForLevel(currentLevel);
    public float BonusDamage => bonusDamage;
    public float BonusMaxHealth => bonusMaxHealth;
    public event Action ExperienceChanged;

    void Awake()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (playerMeleeAttack == null) playerMeleeAttack = GetComponent<PlayerMeleeAttack>();

        currentLevel = Mathf.Max(1, startLevel);
        currentGold = Mathf.Max(0, startGold);
        currentExperience = Mathf.Max(0, startExperience);

        ApplyCombatStats(healToFull: true);
        NotifyExperienceChanged();
    }

    public void AddRewards(int gold, int experience)
    {
        currentGold += Mathf.Max(0, gold);
        currentExperience += Mathf.Max(0, experience);
        TryLevelUp();
        NotifyExperienceChanged();
    }

    public bool TrySpendGold(int amount)
    {
        int spend = Mathf.Max(0, amount);
        if (spend <= 0) return true;
        if (currentGold < spend) return false;

        currentGold -= spend;
        NotifyExperienceChanged();
        return true;
    }

    public void SetProgress(int level, int gold, int experience)
    {
        currentLevel = Mathf.Max(1, level);
        currentGold = Mathf.Max(0, gold);
        currentExperience = Mathf.Max(0, experience);

        TryLevelUp();
        ApplyCombatStats(healToFull: false);
        NotifyExperienceChanged();
    }

    public void SetPermanentBonuses(float damageBonus, float maxHealthBonus, bool healToFull = false)
    {
        bonusDamage = Mathf.Max(0f, damageBonus);
        bonusMaxHealth = Mathf.Max(0f, maxHealthBonus);
        ApplyCombatStats(healToFull);
        NotifyExperienceChanged();
    }

    public void AddPermanentBonuses(float damageBonusAdd, float maxHealthBonusAdd, bool healToFullForHealth = false)
    {
        bonusDamage = Mathf.Max(0f, bonusDamage + Mathf.Max(0f, damageBonusAdd));
        bonusMaxHealth = Mathf.Max(0f, bonusMaxHealth + Mathf.Max(0f, maxHealthBonusAdd));
        ApplyCombatStats(healToFullForHealth && maxHealthBonusAdd > 0f);
        NotifyExperienceChanged();
    }

    public void ResetToInitialProgress()
    {
        currentLevel = Mathf.Max(1, startLevel);
        currentGold = Mathf.Max(0, startGold);
        currentExperience = Mathf.Max(0, startExperience);
        bonusDamage = 0f;
        bonusMaxHealth = 0f;
        ApplyCombatStats(healToFull: true);
        NotifyExperienceChanged();
    }

    void TryLevelUp()
    {
        int requiredExp = GetRequiredExperienceForLevel(currentLevel);
        bool leveledUp = false;

        while (currentExperience >= requiredExp)
        {
            currentExperience -= requiredExp;
            currentLevel++;
            leveledUp = true;
            requiredExp = GetRequiredExperienceForLevel(currentLevel);
        }

        if (leveledUp)
            ApplyCombatStats(healToFullOnLevelUp);
    }

    int GetRequiredExperienceForLevel(int level)
    {
        int clampedLevel = Mathf.Max(1, level);
        float scaled = baseRequiredExperience * Mathf.Pow(requiredExperienceGrowth, clampedLevel - 1);
        return Mathf.Max(1, Mathf.RoundToInt(scaled));
    }

    void ApplyCombatStats(bool healToFull)
    {
        if (statsData == null) return;

        int levelOffset = Mathf.Max(0, currentLevel - 1);
        float maxHealth = statsData.BaseMaxHealth + statsData.HealthPerLevel * levelOffset + bonusMaxHealth;
        float damage = statsData.BaseDamage + statsData.DamagePerLevel * levelOffset + bonusDamage;
        float attacksPerSecond = statsData.BaseAttacksPerSecond + statsData.AttacksPerSecondPerLevel * levelOffset;

        if (playerHealth != null)
            playerHealth.SetMaxHealth(maxHealth, healToFull);

        if (playerMeleeAttack != null)
            playerMeleeAttack.ConfigureStats(damage, attacksPerSecond);
    }

    void NotifyExperienceChanged()
    {
        ExperienceChanged?.Invoke();
    }
}

