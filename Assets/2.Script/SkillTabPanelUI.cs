using UnityEngine;
using UnityEngine.UI;

public class SkillTabPanelUI : MonoBehaviour
{
    const int SlotCount = 20;
    const int PurchasableSkillCount = 2;

    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerSkillLoadout playerSkillLoadout;
    [SerializeField] private Text detailTitleText;
    [SerializeField] private Text detailBodyText;
    [SerializeField] private Text detailCostText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Transform gridRoot;

    int _selectedIndex;
    float _lastActionTime = -10f;
    const float ActionCooldown = 0.2f;

    void OnEnable()
    {
        Subscribe();
        BindSlotButtons();
        BindActionButtons();
        SelectSlot(Mathf.Clamp(_selectedIndex, 0, SlotCount - 1));
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Subscribe()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged += RefreshActionButtons;
        if (playerSkillLoadout != null)
        {
            playerSkillLoadout.LoadoutChanged += RefreshActionButtons;
            playerSkillLoadout.LoadoutChanged += RefreshLockOverlays;
        }
    }

    void Unsubscribe()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged -= RefreshActionButtons;
        if (playerSkillLoadout != null)
        {
            playerSkillLoadout.LoadoutChanged -= RefreshActionButtons;
            playerSkillLoadout.LoadoutChanged -= RefreshLockOverlays;
        }
    }

    void BindSlotButtons()
    {
        if (gridRoot == null) return;

        for (int i = 0; i < SlotCount; i++)
        {
            Transform cell = gridRoot.Find($"SkillSlot_{i}");
            if (cell == null) continue;
            Button btn = cell.GetComponent<Button>();
            if (btn == null) continue;

            int idx = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectSlot(idx));
        }
    }

    void BindActionButtons()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(TryBuySelectedSkill);
        }

        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(TryEquipSelectedSkill);
        }
    }

    void SelectSlot(int index)
    {
        _selectedIndex = Mathf.Clamp(index, 0, SlotCount - 1);
        RefreshDetailTexts();
        RefreshSelectionBorders();
        RefreshActionButtons();
        RefreshLockOverlays();
    }

    void RefreshDetailTexts()
    {
        if (detailTitleText == null || detailBodyText == null) return;

        switch (_selectedIndex)
        {
            case 0:
                detailTitleText.text = "힐링";
                detailBodyText.text = "자동으로 일정 양의 체력을 회복합니다.";
                break;
            case 1:
                detailTitleText.text = "아이스 버스트";
                detailBodyText.text = "제일 먼 적 여러명에게 강한 피해를 줍니다.";
                break;
            default:
                detailTitleText.text = "—";
                detailBodyText.text = "잠금";
                break;
        }
    }

    void RefreshActionButtons()
    {
        bool purchasable = _selectedIndex < PurchasableSkillCount;
        bool owned = playerSkillLoadout != null && playerSkillLoadout.IsOwned(_selectedIndex);
        bool equipped = playerSkillLoadout != null && playerSkillLoadout.IsEquipped(_selectedIndex);
        int cost = GetCost(_selectedIndex);
        int gold = playerProgression != null ? playerProgression.CurrentGold : 0;

        if (detailCostText != null)
            detailCostText.text = purchasable ? $"구매 비용: Gold {cost}" : "구매 불가";

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(purchasable && !owned);
            buyButton.interactable = purchasable && !owned && gold >= cost;
            SetButtonText(buyButton, $"구매 Gold {cost}");
        }

        if (equipButton != null)
        {
            bool show = purchasable && owned;
            equipButton.gameObject.SetActive(show);
            equipButton.interactable = show && !equipped;
            SetButtonText(equipButton, equipped ? "장착됨" : "장착");
        }
    }

    void RefreshLockOverlays()
    {
        if (gridRoot == null) return;
        for (int i = 0; i < SlotCount; i++)
        {
            Transform slot = gridRoot.Find($"SkillSlot_{i}");
            Transform overlay = slot != null ? slot.Find("LockOverlay") : null;
            if (overlay == null) continue;

            bool owned = playerSkillLoadout != null && playerSkillLoadout.IsOwned(i);
            overlay.gameObject.SetActive(i >= PurchasableSkillCount || !owned);
        }
    }

    void TryBuySelectedSkill()
    {
        if (Time.unscaledTime - _lastActionTime < ActionCooldown) return;
        if (_selectedIndex >= PurchasableSkillCount) return;
        if (playerProgression == null || playerSkillLoadout == null) return;
        if (playerSkillLoadout.IsOwned(_selectedIndex)) return;

        int cost = GetCost(_selectedIndex);
        if (!playerProgression.TrySpendGold(cost)) return;
        _lastActionTime = Time.unscaledTime;
        playerSkillLoadout.UnlockSkill(_selectedIndex);
        RefreshActionButtons();
        RefreshLockOverlays();
    }

    void TryEquipSelectedSkill()
    {
        if (Time.unscaledTime - _lastActionTime < ActionCooldown) return;
        if (_selectedIndex >= PurchasableSkillCount) return;
        if (playerSkillLoadout == null) return;
        _lastActionTime = Time.unscaledTime;
        playerSkillLoadout.EquipSkillToFirstAvailable(_selectedIndex);
        RefreshActionButtons();
    }

    void RefreshSelectionBorders()
    {
        if (gridRoot == null) return;
        for (int i = 0; i < SlotCount; i++)
        {
            Transform cell = gridRoot.Find($"SkillSlot_{i}");
            Outline outline = cell != null ? cell.GetComponent<Outline>() : null;
            if (outline != null)
                outline.enabled = i == _selectedIndex;
        }
    }

    int GetCost(int skillIndex) => skillIndex == 0 ? 20 : 35;

    static void SetButtonText(Button button, string value)
    {
        Transform label = button != null ? button.transform.Find("Label") : null;
        Text txt = label != null ? label.GetComponent<Text>() : null;
        if (txt != null) txt.text = value;
    }
}
