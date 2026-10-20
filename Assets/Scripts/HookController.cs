using UnityEngine;

/// Click to fire at the mouse. The first thing the ray touches decides the result:
///  - Crawler/Floater: you get pulled to its head and stomp it on arrival (counts as a bounce).
///  - Platform: you get pulled on top of it (works from below too - platforms are one-way).
///  - Walls or nothing: miss.
/// Click again or press jump to let go early (jump gives an extra boost). All numbers: GameConfig.hook.
public class HookController : MonoBehaviour
{
    public bool IsHooked { get; private set; }
    public Enemy TargetEnemy { get; private set; }

    static GameConfig.HookSettings H => GameConfig.I.hook;

    PlayerClimber player;
    Rigidbody2D rb;
    LineRenderer rope;
    SpriteRenderer head;
    Camera cam;

    Vector2 anchor, ropeEnd, missEnd;
    float timer, speed, cdT, missT;

    Vector2 Origin => (Vector2)transform.position + Vector2.up * 0.3f;

    public void Init(PlayerClimber p)
    {
        player = p;
        rb = p.GetComponent<Rigidbody2D>();
        cam = Camera.main;

        var rg = new GameObject("Rope");
        rg.transform.SetParent(transform);
        rope = rg.AddComponent<LineRenderer>();
        rope.sharedMaterial = ArtLibrary.SpriteMat;
        rope.positionCount = 2;
        rope.useWorldSpace = true;
        rope.sortingOrder = 8;
        rope.enabled = false;

        var hg = new GameObject("HookHead");
        hg.transform.SetParent(transform);
        head = hg.AddComponent<SpriteRenderer>();
        head.sprite = ArtLibrary.HookHead;
        head.sharedMaterial = ArtLibrary.SpriteMat;
        head.sortingOrder = 9;
        head.enabled = false;
    }

    void Update()
    {
        if (player.Dead)
        {
            if (IsHooked) Release(false);
            return;
        }
        cdT -= Time.deltaTime;
        if (missT > 0f) missT -= Time.deltaTime;

        if (GameInput.HookDown)
        {
            if (IsHooked) Release(false);
            else if (cdT <= 0f) TryFire();
        }
    }

    void TryFire()
    {
        if (cam == null) cam = Camera.main;
        Vector2 o = Origin;
        Vector2 aim = GameInput.AimWorld(cam) - o;
        if (aim.sqrMagnitude < 0.04f) aim = Vector2.up;
        Vector2 dir = aim.normalized;

        SoundManager.Play(Sfx.HookFire, 0.8f);

        var hits = Physics2D.CircleCastAll(o, H.aimRadius, dir, H.range);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.gameObject == gameObject) continue;

            var e = h.collider.GetComponent<Enemy>();
            if (e != null)
            {
                if (e.IsDead) continue;
                AttachEnemy(e, h.point);
                return;
            }

            var pl = h.collider.GetComponent<Platform>();
            if (pl != null)
            {
                AttachPlatform(h, pl);
                return;
            }

            if (!h.collider.isTrigger) { Miss(h.point); return; } // walls block the hook
        }
        Miss(o + dir * H.range);
    }

    void Miss(Vector2 end)
    {
        SoundManager.Play(Sfx.HookMiss, 0.7f);
        missEnd = end;
        missT = 0.12f;
        cdT = H.cooldown;
    }

    void AttachEnemy(Enemy e, Vector2 point)
    {
        TargetEnemy = e;
        anchor = e.HeadPoint;
        CommonAttach(point);
    }

    void AttachPlatform(RaycastHit2D h, Platform pl)
    {
        TargetEnemy = null;
        Bounds b = pl.Col.bounds;
        float m = Mathf.Min(0.6f, b.extents.x);
        float ax = Mathf.Clamp(h.point.x, b.min.x + m, b.max.x - m);
        anchor = new Vector2(ax, b.max.y + H.platformAnchorHeight); // stand on top of the platform
        CommonAttach(h.point);
    }

    void CommonAttach(Vector2 point)
    {
        IsHooked = true;
        ropeEnd = point;
        timer = 0f;
        speed = Mathf.Max(H.startSpeedMin, rb.linearVelocity.magnitude * 0.6f);
        player.BeginHook();
        SoundManager.Play(Sfx.HookHit, 0.9f);
        FX.Burst(point, new Color(1f, 0.9f, 0.5f), 8, 5f);
    }

    void FixedUpdate()
    {
        if (!IsHooked) return;

        Vector2 target = anchor;
        if (TargetEnemy != null)
        {
            if (TargetEnemy.IsDead) { Release(false); return; }
            target = TargetEnemy.HeadPoint;
        }

        Vector2 to = target - rb.position;
        float d = to.magnitude;
        timer += Time.fixedDeltaTime;

        if (d <= (TargetEnemy != null ? H.enemyStopDistance : H.platformStopDistance)) { Arrive(); return; }
        if (timer > H.maxPullTime) { Release(false); return; }

        speed = Mathf.MoveTowards(speed, H.pullSpeed, H.pullAccel * Time.fixedDeltaTime);
        float s = speed;
        if (TargetEnemy == null) s = Mathf.Min(s, d * H.platformEaseFactor + 2f); // ease in so we land exactly on the platform
        rb.linearVelocity = to / d * s;
        SoundManager.SetPull(true, speed / H.pullSpeed);
    }

    void Arrive()
    {
        if (TargetEnemy != null) { Release(false); return; }
        Vector2 v = rb.linearVelocity;
        float k = H.platformExitSpeedKeep;
        Release(false, new Vector2(v.x * k, Mathf.Clamp(v.y * k, -3f, 2f)));
    }

    public void Release(bool jump, Vector2? exitVelocity = null)
    {
        if (!IsHooked) return;
        IsHooked = false;
        Vector2 vel = exitVelocity ?? rb.linearVelocity * H.releaseSpeedKeep;
        if (jump) vel.y = Mathf.Max(vel.y, 0f) + H.jumpOffBoost;
        TargetEnemy = null;
        cdT = H.cooldown;
        rope.enabled = false;
        head.enabled = false;
        SoundManager.SetPull(false, 0f);
        player.EndHook(vel);
    }

    void LateUpdate()
    {
        Vector2 o = Origin;
        if (IsHooked)
        {
            Vector2 end = TargetEnemy != null ? TargetEnemy.HeadPoint : ropeEnd;
            DrawRope(o, end);
        }
        else if (missT > 0f)
        {
            float k = missT / 0.12f; // 1 -> 0
            DrawRope(o, Vector2.Lerp(o, missEnd, Mathf.Sin(k * Mathf.PI)));
        }
        else
        {
            rope.enabled = false;
            head.enabled = false;
        }
    }

    void DrawRope(Vector2 a, Vector2 b)
    {
        rope.enabled = true;
        rope.startWidth = rope.endWidth = H.ropeWidth;
        rope.startColor = rope.endColor = H.ropeColor;
        rope.SetPosition(0, new Vector3(a.x, a.y, 0f));
        rope.SetPosition(1, new Vector3(b.x, b.y, 0f));
        head.enabled = true;
        head.transform.position = new Vector3(b.x, b.y, 0f);
        Vector2 d = b - a;
        if (d.sqrMagnitude > 0.001f) head.transform.right = d.normalized;
    }
}
