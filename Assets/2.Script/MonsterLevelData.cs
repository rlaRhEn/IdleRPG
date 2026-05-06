using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterLevelData", menuName = "IdleRPG/Data/Monster Level Data")]
public class MonsterLevelData : ScriptableObject
{
    [Serializable]
    public class MonsterLevelEntry
    {
        public int level = 1;
        public Sprite sprite;
        public RuntimeAnimatorController animatorController;
        public float maxHealth = 10f;
        public float moveSpeed = 2.2f;
        public float attackDistance = 0.8f;
        public float contactDamage = 0.1f;
        public float attacksPerSecond = 1f;
        public float deathReturnDelay = 0.8f;
        public int rewardGold = 1;
        public int rewardExperience = 1;
    }

    [SerializeField] private List<MonsterLevelEntry> levelEntries = new List<MonsterLevelEntry>();

    public MonsterLevelEntry GetEntry(int level)
    {
        if (levelEntries == null || levelEntries.Count == 0) return null;

        MonsterLevelEntry fallback = levelEntries[0];
        for (int i = 0; i < levelEntries.Count; i++)
        {
            MonsterLevelEntry entry = levelEntries[i];
            if (entry == null) continue;
            if (entry.level == level) return entry;
            if (entry.level <= level) fallback = entry;
        }

        return fallback;
    }
}

