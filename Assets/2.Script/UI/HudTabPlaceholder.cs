using UnityEngine;

/// <summary>HUD 탭 — Button의 On Click()에 <see cref="ToggleTabPanelFromButton"/>를 연결해 사용합니다.</summary>
public class HudTabPlaceholder : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;

    /// <summary>버튼 On Click 이벤트에서 호출. 대상 패널 활성 상태를 토글합니다.</summary>
    public void ToggleTabPanelFromButton()
    {
        TryResolveTargetPanel();
        if (targetPanel == null) return;

        bool show = !targetPanel.activeSelf;
        if (show)
            targetPanel.transform.SetAsLastSibling();

        targetPanel.SetActive(show);
    }

    void TryResolveTargetPanel()
    {
        if (targetPanel != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        string buttonName = gameObject.name;
        if (buttonName.Contains("Skill"))
        {
            Transform found = canvas.transform.Find("SkillTabPanel");
            if (found != null)
                targetPanel = found.gameObject;
            return;
        }

        if (buttonName.Contains("Ability"))
        {
            Transform found = canvas.transform.Find("AbilityUpgradePanel");
            if (found != null)
                targetPanel = found.gameObject;
            return;
        }

        if (buttonName.Contains("Equipment") || buttonName.Contains("Weapon"))
        {
            Transform found = canvas.transform.Find("EquipmentTabPanel");
            if (found != null)
                targetPanel = found.gameObject;
            return;
        }

        if (buttonName.Contains("Setting"))
        {
            Transform found = canvas.transform.Find("SettingsPanel");
            if (found != null)
                targetPanel = found.gameObject;
        }
    }
}
