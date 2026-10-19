using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// Pooled one-shot particles + hit-stop. Use the static helpers: VFXController.Spawn(prefab, pos).
/// Particle prefabs should NOT loop and should have a finite duration.
public class VFXController : MonoBehaviour
{
    public static VFXController Instance { get; private set; }

    readonly Dictionary<ParticleSystem, ObjectPool<ParticleSystem>> pools = new();
    Coroutine hitStopRoutine;

    void Awake() => Instance = this;

    void OnDisable() => Time.timeScale = 1f;

    public static ParticleSystem Spawn(ParticleSystem prefab, Vector2 position)
        => Spawn(prefab, position, Quaternion.identity);

    public static ParticleSystem Spawn(ParticleSystem prefab, Vector2 position, Quaternion rotation)
    {
        if (Instance == null || prefab == null) return null;
        return Instance.Play(prefab, position, rotation);
    }

    public ParticleSystem Play(ParticleSystem prefab, Vector2 position, Quaternion rotation)
    {
        if (!pools.TryGetValue(prefab, out var pool))
        {
            ObjectPool<ParticleSystem> created = null;
            created = new ObjectPool<ParticleSystem>(
                createFunc: () =>
                {
                    var p = Instantiate(prefab, transform);
                    var main = p.main;
                    main.stopAction = ParticleSystemStopAction.Callback;
                    var pp = p.GetComponent<PooledParticle>();
                    if (pp == null) pp = p.gameObject.AddComponent<PooledParticle>();
                    pp.Init(x => created.Release(x));
                    return p;
                },
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => Destroy(p.gameObject),
                collectionCheck: false, defaultCapacity: 8, maxSize: 64);
            pools[prefab] = pool = created;
        }

        var ps = pool.Get();
        ps.transform.SetPositionAndRotation(new Vector3(position.x, position.y, 0f), rotation);
        ps.transform.localScale = Vector3.one;
        ps.Clear();
        ps.Play();
        return ps;
    }

    public void HitStop(float duration)
    {
        if (duration <= 0f) return;
        if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
        hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
    }

    IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.03f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        hitStopRoutine = null;
    }
}
