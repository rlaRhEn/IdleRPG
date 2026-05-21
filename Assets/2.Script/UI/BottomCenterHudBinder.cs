using UnityEngine;
using UnityEngine.UI;

/// <summary>인스펙터에 배치된 UI만 갱신합니다. 런타임 생성 없음.</summary>
public class BottomCenterHudBinder : MonoBehaviour
{
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMana playerMana;

    [Header("경험치")]
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private Text experienceText;

    [Header("체력")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Text healthText;

    [Header("마나")]
    [SerializeField] private Slider manaSlider;
    [SerializeField] private Text manaText;

    void OnEnable()
    {
        Subscribe();
        RefreshAll();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Subscribe()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged += OnExperienceChanged;
        if (playerHealth != null)
            playerHealth.HealthChanged += OnHealthChanged;
        if (playerMana != null)
            playerMana.ManaChanged += OnManaChanged;
    }

    void Unsubscribe()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged -= OnExperienceChanged;
        if (playerHealth != null)
            playerHealth.HealthChanged -= OnHealthChanged;
        if (playerMana != null)
            playerMana.ManaChanged -= OnManaChanged;
    }

    void OnExperienceChanged() => RefreshExperience();
    void OnHealthChanged(float current, float max) => RefreshHealth(current, max);
    void OnManaChanged(float current, float max) => RefreshMana(current, max);

    void RefreshAll()
    {
        RefreshExperience();
        if (playerHealth != null)
            RefreshHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        if (playerMana != null)
            RefreshMana(playerMana.CurrentMana, playerMana.MaxMana);
        else if (manaSlider != null)
        {
            manaSlider.minValue = 0f;
            manaSlider.maxValue = 1f;
            manaSlider.value = 1f;
            if (manaText != null)
                manaText.text = "MP --/--";
        }
    }

    void RefreshExperience()
    {
        if (playerProgression == null || experienceSlider == null) return;

        int required = Mathf.Max(1, playerProgression.CurrentRequiredExperience);
        int current = Mathf.Clamp(playerProgression.CurrentExperience, 0, required);
        experienceSlider.minValue = 0f;
        experienceSlider.maxValue = required;
        experienceSlider.value = current;

        if (experienceText != null)
            experienceText.text =
                $"LV.{playerProgression.CurrentLevel}  EXP {current}/{required}";
    }

    void RefreshHealth(float current, float max)
    {
        if (healthSlider == null || healthText == null) return;

        float safeMax = Mathf.Max(1f, max);
        float safeCurrent = Mathf.Clamp(current, 0f, safeMax);
        healthSlider.minValue = 0f;
        healthSlider.maxValue = safeMax;
        healthSlider.value = safeCurrent;
        healthText.text = $"HP {safeCurrent:0}/{safeMax:0}";
    }

    void RefreshMana(float current, float max)
    {
        if (manaSlider == null || manaText == null) return;

        float safeMax = Mathf.Max(1f, max);
        float safeCurrent = Mathf.Clamp(current, 0f, safeMax);
        manaSlider.minValue = 0f;
        manaSlider.maxValue = safeMax;
        manaSlider.value = safeCurrent;
        manaText.text = $"MP {safeCurrent:0}/{safeMax:0}";
    }
}
