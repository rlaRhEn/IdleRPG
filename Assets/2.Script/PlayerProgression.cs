using UnityEngine;
using System;

public class PlayerProgression : MonoBehaviour
{
    [SerializeField] private PlayerCombatStatsData statsData;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMeleeAttack playerMeleeAttack;
    [SerializeField] private int startLevel = 1;
    [SerializeField] private int startGold = 0;
    [SerializeField] private int startExperience = 0;
    [SerializeField] private int baseRequiredExperience = 20;
    [SerializeField] private float requiredExperienceGrowth = 1.35f;
    [SerializeField] private bool healToFullOnLevelUp = true;

    private int currentLevel;
    private int currentGold;
    private int currentExperience;

    public int CurrentLevel => currentLevel;
    public int CurrentGold => currentGold;
    public int CurrentExperience => currentExperience;
    public int CurrentRequiredExperience => GetRequiredExperienceForLevel(currentLevel);
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
        float maxHealth = statsData.BaseMaxHealth + statsData.HealthPerLevel * levelOffset;
        float damage = statsData.BaseDamage + statsData.DamagePerLevel * levelOffset;
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

