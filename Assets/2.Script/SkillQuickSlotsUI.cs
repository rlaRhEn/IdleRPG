using UnityEngine;
using UnityEngine.UI;

public class SkillQuickSlotsUI : MonoBehaviour
{
    [SerializeField] private PlayerSkillLoadout playerSkillLoadout;
    [SerializeField] private Transform skillGridRoot;
    [SerializeField] private Transform slot1Root;
    [SerializeField] private Transform slot2Root;
    [SerializeField] private Transform slot3Root;

    void OnEnable()
    {
        TryResolveReferences();
        Subscribe();
        RefreshAll();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Update()
    {
        // 인스펙터 바인딩이 비어 있어도 플레이 중 자동 복구해서 fillAmount가 계속 갱신되게 유지.
        TryResolveReferences();
        RefreshAll();
    }

    void Subscribe()
    {
        if (playerSkillLoadout == null) return;
        playerSkillLoadout.LoadoutChanged += RefreshAll;
        playerSkillLoadout.CooldownChanged += RefreshAll;
    }

    void Unsubscribe()
    {
        if (playerSkillLoadout == null) return;
        playerSkillLoadout.LoadoutChanged -= RefreshAll;
        playerSkillLoadout.CooldownChanged -= RefreshAll;
    }

    void RefreshAll()
    {
        RefreshSlot(slot1Root, 0);
        RefreshSlot(slot2Root, 1);
        RefreshSlot(slot3Root, 2);
    }

    void RefreshSlot(Transform slotRoot, int slotIndex)
    {
        if (slotRoot == null) return;

        Image icon = slotRoot.Find("Icon")?.GetComponent<Image>();
        Image mask = slotRoot.Find("CooldownMask")?.GetComponent<Image>();
        Text cooldownText = slotRoot.Find("CooldownText")?.GetComponent<Text>();
        Text plusText = slotRoot.Find("PlusText")?.GetComponent<Text>();

        int skillId = playerSkillLoadout != null ? playerSkillLoadout.GetEquippedSkillId(slotIndex) : -1;
        bool equipped = skillId >= 0;

        if (plusText != null)
            plusText.gameObject.SetActive(!equipped);

        if (icon != null)
        {
            icon.enabled = true;
            if (!equipped)
            {
                icon.sprite = null;
                icon.color = new Color(0f, 0f, 0f, 0f);
                icon.type = Image.Type.Simple;
            }
            else
            {
                // 스킬 탭 아이콘 스프라이트를 그대로 가져와 퀵슬롯에 반영
                TryCopySkillIconSprite(skillId, icon);
                icon.color = Color.white;
                icon.preserveAspect = true;
                icon.type = Image.Type.Filled;
                icon.fillMethod = Image.FillMethod.Radial360;
                icon.fillOrigin = 2;
                icon.fillClockwise = false;
            }
        }

        float remaining = 0f;
        float duration = 1f;
        if (equipped && playerSkillLoadout != null)
        {
            remaining = playerSkillLoadout.GetCooldownRemaining(skillId);
            duration = playerSkillLoadout.GetCooldown(skillId);
        }

        float ratio = Mathf.Clamp01(1f - (remaining / Mathf.Max(0.1f, duration)));
        if (mask != null)
        {
            mask.type = Image.Type.Filled;
            mask.fillMethod = Image.FillMethod.Radial360;
            mask.fillOrigin = 2;
            mask.fillClockwise = false;
            mask.fillAmount = ratio;
            mask.enabled = equipped;
        }

        // Icon 자체도 Filled로 동일한 쿨다운 진행도를 사용
        if (icon != null && equipped)
            icon.fillAmount = ratio;

        if (cooldownText != null)
            cooldownText.text = equipped && ratio > 0.001f ? remaining.ToString("0.0") : string.Empty;
    }

    void RefreshAll(float _, float __)
    {
        RefreshAll();
    }

    void TryResolveSkillGridRoot()
    {
        if (skillGridRoot != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Transform panel = canvas.transform.Find("SkillTabPanel");
        if (panel == null) return;

        skillGridRoot = panel.Find("SkillGridSection/SkillGridHolder");
    }

    void TryResolveReferences()
    {
        if (playerSkillLoadout == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerSkillLoadout = player.GetComponent<PlayerSkillLoadout>();
        }

        if (slot1Root == null) slot1Root = transform.Find("SkillSlot_1");
        if (slot2Root == null) slot2Root = transform.Find("SkillSlot_2");
        if (slot3Root == null) slot3Root = transform.Find("SkillSlot_3");

        TryResolveSkillGridRoot();
    }

    void TryCopySkillIconSprite(int skillId, Image targetIcon)
    {
        if (targetIcon == null) return;

        TryResolveSkillGridRoot();
        if (skillGridRoot == null) return;

        Transform srcSlot = skillGridRoot.Find($"SkillSlot_{skillId}");
        Image srcIcon = srcSlot != null ? srcSlot.Find("Icon")?.GetComponent<Image>() : null;
        if (srcIcon == null) return;

        targetIcon.sprite = srcIcon.sprite;
        targetIcon.type = srcIcon.type;
    }
}
