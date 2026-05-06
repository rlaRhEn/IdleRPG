using UnityEngine;
using UnityEngine.UI;

/// <summary>좌측 상단: 프로필 + 골드. UI 오브젝트는 하이어라키에 두고 여기에는 참조만 연결합니다.</summary>
public class PlayerTopLeftHudUI : MonoBehaviour
{
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMeleeAttack playerMeleeAttack;
    [SerializeField] private PlayerSkillLoadout playerSkillLoadout;
    [SerializeField] private PlayerWeaponLoadout playerWeaponLoadout;
    [SerializeField] private OfflineRewardManager offlineRewardManager;
    [SerializeField] private Sprite profileSprite;

    [Header("연결된 UI")]
    [SerializeField] private Image profileImage;
    [SerializeField] private Text goldText;
    [SerializeField] private Text combatPowerText;
    [SerializeField] private Button resetProgressButton;

    void OnEnable()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged += OnProgressChanged;
        if (playerSkillLoadout != null)
            playerSkillLoadout.LoadoutChanged += OnLoadoutChanged;
        if (playerWeaponLoadout != null)
            playerWeaponLoadout.LoadoutChanged += OnLoadoutChanged;

        RefreshAll();
    }

    void OnDisable()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged -= OnProgressChanged;
        if (playerSkillLoadout != null)
            playerSkillLoadout.LoadoutChanged -= OnLoadoutChanged;
        if (playerWeaponLoadout != null)
            playerWeaponLoadout.LoadoutChanged -= OnLoadoutChanged;
    }

    void OnProgressChanged() => RefreshHudStats();
    void OnLoadoutChanged() => RefreshHudStats();

    void RefreshAll()
    {
        if (playerProgression == null)
            playerProgression = GetComponent<PlayerProgression>();
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
        if (playerMeleeAttack == null)
            playerMeleeAttack = GetComponent<PlayerMeleeAttack>();
        if (playerSkillLoadout == null)
            playerSkillLoadout = GetComponent<PlayerSkillLoadout>();
        if (playerWeaponLoadout == null)
            playerWeaponLoadout = GetComponent<PlayerWeaponLoadout>();
        if (offlineRewardManager == null)
            offlineRewardManager = GetComponent<OfflineRewardManager>();

        if (profileImage != null)
        {
            if (profileSprite != null)
                profileImage.sprite = profileSprite;
            profileImage.enabled = profileImage.sprite != null;
        }

        if (resetProgressButton != null)
        {
            resetProgressButton.onClick.RemoveListener(OnClickResetProgress);
            resetProgressButton.onClick.AddListener(OnClickResetProgress);
        }

        RefreshHudStats();
    }

    void OnClickResetProgress()
    {
        if (offlineRewardManager != null)
            offlineRewardManager.ResetAllProgressForTest();
        else
            Debug.LogWarning("[TopLeftHUD] OfflineRewardManager가 없어 초기화를 수행할 수 없습니다.");

        RefreshAll();
    }

    void RefreshHudStats()
    {
        int gold = playerProgression != null ? playerProgression.CurrentGold : 0;
        if (goldText != null)
            goldText.text = $"Gold {gold}";

        if (combatPowerText == null) return;

        int level = playerProgression != null ? playerProgression.CurrentLevel : 1;
        float hp = playerHealth != null ? playerHealth.MaxHealth : 0f;
        float dmg = playerMeleeAttack != null ? playerMeleeAttack.CurrentDamage : 0f;
        float atkSpd = playerMeleeAttack != null ? playerMeleeAttack.CurrentAttacksPerSecond : 0f;
        int ownedSkills = playerSkillLoadout != null ? playerSkillLoadout.OwnedSkillCount : 0;
        int equippedSkills = playerSkillLoadout != null ? playerSkillLoadout.EquippedSkillCount : 0;

        int weaponPower = playerWeaponLoadout != null ? playerWeaponLoadout.CurrentEquippedWeaponPower : 0;

        int combatPower = CalculateCombatPower(
            level,
            hp,
            dmg,
            atkSpd,
            ownedSkills,
            equippedSkills,
            weaponPower);

        combatPowerText.text = $"전투력 {combatPower}";
    }

    static int CalculateCombatPower(
        int level,
        float maxHealth,
        float damage,
        float attackSpeed,
        int ownedSkillCount,
        int equippedSkillCount,
        int weaponPower)
    {
        return Mathf.RoundToInt(
            (Mathf.Max(1, level) * 12f) +
            (Mathf.Max(0f, maxHealth) * 0.7f) +
            (Mathf.Max(0f, damage) * 10f) +
            (Mathf.Max(0f, attackSpeed) * 8f) +
            (Mathf.Max(0, ownedSkillCount) * 18f) +
            (Mathf.Max(0, equippedSkillCount) * 22f) +
            Mathf.Max(0, weaponPower));
    }
}

/// <summary>모바일 해상도/노치 환경에서 HUD 여백을 자동 보정합니다.</summary>
public class HudMobileLayoutAdapter : MonoBehaviour
{
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private RectTransform topLeftHud;
    [SerializeField] private RectTransform bottomCenterHud;
    [SerializeField] private RectTransform bottomRightSkillSlots;
    [SerializeField] private float safePadding = 8f;

    Vector2 _baseTopLeftPos;
    Vector2 _baseBottomCenterPos;
    Vector2 _baseBottomRightPos;
    Rect _lastSafeArea;

    void Awake()
    {
        if (hudCanvas == null)
            hudCanvas = GetComponent<Canvas>();
        if (hudCanvas == null) return;

        CanvasScaler scaler = hudCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = hudCanvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    void Start()
    {
        if (topLeftHud != null) _baseTopLeftPos = topLeftHud.anchoredPosition;
        if (bottomCenterHud != null) _baseBottomCenterPos = bottomCenterHud.anchoredPosition;
        if (bottomRightSkillSlots != null) _baseBottomRightPos = bottomRightSkillSlots.anchoredPosition;
        ApplySafeArea();
    }

    void Update()
    {
        if (_lastSafeArea != Screen.safeArea)
            ApplySafeArea();
    }

    void ApplySafeArea()
    {
        _lastSafeArea = Screen.safeArea;
        float leftInset = _lastSafeArea.xMin;
        float rightInset = Screen.width - _lastSafeArea.xMax;
        float topInset = Screen.height - _lastSafeArea.yMax;
        float bottomInset = _lastSafeArea.yMin;

        if (topLeftHud != null)
            topLeftHud.anchoredPosition = _baseTopLeftPos + new Vector2(leftInset + safePadding, -(topInset + safePadding));
        if (bottomCenterHud != null)
            bottomCenterHud.anchoredPosition = _baseBottomCenterPos + new Vector2(0f, bottomInset + safePadding);
        if (bottomRightSkillSlots != null)
            bottomRightSkillSlots.anchoredPosition = _baseBottomRightPos + new Vector2(-(rightInset + safePadding), bottomInset + safePadding);
    }
}
