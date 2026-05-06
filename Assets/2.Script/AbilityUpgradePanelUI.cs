using UnityEngine;
using UnityEngine.UI;

public class AbilityUpgradePanelUI : MonoBehaviour
{
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMeleeAttack playerMeleeAttack;

    [Header("Left - current stats")]
    [SerializeField] private Text levelAndGoldText;
    [SerializeField] private Text currentStatsText;

    [Header("Right - upgrade actions")]
    [SerializeField] private Text attackUpgradeText;
    [SerializeField] private Button attackUpgradeButton;
    [SerializeField] private Text healthUpgradeText;
    [SerializeField] private Button healthUpgradeButton;
    [SerializeField] private Text manaUpgradeText;
    [SerializeField] private Button manaUpgradeButton;
    [SerializeField] private Text attackSpeedUpgradeText;
    [SerializeField] private Button attackSpeedUpgradeButton;

    private int attackUpgradeLevel;
    private int healthUpgradeLevel;

    const float AttackAddValue = 2f;
    const float HealthAddValue = 30f;

    static readonly Color UpgradeButtonAffordableColor = new Color(0.06f, 0.32f, 0.72f, 1f);
    static readonly Color UpgradeButtonCantAffordColor = new Color(0.32f, 0.47f, 0.62f, 0.75f);
    static readonly Color LockedUpgradeButtonColor = new Color(0.35f, 0.35f, 0.38f, 0.85f);

    bool _buttonsBound;

    void Start()
    {
        TryBindButtonsOnce();
    }

    void OnEnable()
    {
        Subscribe();
        RefreshAll();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void TryBindButtonsOnce()
    {
        if (_buttonsBound) return;
        if (attackUpgradeButton == null && healthUpgradeButton == null) return;
        _buttonsBound = true;
        if (attackUpgradeButton != null)
            attackUpgradeButton.onClick.AddListener(TryUpgradeAttack);
        if (healthUpgradeButton != null)
            healthUpgradeButton.onClick.AddListener(TryUpgradeHealth);
    }

    /// <summary>런타임 팩토리가 하이어라키를 만든 직후 호출해 참조를 채웁니다.</summary>
    public void WireRuntimeBindings(
        PlayerProgression progression,
        PlayerHealth health,
        PlayerMeleeAttack melee,
        Text lvlGold,
        Text stats,
        Text atkLine,
        Button atkBtn,
        Text hpLine,
        Button hpBtn,
        Text manaLine,
        Button manaBtn,
        Text atkSpdLine,
        Button atkSpdBtn)
    {
        playerProgression = progression;
        playerHealth = health;
        playerMeleeAttack = melee;
        levelAndGoldText = lvlGold;
        currentStatsText = stats;
        attackUpgradeText = atkLine;
        attackUpgradeButton = atkBtn;
        healthUpgradeText = hpLine;
        healthUpgradeButton = hpBtn;
        manaUpgradeText = manaLine;
        manaUpgradeButton = manaBtn;
        attackSpeedUpgradeText = atkSpdLine;
        attackSpeedUpgradeButton = atkSpdBtn;
    }

    /// <summary>팩토리에서 Wire 직후 호출 → 버튼 리스너 즉시 등록 (비활성이라 Start가 늦어질 때 대비)</summary>
    public void NotifyRuntimeHierarchyReady()
    {
        TryBindButtonsOnce();
    }

    void Subscribe()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged += RefreshAll;
        if (playerHealth != null)
            playerHealth.HealthChanged += OnHealthChanged;
    }

    void Unsubscribe()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged -= RefreshAll;
        if (playerHealth != null)
            playerHealth.HealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged(float current, float max) => RefreshAll();

    void TryUpgradeAttack()
    {
        int cost = GetAttackCost(attackUpgradeLevel);
        if (!TrySpendGold(cost) || playerMeleeAttack == null) return;
        playerMeleeAttack.AddDamage(AttackAddValue);
        attackUpgradeLevel++;
        RefreshAll();
    }

    void TryUpgradeHealth()
    {
        int cost = GetHealthCost(healthUpgradeLevel);
        if (!TrySpendGold(cost) || playerHealth == null) return;
        playerHealth.SetMaxHealth(playerHealth.MaxHealth + HealthAddValue, true);
        healthUpgradeLevel++;
        RefreshAll();
    }

    bool TrySpendGold(int amount)
    {
        if (playerProgression == null) return false;
        return playerProgression.TrySpendGold(amount);
    }

    void RefreshAll()
    {
        int level = playerProgression != null ? playerProgression.CurrentLevel : 1;
        int gold = playerProgression != null ? playerProgression.CurrentGold : 0;

        if (levelAndGoldText != null)
            levelAndGoldText.text = $"LV {level}   Gold {gold}";

        if (currentStatsText != null)
        {
            float damage = playerMeleeAttack != null ? playerMeleeAttack.CurrentDamage : 0f;
            float maxHealth = playerHealth != null ? playerHealth.MaxHealth : 0f;
            currentStatsText.text = $"공격력  {damage:0}\n최대 체력  {maxHealth:0}";
        }

        UpdateUpgradeLine(attackUpgradeText, attackUpgradeButton, "공격력", AttackAddValue, GetAttackCost(attackUpgradeLevel));
        UpdateUpgradeLine(healthUpgradeText, healthUpgradeButton, "체력", HealthAddValue, GetHealthCost(healthUpgradeLevel));
        ApplyLockedUpgradeRow(manaUpgradeText, manaUpgradeButton, "마나");
        ApplyLockedUpgradeRow(attackSpeedUpgradeText, attackSpeedUpgradeButton, "공격 속도");
    }

    void UpdateUpgradeLine(Text lineText, Button button, string label, float addValue, int cost)
    {
        if (lineText != null)
            lineText.text = $"{label} +{addValue:0.##}  (Gold {cost})";

        if (button != null)
        {
            bool canBuy = playerProgression != null && playerProgression.CurrentGold >= cost;
            button.interactable = canBuy;
            Image img = button.GetComponent<Image>();
            if (img != null)
                img.color = canBuy ? UpgradeButtonAffordableColor : UpgradeButtonCantAffordColor;
            Transform lbl = button.transform.Find("Label");
            Text lblTxt = lbl != null ? lbl.GetComponent<Text>() : null;
            if (lblTxt != null)
            {
                lblTxt.text = "강화";
                lblTxt.color = Color.white;
            }
        }
    }

    void ApplyLockedUpgradeRow(Text lineText, Button button, string statName)
    {
        if (lineText != null)
            lineText.text = $"{statName}  —  (추후 개방)";

        if (button != null)
        {
            button.interactable = false;
            Image img = button.GetComponent<Image>();
            if (img != null)
                img.color = LockedUpgradeButtonColor;
            Transform lbl = button.transform.Find("Label");
            Text lblTxt = lbl != null ? lbl.GetComponent<Text>() : null;
            if (lblTxt != null)
            {
                lblTxt.text = "잠금";
                lblTxt.color = new Color(0.72f, 0.72f, 0.75f, 1f);
            }
        }
    }

    int GetAttackCost(int level) => Mathf.RoundToInt(20f * Mathf.Pow(1.35f, level));
    int GetHealthCost(int level) => Mathf.RoundToInt(24f * Mathf.Pow(1.38f, level));
}

/// <summary>씬에 패널이 없을 때 HUD Canvas 아래 한 번 생성. (별도 파일로 두면 csproj 미갱신 시 CLI 빌드가 실패할 수 있어 동일 파일에 둠)</summary>
public static class AbilityUpgradePanelRuntimeFactory
{
    public static GameObject EnsureUnderCanvas(Transform hudCanvasRoot)
    {
        Transform existing = hudCanvasRoot.Find("AbilityUpgradePanel");
        if (existing != null)
            return existing.gameObject;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var root = new GameObject("AbilityUpgradePanel", typeof(RectTransform), typeof(Image));
        root.SetActive(false);
        root.transform.SetParent(hudCanvasRoot, false);

        RectTransform panelRt = root.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.1f, 0.18f);
        panelRt.anchorMax = new Vector2(0.9f, 0.82f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.14f, 0.95f);

        Transform left = CreateSection(root.transform, "CurrentStatsSection", new Vector2(0f, 0f), new Vector2(0.45f, 1f), font);
        Transform right = CreateSection(root.transform, "UpgradeSection", new Vector2(0.5f, 0f), new Vector2(1f, 1f), font);

        Text lvGold = CreatePanelText(left, "LevelGoldText", "LV 1  Gold 0", 24, TextAnchor.UpperLeft, font,
            new Vector2(20f, -20f), new Vector2(-20f, -70f));
        Text currentStats =
            CreatePanelText(left, "CurrentStatsText", "현재 능력치", 22, TextAnchor.UpperLeft, font,
                new Vector2(20f, -86f), new Vector2(-20f, -20f));

        UpgradeRow atk = CreateUpgradeRow(right, "AttackRow", "공격력", 0, font);
        UpgradeRow hp = CreateUpgradeRow(right, "HealthRow", "체력", 1, font);
        UpgradeRow mn = CreateUpgradeRow(right, "ManaRow", "마나", 2, font);
        UpgradeRow sp = CreateUpgradeRow(right, "AttackSpeedRow", "공속", 3, font);

        AbilityUpgradePanelUI ui = root.AddComponent<AbilityUpgradePanelUI>();
        ui.WireRuntimeBindings(
            player.GetComponent<PlayerProgression>(),
            player.GetComponent<PlayerHealth>(),
            player.GetComponent<PlayerMeleeAttack>(),
            lvGold,
            currentStats,
            atk.lineText,
            atk.button,
            hp.lineText,
            hp.button,
            mn.lineText,
            mn.button,
            sp.lineText,
            sp.button);
        ui.NotifyRuntimeHierarchyReady();

        return root;
    }

    struct UpgradeRow
    {
        public Text lineText;
        public Button button;
    }

    static Transform CreateSection(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(10f, 10f);
        rt.offsetMax = new Vector2(-10f, -10f);
        go.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.22f, 0.75f);
        return go.transform;
    }

    static Text CreatePanelText(
        Transform parent,
        string name,
        string content,
        int fontSize,
        TextAnchor anchor,
        Font font,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(offsetMin.x, offsetMax.y);
        rt.offsetMax = new Vector2(offsetMax.x, offsetMin.y);
        Text txt = go.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = fontSize;
        txt.alignment = anchor;
        txt.color = Color.white;
        txt.text = content;
        txt.raycastTarget = false;
        return txt;
    }

    static UpgradeRow CreateUpgradeRow(Transform parent, string rowName, string title, int rowIndex, Font font)
    {
        GameObject row = new GameObject(rowName, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0f, 1f);
        rrt.anchorMax = new Vector2(1f, 1f);
        rrt.pivot = new Vector2(0.5f, 1f);
        rrt.sizeDelta = new Vector2(0f, 72f);
        rrt.anchoredPosition = new Vector2(0f, -20f - (rowIndex * 82f));

        GameObject lineGo = new GameObject("LineText", typeof(RectTransform), typeof(Text));
        lineGo.transform.SetParent(row.transform, false);
        RectTransform lrt = lineGo.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(0.72f, 1f);
        lrt.offsetMin = new Vector2(8f, 4f);
        lrt.offsetMax = new Vector2(-8f, -4f);
        Text lineText = lineGo.GetComponent<Text>();
        lineText.font = font;
        lineText.fontSize = 20;
        lineText.alignment = TextAnchor.MiddleLeft;
        lineText.color = Color.white;
        lineText.text = $"{title} +0";
        lineText.raycastTarget = false;

        GameObject btnGo = new GameObject("UpgradeButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(row.transform, false);
        RectTransform brt = btnGo.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.74f, 0.12f);
        brt.anchorMax = new Vector2(1f, 0.88f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = new Vector2(-4f, 0f);
        Image bimg = btnGo.GetComponent<Image>();
        bimg.color = new Color(0.15f, 0.45f, 0.8f, 1f);
        Button bt = btnGo.GetComponent<Button>();
        bt.targetGraphic = bimg;

        GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        lblGo.transform.SetParent(btnGo.transform, false);
        RectTransform tlrt = lblGo.GetComponent<RectTransform>();
        tlrt.anchorMin = Vector2.zero;
        tlrt.anchorMax = Vector2.one;
        tlrt.offsetMin = Vector2.zero;
        tlrt.offsetMax = Vector2.zero;
        Text lt = lblGo.GetComponent<Text>();
        lt.font = font;
        lt.fontSize = 18;
        lt.alignment = TextAnchor.MiddleCenter;
        lt.color = Color.white;
        lt.text = "강화";
        lt.raycastTarget = false;

        return new UpgradeRow { lineText = lineText, button = bt };
    }
}
