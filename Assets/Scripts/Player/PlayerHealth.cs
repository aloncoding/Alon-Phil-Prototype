using System;
using System.Collections;
using UnityEngine;

/// Invisible health (no UI): feedback is flicker, knockback, particles and sound.
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] int maxHealth = 3;
    [SerializeField] float invulnerableTime = 1.2f;
    [SerializeField] Vector2 knockback = new Vector2(7f, 6f);
    [SerializeField] float stunTime = 0.3f;
    [SerializeField] SpriteRenderer[] renderers;
    [SerializeField] ParticleSystem hurtParticles;
    [SerializeField] GrapplingHookController hook;

    public event Action Hurt, Died;
    public bool IsDead { get; private set; }

    int current;
    float invulnerableUntil;
    PlayerController player;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        current = maxHealth;
    }

    public bool TakeHit(int damage, Vector2 sourcePosition)
    {
        if (IsDead || Time.time < invulnerableUntil) return false;

        current -= damage;
        invulnerableUntil = Time.time + invulnerableTime;

        if (hook != null) hook.ForceCancel();
        float dir = transform.position.x >= sourcePosition.x ? 1f : -1f;
        player.Stun(new Vector2(dir * knockback.x, knockback.y), stunTime);

        VFXController.Spawn(hurtParticles, transform.position);
        if (VFXController.Instance != null) VFXController.Instance.HitStop(0.06f);
        CameraController.DoShake(0.6f);
        Hurt?.Invoke();

        if (current <= 0) Kill();
        else StartCoroutine(Flicker());
        return true;
    }

    public void Kill()
    {
        if (IsDead) return;
        IsDead = true;
        Died?.Invoke();
        if (LevelSequenceController.Instance != null) LevelSequenceController.Instance.Respawn();
    }

    public void ResetHealth()
    {
        current = maxHealth;
        IsDead = false;
        invulnerableUntil = Time.time + 1f;
        foreach (var r in renderers) r.enabled = true;
    }

    IEnumerator Flicker()
    {
        while (Time.time < invulnerableUntil && !IsDead)
        {
            foreach (var r in renderers) r.enabled = !r.enabled;
            yield return new WaitForSeconds(0.08f);
        }
        foreach (var r in renderers) r.enabled = true;
    }
}
