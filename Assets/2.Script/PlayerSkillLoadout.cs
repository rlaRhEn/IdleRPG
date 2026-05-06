using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>구매/장착/자동 사용을 담당하는 간단한 스킬 로드아웃 컨트롤러.</summary>
public class PlayerSkillLoadout : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMeleeAttack playerMeleeAttack;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject iceBurstFxPrefab;

    [Header("힐링")]
    [SerializeField] private float healAmount = 2f;
    [SerializeField] private float healCooldown = 2.5f;

    [Header("폭발")]
    [SerializeField] private float fireDamageMultiplier = 2f;
    [SerializeField] private float fireCooldown = 4f;
    [SerializeField] private float fireRadius = 5.5f;
    [SerializeField] private float fireFallbackDamage = 6f;
    [SerializeField] private float fireTargetSearchRadius = 12f;
    [SerializeField] private float fireCastLockDuration = 0.35f;

    readonly bool[] _owned = new bool[2];
    readonly int[] _equipped = { -1, -1, -1 };
    readonly float[] _cooldownTimer = new float[2];

    public event Action LoadoutChanged;
    public event Action CooldownChanged;

    void Awake()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (playerMeleeAttack == null) playerMeleeAttack = GetComponent<PlayerMeleeAttack>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (enemyLayerMask.value == 0) enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.IsDead) return;

        HashSet<int> activeSkills = new HashSet<int>();
        for (int i = 0; i < _equipped.Length; i++)
        {
            int id = _equipped[i];
            if (id >= 0 && id < _owned.Length && _owned[id])
                activeSkills.Add(id);
        }

        bool changed = false;
        foreach (int skillId in activeSkills)
        {
            _cooldownTimer[skillId] += Time.deltaTime;
            float cooldown = GetCooldown(skillId);
            if (_cooldownTimer[skillId] < cooldown) continue;

            bool used = ExecuteSkill(skillId);
            if (used)
            {
                _cooldownTimer[skillId] = 0f;
                changed = true;
            }
            else
            {
                // 조건 미충족 시 즉시 재시도 가능하도록 쿨타임 소모를 막습니다.
                _cooldownTimer[skillId] = cooldown;
            }
        }

        // 쿨다운 UI가 부드럽게 돌도록 매 프레임 갱신 신호를 보냅니다.
        if (activeSkills.Count > 0 || changed)
            CooldownChanged?.Invoke();
    }

    public bool IsOwned(int skillId) => skillId >= 0 && skillId < _owned.Length && _owned[skillId];
    public int OwnedSkillCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _owned.Length; i++)
                if (_owned[i]) count++;
            return count;
        }
    }

    public int EquippedSkillCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _equipped.Length; i++)
                if (_equipped[i] >= 0) count++;
            return count;
        }
    }

    public int GetEquippedSkillId(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _equipped.Length) return -1;
        return _equipped[slotIndex];
    }

    public bool IsEquipped(int skillId)
    {
        for (int i = 0; i < _equipped.Length; i++)
            if (_equipped[i] == skillId)
                return true;
        return false;
    }

    public bool UnlockSkill(int skillId)
    {
        if (skillId < 0 || skillId >= _owned.Length) return false;
        if (_owned[skillId]) return true;
        _owned[skillId] = true;
        _cooldownTimer[skillId] = 0f;
        LoadoutChanged?.Invoke();
        CooldownChanged?.Invoke();
        return true;
    }

    public bool EquipSkillToFirstAvailable(int skillId)
    {
        if (!IsOwned(skillId)) return false;
        if (IsEquipped(skillId)) return true;

        for (int i = 0; i < _equipped.Length; i++)
        {
            if (_equipped[i] != -1) continue;
            _equipped[i] = skillId;
            LoadoutChanged?.Invoke();
            CooldownChanged?.Invoke();
            return true;
        }

        _equipped[0] = skillId;
        LoadoutChanged?.Invoke();
        CooldownChanged?.Invoke();
        return true;
    }

    public string GetOwnedStateString()
    {
        char[] chars = new char[_owned.Length];
        for (int i = 0; i < _owned.Length; i++)
            chars[i] = _owned[i] ? '1' : '0';
        return new string(chars);
    }

    public string GetEquippedStateString()
    {
        return string.Join(",", _equipped);
    }

    public void LoadState(string ownedState, string equippedState)
    {
        if (!string.IsNullOrEmpty(ownedState))
        {
            int max = Mathf.Min(_owned.Length, ownedState.Length);
            for (int i = 0; i < max; i++)
                _owned[i] = ownedState[i] == '1';
        }

        if (!string.IsNullOrEmpty(equippedState))
        {
            string[] parts = equippedState.Split(',');
            int max = Mathf.Min(_equipped.Length, parts.Length);
            for (int i = 0; i < max; i++)
            {
                if (int.TryParse(parts[i], out int parsed))
                    _equipped[i] = parsed;
            }
        }

        for (int i = 0; i < _equipped.Length; i++)
        {
            int id = _equipped[i];
            if (id < 0 || id >= _owned.Length || !_owned[id])
                _equipped[i] = -1;
        }

        for (int i = 0; i < _cooldownTimer.Length; i++)
            _cooldownTimer[i] = 0f;

        LoadoutChanged?.Invoke();
        CooldownChanged?.Invoke();
    }

    public void ResetLoadoutForTest()
    {
        for (int i = 0; i < _owned.Length; i++)
            _owned[i] = false;
        for (int i = 0; i < _equipped.Length; i++)
            _equipped[i] = -1;
        for (int i = 0; i < _cooldownTimer.Length; i++)
            _cooldownTimer[i] = 0f;

        LoadoutChanged?.Invoke();
        CooldownChanged?.Invoke();
    }

    public float GetCooldownRemaining(int skillId)
    {
        if (skillId < 0 || skillId >= _owned.Length) return 0f;
        if (!IsEquipped(skillId)) return 0f;
        float cd = GetCooldown(skillId);
        return Mathf.Clamp(cd - _cooldownTimer[skillId], 0f, cd);
    }

    public float GetCooldown(int skillId)
    {
        return skillId == 0 ? Mathf.Max(0.1f, healCooldown) : Mathf.Max(0.1f, fireCooldown);
    }

    bool ExecuteSkill(int skillId)
    {
        if (skillId == 0)
        {
            if (playerHealth == null) return false;

            float missingHealth = playerHealth.MaxHealth - playerHealth.CurrentHealth;
            if (missingHealth <= 0f) return false;
            if (missingHealth > healAmount) return false;

            playerHealth.Heal(healAmount);
            return true;
        }

        float baseDamage = playerMeleeAttack != null ? playerMeleeAttack.CurrentDamage : fireFallbackDamage;
        float finalDamage = Mathf.Max(1f, baseDamage * fireDamageMultiplier);
        Transform farthest = FindFarthestEnemy();
        if (farthest == null) return false;

        if (playerController != null)
            playerController.SendMessage("BeginSkillCast", fireCastLockDuration, SendMessageOptions.DontRequireReceiver);

        if (bulletPrefab != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            Component projectile = bullet.GetComponent("BulletSkillProjectile");
            if (projectile == null)
            {
                Type projectileType = Type.GetType("BulletSkillProjectile, Assembly-CSharp");
                if (projectileType != null)
                    projectile = bullet.AddComponent(projectileType);
            }

            if (projectile != null)
            {
                // Launch(Transform target, float aoeRadius, float damage, LayerMask enemyMask)
                projectile.GetType().GetMethod("Launch")?.Invoke(
                    projectile,
                    new object[] { farthest, fireRadius, finalDamage, enemyLayerMask, iceBurstFxPrefab });
            }
            return true;
        }

        // 프리팹이 없을 때의 안전장치: 즉발 광역
        Collider2D[] hits = Physics2D.OverlapCircleAll(farthest.position, fireRadius, enemyLayerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            MonsterHealth mh = hits[i].GetComponent<MonsterHealth>();
            if (mh == null || mh.IsDead) continue;
            mh.TakeDamage(finalDamage, false);
        }
        return true;
    }

    Transform FindFarthestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, fireTargetSearchRadius, enemyLayerMask);
        Transform farthest = null;
        float farthestSqr = -1f;

        for (int i = 0; i < hits.Length; i++)
        {
            MonsterHealth mh = hits[i].GetComponent<MonsterHealth>();
            if (mh == null || mh.IsDead) continue;

            float sqr = ((Vector2)hits[i].transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr <= farthestSqr) continue;
            farthestSqr = sqr;
            farthest = hits[i].transform;
        }

        return farthest;
    }
}
