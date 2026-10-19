using System;
using UnityEngine;

/// Returns a particle system to its pool when it finishes (stopAction = Callback).
public class PooledParticle : MonoBehaviour
{
    Action<ParticleSystem> release;
    ParticleSystem ps;

    public void Init(Action<ParticleSystem> releaseAction)
    {
        release = releaseAction;
        ps = GetComponent<ParticleSystem>();
    }

    void OnParticleSystemStopped() => release?.Invoke(ps);
}
