using UnityEngine;

/// Pits/out-of-bounds. Respawns the player, destroys stray enemies.
[RequireComponent(typeof(Collider2D))]
public class KillZone : MonoBehaviour
{
    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        var ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null) { ph.Kill(); return; }
        var enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy != null) Destroy(enemy.gameObject);
    }
}
