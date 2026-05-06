using UnityEngine;

/// <summary>스킬 탭 1번(힐링) 보유 상태를 가정한 자동 체력 회복. 나중에 스킬 해금 데이터와 연결할 수 있습니다.</summary>
public class PassiveHealingSkill : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private float healAmount = 1f;
    [SerializeField] private float intervalSeconds = 2.5f;

    float _elapsed;

    void Reset()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (playerHealth == null || playerHealth.IsDead) return;

        _elapsed += Time.deltaTime;
        if (_elapsed < intervalSeconds) return;

        _elapsed = 0f;
        playerHealth.Heal(healAmount);
    }
}
