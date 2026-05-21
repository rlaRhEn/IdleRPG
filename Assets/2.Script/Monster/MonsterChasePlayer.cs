using UnityEngine;

/// <summary>
/// 단순 추격: 타깃을 향해 이동하다가 <see cref="stopDistance"/> 이내면 멈춥니다. AI보다 가벼운 전용 스크립트입니다.
/// </summary>
public class MonsterChasePlayer : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float stopDistance = 0.8f;

    private Rigidbody2D cachedRigidbody;
    private MonsterHealth monsterHealth;

    void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody2D>();
        monsterHealth = GetComponent<MonsterHealth>();
    }

    void FixedUpdate()
    {
        if (target == null || cachedRigidbody == null) return;
        if (monsterHealth != null && monsterHealth.IsDead)
        {
            cachedRigidbody.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = (Vector2)target.position - cachedRigidbody.position;
        float distance = toTarget.magnitude;
        if (distance <= stopDistance)
        {
            cachedRigidbody.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = toTarget.normalized;
        cachedRigidbody.linearVelocity = direction * moveSpeed;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetStopDistance(float newStopDistance)
    {
        stopDistance = Mathf.Max(0.05f, newStopDistance);
    }

    public void SetMoveSpeed(float newMoveSpeed)
    {
        moveSpeed = Mathf.Max(0.01f, newMoveSpeed);
    }
}

