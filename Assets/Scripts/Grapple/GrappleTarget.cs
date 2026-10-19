using UnityEngine;
using UnityEngine.Events;

public struct HookHit
{
    public int damage;
    public Vector2 direction; // direction the hook was travelling
    public Vector2 point;
}

/// Marks a collider (on the "Enemy" layer) as something the hook can interact with.
/// Put it on the enemy root or a child with a collider. Enemy == null means a static grapple anchor.
public class GrappleTarget : MonoBehaviour
{
    public Transform attachPoint;
    public bool canBeHit = true;
    public bool canBeGrappledTo = true;
    public bool canBePulled = true;

    public UnityEvent<HookHit> onHit;
    public UnityEvent onGrappledTo;

    public EnemyBase Enemy { get; private set; }
    public Vector2 AttachPosition => attachPoint != null ? (Vector2)attachPoint.position : (Vector2)transform.position;
    public bool IsValid => isActiveAndEnabled && (Enemy == null || Enemy.IsAlive);

    void Awake() => Enemy = GetComponentInParent<EnemyBase>();

    public void ReceiveHit(HookHit hit)
    {
        if (!canBeHit) return;
        onHit?.Invoke(hit);
        if (Enemy != null) Enemy.TakeHit(hit);
    }
}
