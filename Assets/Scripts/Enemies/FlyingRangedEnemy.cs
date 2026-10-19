using UnityEngine;

/// Hovers at a preferred distance, charges a glowing shot (telegraph), fires a slow projectile
/// at the player's approximate position, then repositions so it never sits still.
/// Animator triggers (optional): Charge, Stagger. Float: Speed.
public class FlyingRangedEnemy : EnemyBase
{
    enum AI { Idle, Hover, Charge, Reposition }

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float accel = 8f;
    [SerializeField] float homeRadius = 5f;
    [SerializeField] float bobAmplitude = 0.25f;
    [SerializeField] float bobFrequency = 1.3f;

    [Header("Positioning")]
    [SerializeField] float preferredDistance = 5.5f;
    [SerializeField] float preferredHeight = 2.5f;

    [Header("Detection")]
    [SerializeField] float detectRange = 13f;
    [SerializeField] LayerMask groundMask; // line-of-sight blockers

    [Header("Attack")]
    [SerializeField] ProjectileController projectilePrefab;
    [SerializeField] Transform muzzle;
    [SerializeField] float projectileSpeed = 5f;
    [SerializeField] float telegraphTime = 0.9f;
    [SerializeField] float attackCooldown = 2.2f;
    [SerializeField] float repositionTime = 1.3f;
    [SerializeField] float leadTime = 0.25f;
    [SerializeField] ParticleSystem chargeParticles;
    [SerializeField] SoundBank chargeSound, fireSound;

    static readonly int ChargeH = Animator.StringToHash("Charge");

    AI ai = AI.Idle;
    Vector2 home, repositionTarget;
    float timer, cooldownTimer;

    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0f;
        pullUsesVerticalAxis = true;
        home = rb.position;
    }

    protected override void Tick()
    {
        float dt = Time.fixedDeltaTime;
        if (playerCtrl == null) { Drift(); return; }

        Vector2 pos = rb.position;
        Vector2 pp = playerCtrl.transform.position;
        float dist = Vector2.Distance(pos, pp);

        if (ai == AI.Idle)
        {
            Drift();
            if (dist <= detectRange) { ai = AI.Hover; cooldownTimer = 1f; }
            return;
        }

        FaceX(pp.x - pos.x);
        cooldownTimer -= dt;

        switch (ai)
        {
            case AI.Hover:
                SteerTo(HoverPoint(pp, pos), moveSpeed);
                if (dist > detectRange * 1.5f) ai = AI.Idle;
                else if (cooldownTimer <= 0f && dist <= detectRange && !Physics2D.Linecast(pos, pp, groundMask)) EnterCharge();
                break;

            case AI.Charge:
                SteerTo(pos + Vector2.up * Mathf.Sin(Time.time * bobFrequency) * 0.1f, 0.5f);
                timer -= dt;
                SetGlow(1f - timer / telegraphTime);
                if (timer <= 0f) Fire(pp);
                break;

            case AI.Reposition:
                SteerTo(repositionTarget, moveSpeed * 1.4f);
                timer -= dt;
                if (timer <= 0f || Vector2.Distance(pos, repositionTarget) < 0.4f) ai = AI.Hover;
                break;
        }
        anim.SafeFloat(SpeedH, rb.linearVelocity.magnitude);
    }

    Vector2 HoverPoint(Vector2 pp, Vector2 pos)
    {
        float side = pos.x >= pp.x ? 1f : -1f;
        Vector2 t = ClampToHome(new Vector2(pp.x + side * preferredDistance, pp.y + preferredHeight));
        t.y += Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        return t;
    }

    Vector2 ClampToHome(Vector2 target)
    {
        Vector2 off = target - home;
        return off.magnitude > homeRadius ? home + off.normalized * homeRadius : target;
    }

    void Drift()
    {
        Vector2 t = home + new Vector2(Mathf.Sin(Time.time * 0.5f) * 0.8f, Mathf.Sin(Time.time * bobFrequency) * bobAmplitude);
        SteerTo(t, moveSpeed * 0.4f);
    }

    void SteerTo(Vector2 target, float speed)
    {
        Vector2 d = target - rb.position;
        Vector2 desired = d.magnitude > 0.05f ? d.normalized * Mathf.Min(speed, d.magnitude * 3f) : Vector2.zero;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desired, accel * Time.fixedDeltaTime);
    }

    void EnterCharge()
    {
        ai = AI.Charge;
        timer = telegraphTime;
        anim.SafeTrigger(ChargeH);
        if (chargeParticles != null) chargeParticles.Play();
        AudioController.Sfx(chargeSound, transform.position);
    }

    void Fire(Vector2 playerPos)
    {
        Vector2 origin = muzzle != null ? (Vector2)muzzle.position : rb.position;
        Vector2 aimPoint = playerPos + playerCtrl.Velocity * leadTime;
        Vector2 dir = (aimPoint - origin).normalized;

        if (projectilePrefab != null)
        {
            var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
            p.Launch(dir, projectileSpeed);
        }
        AudioController.Sfx(fireSound, origin);
        if (chargeParticles != null) chargeParticles.Stop();
        SetGlow(0f);

        cooldownTimer = attackCooldown;
        ai = AI.Reposition;
        timer = repositionTime;
        repositionTarget = ClampToHome(home + Random.insideUnitCircle * homeRadius);
    }

    protected override void OnStaggerStart()
    {
        if (ai != AI.Idle) ai = AI.Hover;
        cooldownTimer = Mathf.Max(cooldownTimer, 0.8f);
        SetGlow(0f);
        if (chargeParticles != null) chargeParticles.Stop();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)home : transform.position, homeRadius);
    }
}
