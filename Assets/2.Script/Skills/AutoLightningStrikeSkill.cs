using System.Collections;
using UnityEngine;

/// <summary>
/// 해금 시 일정 간격으로 주변 적에게 번개 피해·라인 FX. 무기 0번 해금 시 <see cref="PlayerWeaponLoadout"/>에서 활성화할 수 있습니다.
/// </summary>
public class AutoLightningStrikeSkill : MonoBehaviour
{
    [SerializeField] private bool unlocked = false;
    [SerializeField] private float castInterval = 3.5f;
    [SerializeField] private float searchRadius = 6f;
    [SerializeField] private int chainCount = 2;
    [SerializeField] private float damageMultiplier = 1.4f;
    [SerializeField] private float fallbackDamage = 3f;
    [SerializeField] [Range(0f, 1f)] private float criticalChance = 0.2f;
    [SerializeField] private float criticalDamageMultiplier = 2f;
    [SerializeField] private float strikeHeight = 3f;
    [SerializeField] private float lineDuration = 0.08f;
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private LayerMask enemyLayerMask;

    private float castTimer;
    private PlayerHealth playerHealth;
    private PlayerMeleeAttack playerMeleeAttack;
    private float CooldownDuration => Mathf.Max(0.1f, castInterval);

    public float CooldownTime => CooldownDuration;
    public float CooldownRemaining => Mathf.Clamp(CooldownDuration - castTimer, 0f, CooldownDuration);
    public float CooldownNormalized => CooldownDuration <= 0f ? 0f : CooldownRemaining / CooldownDuration;
    public event System.Action<float, float> CooldownChanged;
    public event System.Action LightningCastTriggered;

    public bool IsUnlocked => unlocked;

    public void SetUnlocked(bool value)
    {
        unlocked = value;
        if (!unlocked)
            castTimer = 0f;
        NotifyCooldownChanged();
    }

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerMeleeAttack = GetComponent<PlayerMeleeAttack>();

        if (enemyLayerMask.value == 0)
            enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    void Update()
    {
        if (!unlocked || (playerHealth != null && playerHealth.IsDead))
        {
            NotifyCooldownChanged();
            return;
        }

        castTimer += Time.deltaTime;
        if (castTimer < CooldownDuration)
        {
            NotifyCooldownChanged();
            return;
        }

        castTimer = 0f;
        CastLightning();
        LightningCastTriggered?.Invoke();
        NotifyCooldownChanged();
    }

    void CastLightning()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius, enemyLayerMask);
        if (colliders == null || colliders.Length == 0) return;

        float baseDamage = playerMeleeAttack != null ? playerMeleeAttack.CurrentDamage : fallbackDamage;
        float scaledDamage = Mathf.Max(0.1f, baseDamage * damageMultiplier);

        int hitCount = 0;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (hitCount >= Mathf.Max(1, chainCount)) break;

            Collider2D col = colliders[i];
            if (col == null) continue;

            MonsterHealth monster = col.GetComponent<MonsterHealth>();
            if (monster == null || monster.IsDead) continue;

            bool isCritical = Random.value < criticalChance;
            float finalDamage = isCritical ? scaledDamage * criticalDamageMultiplier : scaledDamage;
            monster.TakeDamage(finalDamage, isCritical);

            ShowLightning(monster.transform.position);
            hitCount++;
        }
    }

    void ShowLightning(Vector3 targetPosition)
    {
        Vector3 start = targetPosition + Vector3.up * strikeHeight;
        GameObject lineObject = new GameObject("LightningFx");
        LineRenderer lr = lineObject.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, targetPosition);
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth * 0.55f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.95f, 0.25f, 1f);
        lr.endColor = new Color(1f, 0.75f, 0.1f, 0.9f);
        lr.sortingOrder = 100;
        StartCoroutine(FadeAndDestroy(lr, lineObject));
    }

    IEnumerator FadeAndDestroy(LineRenderer lr, GameObject lineObject)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, lineDuration);

        Color startA = lr.startColor;
        Color endA = lr.endColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Color s = startA;
            Color e = endA;
            s.a = Mathf.Lerp(startA.a, 0f, t);
            e.a = Mathf.Lerp(endA.a, 0f, t);
            lr.startColor = s;
            lr.endColor = e;

            yield return null;
        }

        if (lineObject != null)
            Destroy(lineObject);
    }

    void OnEnable()
    {
        NotifyCooldownChanged();
    }

    void NotifyCooldownChanged()
    {
        CooldownChanged?.Invoke(CooldownRemaining, CooldownDuration);
    }
}
