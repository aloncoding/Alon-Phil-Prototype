using UnityEngine;

/// Patrols -> detects -> chases -> telegraphs (posture + glow + sound) -> strikes -> recovers.
/// Animator triggers (optional): Telegraph, Strike, Stagger. Float: Speed.
public class GroundedMeleeEnemy : EnemyBase
{
    enum AI { Patrol, Chase, Telegraph, Strike, Recover }

    [Header("Movement")]
    [SerializeField] float patrolSpeed = 1.5f;
    [SerializeField] float chaseSpeed = 3.5f;
    [SerializeField] float patrolDistance = 3f;
    [SerializeField] float idleAtEnds = 1f;

    [Header("Detection")]
    [SerializeField] float detectRange = 6f;
    [SerializeField] float loseRange = 10f;
    [SerializeField] float verticalTolerance = 2.5f;

    [Header("Attack")]
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] float telegraphTime = 0.6f;
    [SerializeField] float strikeTime = 0.2f;
    [SerializeField] float recoverTime = 0.7f;
    [SerializeField] float attackCooldown = 1f;
    [SerializeField] float lungeSpeed = 5f;
    [SerializeField] int damage = 1;
    [SerializeField] Vector2 strikeBoxSize = new Vector2(1.8f, 1.3f);
    [SerializeField] Vector2 strikeBoxOffset = new Vector2(1.1f, 0.7f);
    [SerializeField] LayerMask playerMask;

    [Header("Sensing")]
    [SerializeField] LayerMask groundMask;
    [SerializeField] float wallCheckDistance = 0.6f;
    [SerializeField] float ledgeCheckForward = 0.7f;
    [SerializeField] float ledgeCheckDown = 1.2f;

    [Header("Audio")]
    [SerializeField] SoundBank telegraphSound, strikeSound;

    static readonly int TelegraphH = Animator.StringToHash("Telegraph");
    static readonly int StrikeH = Animator.StringToHash("Strike");

    AI ai = AI.Patrol;
    float timer, cooldownTimer, idleTimer, originX;
    int patrolDir = 1;
    bool struck;

    protected override void Awake()
    {
        base.Awake();
        originX = transform.position.x;
        patrolDir = FacingSign;
    }

    protected override void Tick()
    {
        float dt = Time.fixedDeltaTime;
        cooldownTimer -= dt;

        float dx = 0f, dy = 0f, dist = float.MaxValue;
        bool hasPlayer = playerCtrl != null;
        if (hasPlayer)
        {
            Vector2 pp = playerCtrl.transform.position;
            dx = pp.x - rb.position.x;
            dy = Mathf.Abs(pp.y - rb.position.y);
            dist = Mathf.Abs(dx);
        }
        bool sees = hasPlayer && dist <= detectRange && dy <= verticalTolerance;
        bool lost = !hasPlayer || dist > loseRange || dy > verticalTolerance * 2f;

        switch (ai)
        {
            case AI.Patrol:
                DoPatrol(dt);
                if (sees) ai = AI.Chase;
                break;

            case AI.Chase:
                if (lost) { ai = AI.Patrol; break; }
                FaceX(dx);
                if (Blocked(FacingSign)) Move(0f); else Move(FacingSign * chaseSpeed);
                if (dist <= attackRange && cooldownTimer <= 0f) EnterTelegraph();
                break;

            case AI.Telegraph:
                Move(0f);
                if (hasPlayer) FaceX(dx);
                timer -= dt;
                SetGlow(1f - timer / telegraphTime);
                if (timer <= 0f) EnterStrike();
                break;

            case AI.Strike:
                timer -= dt;
                Move(FacingSign * lungeSpeed * Mathf.Clamp01(timer / strikeTime));
                if (timer <= 0f) { ai = AI.Recover; timer = recoverTime; SetGlow(0f); }
                break;

            case AI.Recover:
                Move(0f);
                timer -= dt;
                if (timer <= 0f) { ai = AI.Chase; cooldownTimer = attackCooldown; }
                break;
        }
        anim.SafeFloat(SpeedH, Mathf.Abs(rb.linearVelocity.x));
    }

    void DoPatrol(float dt)
    {
        if (idleTimer > 0f) { idleTimer -= dt; Move(0f); return; }

        float x = rb.position.x;
        bool atEnd = (patrolDir > 0 && x >= originX + patrolDistance) || (patrolDir < 0 && x <= originX - patrolDistance);
        if (Blocked(patrolDir) || atEnd)
        {
            patrolDir = -patrolDir;
            idleTimer = idleAtEnds;
            Move(0f);
            return;
        }
        FaceX(patrolDir);
        Move(patrolDir * patrolSpeed);
    }

    void EnterTelegraph()
    {
        ai = AI.Telegraph;
        timer = telegraphTime;
        struck = false;
        anim.SafeTrigger(TelegraphH);
        AudioController.Sfx(telegraphSound, transform.position);
    }

    void EnterStrike()
    {
        ai = AI.Strike;
        timer = strikeTime;
        if (!struck) { struck = true; DoStrike(); }
    }

    void DoStrike()
    {
        anim.SafeTrigger(StrikeH);
        AudioController.Sfx(strikeSound, transform.position);
        Vector2 c = rb.position + new Vector2(FacingSign * strikeBoxOffset.x, strikeBoxOffset.y);
        foreach (var h in Physics2D.OverlapBoxAll(c, strikeBoxSize, 0f, playerMask))
        {
            var ph = h.GetComponentInParent<PlayerHealth>();
            if (ph != null) { ph.TakeHit(damage, rb.position); break; }
        }
    }

    protected override void OnStaggerStart()
    {
        ai = AI.Chase;
        SetGlow(0f);
    }

    void Move(float vx) => rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

    bool Blocked(int dir)
    {
        Vector2 p = rb.position;
        bool wall = Physics2D.Raycast(p + Vector2.up * 0.4f, Vector2.right * dir, wallCheckDistance, groundMask);
        bool ground = Physics2D.Raycast(p + new Vector2(dir * ledgeCheckForward, 0.3f), Vector2.down, ledgeCheckDown, groundMask);
        return wall || !ground;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 c = (Vector2)transform.position + new Vector2(FacingSign * strikeBoxOffset.x, strikeBoxOffset.y);
        Gizmos.DrawWireCube(c, strikeBoxSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
