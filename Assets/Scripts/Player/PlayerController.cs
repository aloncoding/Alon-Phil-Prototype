using System;
using UnityEngine;

/// Movement, jumping, grounded detection. Dynamic Rigidbody2D + CapsuleCollider2D.
/// Other systems (grapple, hurt, cutscenes) take over via the External/Stun/Lock APIs.
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Run")]
    [SerializeField] float moveSpeed = 7f;
    [SerializeField] float groundAccel = 70f;
    [SerializeField] float groundDecel = 80f;
    [SerializeField] float airAccel = 40f;
    [SerializeField] float airDecel = 25f;

    [Header("Jump")]
    [SerializeField] float jumpVelocity = 14f;
    [SerializeField] float coyoteTime = 0.1f;
    [SerializeField] float jumpBufferTime = 0.12f;
    [SerializeField, Range(0.1f, 1f)] float jumpCutMultiplier = 0.45f;
    [SerializeField] float gravityScale = 3f;
    [SerializeField] float fallGravityMultiplier = 1.5f;
    [SerializeField] float maxFallSpeed = 24f;

    [Header("Ground")]
    [SerializeField] LayerMask groundMask;

    [Header("Visual (squash & stretch + flip)")]
    [SerializeField] Transform visualRoot;
    [SerializeField] float squashRecovery = 10f;

    public event Action OnJump;
    public event Action<float> OnLand; // 0..1 intensity

    public bool Grounded { get; private set; }
    public int Facing { get; private set; } = 1;
    public bool ExternalControl { get; private set; }
    public bool ControlLocked { get; private set; }
    public SurfaceType CurrentSurface { get; private set; }
    public Vector2 Velocity => rb.linearVelocity;
    public Rigidbody2D Body => rb;
    public Vector2 FeetPosition => new Vector2(col.bounds.center.x, col.bounds.min.y);

    Rigidbody2D rb;
    CapsuleCollider2D col;
    PlayerInputReader input;
    float coyoteTimer, bufferTimer, stunTimer, fallSpeed;
    Vector2 squash = Vector2.one;
    Vector3 baseScale = Vector3.one;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (visualRoot != null) baseScale = new Vector3(Mathf.Abs(visualRoot.localScale.x), visualRoot.localScale.y, 1f);
    }

    void Start()
    {
        input = PlayerInputReader.Instance;
        input.JumpPressed += () => { if (!ControlLocked) bufferTimer = jumpBufferTime; };
        input.JumpReleased += OnJumpReleased;
    }

    void OnJumpReleased()
    {
        if (ExternalControl || ControlLocked) return;
        var v = rb.linearVelocity;
        if (v.y > 0f) rb.linearVelocity = new Vector2(v.x, v.y * jumpCutMultiplier);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        bool wasGrounded = Grounded;
        CheckGround();

        if (!Grounded) fallSpeed = Mathf.Min(fallSpeed, rb.linearVelocity.y);
        if (Grounded && !wasGrounded)
        {
            if (-fallSpeed > 2f)
            {
                float i = Mathf.Clamp01(-fallSpeed / 22f);
                squash = new Vector2(1f + 0.25f * i, 1f - 0.3f * i);
                OnLand?.Invoke(i);
            }
        }
        if (Grounded) fallSpeed = 0f;

        if (ExternalControl) return;

        if (stunTimer > 0f)
        {
            stunTimer -= dt;
            ApplyGravity();
            return;
        }

        float moveInput = ControlLocked ? 0f : input.Move;
        if (Mathf.Abs(moveInput) > 0.01f) Facing = moveInput > 0 ? 1 : -1;

        // Horizontal: accelerate toward target; over-speed (grapple momentum) bleeds off naturally.
        float vx = rb.linearVelocity.x;
        float target = moveInput * moveSpeed;
        float accel = Mathf.Abs(moveInput) > 0.01f
            ? (Grounded ? groundAccel : airAccel)
            : (Grounded ? groundDecel : airDecel);
        vx = Mathf.MoveTowards(vx, target, accel * dt);
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        // Jump (coyote + buffer)
        coyoteTimer = Grounded ? coyoteTime : coyoteTimer - dt;
        bufferTimer -= dt;
        if (bufferTimer > 0f && coyoteTimer > 0f && !ControlLocked)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
            bufferTimer = 0f;
            coyoteTimer = 0f;
            Grounded = false;
            squash = new Vector2(0.85f, 1.2f);
            OnJump?.Invoke();
        }

        ApplyGravity();
    }

    void ApplyGravity()
    {
        float vy = rb.linearVelocity.y;
        rb.gravityScale = vy < 0f ? gravityScale * fallGravityMultiplier : gravityScale;
        if (vy < -maxFallSpeed) rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    void CheckGround()
    {
        Bounds b = col.bounds;
        Vector2 p = new Vector2(b.center.x, b.min.y - 0.02f);
        Collider2D hit = Physics2D.OverlapBox(p, new Vector2(b.size.x * 0.8f, 0.06f), 0f, groundMask);
        Grounded = hit != null && rb.linearVelocity.y <= 0.1f;
        if (Grounded)
            CurrentSurface = hit.TryGetComponent(out SurfaceMarker m) ? m.surface : SurfaceType.Stone;
    }

    void Update()
    {
        squash = Vector2.Lerp(squash, Vector2.one, squashRecovery * Time.unscaledDeltaTime);
        if (visualRoot != null)
            visualRoot.localScale = new Vector3(Facing * baseScale.x * squash.x, baseScale.y * squash.y, 1f);
    }

    // ---------- External API ----------
    public void FaceDirection(int dir) { if (dir != 0) Facing = dir > 0 ? 1 : -1; }

    public void BeginExternalControl()
    {
        ExternalControl = true;
        rb.gravityScale = 0f;
    }

    public void EndExternalControl(Vector2 exitVelocity)
    {
        if (!ExternalControl) return;
        ExternalControl = false;
        rb.gravityScale = gravityScale;
        rb.linearVelocity = exitVelocity;
    }

    public void Stun(Vector2 velocity, float duration)
    {
        stunTimer = duration;
        rb.linearVelocity = velocity;
    }

    public void SetControlLocked(bool locked) => ControlLocked = locked;

    public void Stretch(Vector2 scale) => squash = scale;

    public void Teleport(Vector2 position)
    {
        rb.position = position;
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
        ExternalControl = false;
        rb.gravityScale = gravityScale;
    }
}
