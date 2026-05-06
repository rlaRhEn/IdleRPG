using System.Collections;
using UnityEngine;

public enum PlayerState{Idle, Chase, Attack}
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform detectedTarget;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMeleeAttack meleeAttack;
    [SerializeField] private float detectedDistance = 20f; // 적 탐색
    [SerializeField] private float stopDistance = 1.0f; //적과의 거리
    public float AttackRange => stopDistance;

    
    
    Animator ani;
    Rigidbody2D rb;
    PlayerState currentState = PlayerState.Idle;
    void Awake()
    {
        ani = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (meleeAttack == null)
            meleeAttack = GetComponent<PlayerMeleeAttack>();
    }


    void FixedUpdate()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        switch(currentState)
        {
            case PlayerState.Idle:
                DetectEnemy();
                break;
            case PlayerState.Chase:
                ChaseEnemy();
                break;
            case PlayerState.Attack:
                AttackEnemy();
                break;
            }

    }
    void ChangeState(PlayerState state)
    {
        if(currentState == state) return;
        Debug.Log($"CurrentState:  {state}");
        currentState = state;
    }

    void UpdateFlip()
    {
        if (spriteRenderer == null) return; // 컴포넌트 미설정 시 NRE 방지
        if (!IsTargetValid()) return;

        float dx = detectedTarget.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.0001f) return; // 완전 정면이면 불필요한 토글 방지

        // 방치형: X좌표 차이만 보고 flipX를 결정
        bool targetIsLeft = dx < 0f;
        spriteRenderer.flipX = targetIsLeft;
    }

    bool IsTargetValid()
    {
        if (detectedTarget == null) return false;
        if (!detectedTarget.gameObject.activeInHierarchy) return false;

        MonsterHealth monsterHealth = detectedTarget.GetComponent<MonsterHealth>();
        if (monsterHealth != null && monsterHealth.IsDead) return false;

        return true;
    }

    void DetectEnemy()
    {
        if (meleeAttack != null)
            meleeAttack.ResetAnimationSpeed(ani);

        rb.linearVelocity = Vector2.zero;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            detectedDistance,
            LayerMask.GetMask("Enemy"));

        Transform closestTarget = null;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.gameObject.activeInHierarchy) continue;

            MonsterHealth monsterHealth = hit.GetComponent<MonsterHealth>();
            if (monsterHealth != null && monsterHealth.IsDead) continue;

            float sqrDistance = ((Vector2)hit.transform.position - rb.position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestTarget = hit.transform;
            }
        }

        detectedTarget = closestTarget;
        if (detectedTarget != null) 
        {
            UpdateFlip();
            ChangeState(PlayerState.Chase);
        }
    }

    void ChaseEnemy()
    {
        if (meleeAttack != null)
            meleeAttack.ResetAnimationSpeed(ani);

        if (!IsTargetValid())
        {
            detectedTarget = null;
            rb.linearVelocity = Vector2.zero;
            ani.SetBool("DoRun", false);
            ChangeState(PlayerState.Idle);
            return;
        }

        Vector2 toTarget = (Vector2)detectedTarget.position - rb.position;
        UpdateFlip();
        float distance = toTarget.magnitude;
        if(distance <= stopDistance)
        {
            ChangeState(PlayerState.Attack);
            ani.SetBool("DoRun", false);
            rb.linearVelocity = Vector2.zero;
            return;
        }
        ani.SetBool("DoRun", true);
        Vector2 direction = toTarget.normalized;
        rb.linearVelocity = direction * moveSpeed;
        
    }
    void AttackEnemy()
    {
        if (!IsTargetValid())
        {
            detectedTarget = null;
            if (meleeAttack != null)
                meleeAttack.ResetAnimationSpeed(ani);
            ani.SetBool("DoRun", false);
            ChangeState(PlayerState.Idle);
            return;
        }

        // 공격 중 거리 벗어나면 다시 추격
        Vector2 toTarget = (Vector2)detectedTarget.position - rb.position;
        float distance = toTarget.magnitude;
        if (distance > stopDistance)
        {
            if (meleeAttack != null)
                meleeAttack.ResetAnimationSpeed(ani);
            ChangeState(PlayerState.Chase);
            ani.SetBool("DoRun", true);
            Vector2 direction = toTarget.normalized;
            UpdateFlip();
            rb.linearVelocity = direction * moveSpeed;
            return;
        }

        UpdateFlip();
        ani.SetBool("DoRun", false);

        // 공격 로직(쿨다운은 PlayerMeleeAttack에서 처리)
        if (meleeAttack != null)
        {
            meleeAttack.ApplyAttackAnimationSpeed(ani);
            if (meleeAttack.BeginAttack(detectedTarget))
                ani.SetTrigger("DoAttack");
        }
        else
        {
            // 컴포넌트가 연결되지 않은 경우(임시) 기존 동작 유지
            ani.SetTrigger("DoAttack");
        }

        rb.linearVelocity = Vector2.zero;
        
    }

}
