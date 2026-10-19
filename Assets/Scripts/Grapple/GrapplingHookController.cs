using System;
using UnityEngine;

public enum HookMode { Tap, Grapple, Pull }

/// Hook state machine: Idle -> Launching -> (Retracting | Grappling | Pulling) -> Retracting -> Idle.
///  Tap LMB    : hook attack
///  Hold LMB   : grapple to enemy (release to cancel)
///  RMB        : pull enemy to player
/// Tap vs hold is decided by holdThreshold; one click can never trigger both.
public class GrapplingHookController : MonoBehaviour
{
    enum HookState { Idle, Launching, Retracting, Grappling, Pulling }

    [Header("References")]
    [SerializeField] PlayerController player;
    [SerializeField] Transform hookOrigin;
    [SerializeField] Transform hookHead;      // world-space object with TrailRenderer (unparented at runtime)
    [SerializeField] TetherRenderer tether;

    [Header("Layers")]
    [SerializeField] LayerMask targetMask;    // Enemy layer
    [SerializeField] LayerMask blockMask;     // Ground layer

    [Header("Unlock")]
    [SerializeField] bool startUnlocked = false; // tick for testing

    [Header("Hook")]
    [SerializeField] float holdThreshold = 0.18f;
    [SerializeField] float maxRange = 9f;
    [SerializeField] float launchSpeed = 40f;
    [SerializeField] float retractSpeed = 55f;
    [SerializeField] float hookRadius = 0.15f;
    [SerializeField] float cooldown = 0.2f;

    [Header("Tap attack")]
    [SerializeField] int tapDamage = 1;
    [SerializeField] float hitStop = 0.07f;
    [SerializeField] float hitShake = 0.5f;

    [Header("Grapple to enemy")]
    [SerializeField] float grappleSpeed = 24f;
    [SerializeField] float grappleAccel = 140f;
    [SerializeField] float grappleStopDistance = 1.2f;
    [SerializeField] float grappleMaxTime = 1.2f;
    [SerializeField, Range(0f, 1f)] float exitMomentum = 0.6f;

    [Header("Pull enemy")]
    [SerializeField] float pullSpeed = 20f;
    [SerializeField] float pullStopDistance = 1.5f;
    [SerializeField] float pullMaxTime = 1.5f;
    [SerializeField] float staggerAfterPull = 0.9f;

    [Header("Feedback")]
    [SerializeField] ParticleSystem contactFlash;
    [SerializeField] ParticleSystem missSpark;
    [SerializeField] SoundBank launchSound, hitSound, missSound, attachSound, strainSound, retractSound, completeSound;

    public event Action<HookMode> Launched;
    public event Action Missed, GrappleStarted, GrappleEnded, Unlocked;
    public event Action<HookMode, GrappleTarget> Hit;

    public bool IsUnlocked { get; private set; }
    public bool IsBusy => state != HookState.Idle;

    PlayerInputReader input;
    HookState state;
    HookMode mode;
    Vector2 headPos, launchDir;
    GrappleTarget attached;
    float pressTime, nextReadyTime, timer, currentGrappleSpeed;
    bool pendingHold, grappleReleased;

    void Awake()
    {
        hookHead.SetParent(null);
        hookHead.gameObject.SetActive(false);
        IsUnlocked = startUnlocked;
    }

    void Start()
    {
        input = PlayerInputReader.Instance;
        input.HookPressed += OnHookPressed;
        input.HookReleased += OnHookReleased;
        input.PullPressed += OnPullPressed;
        input.JumpPressed += OnJumpPressed;
    }

    void OnDestroy()
    {
        if (input == null) return;
        input.HookPressed -= OnHookPressed;
        input.HookReleased -= OnHookReleased;
        input.PullPressed -= OnPullPressed;
        input.JumpPressed -= OnJumpPressed;
    }

    public void Unlock()
    {
        IsUnlocked = true;
        Unlocked?.Invoke();
    }

    bool CanStart() => IsUnlocked && state == HookState.Idle && Time.time >= nextReadyTime && !player.ControlLocked;

    // ---------------- Input ----------------
    void OnHookPressed()
    {
        if (!CanStart()) return;
        pendingHold = true;
        pressTime = Time.time;
    }

    void OnHookReleased()
    {
        if (pendingHold)
        {
            pendingHold = false;
            Fire(HookMode.Tap);
        }
        else if (state == HookState.Grappling) CancelGrapple();
        else if (state == HookState.Launching && mode == HookMode.Grapple) grappleReleased = true;
    }

    void OnPullPressed()
    {
        if (!CanStart()) return;
        pendingHold = false;
        Fire(HookMode.Pull);
    }

    void OnJumpPressed()
    {
        if (state == HookState.Grappling) CancelGrapple(); // jump cancels a grapple
    }

    // ---------------- Update loop ----------------
    void Update()
    {
        if (pendingHold && Time.time - pressTime >= holdThreshold)
        {
            pendingHold = false;
            Fire(HookMode.Grapple);
        }

        switch (state)
        {
            case HookState.Launching: UpdateLaunching(Time.deltaTime); break;
            case HookState.Retracting: UpdateRetracting(Time.deltaTime); break;
            case HookState.Grappling:
            case HookState.Pulling:
                if (attached != null) headPos = attached.AttachPosition;
                break;
        }
    }

    void LateUpdate()
    {
        if (state == HookState.Idle) return;
        Vector2 origin = hookOrigin.position;
        hookHead.position = headPos;
        Vector2 toHead = headPos - origin;
        if (toHead.sqrMagnitude > 0.001f) hookHead.right = toHead.normalized;

        float tension = state switch
        {
            HookState.Launching => 0.25f,
            HookState.Retracting => 0.1f,
            HookState.Grappling => 1f,
            HookState.Pulling => 0.9f,
            _ => 0f
        };
        tether.Draw(origin, headPos, tension);
    }

    void FixedUpdate()
    {
        if (state == HookState.Grappling) StepGrapple(Time.fixedDeltaTime);
        else if (state == HookState.Pulling) StepPull(Time.fixedDeltaTime);
    }

    // ---------------- Launch ----------------
    void Fire(HookMode m)
    {
        Vector2 origin = hookOrigin.position;
        Vector2 aim = input.AimWorldPoint() - origin;
        if (aim.sqrMagnitude < 0.01f) aim = Vector2.right * player.Facing;

        mode = m;
        state = HookState.Launching;
        grappleReleased = false;
        launchDir = aim.normalized;
        headPos = origin;
        player.FaceDirection(launchDir.x >= 0f ? 1 : -1);

        hookHead.position = origin;
        hookHead.right = launchDir;
        hookHead.gameObject.SetActive(true);
        var trail = hookHead.GetComponentInChildren<TrailRenderer>();
        if (trail != null) trail.Clear();
        tether.Show();

        AudioController.Sfx(launchSound, origin);
        Launched?.Invoke(m);
    }

    void UpdateLaunching(float dt)
    {
        Vector2 origin = hookOrigin.position;
        float remaining = maxRange - Vector2.Distance(origin, headPos);
        if (remaining <= 0f) { Miss(headPos, false); return; }

        float step = Mathf.Min(launchSpeed * dt, remaining);
        RaycastHit2D hit = Physics2D.CircleCast(headPos, hookRadius, launchDir, step, targetMask | blockMask);
        if (hit.collider != null)
        {
            headPos = hit.centroid;
            ResolveHit(hit);
            return;
        }
        headPos += launchDir * step;
    }

    void ResolveHit(RaycastHit2D hit)
    {
        int bit = 1 << hit.collider.gameObject.layer;
        GrappleTarget t = (blockMask & bit) != 0 ? null : hit.collider.GetComponentInParent<GrappleTarget>();
        if (t == null || !t.IsValid) { Miss(hit.point, true); return; }

        switch (mode)
        {
            case HookMode.Tap:
                TapHit(t, hit.point);
                break;
            case HookMode.Grapple:
                if (!t.canBeGrappledTo || grappleReleased) TapHit(t, hit.point);
                else BeginGrapple(t, hit.point);
                break;
            case HookMode.Pull:
                if (!t.canBePulled || t.Enemy == null) TapHit(t, hit.point);
                else BeginPull(t, hit.point);
                break;
        }
    }

    // ---------------- Outcomes ----------------
    void TapHit(GrappleTarget t, Vector2 point)
    {
        t.ReceiveHit(new HookHit { damage = tapDamage, direction = launchDir, point = point });
        ContactFeedback(point, hitSound, hitStop, hitShake);
        Hit?.Invoke(mode, t);
        BeginRetract();
    }

    void BeginGrapple(GrappleTarget t, Vector2 point)
    {
        attached = t;
        state = HookState.Grappling;
        timer = 0f;
        currentGrappleSpeed = Mathf.Max(player.Velocity.magnitude * 0.5f, 6f);
        player.BeginExternalControl();

        ContactFeedback(point, attachSound, 0.04f, 0.3f);
        t.onGrappledTo?.Invoke();
        if (t.Enemy != null) t.Enemy.OnGrappledTo(launchDir);
        AudioController.Sfx(strainSound, player.transform.position, 0.8f);
        Hit?.Invoke(mode, t);
        GrappleStarted?.Invoke();
    }

    void BeginPull(GrappleTarget t, Vector2 point)
    {
        attached = t;
        state = HookState.Pulling;
        timer = 0f;
        t.Enemy.BeginPull();
        ContactFeedback(point, attachSound, 0.04f, 0.3f);
        AudioController.Sfx(strainSound, t.AttachPosition, 0.9f);
        Hit?.Invoke(mode, t);
    }

    void StepGrapple(float dt)
    {
        if (attached == null || !attached.IsValid) { CancelGrapple(); return; }

        Rigidbody2D rb = player.Body;
        Vector2 dest = attached.AttachPosition;
        Vector2 to = dest - rb.position;
        float dist = to.magnitude;

        // Fail gracefully when level geometry gets in the way.
        if (Physics2D.Linecast(hookOrigin.position, dest, blockMask)) { CancelGrapple(); return; }

        timer += dt;
        if (dist <= grappleStopDistance) { CompleteGrapple(to.normalized); return; }
        if (timer > grappleMaxTime) { CancelGrapple(); return; }

        currentGrappleSpeed = Mathf.MoveTowards(currentGrappleSpeed, grappleSpeed, grappleAccel * dt);
        rb.linearVelocity = to.normalized * currentGrappleSpeed;
        player.FaceDirection(to.x >= 0f ? 1 : -1);
    }

    void CompleteGrapple(Vector2 dir)
    {
        Vector2 exit = dir * currentGrappleSpeed * exitMomentum;
        if (attached != null && attached.Enemy != null) attached.Enemy.OnGrappleArrival(dir);
        ContactFeedback(headPos, completeSound, 0.05f, 0.7f);
        player.Stretch(new Vector2(0.85f, 1.2f));
        EndGrapple(exit);
    }

    public void CancelGrapple()
    {
        if (state != HookState.Grappling) return;
        EndGrapple(player.Velocity * exitMomentum);
    }

    void EndGrapple(Vector2 exitVelocity)
    {
        player.EndExternalControl(exitVelocity);
        GrappleEnded?.Invoke();
        BeginRetract();
    }

    void StepPull(float dt)
    {
        EnemyBase enemy = attached != null ? attached.Enemy : null;
        if (enemy == null || !enemy.IsAlive) { BeginRetract(); return; }

        Vector2 dest = hookOrigin.position;
        if (Physics2D.Linecast(dest, enemy.Body.position, blockMask))
        {
            enemy.EndPull(0.3f);
            BeginRetract();
            return;
        }

        timer += dt;
        float remaining = enemy.PullStep(dest, pullSpeed);
        if (remaining <= pullStopDistance)
        {
            enemy.EndPull(staggerAfterPull);
            ContactFeedback(enemy.Body.position, completeSound, 0.06f, 0.8f);
            BeginRetract();
        }
        else if (timer > pullMaxTime)
        {
            enemy.EndPull(0.3f);
            BeginRetract();
        }
    }

    void Miss(Vector2 point, bool blocked)
    {
        Missed?.Invoke();
        AudioController.Sfx(missSound, point, 0.7f);
        if (blocked) VFXController.Spawn(missSpark, point);
        BeginRetract();
    }

    // ---------------- Retract ----------------
    void BeginRetract()
    {
        attached = null;
        state = HookState.Retracting;
        AudioController.Sfx(retractSound, hookOrigin.position, 0.7f);
    }

    void UpdateRetracting(float dt)
    {
        Vector2 origin = hookOrigin.position;
        headPos = Vector2.MoveTowards(headPos, origin, retractSpeed * dt);
        if (Vector2.Distance(headPos, origin) < 0.15f) EndHook();
    }

    void EndHook()
    {
        hookHead.gameObject.SetActive(false);
        tether.Hide();
        state = HookState.Idle;
        nextReadyTime = Time.time + cooldown;
    }

    /// Hard reset (player hurt, respawn, cutscene).
    public void ForceCancel()
    {
        pendingHold = false;
        if (state == HookState.Grappling) { player.EndExternalControl(player.Velocity * exitMomentum); GrappleEnded?.Invoke(); }
        if (state == HookState.Pulling && attached != null && attached.Enemy != null) attached.Enemy.EndPull(0.2f);
        attached = null;
        if (state != HookState.Idle) EndHook();
    }

    void ContactFeedback(Vector2 point, SoundBank sound, float stop, float shake)
    {
        VFXController.Spawn(contactFlash, point);
        AudioController.Sfx(sound, point);
        if (VFXController.Instance != null) VFXController.Instance.HitStop(stop);
        CameraController.DoShake(shake);
    }
}
