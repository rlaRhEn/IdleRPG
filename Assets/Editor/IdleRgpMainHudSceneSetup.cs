#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// IdleRPG/메인 HUD — 하이어라키 정렬 (GameScene)
/// Canvas 아래 오브젝트를 재배치·생성해 하이어라키에 고정입니다. 플레이 중 동적 생성 금지.
/// </summary>
public static class IdleRgpMainHudSceneSetup
{
    const string GameScenePath = "Assets/1.Scenes/GameScene.unity";

    [MenuItem("IdleRPG/메인 HUD — 하이어라키 정렬 (GameScene)")]
    static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(GameScenePath);

        EnsureEventSystemForUi();

        Canvas hud = FindHudCanvas();
        if (hud == null)
        {
            EditorUtility.DisplayDialog("IdleRPG", "HUDCanvas를 찾을 수 없습니다.", "확인");
            return;
        }

        EnsureCanvasScalerForMobile(hud);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("IdleRPG", "Player 태그 오브젝트가 없습니다.", "확인");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("IdleRPG — Main HUD layout");

        Transform bottomRoot = GetOrCreateBottomRoot(hud);
        Transform topLeftRoot = EnsureTopLeftProfileHud(hud);

        Transform hpSliderTf = EnsureHealthElements(hud.transform, bottomRoot, topLeftRoot);
        Transform hpTextTf = bottomRoot.Find("HealthText");

        Transform expTf = hud.transform.Find("PlayerExpSlider");
        if (expTf != null)
            ReparentExpBar(expTf, bottomRoot);

        Transform manaTf = EnsureMana(bottomRoot, hpSliderTf);

        EnsureTabRow(hud, bottomRoot, player);

        var progression = player.GetComponent<PlayerProgression>();
        var health = player.GetComponent<PlayerHealth>();

        PlayerMana mana = player.GetComponent<PlayerMana>();
        if (mana == null)
            mana = Undo.AddComponent<PlayerMana>(player);

        RemoveComponentIfExists<PlayerExperienceSliderUI>(player);

        BottomCenterHudBinder binder = GetOrAddComponent<BottomCenterHudBinder>(bottomRoot.gameObject);
        using (var so = new SerializedObject(binder))
        {
            so.FindProperty("playerProgression").objectReferenceValue = progression;
            so.FindProperty("playerHealth").objectReferenceValue = health;
            so.FindProperty("playerMana").objectReferenceValue = mana;
            so.FindProperty("experienceSlider").objectReferenceValue =
                bottomRoot.Find("PlayerExpSlider")?.GetComponent<Slider>();
            so.FindProperty("experienceText").objectReferenceValue =
                bottomRoot.Find("PlayerExpSlider/Fill Area/ExpText")?.GetComponent<Text>();
            so.FindProperty("healthSlider").objectReferenceValue = hpSliderTf?.GetComponent<Slider>();
            so.FindProperty("healthText").objectReferenceValue = hpTextTf?.GetComponent<Text>();
            so.FindProperty("manaSlider").objectReferenceValue =
                manaTf != null ? manaTf.GetComponent<Slider>() : null;
            so.FindProperty("manaText").objectReferenceValue =
                bottomRoot.Find("ManaText")?.GetComponent<Text>();
            so.ApplyModifiedProperties();
        }

        PlayerTopLeftHudUI topHud = GetOrAddComponent<PlayerTopLeftHudUI>(player);
        using (var sto = new SerializedObject(topHud))
        {
            sto.FindProperty("playerProgression").objectReferenceValue = progression;
            sto.FindProperty("playerHealth").objectReferenceValue = health;
            sto.FindProperty("playerMeleeAttack").objectReferenceValue = player.GetComponent<PlayerMeleeAttack>();
            sto.FindProperty("playerSkillLoadout").objectReferenceValue = player.GetComponent("PlayerSkillLoadout");
            sto.FindProperty("playerWeaponLoadout").objectReferenceValue = player.GetComponent("PlayerWeaponLoadout");
            sto.FindProperty("offlineRewardManager").objectReferenceValue = player.GetComponent("OfflineRewardManager");
            sto.FindProperty("profileImage").objectReferenceValue =
                topLeftRoot.Find("ProfileImage")?.GetComponent<Image>();
            sto.FindProperty("goldText").objectReferenceValue =
                topLeftRoot.Find("GoldText")?.GetComponent<Text>();
            sto.FindProperty("combatPowerText").objectReferenceValue =
                topLeftRoot.Find("CombatPowerText")?.GetComponent<Text>();
            sto.FindProperty("resetProgressButton").objectReferenceValue =
                topLeftRoot.Find("ResetProgressButton")?.GetComponent<Button>();
            sto.ApplyModifiedProperties();
        }

        Transform settingsPanel = EnsureSettingsPanel(hud);
        EnsureTopRightSettingsButton(hud, settingsPanel);

        EnsureOfflineRewardRoot(hud, player);
        EnsureBottomRightSkillSlots(hud, player);
        EnsureSafeAreaAdapter(hud);

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "IdleRPG",
            "HUD 하이어라키 정리가 끝났습니다. 씬을 저장하세요.\n\n※ 이전에 수동으로 꾸민 GameScene 내용 중 Unity 백업에 없던 부분은 _Recovery 버전 기준입니다.",
            "확인");
    }

    static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null)
            c = Undo.AddComponent<T>(go);
        return c;
    }

    /// <summary>uGUI 클릭에는 EventSystem + (새 Input System 프로젝트에서는) InputSystemUIInputModule 이 필요합니다.</summary>
    static void EnsureEventSystemForUi()
    {
        Scene active = EditorSceneManager.GetActiveScene();
        if (!active.IsValid()) return;

        foreach (GameObject root in active.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<EventSystem>(true) != null)
                return;
        }

        GameObject go = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(go, "EventSystem");
        EditorSceneManager.MoveGameObjectToScene(go, active);
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    static void ReparentExpBar(Transform expTf, Transform bottomParent)
    {
        Undo.SetTransformParent(expTf, bottomParent, "EXP bar");
        var rt = expTf.GetComponent<RectTransform>();
        if (rt != null)
            LayoutHorizontalBar(rt, 620f, 30f, new Vector2(0f, 168f));
    }

    static void EnsureCanvasScalerForMobile(Canvas hud)
    {
        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = Undo.AddComponent<CanvasScaler>(hud.gameObject);

        Undo.RecordObject(scaler, "CanvasScaler mobile");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    static void EnsureSafeAreaAdapter(Canvas hud)
    {
        Component adapter = GetOrAddComponentByTypeName(hud.gameObject, "HudMobileLayoutAdapter");
        if (adapter == null) return;

        using SerializedObject so = new SerializedObject(adapter);
        so.FindProperty("hudCanvas").objectReferenceValue = hud;
        so.FindProperty("topLeftHud").objectReferenceValue = hud.transform.Find("TopLeft_ProfileHud")?.GetComponent<RectTransform>();
        so.FindProperty("topRightSettingsButton").objectReferenceValue =
            hud.transform.Find("TopRight_SettingsButton")?.GetComponent<RectTransform>();
        so.FindProperty("bottomCenterHud").objectReferenceValue = hud.transform.Find("BottomCenter_Hud")?.GetComponent<RectTransform>();
        so.FindProperty("bottomRightSkillSlots").objectReferenceValue = hud.transform.Find("BottomRight_SkillSlots")?.GetComponent<RectTransform>();
        so.ApplyModifiedProperties();
    }

    static void RemoveComponentIfExists<T>(GameObject go) where T : Component
    {
        foreach (var c in go.GetComponents<T>())
            Undo.DestroyObjectImmediate(c);
    }

    static Canvas FindHudCanvas()
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (c != null && c.gameObject.name == "HUDCanvas" &&
                string.IsNullOrEmpty(AssetDatabase.GetAssetPath(c.gameObject)))
                return c;
        }

        return null;
    }

    static Transform GetOrCreateBottomRoot(Canvas hud)
    {
        Transform t = hud.transform.Find("BottomCenter_Hud");
        if (t != null)
            return t;

        GameObject go = new GameObject("BottomCenter_Hud", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "BottomCenter_Hud");
        go.transform.SetParent(hud.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 230f);
        return go.transform;
    }

    static Transform EnsureTopLeftProfileHud(Canvas hud)
    {
        Transform t = hud.transform.Find("TopLeft_ProfileHud");
        if (t == null)
            t = hud.transform.Find("PlayerTopLeftHud");

        if (t == null)
        {
            GameObject go = new GameObject("TopLeft_ProfileHud", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "TopLeft_ProfileHud");
            go.transform.SetParent(hud.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -12f);
            rt.sizeDelta = new Vector2(360f, 220f);
            go.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.88f);

            CreateUiImageChild(go.transform, "ProfileImage", new Vector2(72f, 72f), new Vector2(44f, -48f),
                new Color(0.2f, 0.22f, 0.28f, 1f));

            CreateUiTextChild(go.transform, "GoldText", "Gold 0", 20, TextAnchor.MiddleLeft,
                new Vector2(200f, 28f), new Vector2(108f, -30f), new Color(1f, 0.9f, 0.35f, 1f));
            CreateUiTextChild(go.transform, "CombatPowerText", "전투력 0", 18, TextAnchor.MiddleLeft,
                new Vector2(220f, 28f), new Vector2(108f, -60f), new Color(0.72f, 0.9f, 1f, 1f));
            EnsurePanelActionButton(go.transform, "ResetProgressButton", "기록 초기화",
                new Vector2(16f, 14f), new Vector2(210f, 52f), new Color(0.58f, 0.18f, 0.18f, 1f));

            t = go.transform;
        }
        else
        {
            Undo.RecordObject(t.gameObject, "Rename TL");
            t.gameObject.name = "TopLeft_ProfileHud";
        }

        if (t.Find("ProfileImage") == null)
            CreateUiImageChild(t, "ProfileImage", new Vector2(72f, 72f), new Vector2(44f, -48f),
                new Color(0.2f, 0.22f, 0.28f, 1f));

        if (t.Find("GoldText") == null)
            CreateUiTextChild(t, "GoldText", "Gold 0", 20, TextAnchor.MiddleLeft,
                new Vector2(220f, 28f), new Vector2(120f, -30f), new Color(1f, 0.9f, 0.35f, 1f));
        if (t.Find("CombatPowerText") == null)
            CreateUiTextChild(t, "CombatPowerText", "전투력 0", 18, TextAnchor.MiddleLeft,
                new Vector2(220f, 28f), new Vector2(120f, -60f), new Color(0.72f, 0.9f, 1f, 1f));
        if (t.Find("ResetProgressButton") == null)
            EnsurePanelActionButton(t, "ResetProgressButton", "기록 초기화",
                new Vector2(16f, 14f), new Vector2(210f, 52f), new Color(0.58f, 0.18f, 0.18f, 1f));
        Transform oldShopButton = t.Find("ShopButton");
        if (oldShopButton != null)
            Undo.DestroyObjectImmediate(oldShopButton.gameObject);

        return t;
    }

    static Transform EnsureHealthElements(
        Transform canvasRoot,
        Transform bottomRoot,
        Transform topLeftRoot)
    {
        Color hpFillColor = new Color(0.9f, 0.22f, 0.22f, 1f);
        Transform hp = bottomRoot.Find("HealthSlider");
        if (hp != null)
        {
            SetSliderFillColor(hp, hpFillColor);
            return hp;
        }

        if (topLeftRoot != null)
        {
            var fromTop = topLeftRoot.Find("HealthSlider");
            if (fromTop != null)
            {
                MoveToBottomParent(fromTop, bottomRoot, LayoutHorizontalBar, 560f, 24f,
                    new Vector2(0f, 108f));
                var ht = topLeftRoot.Find("HealthText");
                if (ht != null)
                    MoveTextLabel(ht, bottomRoot, new Vector2(560f, 22f), new Vector2(0f, 132f));
                Transform movedHp = bottomRoot.Find("HealthSlider");
                if (movedHp != null)
                    SetSliderFillColor(movedHp, hpFillColor);
                return movedHp;
            }
        }

        Slider s = BuildHudSlider(bottomRoot, "HealthSlider", hpFillColor);
        LayoutHorizontalBar(s.GetComponent<RectTransform>(), 560f, 24f, new Vector2(0f, 108f));
        SetSliderFillColor(s.transform, hpFillColor);
        CreateUiTextChild(bottomRoot, "HealthText", "HP 10/10", 17, TextAnchor.MiddleCenter,
            new Vector2(560f, 22f), new Vector2(0f, 132f), Color.white);
        return s.transform;
    }

    static void SetSliderFillColor(Transform sliderRoot, Color fillColor)
    {
        if (sliderRoot == null) return;
        Transform fill = sliderRoot.Find("Fill Area/Fill");
        Image fillImg = fill != null ? fill.GetComponent<Image>() : null;
        if (fillImg != null)
            fillImg.color = fillColor;
    }

    static Transform EnsureMana(Transform bottomRoot, Transform hpSliderTf)
    {
        Transform mana = bottomRoot.Find("ManaSlider");
        if (mana != null)
        {
            if (bottomRoot.Find("ManaText") == null)
                CreateUiTextChild(bottomRoot, "ManaText", "MP 100/100", 17, TextAnchor.MiddleCenter,
                    new Vector2(560f, 22f), new Vector2(0f, 88f), new Color(0.55f, 0.85f, 1f, 1f));
            return mana;
        }

        if (hpSliderTf != null)
        {
            GameObject clone = Object.Instantiate(hpSliderTf.gameObject, bottomRoot);
            Undo.RegisterCreatedObjectUndo(clone, "Mana slider");
            clone.name = "ManaSlider";
            LayoutHorizontalBar(clone.GetComponent<RectTransform>(), 560f, 24f, new Vector2(0f, 64f));
            var fill = clone.transform.Find("Fill Area/Fill");
            if (fill != null)
            {
                var img = fill.GetComponent<Image>();
                Undo.RecordObject(img, "Mana color");
                img.color = new Color(0.35f, 0.65f, 1f, 1f);
            }

            Slider manaSliderCmp = clone.GetComponent<Slider>();
            Undo.RecordObject(manaSliderCmp, "Mana init");
            manaSliderCmp.value = manaSliderCmp.maxValue;
            CreateUiTextChild(bottomRoot, "ManaText", "MP 100/100", 17, TextAnchor.MiddleCenter,
                new Vector2(560f, 22f), new Vector2(0f, 88f), new Color(0.55f, 0.85f, 1f, 1f));
            return clone.transform;
        }

        Slider s = BuildHudSlider(bottomRoot, "ManaSlider", new Color(0.35f, 0.65f, 1f, 1f));
        LayoutHorizontalBar(s.GetComponent<RectTransform>(), 560f, 24f, new Vector2(0f, 64f));
        CreateUiTextChild(bottomRoot, "ManaText", "MP 100/100", 17, TextAnchor.MiddleCenter,
            new Vector2(560f, 22f), new Vector2(0f, 88f), new Color(0.55f, 0.85f, 1f, 1f));
        return s.transform;
    }

    static void EnsureTabRow(Canvas hud, Transform bottomRoot, GameObject player)
    {
        Transform row = bottomRoot.Find("TabRow");
        RectTransform rt;
        if (row == null)
        {
            GameObject go = new GameObject("TabRow", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "TabRow");
            go.transform.SetParent(bottomRoot, false);
            row = go.transform;
        }

        rt = row.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(660f, 52f);
        rt.anchoredPosition = new Vector2(0f, 14f);

        Transform abilityPanel = EnsureAbilityUpgradePanel(hud, player);
        Transform skillPanel = EnsureSkillTabPanel(hud, player);
        Transform equipmentPanel = EnsureEquipmentTabPanel(hud, player);

        CreateTab(row, "Btn_Ability", "능력치", new Vector2(-220f, 0f),
            abilityPanel != null ? abilityPanel.gameObject : null);
        CreateTab(row, "Btn_Skill", "스킬", new Vector2(0f, 0f),
            skillPanel != null ? skillPanel.gameObject : null);
        CreateTab(row, "Btn_Equipment", "장비", new Vector2(220f, 0f),
            equipmentPanel != null ? equipmentPanel.gameObject : null);

        WireHudTabToggleClick(row.Find("Btn_Ability")?.gameObject);
        WireHudTabToggleClick(row.Find("Btn_Skill")?.gameObject);
        WireHudTabToggleClick(row.Find("Btn_Equipment")?.gameObject);
    }

    delegate void HudBarLayout(RectTransform rt, float width, float height, Vector2 anchored);

    static void LayoutHorizontalBar(RectTransform rt, float width, float height, Vector2 anchored)
    {
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = anchored;
    }

    static void MoveToBottomParent(
        Transform child,
        Transform bottomParent,
        HudBarLayout layout,
        float w,
        float h,
        Vector2 pos)
    {
        Undo.SetTransformParent(child, bottomParent, "Move HUD");
        var rt = child.GetComponent<RectTransform>();
        if (rt != null)
            layout(rt, w, h, pos);
    }

    static void MoveTextLabel(Transform child, Transform bottomParent, Vector2 size, Vector2 pos)
    {
        Undo.SetTransformParent(child, bottomParent, "Move label");
        var rt = child.GetComponent<RectTransform>();
        if (rt != null)
            LayoutHorizontalBar(rt, size.x, size.y, pos);
    }

    static Slider BuildHudSlider(Transform parent, string name, Color fillColor)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Slider");
        root.transform.SetParent(parent, false);

        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.14f, 0.9f);

        Slider slider = root.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        RectTransform far = fillArea.GetComponent<RectTransform>();
        far.anchorMin = Vector2.zero;
        far.anchorMax = Vector2.one;
        far.offsetMin = new Vector2(3f, 3f);
        far.offsetMax = new Vector2(-3f, -3f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fr = fill.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;
        slider.fillRect = fr;
        slider.targetGraphic = fillImg;
        slider.handleRect = null;
        slider.minValue = 0f;

        CanvasRenderer renderer = root.GetComponent<CanvasRenderer>();
        if (renderer == null)
            root.AddComponent<CanvasRenderer>();

        return slider;
    }

    static void CreateUiImageChild(
        Transform parent,
        string name,
        Vector2 size,
        Vector2 anchoredPosition,
        Color color)
    {
        if (parent.Find(name) != null) return;

        GameObject imageGo = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(imageGo, name);
        imageGo.transform.SetParent(parent, false);
        RectTransform rt = imageGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;
        Image img = imageGo.AddComponent<Image>();
        img.color = color;
    }

    static void CreateUiTextChild(
        Transform parent,
        string name,
        string textContent,
        int fontSize,
        TextAnchor anchor,
        Vector2 size,
        Vector2 anchoredPosition,
        Color color)
    {
        Transform existing = parent.Find(name);
        Text text;
        if (existing != null)
        {
            text = existing.GetComponent<Text>();
            if (text != null)
            {
                Undo.RecordObject(text, "Text");
                text.text = textContent;
            }

            return;
        }

        GameObject textGo = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(textGo, name);
        textGo.transform.SetParent(parent, false);
        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;
        text = textGo.AddComponent<Text>();
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.text = textContent;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CanvasRenderer renderer = textGo.GetComponent<CanvasRenderer>();
        if (renderer == null)
            textGo.AddComponent<CanvasRenderer>();
    }

    static void CreateTab(
        Transform tabRow,
        string name,
        string labelText,
        Vector2 anchoredPosition,
        GameObject targetPanel)
    {
        Transform existing = tabRow.Find(name);
        GameObject btnGo;

        if (existing != null)
            btnGo = existing.gameObject;
        else
        {
            btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(btnGo, name);
            btnGo.transform.SetParent(tabRow, false);
            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.15f, 0.17f, 0.22f, 0.95f);
            var bt = btnGo.GetComponent<Button>();
            bt.targetGraphic = img;
            var brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(220f, 84f);

            GameObject lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(btnGo.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            Text tx = lbl.AddComponent<Text>();
            tx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tx.fontSize = 20;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = Color.white;
            tx.text = labelText;
            lbl.AddComponent<CanvasRenderer>();
        }

        var rect = btnGo.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;

        HudTabPlaceholder tab = btnGo.GetComponent<HudTabPlaceholder>();
        if (tab == null)
            tab = Undo.AddComponent<HudTabPlaceholder>(btnGo);

        using (SerializedObject soTab = new SerializedObject(tab))
        {
            SerializedProperty panelProp = soTab.FindProperty("targetPanel");
            if (panelProp != null)
                panelProp.objectReferenceValue = targetPanel;
            soTab.ApplyModifiedProperties();
        }

        Transform lblTr = btnGo.transform.Find("Label");
        Text lblTxt = lblTr != null ? lblTr.GetComponent<Text>() : null;
        if (lblTxt != null)
        {
            Undo.RecordObject(lblTxt, "Tab label");
            lblTxt.text = labelText;
        }
    }

    static Transform EnsureAbilityUpgradePanel(Canvas hud, GameObject player)
    {
        Transform panel = hud.transform.Find("AbilityUpgradePanel");
        if (panel == null)
        {
            GameObject go = new GameObject("AbilityUpgradePanel", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "AbilityUpgradePanel");
            go.transform.SetParent(hud.transform, false);
            panel = go.transform;
        }

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.1f, 0.18f);
        panelRt.anchorMax = new Vector2(0.9f, 0.82f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        Image panelBg = panel.GetComponent<Image>();
        panelBg.color = new Color(0.06f, 0.09f, 0.14f, 0.95f);

        Transform left = EnsurePanelSection(panel, "CurrentStatsSection", new Vector2(0f, 0f), new Vector2(0.45f, 1f));
        Transform right = EnsurePanelSection(panel, "UpgradeSection", new Vector2(0.5f, 0f), new Vector2(1f, 1f));

        Text lvGold = EnsurePanelText(left, "LevelGoldText", "LV 1  Gold 0", 24, TextAnchor.UpperLeft, new Vector2(20f, -20f), new Vector2(-20f, -70f));
        Text currentStats = EnsurePanelText(left, "CurrentStatsText", "현재 능력치", 22, TextAnchor.UpperLeft, new Vector2(20f, -86f), new Vector2(-20f, -20f));

        Text attackLine;
        Button attackButton;
        EnsureUpgradeRow(right, "AttackRow", "공격력", 0, out attackLine, out attackButton);
        Text healthLine;
        Button healthButton;
        EnsureUpgradeRow(right, "HealthRow", "체력", 1, out healthLine, out healthButton);
        Text manaLine;
        Button manaButton;
        EnsureUpgradeRow(right, "ManaRow", "마나", 2, out manaLine, out manaButton);
        Text atkSpdLine;
        Button atkSpdButton;
        EnsureUpgradeRow(right, "AttackSpeedRow", "공속", 3, out atkSpdLine, out atkSpdButton);

        Component ui = GetOrAddComponentByTypeName(panel.gameObject, "AbilityUpgradePanelUI");
        if (ui != null)
        {
            using SerializedObject so = new SerializedObject(ui);
            so.FindProperty("playerProgression").objectReferenceValue = player.GetComponent<PlayerProgression>();
            so.FindProperty("playerHealth").objectReferenceValue = player.GetComponent<PlayerHealth>();
            so.FindProperty("playerMeleeAttack").objectReferenceValue = player.GetComponent<PlayerMeleeAttack>();
            so.FindProperty("levelAndGoldText").objectReferenceValue = lvGold;
            so.FindProperty("currentStatsText").objectReferenceValue = currentStats;
            so.FindProperty("attackUpgradeText").objectReferenceValue = attackLine;
            so.FindProperty("attackUpgradeButton").objectReferenceValue = attackButton;
            so.FindProperty("healthUpgradeText").objectReferenceValue = healthLine;
            so.FindProperty("healthUpgradeButton").objectReferenceValue = healthButton;
            so.FindProperty("manaUpgradeText").objectReferenceValue = manaLine;
            so.FindProperty("manaUpgradeButton").objectReferenceValue = manaButton;
            so.FindProperty("attackSpeedUpgradeText").objectReferenceValue = atkSpdLine;
            so.FindProperty("attackSpeedUpgradeButton").objectReferenceValue = atkSpdButton;
            so.ApplyModifiedProperties();
        }

        panel.gameObject.SetActive(false);
        return panel;
    }

    static Transform EnsureSkillTabPanel(Canvas hud, GameObject player)
    {
        Transform panel = hud.transform.Find("SkillTabPanel");
        if (panel == null)
        {
            GameObject go = new GameObject("SkillTabPanel", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "SkillTabPanel");
            go.transform.SetParent(hud.transform, false);
            panel = go.transform;
        }

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.1f, 0.18f);
        panelRt.anchorMax = new Vector2(0.9f, 0.82f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panel.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.14f, 0.95f);

        Transform left = EnsurePanelSection(panel, "SkillDetailSection", new Vector2(0f, 0f), new Vector2(0.38f, 1f));
        Transform right = EnsurePanelSection(panel, "SkillGridSection", new Vector2(0.40f, 0f), new Vector2(1f, 1f));

        EnsurePanelText(left, "SkillHeaderText", "스킬", 26, TextAnchor.UpperLeft,
            new Vector2(16f, -14f), new Vector2(-16f, -54f));

        Text dTitle = EnsurePanelText(left, "DetailTitleText", "힐링", 22, TextAnchor.UpperLeft,
            new Vector2(16f, -60f), new Vector2(-16f, -94f));

        Text dBody = EnsurePanelText(left, "DetailBodyText", "", 16, TextAnchor.UpperLeft,
            new Vector2(16f, -100f), new Vector2(-16f, -20f));
        Text costText = EnsurePanelText(left, "DetailCostText", "구매 비용: Gold 120", 16, TextAnchor.LowerLeft,
            new Vector2(16f, -52f), new Vector2(-16f, -20f));
        Button buyButton = EnsurePanelActionButton(left, "BuyButton", "구매 Gold 120",
            new Vector2(16f, 16f), new Vector2(190f, 48f), new Color(0.18f, 0.48f, 0.18f, 1f));
        Button equipButton = EnsurePanelActionButton(left, "EquipButton", "장착",
            new Vector2(214f, 16f), new Vector2(148f, 48f), new Color(0.15f, 0.40f, 0.72f, 1f));

        Transform gridHolder = right.Find("SkillGridHolder");
        if (gridHolder == null)
        {
            GameObject gh = new GameObject("SkillGridHolder", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(gh, "SkillGridHolder");
            gh.transform.SetParent(right, false);
            gridHolder = gh.transform;
        }

        RectTransform ghRt = gridHolder.GetComponent<RectTransform>();
        ghRt.anchorMin = Vector2.zero;
        ghRt.anchorMax = Vector2.one;
        ghRt.offsetMin = new Vector2(8f, 8f);
        ghRt.offsetMax = new Vector2(-8f, -8f);

        GridLayoutGroup glg = gridHolder.GetComponent<GridLayoutGroup>();
        if (glg == null)
            glg = Undo.AddComponent<GridLayoutGroup>(gridHolder.gameObject);

        glg.padding = new RectOffset(6, 6, 6, 6);
        glg.spacing = new Vector2(8f, 8f);
        glg.cellSize = new Vector2(104f, 96f);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;

        EnsureSkillGridSlot(gridHolder, 0, Color.white, true, "잠금");
        EnsureSkillGridSlot(gridHolder, 1, Color.white, true, "잠금");

        for (int s = 2; s < 20; s++)
            EnsureSkillGridSlot(gridHolder, s, Color.white, true, "미구현");

        System.Type loadoutType = System.Type.GetType("PlayerSkillLoadout, Assembly-CSharp");
        Component loadoutCmp = null;
        if (loadoutType != null)
        {
            loadoutCmp = player.GetComponent(loadoutType);
            if (loadoutCmp == null)
                loadoutCmp = Undo.AddComponent(player, loadoutType);

            using SerializedObject soLoadout = new SerializedObject(loadoutCmp);
            SerializedProperty ctrlProp = soLoadout.FindProperty("playerController");
            if (ctrlProp != null)
                ctrlProp.objectReferenceValue = player.GetComponent<PlayerController>();
            SerializedProperty prefabProp = soLoadout.FindProperty("bulletPrefab");
            if (prefabProp != null)
                prefabProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3.Prefab/Bullet.prefab");
            SerializedProperty fxProp = soLoadout.FindProperty("iceBurstFxPrefab");
            if (fxProp != null)
                fxProp.objectReferenceValue = EnsureIceBurstFxPrefabAsset();
            soLoadout.ApplyModifiedProperties();
        }

        Component ui = GetOrAddComponentByTypeName(panel.gameObject, "SkillTabPanelUI");
        if (ui != null)
        {
            using SerializedObject so = new SerializedObject(ui);
            so.FindProperty("playerProgression").objectReferenceValue = player.GetComponent<PlayerProgression>();
            so.FindProperty("playerSkillLoadout").objectReferenceValue = loadoutCmp;
            so.FindProperty("detailTitleText").objectReferenceValue = dTitle;
            so.FindProperty("detailBodyText").objectReferenceValue = dBody;
            so.FindProperty("detailCostText").objectReferenceValue = costText;
            so.FindProperty("buyButton").objectReferenceValue = buyButton;
            so.FindProperty("equipButton").objectReferenceValue = equipButton;
            so.FindProperty("gridRoot").objectReferenceValue = gridHolder;
            so.ApplyModifiedProperties();
        }

        panel.gameObject.SetActive(false);
        return panel;
    }

    static Transform EnsureEquipmentTabPanel(Canvas hud, GameObject player)
    {
        Transform panel = hud.transform.Find("EquipmentTabPanel");
        if (panel == null)
        {
            GameObject go = new GameObject("EquipmentTabPanel", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "EquipmentTabPanel");
            go.transform.SetParent(hud.transform, false);
            panel = go.transform;
        }

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.1f, 0.18f);
        panelRt.anchorMax = new Vector2(0.9f, 0.82f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panel.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.14f, 0.95f);

        Transform left = EnsurePanelSection(panel, "EquipmentDetailSection", new Vector2(0f, 0f), new Vector2(0.42f, 1f));
        Transform right = EnsurePanelSection(panel, "EquipmentGridSection", new Vector2(0.44f, 0f), new Vector2(1f, 1f));

        EnsurePanelText(left, "EquipmentHeaderText", "장비", 26, TextAnchor.UpperLeft,
            new Vector2(16f, -14f), new Vector2(-16f, -54f));

        Text dTitle = EnsurePanelText(left, "DetailTitleText", "번개검", 22, TextAnchor.UpperLeft,
            new Vector2(16f, -60f), new Vector2(-16f, -94f));
        Text dBody = EnsurePanelText(left, "DetailBodyText",
            "무작위 적에게 자동으로 번개 공격을 한다.\n\n전투력 증가량: +45\n구매 시 즉시 장착",
            17, TextAnchor.UpperLeft,
            new Vector2(16f, -112f), new Vector2(-16f, -110f));
        dBody.lineSpacing = 1.15f;
        Text costText = EnsurePanelText(left, "DetailCostText", "구매 비용: Gold 40", 16, TextAnchor.LowerLeft,
            new Vector2(16f, -56f), new Vector2(-16f, -64f));
        Button actionButton = EnsurePanelActionButton(left, "ActionButton", "구매 후 장착 Gold 40",
            new Vector2(16f, 16f), new Vector2(368f, 48f), new Color(0.25f, 0.46f, 0.18f, 1f));

        Transform gridHolder = right.Find("WeaponGridHolder");
        if (gridHolder == null)
        {
            GameObject gh = new GameObject("WeaponGridHolder", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(gh, "WeaponGridHolder");
            gh.transform.SetParent(right, false);
            gridHolder = gh.transform;
        }

        RectTransform ghRt = gridHolder.GetComponent<RectTransform>();
        ghRt.anchorMin = Vector2.zero;
        ghRt.anchorMax = Vector2.one;
        ghRt.offsetMin = new Vector2(8f, 8f);
        ghRt.offsetMax = new Vector2(-8f, -8f);

        GridLayoutGroup glg = gridHolder.GetComponent<GridLayoutGroup>();
        if (glg == null)
            glg = Undo.AddComponent<GridLayoutGroup>(gridHolder.gameObject);
        glg.padding = new RectOffset(6, 6, 6, 6);
        glg.spacing = new Vector2(8f, 8f);
        glg.cellSize = new Vector2(104f, 96f);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;

        EnsureWeaponGridSlot(gridHolder, 0, Color.white, true, "잠금");
        EnsureWeaponGridSlot(gridHolder, 1, Color.white, true, "잠금");
        for (int s = 2; s < 20; s++)
            EnsureWeaponGridSlot(gridHolder, s, Color.white, true, "미구현");

        System.Type weaponLoadoutType = System.Type.GetType("PlayerWeaponLoadout, Assembly-CSharp");
        Component weaponLoadoutCmp = null;
        if (weaponLoadoutType != null)
        {
            weaponLoadoutCmp = player.GetComponent(weaponLoadoutType);
            if (weaponLoadoutCmp == null)
                weaponLoadoutCmp = Undo.AddComponent(player, weaponLoadoutType);
        }

        Component ui = GetOrAddComponentByTypeName(panel.gameObject, "WeaponTabPanelUI");
        if (ui != null)
        {
            using SerializedObject so = new SerializedObject(ui);
            so.FindProperty("playerProgression").objectReferenceValue = player.GetComponent<PlayerProgression>();
            so.FindProperty("playerWeaponLoadout").objectReferenceValue = weaponLoadoutCmp;
            so.FindProperty("detailTitleText").objectReferenceValue = dTitle;
            so.FindProperty("detailBodyText").objectReferenceValue = dBody;
            so.FindProperty("detailCostText").objectReferenceValue = costText;
            so.FindProperty("actionButton").objectReferenceValue = actionButton;
            so.FindProperty("gridRoot").objectReferenceValue = gridHolder;
            so.ApplyModifiedProperties();
        }

        panel.gameObject.SetActive(false);
        return panel;
    }

    static Transform EnsureSettingsPanel(Canvas hud)
    {
        Transform panel = hud.transform.Find("SettingsPanel");
        if (panel == null)
        {
            GameObject go = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "SettingsPanel");
            go.transform.SetParent(hud.transform, false);
            panel = go.transform;
        }

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.12f, 0.22f);
        panelRt.anchorMax = new Vector2(0.88f, 0.78f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panel.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.14f, 0.96f);

        Transform content = EnsurePanelSection(panel, "SettingsContentSection", new Vector2(0f, 0f), new Vector2(1f, 1f));

        EnsurePanelText(content, "SettingsHeaderText", "설정", 28, TextAnchor.UpperLeft,
            new Vector2(20f, -20f), new Vector2(-120f, -64f));

        Text infoText = EnsurePanelText(content, "SettingsInfoText",
            "게임 설정\n\n· 사운드 (준비 중)\n· 알림 (준비 중)\n· 언어 (준비 중)",
            20, TextAnchor.UpperLeft,
            new Vector2(20f, -80f), new Vector2(-20f, -24f));
        infoText.lineSpacing = 1.2f;

        Button closeButton = EnsureTopAnchoredPanelButton(panel, "SettingsCloseButton", "닫기",
            new Vector2(-16f, -16f), new Vector2(96f, 48f), new Color(0.28f, 0.32f, 0.4f, 1f));

        Component ui = GetOrAddComponentByTypeName(panel.gameObject, "SettingsPanelUI");
        if (ui != null)
        {
            using SerializedObject so = new SerializedObject(ui);
            so.FindProperty("panelRoot").objectReferenceValue = panel.gameObject;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.ApplyModifiedProperties();
        }

        panel.gameObject.SetActive(false);
        return panel;
    }

    static Button EnsureTopAnchoredPanelButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 size,
        Color bgColor)
    {
        Transform btnTf = parent.Find(name);
        if (btnTf == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, name);
            go.transform.SetParent(parent, false);
            btnTf = go.transform;
        }

        RectTransform rt = btnTf.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        Image img = btnTf.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnTf.GetComponent<Button>();
        btn.targetGraphic = img;

        Transform lblTf = btnTf.Find("Label");
        if (lblTf == null)
        {
            GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(lblGo, "Label");
            lblGo.transform.SetParent(btnTf, false);
            lblTf = lblGo.transform;
        }

        RectTransform lrt = lblTf.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        Text txt = lblTf.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;
        txt.raycastTarget = false;

        return btn;
    }

    static Transform EnsureTopRightSettingsButton(Canvas hud, Transform settingsPanel)
    {
        Transform root = hud.transform.Find("TopRight_SettingsButton");
        if (root == null)
        {
            GameObject rootGo = new GameObject("TopRight_SettingsButton", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rootGo, "TopRight_SettingsButton");
            rootGo.transform.SetParent(hud.transform, false);
            root = rootGo.transform;
        }

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(1f, 1f);
        rootRt.anchoredPosition = new Vector2(-12f, -12f);
        rootRt.sizeDelta = new Vector2(72f, 72f);

        Transform btnTf = root.Find("Btn_Settings");
        GameObject btnGo;
        if (btnTf != null)
            btnGo = btnTf.gameObject;
        else
        {
            btnGo = new GameObject("Btn_Settings", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(btnGo, "Btn_Settings");
            btnGo.transform.SetParent(root, false);
            Image img = btnGo.GetComponent<Image>();
            img.color = new Color(0.15f, 0.17f, 0.22f, 0.95f);
            Button bt = btnGo.GetComponent<Button>();
            bt.targetGraphic = img;

            RectTransform brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;

            GameObject lbl = new GameObject("Label", typeof(RectTransform), typeof(Text));
            lbl.transform.SetParent(btnGo.transform, false);
            RectTransform lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            Text tx = lbl.GetComponent<Text>();
            tx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tx.fontSize = 20;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = Color.white;
            tx.text = "설정";
            tx.raycastTarget = false;
        }

        HudTabPlaceholder tab = btnGo.GetComponent<HudTabPlaceholder>();
        if (tab == null)
            tab = Undo.AddComponent<HudTabPlaceholder>(btnGo);

        using (SerializedObject soTab = new SerializedObject(tab))
        {
            SerializedProperty panelProp = soTab.FindProperty("targetPanel");
            if (panelProp != null && settingsPanel != null)
                panelProp.objectReferenceValue = settingsPanel.gameObject;
            soTab.ApplyModifiedProperties();
        }

        ApplySettingsButtonVisual(btnGo);

        WireHudTabToggleClick(btnGo);
        return root;
    }

    static void ApplySettingsButtonVisual(GameObject btnGo)
    {
        if (btnGo == null)
            return;

        const string gearIconPath = "Assets/0.Asset/500FreeSkillIcons/Icons/skill_522.png";
        const string uiSheetPath = "Assets/0.Asset/Undead Survivor/Sprites/UI.png";

        Sprite gearIcon = AssetDatabase.LoadAssetAtPath<Sprite>(gearIconPath);
        Sprite frameSprite = null;
        Object[] uiAssets = AssetDatabase.LoadAllAssetsAtPath(uiSheetPath);
        for (int i = 0; i < uiAssets.Length; i++)
        {
            if (uiAssets[i] is Sprite sprite && sprite.name == "Box 1")
            {
                frameSprite = sprite;
                break;
            }
        }

        Image bg = btnGo.GetComponent<Image>();
        if (bg != null)
        {
            Undo.RecordObject(bg, "Settings button frame");
            if (frameSprite != null)
            {
                bg.sprite = frameSprite;
                bg.color = Color.white;
                bg.type = Image.Type.Simple;
            }
            else
            {
                bg.sprite = null;
                bg.color = new Color(0.15f, 0.17f, 0.22f, 0.95f);
                bg.type = Image.Type.Simple;
            }
            bg.preserveAspect = false;
        }

        Transform iconTf = btnGo.transform.Find("Icon");
        if (iconTf == null)
        {
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(iconGo, "SettingsIcon");
            iconGo.transform.SetParent(btnGo.transform, false);
            iconTf = iconGo.transform;
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
        }

        Image iconImg = iconTf.GetComponent<Image>();
        if (iconImg == null)
            iconImg = Undo.AddComponent<Image>(iconTf.gameObject);

        Undo.RecordObject(iconImg, "Settings gear icon");
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;
        iconImg.color = Color.white;
        iconImg.sprite = gearIcon;

        Transform lblTr = btnGo.transform.Find("Label");
        if (lblTr != null)
        {
            Undo.RecordObject(lblTr.gameObject, "Hide settings label");
            lblTr.gameObject.SetActive(false);
        }
    }

    static void EnsureWeaponGridSlot(
        Transform gridHolder,
        int index,
        Color iconColor,
        bool showLockOverlay,
        string overlayCenterLabel)
    {
        string slotName = $"WeaponSlot_{index}";
        Transform slot = gridHolder.Find(slotName);
        GameObject slotGo;
        if (slot == null)
        {
            slotGo = new GameObject(slotName, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(slotGo, slotName);
            slotGo.transform.SetParent(gridHolder, false);
        }
        else
        {
            slotGo = slot.gameObject;
        }

        Image bg = slotGo.GetComponent<Image>();
        if (bg == null)
            bg = Undo.AddComponent<Image>(slotGo);
        bg.color = new Color(0.11f, 0.13f, 0.18f, 0.92f);
        bg.raycastTarget = true;

        Button bt = slotGo.GetComponent<Button>();
        if (bt == null)
            bt = Undo.AddComponent<Button>(slotGo);
        bt.targetGraphic = bg;

        Outline ol = slotGo.GetComponent<Outline>();
        if (ol == null)
            ol = Undo.AddComponent<Outline>(slotGo);
        ol.effectColor = Color.white;
        ol.effectDistance = new Vector2(2f, -2f);
        ol.useGraphicAlpha = true;
        ol.enabled = index == 0;

        EnsureSkillSlotIcon(slotGo.transform, iconColor);
        EnsureSkillSlotLockOverlay(slotGo.transform, showLockOverlay, overlayCenterLabel);
    }

    static void EnsureSkillGridSlot(
        Transform gridHolder,
        int index,
        Color iconColor,
        bool showLockOverlay,
        string overlayCenterLabel)
    {
        string slotName = $"SkillSlot_{index}";
        Transform slot = gridHolder.Find(slotName);

        GameObject slotGo;
        if (slot == null)
        {
            slotGo = new GameObject(slotName, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(slotGo, slotName);
            slotGo.transform.SetParent(gridHolder, false);
        }
        else
            slotGo = slot.gameObject;

        if (slotGo.GetComponent<RectTransform>() == null)
            slotGo.AddComponent<RectTransform>();

        Image bg = slotGo.GetComponent<Image>();
        if (bg == null)
            bg = Undo.AddComponent<Image>(slotGo);

        bg.color = new Color(0.11f, 0.13f, 0.18f, 0.92f);

        bg.raycastTarget = true;

        Button bt = slotGo.GetComponent<Button>();
        if (bt == null)
            bt = Undo.AddComponent<Button>(slotGo);

        bt.targetGraphic = bg;

        Outline ol = slotGo.GetComponent<Outline>();
        if (ol == null)
            ol = Undo.AddComponent<Outline>(slotGo);

        ol.effectColor = Color.white;
        ol.effectDistance = new Vector2(2f, -2f);
        ol.useGraphicAlpha = true;
        ol.enabled = index == 0;

        EnsureSkillSlotIcon(slotGo.transform, iconColor);
        EnsureSkillSlotLockOverlay(slotGo.transform, showLockOverlay, overlayCenterLabel);

        Undo.RecordObject(slotGo, "Skill slot layout");
    }

    static void EnsureSkillSlotIcon(Transform slotTransform, Color iconColor)
    {
        Transform iconTf = slotTransform.Find("Icon");
        if (iconTf == null)
        {
            GameObject go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Skill slot icon");
            go.transform.SetParent(slotTransform, false);
            iconTf = go.transform;
        }

        RectTransform irt = iconTf.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.5f, 0.5f);
        irt.anchorMax = new Vector2(0.5f, 0.5f);
        irt.pivot = new Vector2(0.5f, 0.5f);
        irt.sizeDelta = new Vector2(94f, 94f);
        irt.anchoredPosition = Vector2.zero;

        Image icon = iconTf.GetComponent<Image>();
        if (icon.sprite == null)
            icon.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        icon.type = Image.Type.Sliced;
        icon.preserveAspect = true;
        // 이미 사용자가 아이콘 이미지를 넣어둔 경우 색 틴트 덮어쓰지 않음
        if (icon.sprite == null || icon.sprite == AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"))
            icon.color = iconColor;
        icon.raycastTarget = false;
    }

    static void EnsureSkillSlotLockOverlay(Transform slotTransform, bool visible, string centerLabel)
    {
        Transform lockTf = slotTransform.Find("LockOverlay");
        GameObject overlayGo;

        if (lockTf == null)
        {
            overlayGo =
                new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(overlayGo, "LockOverlay");
            overlayGo.transform.SetParent(slotTransform, false);
            lockTf = overlayGo.transform;
        }
        else
            overlayGo = lockTf.gameObject;

        RectTransform lrt = lockTf.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        Image lim = lockTf.GetComponent<Image>();
        lim.color = new Color(0f, 0f, 0f, 0.58f);
        lim.raycastTarget = false;

        Transform labelTf = lockTf.Find("LockLabel");
        if (labelTf == null)
        {
            GameObject lg =
                new GameObject("LockLabel", typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(lg, "LockLabel");
            lg.transform.SetParent(lockTf, false);
            labelTf = lg.transform;
        }

        RectTransform llrt = labelTf.GetComponent<RectTransform>();
        llrt.anchorMin = new Vector2(0f, 0f);
        llrt.anchorMax = new Vector2(1f, 1f);
        llrt.offsetMin = new Vector2(4f, 4f);
        llrt.offsetMax = new Vector2(-4f, -4f);

        Text lt = labelTf.GetComponent<Text>();
        lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lt.fontSize = 15;
        lt.fontStyle = FontStyle.Bold;
        lt.alignment = TextAnchor.MiddleCenter;
        lt.color = new Color(0.94f, 0.94f, 0.96f, 1f);
        lt.text = string.IsNullOrEmpty(centerLabel) ? "잠금" : centerLabel;
        lt.horizontalOverflow = HorizontalWrapMode.Wrap;
        lt.verticalOverflow = VerticalWrapMode.Truncate;
        lt.raycastTarget = false;

        overlayGo.SetActive(visible);
    }

    static Button EnsurePanelActionButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPos,
        Vector2 size,
        Color bgColor)
    {
        Transform btnTf = parent.Find(name);
        if (btnTf == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, name);
            go.transform.SetParent(parent, false);
            btnTf = go.transform;
        }

        RectTransform rt = btnTf.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(Mathf.Max(160f, size.x), Mathf.Max(72f, size.y));

        Image img = btnTf.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnTf.GetComponent<Button>();
        btn.targetGraphic = img;

        Transform lblTf = btnTf.Find("Label");
        if (lblTf == null)
        {
            GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(lblGo, "Label");
            lblGo.transform.SetParent(btnTf, false);
            lblTf = lblGo.transform;
        }

        RectTransform lrt = lblTf.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        Text txt = lblTf.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;
        txt.raycastTarget = false;

        return btn;
    }

    static GameObject EnsureIceBurstFxPrefabAsset()
    {
        const string fxPath = "Assets/3.Prefab/IceBurstFx.prefab";
        const string matPath = "Assets/3.Prefab/IceBurstFx_Mat.mat";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(fxPath);

        Material fxMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (fxMat == null)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ??
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Sprites/Default");

            if (shader != null)
            {
                fxMat = new Material(shader);
                AssetDatabase.CreateAsset(fxMat, matPath);
            }
        }

        if (existing != null)
        {
            ParticleSystemRenderer existingRenderer = existing.GetComponent<ParticleSystemRenderer>();
            if (existingRenderer != null && fxMat != null)
            {
                existingRenderer.sharedMaterial = fxMat;
                EditorUtility.SetDirty(existingRenderer);
                PrefabUtility.SavePrefabAsset(existing);
                AssetDatabase.SaveAssets();
            }
            return existing;
        }

        GameObject temp = new GameObject("IceBurstFx");
        ParticleSystem ps = temp.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.9f, 1f, 0.95f),
            new Color(0.2f, 0.6f, 1f, 0.8f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.maxParticles = 80;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.65f, 0.95f, 1f), 0f),
                new GradientColorKey(new Color(0.3f, 0.7f, 1f), 1f)
            },
            new[] {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = g;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f, AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1f));

        ParticleSystemRenderer renderer = temp.GetComponent<ParticleSystemRenderer>();
        if (fxMat != null)
            renderer.sharedMaterial = fxMat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, fxPath);
        Undo.DestroyObjectImmediate(temp);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return prefab;
    }

    static void WireHudTabToggleClick(GameObject tabButtonGo)
    {
        if (tabButtonGo == null)
            return;

        HudTabPlaceholder tab = tabButtonGo.GetComponent<HudTabPlaceholder>();
        Button button = tabButtonGo.GetComponent<Button>();
        if (tab == null || button == null)
            return;

        Undo.RecordObject(button, "HUD 탭 Toggle 연결");

        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);

        UnityEventTools.AddVoidPersistentListener(button.onClick, tab.ToggleTabPanelFromButton);
        EditorUtility.SetDirty(button);
    }

    static Transform EnsurePanelSection(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        Transform section = parent.Find(name);
        if (section == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, name);
            go.transform.SetParent(parent, false);
            section = go.transform;
        }

        RectTransform rt = section.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(10f, 10f);
        rt.offsetMax = new Vector2(-10f, -10f);
        Image img = section.GetComponent<Image>();
        img.color = new Color(0.12f, 0.15f, 0.22f, 0.75f);
        return section;
    }

    static Text EnsurePanelText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Vector2 offsetMin, Vector2 offsetMax)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(go, name);
            go.transform.SetParent(parent, false);
            child = go.transform;
        }

        RectTransform rt = child.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(offsetMin.x, offsetMax.y);
        rt.offsetMax = new Vector2(offsetMax.x, offsetMin.y);

        Text txt = child.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.alignment = anchor;
        txt.color = Color.white;
        txt.text = content;
        txt.raycastTarget = false;
        return txt;
    }

    static void EnsureUpgradeRow(Transform parent, string rowName, string title, int rowIndex, out Text lineText, out Button upgradeButton)
    {
        Transform row = parent.Find(rowName);
        if (row == null)
        {
            GameObject go = new GameObject(rowName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, rowName);
            go.transform.SetParent(parent, false);
            row = go.transform;
        }

        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0f, 1f);
        rrt.anchorMax = new Vector2(1f, 1f);
        rrt.pivot = new Vector2(0.5f, 1f);
        rrt.sizeDelta = new Vector2(0f, 72f);
        rrt.anchoredPosition = new Vector2(0f, -20f - (rowIndex * 82f));

        Transform line = row.Find("LineText");
        if (line == null)
        {
            GameObject go = new GameObject("LineText", typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(go, "LineText");
            go.transform.SetParent(row, false);
            line = go.transform;
        }

        RectTransform lrt = line.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(0.72f, 1f);
        lrt.offsetMin = new Vector2(8f, 4f);
        lrt.offsetMax = new Vector2(-8f, -4f);

        lineText = line.GetComponent<Text>();
        lineText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lineText.fontSize = 20;
        lineText.alignment = TextAnchor.MiddleLeft;
        lineText.color = Color.white;
        lineText.text = $"{title} +0";
        lineText.raycastTarget = false;

        Transform btn = row.Find("UpgradeButton");
        if (btn == null)
        {
            GameObject go = new GameObject("UpgradeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "UpgradeButton");
            go.transform.SetParent(row, false);
            btn = go.transform;
        }

        RectTransform brt = btn.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.74f, 0.12f);
        brt.anchorMax = new Vector2(1f, 0.88f);
        brt.offsetMin = new Vector2(0f, 0f);
        brt.offsetMax = new Vector2(-4f, 0f);

        Image bimg = btn.GetComponent<Image>();
        bimg.color = new Color(0.15f, 0.45f, 0.8f, 1f);
        upgradeButton = btn.GetComponent<Button>();
        upgradeButton.targetGraphic = bimg;

        Transform label = btn.Find("Label");
        if (label == null)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(go, "Label");
            go.transform.SetParent(btn, false);
            label = go.transform;
        }

        RectTransform tlrt = label.GetComponent<RectTransform>();
        tlrt.anchorMin = Vector2.zero;
        tlrt.anchorMax = Vector2.one;
        tlrt.offsetMin = Vector2.zero;
        tlrt.offsetMax = Vector2.zero;
        Text t = label.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 18;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.text = "강화";
        t.raycastTarget = false;
    }

    static void EnsureOfflineRewardRoot(Canvas hud, GameObject player)
    {
        Component rewardMgr = GetOrAddComponentByTypeName(player, "OfflineRewardManager");
        Component popupUi = GetOrAddComponentByTypeName(player, "OfflineRewardPopupUI");

        if (rewardMgr != null)
        {
            using SerializedObject soMgr = new SerializedObject(rewardMgr);
            SerializedProperty p = soMgr.FindProperty("enableOfflineRewards");
            if (p != null) p.boolValue = true;
            soMgr.ApplyModifiedProperties();
        }

        Transform popup = hud.transform.Find("OfflineRewardPopup");
        Image backdrop;
        Text body;

        if (popup == null)
        {
            GameObject root = new GameObject("OfflineRewardPopup", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(root, "Offline popup");
            root.transform.SetParent(hud.transform, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520f, 120f);
            rt.anchoredPosition = new Vector2(0f, 140f);

            backdrop = root.GetComponent<Image>();
            backdrop.color = new Color(0.06f, 0.09f, 0.14f, 0.93f);

            GameObject txtGo =
                new GameObject("PopupText", typeof(RectTransform));
            txtGo.transform.SetParent(root.transform, false);
            var tr = txtGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(14f, 14f);
            tr.offsetMax = new Vector2(-14f, -14f);

            body = txtGo.AddComponent<Text>();
            body.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            body.fontSize = 20;
            body.alignment = TextAnchor.MiddleCenter;
            body.color = new Color(1f, 0.95f, 0.8f);
            body.text = "Offline Reward";

            popup = root.transform;
            root.SetActive(false);
        }
        else
        {
            backdrop = popup.GetComponent<Image>();
            Transform tBody = popup.Find("PopupText");
            body = tBody != null ? tBody.GetComponent<Text>() : popup.GetComponentInChildren<Text>();
        }

        if (popupUi != null && backdrop != null && body != null)
        {
            using SerializedObject sop = new SerializedObject(popupUi);
            sop.FindProperty("offlineRewardManager").objectReferenceValue =
                rewardMgr;
            sop.FindProperty("popupBackground").objectReferenceValue = backdrop;
            sop.FindProperty("popupText").objectReferenceValue = body;
            sop.ApplyModifiedProperties();
        }
    }

    static void EnsureBottomRightSkillSlots(Canvas hud, GameObject player)
    {
        Transform root = hud.transform.Find("BottomRight_SkillSlots");
        if (root == null)
        {
            GameObject go = new GameObject("BottomRight_SkillSlots", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "BottomRight_SkillSlots");
            go.transform.SetParent(hud.transform, false);
            root = go.transform;
        }

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 0f);
        rootRt.anchorMax = new Vector2(1f, 0f);
        rootRt.pivot = new Vector2(1f, 0f);
        rootRt.anchoredPosition = new Vector2(-18f, 18f);
        rootRt.sizeDelta = new Vector2(330f, 120f);

        Transform slot1 = EnsureSkillSlot(root, "SkillSlot_1", new Vector2(-220f, 0f), false);
        Transform slot2 = EnsureSkillSlot(root, "SkillSlot_2", new Vector2(-110f, 0f), false);
        Transform slot3 = EnsureSkillSlot(root, "SkillSlot_3", new Vector2(0f, 0f), false);

        System.Type loadoutType = System.Type.GetType("PlayerSkillLoadout, Assembly-CSharp");
        Component loadoutCmp = null;
        if (loadoutType != null)
        {
            loadoutCmp = player.GetComponent(loadoutType);
            if (loadoutCmp == null)
                loadoutCmp = Undo.AddComponent(player, loadoutType);
        }

        Component quickSlotsUi = GetOrAddComponentByTypeName(root.gameObject, "SkillQuickSlotsUI");
        if (quickSlotsUi == null)
            return;

        using SerializedObject so = new SerializedObject(quickSlotsUi);
        so.FindProperty("playerSkillLoadout").objectReferenceValue = loadoutCmp;
        so.FindProperty("slot1Root").objectReferenceValue = slot1;
        so.FindProperty("slot2Root").objectReferenceValue = slot2;
        so.FindProperty("slot3Root").objectReferenceValue = slot3;
        so.ApplyModifiedProperties();
    }

    static Component GetOrAddComponentByTypeName(GameObject go, string typeName)
    {
        Component found = go.GetComponent(typeName);
        if (found != null)
            return found;

        System.Type type = System.Type.GetType(typeName + ", Assembly-CSharp");
        if (type == null)
            return null;

        return Undo.AddComponent(go, type);
    }

    static Transform EnsureSkillSlot(Transform parent, string name, Vector2 anchoredPos, bool isLightningSlot)
    {
        Transform existing = parent.Find(name);
        GameObject slotGo;
        if (existing == null)
        {
            slotGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(slotGo, name);
            slotGo.transform.SetParent(parent, false);
            Image bg = slotGo.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            Button bt = slotGo.GetComponent<Button>();
            bt.targetGraphic = bg;
            bt.transition = Selectable.Transition.ColorTint;
        }
        else
        {
            slotGo = existing.gameObject;
        }

        RectTransform rt = slotGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(96f, 96f);
        rt.anchoredPosition = anchoredPos;

        Transform icon = slotGo.transform.Find("Icon");
        if (icon == null)
        {
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(iconGo, "Icon");
            iconGo.transform.SetParent(slotGo.transform, false);
            icon = iconGo.transform;
            RectTransform irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(86f, 86f);
            irt.anchoredPosition = Vector2.zero;
        }

        Image iconImg = icon.GetComponent<Image>();
        // 유저가 아이콘 스프라이트를 넣어둔 경우 기존 색을 유지
        if (iconImg.sprite == null)
        {
            iconImg.color = isLightningSlot ? new Color(1f, 0.84f, 0.22f, 1f) : new Color(0f, 0f, 0f, 0f);
        }

        Transform mask = slotGo.transform.Find("CooldownMask");
        if (mask == null)
        {
            GameObject maskGo = new GameObject("CooldownMask", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(maskGo, "CooldownMask");
            maskGo.transform.SetParent(slotGo.transform, false);
            mask = maskGo.transform;
            RectTransform mrt = maskGo.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero;
            mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero;
            mrt.offsetMax = Vector2.zero;
        }

        Image maskImg = mask.GetComponent<Image>();
        maskImg.color = new Color(0f, 0f, 0f, 0.62f);
        maskImg.type = Image.Type.Filled;
        maskImg.fillMethod = Image.FillMethod.Radial360;
        maskImg.fillOrigin = 2;
        maskImg.fillClockwise = false;
        maskImg.fillAmount = 0f;
        maskImg.raycastTarget = false;

        Transform text = slotGo.transform.Find("CooldownText");
        if (text == null)
        {
            GameObject textGo = new GameObject("CooldownText", typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(textGo, "CooldownText");
            textGo.transform.SetParent(slotGo.transform, false);
            text = textGo.transform;
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            Text txt = textGo.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 24;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = string.Empty;
            txt.raycastTarget = false;
        }

        Transform plus = slotGo.transform.Find("PlusText");
        if (!isLightningSlot)
        {
            if (plus == null)
            {
                GameObject plusGo = new GameObject("PlusText", typeof(RectTransform), typeof(Text));
                Undo.RegisterCreatedObjectUndo(plusGo, "PlusText");
                plusGo.transform.SetParent(slotGo.transform, false);
                plus = plusGo.transform;
                RectTransform prt = plusGo.GetComponent<RectTransform>();
                prt.anchorMin = Vector2.zero;
                prt.anchorMax = Vector2.one;
                prt.offsetMin = Vector2.zero;
                prt.offsetMax = Vector2.zero;
            }

            Text plusTxt = plus.GetComponent<Text>();
            plusTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            plusTxt.fontSize = 54;
            plusTxt.alignment = TextAnchor.MiddleCenter;
            plusTxt.color = new Color(1f, 1f, 1f, 0.92f);
            plusTxt.text = "+";
            plusTxt.raycastTarget = false;
        }
        else if (plus != null)
        {
            Undo.DestroyObjectImmediate(plus.gameObject);
        }

        return slotGo.transform;
    }
}
#endif
