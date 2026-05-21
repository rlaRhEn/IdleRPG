using UnityEngine;

/// <summary>2번 스킬용 탄환. 대상까지 비행 후 폭발(광역) 처리.</summary>
public class BulletSkillProjectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float hitDistance = 0.2f;
    [SerializeField] private GameObject explosionFxPrefab;

    Transform _target;
    float _aoeRadius;
    float _damage;
    LayerMask _enemyMask;
    bool _exploded;
    Animator _animator;
    SpriteRenderer _spriteRenderer;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Launch(Transform target, float aoeRadius, float damage, LayerMask enemyMask, GameObject fxPrefab = null)
    {
        _target = target;
        _aoeRadius = Mathf.Max(0.1f, aoeRadius);
        _damage = Mathf.Max(0.1f, damage);
        _enemyMask = enemyMask;
        if (fxPrefab != null)
            explosionFxPrefab = fxPrefab;

        if (_animator != null)
        {
            _animator.SetBool("DoRun", true);
            _animator.Play("Run", 0, 0f);
        }
    }

    void Update()
    {
        if (_exploded) return;
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            Explode();
            return;
        }

        Vector3 dest = _target.position;
        transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);

        if ((transform.position - dest).sqrMagnitude <= hitDistance * hitDistance)
            Explode();
    }

    void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _aoeRadius, _enemyMask);
        for (int i = 0; i < hits.Length; i++)
        {
            MonsterHealth mh = hits[i].GetComponent<MonsterHealth>();
            if (mh == null || mh.IsDead) continue;
            mh.TakeDamage(_damage, false);
        }

        SpawnIceBurstParticle();

        if (_animator != null)
        {
            _animator.SetBool("DoRun", false);
            _animator.Play("Death", 0, 0f);
        }

        if (_spriteRenderer != null)
            _spriteRenderer.enabled = false;

        Destroy(gameObject, 0.35f);
    }

    void SpawnIceBurstParticle()
    {
        if (explosionFxPrefab != null)
        {
            GameObject fxObj = Instantiate(explosionFxPrefab, transform.position, Quaternion.identity);
            ParticleSystem psPrefab = fxObj.GetComponent<ParticleSystem>();
            float alive = psPrefab != null
                ? psPrefab.main.duration + psPrefab.main.startLifetime.constantMax + 0.3f
                : 1.2f;
            Destroy(fxObj, alive);
            return;
        }

        // 폴백: 프리팹이 없을 때만 런타임 생성
        GameObject fx = new GameObject("IceBurstFx");
        fx.transform.position = transform.position;
        var ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
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

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;

        var colorOverLifetime = ps.colorOverLifetime;
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

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f, AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1f));

        // URP/Built-in 모두에서 분홍(셰이더 누락) 방지를 위해 머티리얼을 명시 지정
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        Shader shader =
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Sprites/Default");
        if (shader != null)
            renderer.sharedMaterial = new Material(shader);

        ps.Play();
        Destroy(fx, 1.2f);
    }
}
