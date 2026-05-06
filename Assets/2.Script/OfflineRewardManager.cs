using System;
using UnityEngine;

public class OfflineRewardManager : MonoBehaviour
{
    public event Action<int, int, int> OfflineRewardApplied;

    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerSkillLoadout playerSkillLoadout;
    [SerializeField] private PlayerWeaponLoadout playerWeaponLoadout;
    [SerializeField] private bool enableOfflineRewards = true;
    [SerializeField] private float maxOfflineHours = 8f;
    [SerializeField] private float baseGoldPerSecond = 0.25f;
    [SerializeField] private float baseExperiencePerSecond = 0.2f;
    [SerializeField] private float perLevelRewardGrowth = 0.12f;
    [SerializeField] private float autoSaveInterval = 10f;

    private const string KeyLevel = "idle_progress_level";
    private const string KeyGold = "idle_progress_gold";
    private const string KeyExperience = "idle_progress_exp";
    private const string KeyBonusDamage = "idle_bonus_damage";
    private const string KeyBonusHealth = "idle_bonus_health";
    private const string KeySkillOwned = "idle_skill_owned";
    private const string KeySkillEquipped = "idle_skill_equipped";
    private const string KeyWeaponOwned = "idle_weapon_owned";
    private const string KeyWeaponEquipped = "idle_weapon_equipped";
    private const string KeyLastUtcSeconds = "idle_progress_last_utc";

    private float saveTimer;
    private int pendingOfflineSeconds;
    private int pendingOfflineGold;
    private int pendingOfflineExperience;

    void Awake()
    {
        if (playerProgression == null)
            playerProgression = GetComponent<PlayerProgression>();
        if (playerSkillLoadout == null)
            playerSkillLoadout = GetComponent<PlayerSkillLoadout>();
        if (playerWeaponLoadout == null)
            playerWeaponLoadout = GetComponent<PlayerWeaponLoadout>();
    }

    void Start()
    {
        LoadProgress();
        ApplyOfflineRewards();
        SaveProgress();
    }

    void Update()
    {
        if (playerProgression == null) return;

        saveTimer += Time.deltaTime;
        if (saveTimer < Mathf.Max(1f, autoSaveInterval)) return;

        saveTimer = 0f;
        SaveProgress();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveProgress();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveProgress();
    }

    void OnApplicationQuit()
    {
        SaveProgress();
    }

    void LoadProgress()
    {
        if (playerProgression == null) return;

        int savedLevel = PlayerPrefs.GetInt(KeyLevel, playerProgression.CurrentLevel);
        int savedGold = PlayerPrefs.GetInt(KeyGold, playerProgression.CurrentGold);
        int savedExperience = PlayerPrefs.GetInt(KeyExperience, playerProgression.CurrentExperience);
        playerProgression.SetProgress(savedLevel, savedGold, savedExperience);
        float savedBonusDamage = PlayerPrefs.GetFloat(KeyBonusDamage, playerProgression.BonusDamage);
        float savedBonusHealth = PlayerPrefs.GetFloat(KeyBonusHealth, playerProgression.BonusMaxHealth);
        playerProgression.SetPermanentBonuses(savedBonusDamage, savedBonusHealth, false);

        if (playerSkillLoadout != null)
        {
            string skillOwned = PlayerPrefs.GetString(KeySkillOwned, string.Empty);
            string skillEquipped = PlayerPrefs.GetString(KeySkillEquipped, string.Empty);
            playerSkillLoadout.LoadState(skillOwned, skillEquipped);
        }

        if (playerWeaponLoadout != null)
        {
            string weaponOwned = PlayerPrefs.GetString(KeyWeaponOwned, string.Empty);
            int weaponEquipped = PlayerPrefs.GetInt(KeyWeaponEquipped, -1);
            playerWeaponLoadout.LoadState(weaponOwned, weaponEquipped);
        }
    }

    void SaveProgress()
    {
        if (playerProgression == null) return;

        PlayerPrefs.SetInt(KeyLevel, playerProgression.CurrentLevel);
        PlayerPrefs.SetInt(KeyGold, playerProgression.CurrentGold);
        PlayerPrefs.SetInt(KeyExperience, playerProgression.CurrentExperience);
        PlayerPrefs.SetFloat(KeyBonusDamage, playerProgression.BonusDamage);
        PlayerPrefs.SetFloat(KeyBonusHealth, playerProgression.BonusMaxHealth);
        if (playerSkillLoadout != null)
        {
            PlayerPrefs.SetString(KeySkillOwned, playerSkillLoadout.GetOwnedStateString());
            PlayerPrefs.SetString(KeySkillEquipped, playerSkillLoadout.GetEquippedStateString());
        }
        if (playerWeaponLoadout != null)
        {
            PlayerPrefs.SetString(KeyWeaponOwned, playerWeaponLoadout.GetOwnedStateString());
            PlayerPrefs.SetInt(KeyWeaponEquipped, playerWeaponLoadout.EquippedWeaponIndex);
        }
        PlayerPrefs.SetString(KeyLastUtcSeconds, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        PlayerPrefs.Save();
    }

    void ApplyOfflineRewards()
    {
        if (!enableOfflineRewards) return;
        if (playerProgression == null) return;

        string rawLastUtcSeconds = PlayerPrefs.GetString(KeyLastUtcSeconds, string.Empty);
        if (string.IsNullOrEmpty(rawLastUtcSeconds)) return;
        if (!long.TryParse(rawLastUtcSeconds, out long lastUtcSeconds)) return;

        long nowUtcSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsedSeconds = Mathf.Max(0, (int)(nowUtcSeconds - lastUtcSeconds));
        int cappedSeconds = Mathf.Min(elapsedSeconds > int.MaxValue ? int.MaxValue : (int)elapsedSeconds, Mathf.RoundToInt(maxOfflineHours * 3600f));
        if (cappedSeconds <= 0) return;

        int level = Mathf.Max(1, playerProgression.CurrentLevel);
        float growthScale = 1f + (level - 1) * perLevelRewardGrowth;
        float goldPerSecond = baseGoldPerSecond * growthScale;
        float expPerSecond = baseExperiencePerSecond * growthScale;

        int rewardGold = Mathf.FloorToInt(goldPerSecond * cappedSeconds);
        int rewardExperience = Mathf.FloorToInt(expPerSecond * cappedSeconds);
        if (rewardGold <= 0 && rewardExperience <= 0) return;

        playerProgression.AddRewards(rewardGold, rewardExperience);
        pendingOfflineSeconds = cappedSeconds;
        pendingOfflineGold = rewardGold;
        pendingOfflineExperience = rewardExperience;
        OfflineRewardApplied?.Invoke(cappedSeconds, rewardGold, rewardExperience);
    }

    public bool TryConsumePendingReward(out int offlineSeconds, out int rewardGold, out int rewardExperience)
    {
        offlineSeconds = pendingOfflineSeconds;
        rewardGold = pendingOfflineGold;
        rewardExperience = pendingOfflineExperience;

        bool hasReward = pendingOfflineSeconds > 0 && (pendingOfflineGold > 0 || pendingOfflineExperience > 0);
        if (!hasReward) return false;

        pendingOfflineSeconds = 0;
        pendingOfflineGold = 0;
        pendingOfflineExperience = 0;
        return true;
    }

    public void ResetAllProgressForTest()
    {
        if (playerProgression != null)
            playerProgression.ResetToInitialProgress();
        if (playerSkillLoadout != null)
            playerSkillLoadout.ResetLoadoutForTest();
        if (playerWeaponLoadout != null)
            playerWeaponLoadout.ResetLoadoutForTest();

        PlayerPrefs.DeleteKey(KeyLevel);
        PlayerPrefs.DeleteKey(KeyGold);
        PlayerPrefs.DeleteKey(KeyExperience);
        PlayerPrefs.DeleteKey(KeyBonusDamage);
        PlayerPrefs.DeleteKey(KeyBonusHealth);
        PlayerPrefs.DeleteKey(KeySkillOwned);
        PlayerPrefs.DeleteKey(KeySkillEquipped);
        PlayerPrefs.DeleteKey(KeyWeaponOwned);
        PlayerPrefs.DeleteKey(KeyWeaponEquipped);
        PlayerPrefs.SetString(KeyLastUtcSeconds, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        PlayerPrefs.Save();

        pendingOfflineSeconds = 0;
        pendingOfflineGold = 0;
        pendingOfflineExperience = 0;
    }
}
