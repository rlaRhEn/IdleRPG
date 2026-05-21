using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 오브젝트 풀링·주기적 스폰, 플레이어 레벨에 따른 몬스터 단계 매핑, <see cref="MonsterLevelData"/>로 프리셋 적용.
/// </summary>
public class MonsterSpawnerPool : MonoBehaviour
{
    /// <summary>플레이어 레벨 구간 → 스폰할 몬스터 난이도 단계 매핑 한 줄.</summary>
    [System.Serializable]
    private class MonsterLevelMapEntry
    {
        public int minPlayerLevel = 1;
        public int monsterLevel = 1;
    }

    [SerializeField] private Transform player;
    [SerializeField] private MonsterHealth monsterPrefab;
    [SerializeField] private int initialPoolSize = 40;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private float minSpawnRadius = 0.2f;
    [SerializeField, Range(0.1f, 1f)] private float maxSpawnRadiusRatioToAttackRange = 0.9f;
    [SerializeField] private int maxAliveMonsters = 40;
    [SerializeField] private MonsterLevelData monsterLevelData;
    [SerializeField] private int currentMonsterLevel = 1;
    [SerializeField] private bool autoMapMonsterLevelByPlayerLevel = true;
    [SerializeField] private List<MonsterLevelMapEntry> monsterLevelMap = new List<MonsterLevelMapEntry>
    {
        new MonsterLevelMapEntry { minPlayerLevel = 1, monsterLevel = 1 },
        new MonsterLevelMapEntry { minPlayerLevel = 3, monsterLevel = 2 },
        new MonsterLevelMapEntry { minPlayerLevel = 5, monsterLevel = 3 },
        new MonsterLevelMapEntry { minPlayerLevel = 8, monsterLevel = 4 },
        new MonsterLevelMapEntry { minPlayerLevel = 12, monsterLevel = 5 },
    };

    private readonly Queue<MonsterHealth> pool = new Queue<MonsterHealth>();
    private readonly HashSet<MonsterHealth> aliveMonsters = new HashSet<MonsterHealth>();
    private float spawnTimer;
    private PlayerController playerController;
    private PlayerProgression playerProgression;
    private int lastMappedPlayerLevel = -1;

    void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null) playerObject = GameObject.Find("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            playerProgression = player.GetComponent<PlayerProgression>();
        }

        PrewarmPool();
    }

    void Update()
    {
        if (player == null || monsterPrefab == null) return;
        UpdateMonsterLevelMapping();
        if (aliveMonsters.Count >= maxAliveMonsters) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer < spawnInterval) return;

        spawnTimer = 0f;
        SpawnFromPool();
    }

    void UpdateMonsterLevelMapping()
    {
        if (!autoMapMonsterLevelByPlayerLevel) return;
        if (playerProgression == null) return;

        int playerLevel = Mathf.Max(1, playerProgression.CurrentLevel);
        if (playerLevel == lastMappedPlayerLevel) return;

        lastMappedPlayerLevel = playerLevel;
        currentMonsterLevel = GetMappedMonsterLevel(playerLevel);
    }

    int GetMappedMonsterLevel(int playerLevel)
    {
        if (monsterLevelMap == null || monsterLevelMap.Count == 0)
            return Mathf.Max(1, currentMonsterLevel);

        int mappedLevel = 1;
        int highestMatchedMinLevel = int.MinValue;

        for (int i = 0; i < monsterLevelMap.Count; i++)
        {
            MonsterLevelMapEntry entry = monsterLevelMap[i];
            if (entry == null) continue;
            if (entry.minPlayerLevel > playerLevel) continue;
            if (entry.minPlayerLevel < highestMatchedMinLevel) continue;

            highestMatchedMinLevel = entry.minPlayerLevel;
            mappedLevel = Mathf.Max(1, entry.monsterLevel);
        }

        return mappedLevel;
    }

    void PrewarmPool()
    {
        if (monsterPrefab == null) return;

        for (int i = 0; i < initialPoolSize; i++)
        {
            MonsterHealth monster = CreatePooledMonster();
            monster.gameObject.SetActive(false);
            pool.Enqueue(monster);
        }
    }

    MonsterHealth CreatePooledMonster()
    {
        MonsterHealth monster = Instantiate(monsterPrefab, transform);
        monster.SetPoolReturnAction(ReturnToPool);
        PrepareMonsterComponents(monster);
        return monster;
    }

    void PrepareMonsterComponents(MonsterHealth monster)
    {
        MonsterMeleeDamageOnCollision damageOnCollision = monster.GetComponent<MonsterMeleeDamageOnCollision>();
        if (damageOnCollision != null) damageOnCollision.enabled = false;

        MonsterMeleeAttack meleeAttack = monster.GetComponent<MonsterMeleeAttack>();
        if (meleeAttack == null)
            meleeAttack = monster.gameObject.AddComponent<MonsterMeleeAttack>();

        MonsterController monsterController = monster.GetComponent<MonsterController>();
        if (monsterController == null)
            monsterController = monster.gameObject.AddComponent<MonsterController>();

        monsterController.SetTarget(player);

        Rigidbody2D monsterRb = monster.GetComponent<Rigidbody2D>();
        if (monsterRb != null)
            monsterRb.linearDamping = 0f;
    }

    void SpawnFromPool()
    {
        if (pool.Count == 0) return;

        MonsterHealth monster = pool.Dequeue();
        if (monster == null) return;

        ApplyMonsterLevelPreset(monster);
        monster.transform.position = GetSpawnPosition();
        monster.transform.rotation = Quaternion.identity;
        monster.gameObject.SetActive(true);
        aliveMonsters.Add(monster);
    }

    void ApplyMonsterLevelPreset(MonsterHealth monster)
    {
        MonsterLevelData.MonsterLevelEntry entry =
            monsterLevelData != null ? monsterLevelData.GetEntry(currentMonsterLevel) : null;
        if (entry == null) return;

        SpriteRenderer spriteRenderer = monster.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && entry.sprite != null)
            spriteRenderer.sprite = entry.sprite;

        Animator animator = monster.GetComponent<Animator>();
        if (animator != null && entry.animatorController != null)
            animator.runtimeAnimatorController = entry.animatorController;

        MonsterController monsterController = monster.GetComponent<MonsterController>();
        if (monsterController != null)
        {
            monsterController.SetMoveSpeed(entry.moveSpeed);
            monsterController.SetAttackDistance(entry.attackDistance);
            monsterController.SetDeathReturnDelay(entry.deathReturnDelay);
            monsterController.RefreshAnimatorCache();
        }

        MonsterMeleeAttack meleeAttack = monster.GetComponent<MonsterMeleeAttack>();
        if (meleeAttack != null)
        {
            meleeAttack.SetDamage(entry.contactDamage);
            meleeAttack.SetAttacksPerSecond(entry.attacksPerSecond);
        }

        monster.SetMaxHealth(entry.maxHealth);
        monster.SetRewardInfo(playerProgression, entry.rewardGold, entry.rewardExperience);
    }

    Vector3 GetSpawnPosition()
    {
        float attackRange = playerController != null ? playerController.AttackRange : 1f;
        float maxRadius = Mathf.Max(minSpawnRadius, attackRange * maxSpawnRadiusRatioToAttackRange);
        float minRadius = Mathf.Min(minSpawnRadius, maxRadius);

        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        float radius = Random.Range(minRadius, maxRadius);
        Vector3 offset = new Vector3(direction.x, direction.y, 0f) * radius;
        Vector3 basePosition = player.position;
        return new Vector3(basePosition.x + offset.x, basePosition.y + offset.y, basePosition.z);
    }

    void ReturnToPool(MonsterHealth monster)
    {
        if (monster == null) return;

        aliveMonsters.Remove(monster);
        monster.gameObject.SetActive(false);
        pool.Enqueue(monster);
    }

    public void SetCurrentMonsterLevel(int newLevel)
    {
        currentMonsterLevel = Mathf.Max(1, newLevel);
    }
}

