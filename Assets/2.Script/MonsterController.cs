using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float attackDistance = 0.8f;
    [SerializeField] private float detectDistance = 20f;
    [SerializeField] private float deathReturnDelay = 0.8f;
    [SerializeField] private string runBoolParam = "DoRun";
    [SerializeField] private string attackTriggerParam = "DoAttack";
    [SerializeField] private string dieTriggerParam = "DoDie";

    private Animator cachedAnimator;
    private Rigidbody2D cachedRigidbody;
    private SpriteRenderer cachedSpriteRenderer;
    private MonsterHealth monsterHealth;
    private MonsterMeleeAttack meleeAttack;
    private PlayerHealth targetHealth;
    private HashSet<string> animatorParams = new HashSet<string>();
    private Coroutine deathRoutine;

    void Awake()
    {
        cachedAnimator = GetComponent<Animator>();
        cachedRigidbody = GetComponent<Rigidbody2D>();
        cachedSpriteRenderer = GetComponent<SpriteRenderer>();
        monsterHealth = GetComponent<MonsterHealth>();
        meleeAttack = GetComponent<MonsterMeleeAttack>();

        if (target == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null) playerObject = GameObject.Find("Player");
            if (playerObject != null) target = playerObject.transform;
        }

        if (target != null)
            targetHealth = target.GetComponent<PlayerHealth>();

        CacheAnimatorParams();
    }

    void OnEnable()
    {
        if (cachedRigidbody != null)
            cachedRigidbody.linearVelocity = Vector2.zero;

        if (monsterHealth != null)
        {
            monsterHealth.SetAutoFinalizeDeath(false);
            monsterHealth.Died += OnDied;
        }
    }

    void OnDisable()
    {
        if (monsterHealth != null)
            monsterHealth.Died -= OnDied;

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }
    }

    void FixedUpdate()
    {
        if (monsterHealth != null && monsterHealth.IsDead) return;
        if (!IsTargetValid())
        {
            SetRun(false);
            if (cachedRigidbody != null)
                cachedRigidbody.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = (Vector2)target.position - cachedRigidbody.position;
        float distance = toTarget.magnitude;
        UpdateFlipByTargetX();

        if (distance <= attackDistance)
        {
            SetRun(false);
            cachedRigidbody.linearVelocity = Vector2.zero;

            if (meleeAttack != null)
            {
                meleeAttack.ApplyAttackAnimationSpeed(cachedAnimator);
                if (meleeAttack.BeginAttack(targetHealth))
                    SetTrigger(attackTriggerParam);
            }
            return;
        }

        if (distance <= detectDistance)
        {
            if (meleeAttack != null)
                meleeAttack.ResetAnimationSpeed(cachedAnimator);

            SetRun(true);
            Vector2 direction = toTarget.normalized;
            cachedRigidbody.linearVelocity = direction * moveSpeed;
            return;
        }

        if (meleeAttack != null)
            meleeAttack.ResetAnimationSpeed(cachedAnimator);
        SetRun(false);
        cachedRigidbody.linearVelocity = Vector2.zero;
    }

    void OnDied()
    {
        SetRun(false);
        SetTrigger(dieTriggerParam);

        if (deathRoutine != null)
            StopCoroutine(deathRoutine);

        deathRoutine = StartCoroutine(ReturnAfterDeathAnimation());
    }

    IEnumerator ReturnAfterDeathAnimation()
    {
        yield return new WaitForSeconds(deathReturnDelay);
        if (monsterHealth != null)
            monsterHealth.FinalizeDeath();
    }

    bool IsTargetValid()
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        if (targetHealth != null && targetHealth.IsDead) return false;
        if (cachedRigidbody == null) return false;
        return true;
    }

    void UpdateFlipByTargetX()
    {
        if (cachedSpriteRenderer == null || target == null) return;

        float dx = target.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.0001f) return;
        cachedSpriteRenderer.flipX = dx < 0f;
    }

    void CacheAnimatorParams()
    {
        animatorParams.Clear();
        if (cachedAnimator == null) return;

        AnimatorControllerParameter[] parameters = cachedAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
            animatorParams.Add(parameters[i].name);
    }

    void SetRun(bool isRunning)
    {
        if (cachedAnimator == null || !animatorParams.Contains(runBoolParam)) return;
        cachedAnimator.SetBool(runBoolParam, isRunning);
    }

    void SetTrigger(string triggerName)
    {
        if (cachedAnimator == null || string.IsNullOrEmpty(triggerName)) return;
        if (!animatorParams.Contains(triggerName)) return;
        cachedAnimator.SetTrigger(triggerName);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetHealth = target != null ? target.GetComponent<PlayerHealth>() : null;
    }

    public void SetMoveSpeed(float newMoveSpeed)
    {
        moveSpeed = Mathf.Max(0.01f, newMoveSpeed);
    }

    public void SetAttackDistance(float newAttackDistance)
    {
        attackDistance = Mathf.Max(0.05f, newAttackDistance);
    }

    public void SetDeathReturnDelay(float newDelay)
    {
        deathReturnDelay = Mathf.Max(0.01f, newDelay);
    }

    public void RefreshAnimatorCache()
    {
        CacheAnimatorParams();
    }
}

