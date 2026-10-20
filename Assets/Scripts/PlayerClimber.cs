using System;
using UnityEngine;

/// Vertical-climber player: A/D to move, Space to jump (hold = higher), land on enemy heads to bounce,
/// click to grapple. Rigidbody, collider, sprite and hook are all created in Awake.
/// All numbers live in GameConfig.player (they apply live while playing, except maxHealth/colliderSize).
public class PlayerClimber : MonoBehaviour
{
    public int Health { get; private set; }
    public int Combo { get; private set; }
    public bool Dead { get; private set; }
    public float StartY { get; set; }
    public float MaxY { get; private set; }
    public float Height => Mathf.Max(0f, MaxY - StartY);

    public event Action Hurt, Died;
    public event Action<int> Stomped;

    static GameConfig.PlayerSettings P => GameConfig.I.player;

    Rigidbody2D rb;
    CapsuleCollider2D col;
    SpriteRenderer sr;
    Transform visual;
    HookController hook;

    float coyoteT, bufferT, invulnT, stompGrace, fallSpeed;
    bool grounded, wasGrounded, jumpHeld;
    int facing = 1;
    Vector2 squash = Vector2.one;

    void Awake()
    {
        gameObject.name = "Player";
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = P.gravityScale;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        col = gameObject.AddComponent<CapsuleCollider2D>();
        col.size = P.colliderSize;
        col.direction = CapsuleDirection2D.Vertical;
        col.sharedMaterial = GameManager.NoFriction;

        var v = new GameObject("Visual");
        v.transform.SetParent(transform, false);
        v.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        visual = v.transform;
        sr = v.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = ArtLibrary.SpriteMat;
        sr.sortingOrder = 10;
        sr.sprite = ArtLibrary.PlayerRun[0];

        Health = P.maxHealth;
        hook = gameObject.AddComponent<HookController>();
        hook.Init(this);
    }

    void Update()
    {
        if (Dead) { sr.enabled = true; return; }

        if (transform.position.y > MaxY) MaxY = transform.position.y;

        jumpHeld = GameInput.JumpHeld;
        if (GameInput.JumpDown)
        {
            if (hook.IsHooked) hook.Release(true);
            else bufferT = P.jumpBufferTime;
        }
        if (GameInput.JumpUp && rb.linearVelocity.y > 0f && !hook.IsHooked)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * P.jumpCutMultiplier);

        float ax = GameInput.Axis;
        if (Mathf.Abs(ax) > 0.1f) facing = ax > 0f ? 1 : -1;

        // animation
        Sprite s;
        if (hook.IsHooked) s = ArtLibrary.PlayerHook;
        else if (!grounded) s = rb.linearVelocity.y > 0f ? ArtLibrary.PlayerJump : ArtLibrary.PlayerFall;
        else if (Mathf.Abs(rb.linearVelocity.x) < 0.5f) s = ArtLibrary.PlayerRun[0];
        else s = ArtLibrary.PlayerRun[(int)(Time.time * 9f) % 2];
        sr.sprite = s;

        squash = Vector2.Lerp(squash, Vector2.one, 10f * Time.deltaTime);
        visual.localScale = new Vector3(facing * squash.x, squash.y, 1f);

        // health is shown only by body tint + flicker (no UI)
        sr.color = P.tintByHealth
            ? Color.Lerp(P.lowHealthTint, Color.white, Mathf.Clamp01((float)Health / Mathf.Max(1, P.maxHealth)))
            : Color.white;

        if (invulnT > 0f)
        {
            invulnT -= Time.deltaTime;
            sr.enabled = ((int)(invulnT * 20f) % 2) == 0;
        }
        else sr.enabled = true;
        if (stompGrace > 0f) stompGrace -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (Dead) return;
        float dt = Time.fixedDeltaTime;

        wasGrounded = grounded;
        CheckGround();
        if (!grounded) fallSpeed = Mathf.Min(fallSpeed, rb.linearVelocity.y);
        if (grounded && !wasGrounded)
        {
            if (-fallSpeed > 4f)
            {
                SoundManager.Play(Sfx.Land, Mathf.Clamp01(-fallSpeed / 20f));
                FX.Dust(new Vector2(transform.position.x, col.bounds.min.y));
                squash = new Vector2(1.25f, 0.75f);
            }
            Combo = 0;
        }
        if (grounded) fallSpeed = 0f;

        HandleEnemyContacts();
        if (Dead || hook.IsHooked) return;

        // horizontal movement
        float input = GameInput.Axis;
        bool moving = Mathf.Abs(input) > 0.01f;
        float accel = moving ? (grounded ? P.groundAccel : P.airAccel) : (grounded ? P.groundDecel : P.airDecel);
        float vx = Mathf.MoveTowards(rb.linearVelocity.x, input * P.moveSpeed, accel * dt);
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        // jump
        coyoteT = grounded ? P.coyoteTime : coyoteT - dt;
        bufferT -= dt;
        if (bufferT > 0f && coyoteT > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, P.jumpVelocity);
            bufferT = 0f; coyoteT = 0f; grounded = false;
            squash = new Vector2(0.8f, 1.25f);
            SoundManager.Play(Sfx.Jump);
            FX.Dust(new Vector2(transform.position.x, col.bounds.min.y), 4);
        }

        // gravity
        float vy = rb.linearVelocity.y;
        rb.gravityScale = vy < 0f ? P.gravityScale * P.fallMultiplier : P.gravityScale;
        if (vy < -P.maxFallSpeed) rb.linearVelocity = new Vector2(rb.linearVelocity.x, -P.maxFallSpeed);
    }

    void CheckGround()
    {
        Bounds b = col.bounds;
        var hits = Physics2D.OverlapBoxAll(new Vector2(b.center.x, b.min.y - 0.05f), new Vector2(b.size.x * 0.8f, 0.1f), 0f);
        bool found = false;
        foreach (var h in hits)
            if (h != null && h.GetComponent<Platform>() != null) { found = true; break; }
        grounded = found && rb.linearVelocity.y <= 0.1f;
    }

    void HandleEnemyContacts()
    {
        var hits = Physics2D.OverlapBoxAll(col.bounds.center, col.bounds.size, 0f);
        foreach (var h in hits)
        {
            if (h == null) continue;
            var e = h.GetComponent<Enemy>();
            if (e == null || e.IsDead) continue;

            bool hookTarget = hook.IsHooked && hook.TargetEnemy == e;
            bool above = col.bounds.min.y > h.bounds.center.y && rb.linearVelocity.y <= 3f;

            if (hookTarget || above) Stomp(e);
            else TakeHit();
            if (Dead) return;
        }
    }

    void Stomp(Enemy e)
    {
        e.Kill();
        Combo++;
        float vy = jumpHeld ? P.stompBounceHeld : P.stompBounce;
        float vx = rb.linearVelocity.x;
        if (hook.IsHooked) hook.Release(false, new Vector2(vx * P.hookStompHorizontalKeep, vy));
        else rb.linearVelocity = new Vector2(vx, vy);
        bufferT = 0f;
        stompGrace = P.stompGraceTime;
        squash = new Vector2(1.25f, 0.75f);
        var a = GameConfig.I.audio;
        SoundManager.Play(Sfx.Stomp, 1f, 1f + a.stompPitchPerCombo * Combo);
        SoundManager.Play(Sfx.Bell, 0.5f, 1f + a.bellPitchPerCombo * Mathf.Min(Combo, 8));
        Stomped?.Invoke(Combo);
    }

    void TakeHit()
    {
        if (invulnT > 0f || stompGrace > 0f) return;
        Health--;
        invulnT = P.invulnerableTime;
        Combo = 0;
        Vector2 kb = new Vector2(rb.linearVelocity.x * P.hurtHorizontalKeep, P.hurtBounceSpeed);
        if (hook.IsHooked) hook.Release(false, kb); else rb.linearVelocity = kb;
        FX.Burst(transform.position, new Color(1f, 0.35f, 0.35f), 14, 6f);
        SoundManager.Play(Sfx.Hurt);
        Hurt?.Invoke();
        if (Health <= 0) Die();
    }

    void Die()
    {
        if (Dead) return;
        Dead = true;
        if (hook.IsHooked) hook.Release(false);
        col.enabled = false;
        rb.gravityScale = P.gravityScale;
        rb.linearVelocity = new Vector2(-3f, 10f);
        sr.sprite = ArtLibrary.PlayerFall;
        sr.color = P.lowHealthTint;
        sr.enabled = true;
        SoundManager.Play(Sfx.Death);
        Died?.Invoke();
    }

    /// Falling into the lava.
    public void Kill()
    {
        if (Dead) return;
        Health = 0;
        Die();
    }

    // ---- called by HookController ----
    public void BeginHook() => rb.gravityScale = 0f;

    public void EndHook(Vector2 velocity)
    {
        rb.gravityScale = P.gravityScale;
        rb.linearVelocity = velocity;
    }
}
