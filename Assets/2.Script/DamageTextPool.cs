using System.Collections.Generic;
using UnityEngine;

public enum CombatTextKind
{
    MonsterHitNormal,
    MonsterHitCritical,
    PlayerHitNormal,
    PlayerHitCritical,
    Heal
}

public class DamageTextPool : MonoBehaviour
{
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private float randomXOffset = 0.2f;
    [SerializeField] private float spawnYOffset = 0.6f;
    [SerializeField] private Color monsterHitColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color monsterCritColor = new Color(1f, 0.45f, 0.1f, 1f);
    [SerializeField] private Color playerHitColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color playerCritColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color healColor = new Color(0.35f, 1f, 0.55f, 1f);
    [SerializeField] private float critCharacterScale = 1.35f;
    [SerializeField] private float critFloatSpeedScale = 1.15f;
    [SerializeField] private float critLifeTimeScale = 1.1f;

    private readonly Queue<DamageTextItem> pooledItems = new Queue<DamageTextItem>();
    private static DamageTextPool instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Prewarm();
    }

    void Prewarm()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            DamageTextItem item = CreateItem();
            item.gameObject.SetActive(false);
            pooledItems.Enqueue(item);
        }
    }

    DamageTextItem CreateItem()
    {
        GameObject itemObject = new GameObject("DamageTextItem");
        itemObject.transform.SetParent(transform, false);
        TextMesh textMesh = itemObject.AddComponent<TextMesh>();
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.07f;
        textMesh.fontSize = 72;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.text = "0";
        textMesh.color = monsterHitColor;

        DamageTextItem item = itemObject.AddComponent<DamageTextItem>();
        return item;
    }

    /// <summary>하위 호환: 몬스터 피격 일반 데미지.</summary>
    public static void ShowDamage(Vector3 worldPosition, float damage, bool isPlayerTarget)
    {
        CombatTextKind kind = isPlayerTarget ? CombatTextKind.PlayerHitNormal : CombatTextKind.MonsterHitNormal;
        Show(worldPosition, damage, kind);
    }

    public static void Show(Vector3 worldPosition, float amount, CombatTextKind kind)
    {
        DamageTextPool pool = GetOrCreatePool();
        if (pool == null) return;
        pool.Spawn(worldPosition, amount, kind);
    }

    static DamageTextPool GetOrCreatePool()
    {
        if (instance != null) return instance;

#pragma warning disable CS0618
        DamageTextPool existing = Object.FindObjectOfType<DamageTextPool>();
#pragma warning restore CS0618
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject root = new GameObject("DamageTextPool");
        instance = root.AddComponent<DamageTextPool>();
        return instance;
    }

    void Spawn(Vector3 worldPosition, float amount, CombatTextKind kind)
    {
        DamageTextItem item = pooledItems.Count > 0 ? pooledItems.Dequeue() : CreateItem();
        if (item == null) return;

        Vector3 spawnPosition = worldPosition;
        spawnPosition.x += Random.Range(-randomXOffset, randomXOffset);
        spawnPosition.y += spawnYOffset;

        bool isCrit = kind == CombatTextKind.MonsterHitCritical || kind == CombatTextKind.PlayerHitCritical;
        Color color = monsterHitColor;
        string text = Mathf.Max(0f, amount).ToString("0.0");

        switch (kind)
        {
            case CombatTextKind.MonsterHitCritical:
                color = monsterCritColor;
                text = Mathf.Max(0f, amount).ToString("0.0") + "!";
                break;
            case CombatTextKind.MonsterHitNormal:
                color = monsterHitColor;
                text = Mathf.Max(0f, amount).ToString("0.0");
                break;
            case CombatTextKind.PlayerHitCritical:
                color = playerCritColor;
                text = Mathf.Max(0f, amount).ToString("0.0") + "!";
                break;
            case CombatTextKind.PlayerHitNormal:
                color = playerHitColor;
                text = Mathf.Max(0f, amount).ToString("0.0");
                break;
            case CombatTextKind.Heal:
                color = healColor;
                text = "+" + Mathf.Max(0f, amount).ToString("0.0");
                break;
        }

        float sizeScale = isCrit ? critCharacterScale : 1f;
        float floatScale = isCrit ? critFloatSpeedScale : 1f;
        float lifeScale = isCrit ? critLifeTimeScale : 1f;

        item.gameObject.SetActive(true);
        item.Play(text, spawnPosition, color, ReturnToPool, sizeScale, floatScale, lifeScale);
    }

    void ReturnToPool(DamageTextItem item)
    {
        if (item == null) return;
        item.gameObject.SetActive(false);
        pooledItems.Enqueue(item);
    }
}
