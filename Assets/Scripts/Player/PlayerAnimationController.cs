using System;
using UnityEngine;

/// Drives the Animator from gameplay events and spawns dust/footstep/jump/land feedback.
/// Animator parameters (all optional - missing ones are skipped):
///  Speed(float) VerticalSpeed(float) Grounded(bool) Grappling(bool) HookEquipped(bool)
///  Jump(trigger) Land(trigger) HookLaunch(trigger) Hurt(trigger) Acquire(trigger)
public class PlayerAnimationController : MonoBehaviour
{
    [Serializable] public class SurfaceSound { public SurfaceType surface; public SoundBank bank; }

    [Header("References")]
    [SerializeField] Animator animator;
    [SerializeField] PlayerController player;
    [SerializeField] GrapplingHookController hook;
    [SerializeField] PlayerHealth health;
    [SerializeField] GameObject hookVisual; // shown after acquisition (visible change to the character)

    [Header("Dust")]
    [SerializeField] ParticleSystem stepDust;
    [SerializeField] ParticleSystem jumpDust;
    [SerializeField] ParticleSystem landDust;

    [Header("Audio")]
    [SerializeField] SurfaceSound[] footsteps;
    [SerializeField] SoundBank jumpSound, landSound, hurtSound;
    [SerializeField] float stepDistance = 1.2f;

    static readonly int SpeedH = Animator.StringToHash("Speed");
    static readonly int VSpeedH = Animator.StringToHash("VerticalSpeed");
    static readonly int GroundedH = Animator.StringToHash("Grounded");
    static readonly int GrapplingH = Animator.StringToHash("Grappling");
    static readonly int EquippedH = Animator.StringToHash("HookEquipped");
    static readonly int JumpH = Animator.StringToHash("Jump");
    static readonly int LandH = Animator.StringToHash("Land");
    static readonly int LaunchH = Animator.StringToHash("HookLaunch");
    static readonly int HurtH = Animator.StringToHash("Hurt");
    static readonly int AcquireH = Animator.StringToHash("Acquire");

    float stepAccum;

    void Start()
    {
        if (hookVisual != null) hookVisual.SetActive(false);

        player.OnJump += HandleJump;
        player.OnLand += HandleLand;
        if (hook != null)
        {
            hook.Launched += _ => animator.SafeTrigger(LaunchH);
            hook.GrappleStarted += () => animator.SafeBool(GrapplingH, true);
            hook.GrappleEnded += () => animator.SafeBool(GrapplingH, false);
        }
        if (health != null) health.Hurt += () => { animator.SafeTrigger(HurtH); AudioController.Sfx(hurtSound, transform.position); };

        var seq = LevelSequenceController.Instance;
        if (seq != null)
        {
            seq.AcquisitionStarted += () => animator.SafeTrigger(AcquireH);
            seq.HookAcquired += () =>
            {
                animator.SafeBool(EquippedH, true);
                if (hookVisual != null) hookVisual.SetActive(true);
            };
        }
    }

    void Update()
    {
        Vector2 v = player.Velocity;
        animator.SafeFloat(SpeedH, Mathf.Abs(v.x));
        animator.SafeFloat(VSpeedH, v.y);
        animator.SafeBool(GroundedH, player.Grounded);

        if (player.Grounded && !player.ExternalControl && Mathf.Abs(v.x) > 0.5f)
        {
            stepAccum += Mathf.Abs(v.x) * Time.deltaTime;
            if (stepAccum >= stepDistance) { stepAccum = 0f; Footstep(); }
        }
        else if (!player.Grounded) stepAccum = stepDistance * 0.5f;
    }

    /// Also callable from an animation event if you prefer frame-accurate steps.
    public void Footstep()
    {
        SoundBank bank = null;
        foreach (var s in footsteps) if (s.surface == player.CurrentSurface) { bank = s.bank; break; }
        AudioController.Sfx(bank, player.FeetPosition, 0.8f);
        VFXController.Spawn(stepDust, player.FeetPosition);
    }

    void HandleJump()
    {
        animator.SafeTrigger(JumpH);
        VFXController.Spawn(jumpDust, player.FeetPosition);
        AudioController.Sfx(jumpSound, player.FeetPosition);
    }

    void HandleLand(float intensity)
    {
        animator.SafeTrigger(LandH);
        var p = VFXController.Spawn(landDust, player.FeetPosition);
        if (p != null) p.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.4f, intensity);
        AudioController.Sfx(landSound, player.FeetPosition, Mathf.Lerp(0.4f, 1f, intensity));
        if (intensity > 0.5f) CameraController.DoShake(intensity * 0.35f);
    }
}
