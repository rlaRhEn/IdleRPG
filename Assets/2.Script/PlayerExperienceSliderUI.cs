using UnityEngine;
using UnityEngine.UI;

/// <summary>예전처럼 경험치 슬라이더 한 줄만 쓸 때용. 새 레이아웃에서는 <see cref="BottomCenterHudBinder"/>를 씁니다.</summary>
public class PlayerExperienceSliderUI : MonoBehaviour
{
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private Text experienceText;

    void OnEnable()
    {
        if (Application.isPlaying && playerProgression != null)
            playerProgression.ExperienceChanged += RefreshSlider;

        RefreshSlider();
    }

    void OnDisable()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged -= RefreshSlider;
    }

    void RefreshSlider()
    {
        if (playerProgression == null || experienceSlider == null) return;

        int required = Mathf.Max(1, playerProgression.CurrentRequiredExperience);
        int current = Mathf.Clamp(playerProgression.CurrentExperience, 0, required);

        experienceSlider.minValue = 0f;
        experienceSlider.maxValue = required;
        experienceSlider.value = current;

        if (experienceText != null)
            experienceText.text = $"Lv {playerProgression.CurrentLevel}  XP {current}/{required}";
    }
}
