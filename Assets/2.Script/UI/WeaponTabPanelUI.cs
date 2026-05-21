using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무기 탭: 슬롯 선택·상세·구매/장착 버튼. <see cref="PlayerWeaponLoadout"/>과 골드 소모를 연동합니다.
/// </summary>
public class WeaponTabPanelUI : MonoBehaviour
{
    const int SlotCount = 20;
    const int PurchasableWeaponCount = 2;

    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerWeaponLoadout playerWeaponLoadout;
    [SerializeField] private Text detailTitleText;
    [SerializeField] private Text detailBodyText;
    [SerializeField] private Text detailCostText;
    [SerializeField] private Button actionButton;
    [SerializeField] private Transform gridRoot;

    int _selectedIndex;
    float _lastActionTime = -10f;
    const float ActionCooldown = 0.2f;

    void OnEnable()
    {
        Subscribe();
        BindSlotButtons();
        BindActionButton();
        SelectSlot(Mathf.Clamp(_selectedIndex, 0, SlotCount - 1));
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Subscribe()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged += RefreshActionButton;
        if (playerWeaponLoadout != null)
            playerWeaponLoadout.LoadoutChanged += OnLoadoutChanged;
    }

    void Unsubscribe()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged -= RefreshActionButton;
        if (playerWeaponLoadout != null)
            playerWeaponLoadout.LoadoutChanged -= OnLoadoutChanged;
    }

    void OnLoadoutChanged()
    {
        RefreshActionButton();
        RefreshLockOverlays();
    }

    void BindSlotButtons()
    {
        if (gridRoot == null) return;
        for (int i = 0; i < SlotCount; i++)
        {
            Transform cell = gridRoot.Find($"WeaponSlot_{i}");
            Button btn = cell != null ? cell.GetComponent<Button>() : null;
            if (btn == null) continue;
            int idx = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectSlot(idx));
        }
    }

    void BindActionButton()
    {
        if (actionButton == null) return;
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(TryBuyOrEquipSelectedWeapon);
    }

    void SelectSlot(int index)
    {
        _selectedIndex = Mathf.Clamp(index, 0, SlotCount - 1);
        RefreshDetailTexts();
        RefreshSelectionBorders();
        RefreshActionButton();
        RefreshLockOverlays();
    }

    void RefreshDetailTexts()
    {
        if (detailTitleText == null || detailBodyText == null) return;

        switch (_selectedIndex)
        {
            case 0:
                detailTitleText.text = "번개검";
                detailBodyText.text =
                    "무작위 적에게 자동으로 번개 공격을 한다.\n\n" +
                    "전투력 증가량: +45\n" +
                    "구매 시 즉시 장착";
                break;
            case 1:
                detailTitleText.text = "강화 완드";
                detailBodyText.text =
                    "강한 마력으로 전투를 보조한다.\n\n" +
                    "전투력 증가량: +80\n" +
                    "구매 시 즉시 장착";
                break;
            default:
                detailTitleText.text = "—";
                detailBodyText.text = "잠금 (추후 개방)";
                break;
        }
    }

    void RefreshActionButton()
    {
        if (actionButton == null) return;

        bool purchasable = _selectedIndex < PurchasableWeaponCount;
        bool owned = playerWeaponLoadout != null && playerWeaponLoadout.IsOwned(_selectedIndex);
        bool equipped = playerWeaponLoadout != null && playerWeaponLoadout.IsEquipped(_selectedIndex);
        int cost = playerWeaponLoadout != null ? playerWeaponLoadout.GetWeaponCost(_selectedIndex) : int.MaxValue;
        int gold = playerProgression != null ? playerProgression.CurrentGold : 0;

        if (detailCostText != null)
            detailCostText.text = purchasable ? $"구매 비용: Gold {cost}" : "구매 불가";

        if (!purchasable)
        {
            actionButton.gameObject.SetActive(false);
            return;
        }

        actionButton.gameObject.SetActive(true);
        if (!owned)
        {
            actionButton.interactable = gold >= cost;
            SetButtonText(actionButton, $"구매 후 장착 Gold {cost}");
        }
        else
        {
            actionButton.interactable = !equipped;
            SetButtonText(actionButton, equipped ? "장착됨" : "장착");
        }
    }

    void TryBuyOrEquipSelectedWeapon()
    {
        if (Time.unscaledTime - _lastActionTime < ActionCooldown) return;
        if (_selectedIndex >= PurchasableWeaponCount) return;
        if (playerProgression == null || playerWeaponLoadout == null) return;

        bool owned = playerWeaponLoadout.IsOwned(_selectedIndex);
        if (!owned)
        {
            int cost = playerWeaponLoadout.GetWeaponCost(_selectedIndex);
            if (!playerProgression.TrySpendGold(cost)) return;
            _lastActionTime = Time.unscaledTime;
            playerWeaponLoadout.BuyAndEquip(_selectedIndex);
        }
        else
        {
            _lastActionTime = Time.unscaledTime;
            playerWeaponLoadout.Equip(_selectedIndex);
        }

        RefreshActionButton();
    }

    void RefreshLockOverlays()
    {
        if (gridRoot == null) return;
        for (int i = 0; i < SlotCount; i++)
        {
            Transform slot = gridRoot.Find($"WeaponSlot_{i}");
            Transform overlay = slot != null ? slot.Find("LockOverlay") : null;
            if (overlay == null) continue;
            bool owned = playerWeaponLoadout != null && playerWeaponLoadout.IsOwned(i);
            overlay.gameObject.SetActive(i >= PurchasableWeaponCount || !owned);
        }
    }

    void RefreshSelectionBorders()
    {
        if (gridRoot == null) return;
        for (int i = 0; i < SlotCount; i++)
        {
            Transform cell = gridRoot.Find($"WeaponSlot_{i}");
            Outline outline = cell != null ? cell.GetComponent<Outline>() : null;
            if (outline != null)
                outline.enabled = i == _selectedIndex;
        }
    }

    static void SetButtonText(Button button, string value)
    {
        Transform label = button != null ? button.transform.Find("Label") : null;
        Text txt = label != null ? label.GetComponent<Text>() : null;
        if (txt != null) txt.text = value;
    }
}
