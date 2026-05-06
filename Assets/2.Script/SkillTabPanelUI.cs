using UnityEngine;
using UnityEngine.UI;

/// <summary>스킬 탭 패널. 1번 힐링(보유), 2번 잠금, 그 외 미구현 슬롯만 표시합니다.</summary>
public class SkillTabPanelUI : MonoBehaviour
{
    const int SlotCount = 8;

    [SerializeField] private Text detailTitleText;
    [SerializeField] private Text detailBodyText;
    [SerializeField] private Transform gridRoot;

    int _selectedIndex;

    void OnEnable()
    {
        BindSlotButtons();
        SelectSlot(Mathf.Clamp(_selectedIndex, 0, SlotCount - 1));
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

    void SelectSlot(int index)
    {
        _selectedIndex = Mathf.Clamp(index, 0, SlotCount - 1);
        RefreshDetailTexts();
        RefreshSelectionBorders();
    }

    void RefreshDetailTexts()
    {
        if (detailTitleText == null || detailBodyText == null) return;

        switch (_selectedIndex)
        {
            case 0:
                detailTitleText.text = "힐링";
                detailBodyText.text =
                    "보유 효과: 일정 시간마다 체력을 조금 회복합니다.\n\n" +
                    "※ 오른쪽 하단 번개 슬롯은 추후 무기 전용 스킬로 전환 예정입니다.";
                break;
            case 1:
                detailTitleText.text = "???";
                detailBodyText.text = "잠금된 스킬입니다.";
                break;
            default:
                detailTitleText.text = "—";
                detailBodyText.text = "미구현 슬롯입니다.";
                break;
        }
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
}
