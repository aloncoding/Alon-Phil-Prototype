using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// Shared enemy logic: hidden health, damage reactions, stagger, being pulled, death.
/// NOTE: place the pivot at the enemy's feet (grounded) so ledge/wall checks line up.
[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    public enum EnemyState { Normal, Stagger, Pulled, Dead }

    [Header("Health (never shown on screen)")]
    [SerializeField] protected int maxHealth = 4;
    [SerializeField] protected float hitKnockback = 5f;
    [SerializeField] protected float hitStagger = 0.3f;

    [Header("Pull")]
    [SerializeField, Range(0f, 0.9f)] protected float pullResistance = 0f; // heavy enemies resist
    [SerializeField] protected bool pullUsesVerticalAxis = false;           // true for flyers

    [Header("Feedback")]
    [SerializeField] protected SpriteRenderer[] renderers;
    [SerializeField] protected Color damagedTint = new Color(0.65f, 0.5f, 0.5f);
    [SerializeField] protected Color flashColor = new Color(1f, 0.9f, 0.7f);
    [SerializeField] protected ParticleSystem hitParticles, deathParticles;
    [SerializeField] protected SoundBank hitSound, staggerSound, pullStrainSound, deathSound;
    [SerializeField] protected Light2D glowLight;
    [SerializeField] protected float peakGlow = 3f;

    public event Action<EnemyBase> Died;
    public EnemyState State { get; protected set; } = EnemyState.Normal;
    public bool IsAlive => State != EnemyState.Dead;
    public Rigidbody2D Body => rb;

    protected Rigidbody2D rb;
    protected Animator anim;
    protected PlayerController playerCtrl;

    protected static readonly int StaggerH = Animator.StringToHash("Stagger");
    protected static readonly int SpeedH = Animator.StringToHash("Speed");
    static readonly int FlashId = Shader.PropertyToID("_FlashAmount");

    int health;
    float staggerTimer, baseGlow;
    Color currentTint = Color.white;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        rb.freezeRotation = true;
        health = maxHealth;
        if (glowLight != null) baseGlow = glowLight.intensity;
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    protected virtual void Start() => playerCtrl = FindFirstObjectByType<PlayerController>();

    void FixedUpdate()
    {
        switch (State)
        {
            case EnemyState.Normal:
                Tick();
                break;
            case EnemyState.Stagger:
                staggerTimer -= Time.fixedDeltaTime;
                Vector2 v = rb.linearVelocity;
                v.x = Mathf.MoveTowards(v.x, 0f, 25f * Time.fixedDeltaTime);
                if (pullUsesVerticalAxis) v.y = Mathf.MoveTowards(v.y, 0f, 25f * Time.fixedDeltaTime);
                rb.linearVelocity = v;
                if (staggerTimer <= 0f) { State = EnemyState.Normal; OnStaggerEnd(); }
                break;
        }
    }

    /// Per-physics-step AI, only called while State == Normal.
    protected abstract void Tick();
    protected virtual void OnStaggerStart() { }
    protected virtual void OnStaggerEnd() { }

    // ---------------- Damage ----------------
    public virtual void TakeHit(HookHit hit)
    {
        if (State == EnemyState.Dead) return;

        health -= hit.damage;
        currentTint = Color.Lerp(Color.white, damagedTint, 1f - (float)Mathf.Max(health, 0) / maxHealth);
        StartCoroutine(FlashRoutine());
        VFXController.Spawn(hitParticles, hit.point);
        AudioController.Sfx(hitSound, hit.point);

        Vector2 dir = hit.direction.sqrMagnitude > 0.01f ? hit.direction.normalized : Vector2.right;
        if (health <= 0) { Die(dir); return; }

        Vector2 kb = dir * hitKnockback;
        if (!pullUsesVerticalAxis) kb.y = 2f;
        rb.linearVelocity = kb;
        Stagger(hitStagger);
    }

    public void Stagger(float duration)
    {
        if (State == EnemyState.Dead) return;
        bool wasStaggered = State == EnemyState.Stagger;
        State = EnemyState.Stagger;
        staggerTimer = Mathf.Max(staggerTimer, duration);
        if (!wasStaggered)
        {
            OnStaggerStart();
            anim.SafeTrigger(StaggerH);
            AudioController.Sfx(staggerSound, transform.position);
        }
    }

    /// Hook attached for a grapple: small recoil so the target visibly reacts.
    public virtual void OnGrappledTo(Vector2 hookDir)
    {
        rb.linearVelocity = hookDir * 2f;
        Stagger(0.25f);
    }

    /// Player arrived at the enemy via grapple: shove + stagger = follow-up window.
    public virtual void OnGrappleArrival(Vector2 dir)
    {
        Vector2 kb = dir * 4f;
        if (!pullUsesVerticalAxis) kb.y = 1.5f;
        rb.linearVelocity = kb;
        Stagger(0.4f);
    }

    // ---------------- Pull ----------------
    public virtual void BeginPull()
    {
        if (State == EnemyState.Dead) return;
        State = EnemyState.Pulled;
        OnStaggerStart(); // cancels any attack in progress
        anim.SafeTrigger(StaggerH);
        AudioController.Sfx(pullStrainSound, transform.position);
    }

    /// Returns remaining distance (horizontal only for grounded enemies).
    public float PullStep(Vector2 destination, float speed)
    {
        Vector2 d = destination - rb.position;
        Vector2 v = d.normalized * speed * (1f - pullResistance);
        if (!pullUsesVerticalAxis) v.y = rb.linearVelocity.y;
        rb.linearVelocity = v;
        return pullUsesVerticalAxis ? d.magnitude : Mathf.Abs(d.x);
    }

    public void EndPull(float staggerDuration)
    {
        if (State == EnemyState.Dead) return;
        rb.linearVelocity *= 0.25f;
        State = EnemyState.Normal;
        Stagger(staggerDuration);
    }

    // ---------------- Death ----------------
    protected virtual void Die(Vector2 dir)
    {
        State = EnemyState.Dead;
        SetGlow(0f);
        foreach (var t in GetComponentsInChildren<GrappleTarget>()) t.enabled = false;
        if (pullUsesVerticalAxis) rb.gravityScale = 2f;
        rb.linearVelocity = dir * 4f + Vector2.up * 2f;
        VFXController.Spawn(deathParticles, transform.position);
        AudioController.Sfx(deathSound, transform.position);
        Died?.Invoke(this);
        StartCoroutine(FadeAndDestroy());
    }

    IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(0.6f);
        float t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            foreach (var r in renderers) { var c = r.color; c.a = 1f - t / 0.8f; r.color = c; }
            yield return null;
        }
        Destroy(gameObject);
    }

    // ---------------- Helpers ----------------
    IEnumerator FlashRoutine()
    {
        const float flashTime = 0.12f;
        float t = 0f;
        while (t < flashTime)
        {
            float k = 1f - t / flashTime;
            foreach (var r in renderers)
            {
                if (r.material.HasProperty(FlashId)) r.material.SetFloat(FlashId, k); // optional Shader Graph "_FlashAmount"
                r.color = Color.Lerp(currentTint, flashColor, k);
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        foreach (var r in renderers)
        {
            if (r.material.HasProperty(FlashId)) r.material.SetFloat(FlashId, 0f);
            r.color = currentTint;
        }
    }

    protected void SetGlow(float t)
    {
        if (glowLight != null) glowLight.intensity = Mathf.Lerp(baseGlow, peakGlow, Mathf.Clamp01(t));
    }

    protected void FaceX(float dx)
    {
        if (Mathf.Abs(dx) < 0.05f) return;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dx > 0 ? 1f : -1f);
        transform.localScale = s;
    }

    protected int FacingSign => transform.localScale.x >= 0f ? 1 : -1;
}
