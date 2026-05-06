using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오른쪽 아래 스킬 슬롯 UI(3칸) 중 1번 슬롯(번개)의 쿨다운 표시를 담당합니다.
/// 하이어라키에 배치된 UI만 갱신하며, 런타임 생성은 하지 않습니다.
/// </summary>
public class SkillQuickSlotsUI : MonoBehaviour
{
    [SerializeField] private AutoLightningStrikeSkill lightningSkill;
    [SerializeField] private bool cooldownClockwise = true;
    [SerializeField] private int cooldownFillOrigin = 2; // Top

    [Header("슬롯 1(번개)")]
    [SerializeField] private Image slot1Icon;
    [SerializeField] private Image slot1CooldownFillImage;
    [SerializeField] private Image slot1CooldownMask;
    [SerializeField] private Text slot1CooldownText;

    void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Subscribe()
    {
        if (lightningSkill != null)
            lightningSkill.CooldownChanged += OnCooldownChanged;
    }

    void Unsubscribe()
    {
        if (lightningSkill != null)
            lightningSkill.CooldownChanged -= OnCooldownChanged;
    }

    void OnCooldownChanged(float remaining, float duration)
    {
        RefreshCooldown(remaining, duration);
    }

    void Refresh()
    {
        if (slot1Icon != null)
            slot1Icon.color = Color.white;

        if (lightningSkill == null)
        {
            RefreshCooldown(0f, 1f);
            if (slot1CooldownText != null)
                slot1CooldownText.text = "N/A";
            return;
        }

        RefreshCooldown(lightningSkill.CooldownRemaining, lightningSkill.CooldownTime);
    }

    void RefreshCooldown(float remaining, float duration)
    {
        float safeDuration = Mathf.Max(0.1f, duration);
        float ratio = Mathf.Clamp01(1f - (remaining / safeDuration));
        Image fillTarget = slot1CooldownFillImage != null ? slot1CooldownFillImage : slot1Icon;

        if (fillTarget != null)
        {
            fillTarget.type = Image.Type.Filled;
            fillTarget.fillMethod = Image.FillMethod.Radial360;
            fillTarget.fillOrigin = Mathf.Clamp(cooldownFillOrigin, 0, 3);
            fillTarget.fillClockwise = cooldownClockwise;
            fillTarget.fillAmount = ratio;
            fillTarget.enabled = true;
        }

        if (slot1CooldownMask != null && slot1CooldownMask != fillTarget)
        {
            slot1CooldownMask.fillAmount = ratio;
            slot1CooldownMask.enabled = ratio > 0.001f;
        }

        if (slot1CooldownText == null) return;

        if (ratio <= 0.001f)
        {
            slot1CooldownText.text = string.Empty;
            return;
        }

        slot1CooldownText.text = remaining.ToString("0.0");
    }
}
