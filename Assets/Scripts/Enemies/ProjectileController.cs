using UnityEngine;

/// Slow, readable projectile. Needs: Rigidbody2D (gravity 0), trigger Collider2D, optional TrailRenderer/Light2D.
[RequireComponent(typeof(Rigidbody2D))]
public class ProjectileController : MonoBehaviour
{
    [SerializeField] float lifetime = 6f;
    [SerializeField] int damage = 1;
    [SerializeField] LayerMask playerMask;
    [SerializeField] LayerMask groundMask;
    [SerializeField] ParticleSystem impactParticles;
    [SerializeField] SoundBank impactSound;

    Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Launch(Vector2 direction, float speed)
    {
        rb.gravityScale = 0f;
        rb.linearVelocity = direction * speed;
        transform.right = direction;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        int bit = 1 << other.gameObject.layer;
        if ((playerMask & bit) != 0)
        {
            var ph = other.GetComponentInParent<PlayerHealth>();
            if (ph != null) ph.TakeHit(damage, transform.position);
            Impact();
        }
        else if ((groundMask & bit) != 0) Impact();
    }

    void Impact()
    {
        VFXController.Spawn(impactParticles, transform.position);
        AudioController.Sfx(impactSound, transform.position);
        Destroy(gameObject);
    }
}
