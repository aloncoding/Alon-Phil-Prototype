using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// The grappling hook relic: bobs and pulses to draw the eye, triggers the acquisition sequence on touch.
/// Needs a trigger Collider2D. Animator trigger "Collected" is optional.
[RequireComponent(typeof(Collider2D))]
public class HookRelic : MonoBehaviour
{
    [SerializeField] Light2D glow;
    [SerializeField] ParticleSystem ambientParticles;
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer[] renderers;
    [SerializeField] float bobAmplitude = 0.15f;
    [SerializeField] float bobSpeed = 1.5f;
    [SerializeField] Vector2 pulseIntensity = new Vector2(1.2f, 2.6f);
    [SerializeField] float pulseSpeed = 2f;
    [SerializeField] float collectSwellIntensity = 6f;

    static readonly int CollectedH = Animator.StringToHash("Collected");

    bool collected;
    Vector3 startPos;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;
    void Start() => startPos = transform.position;

    void Update()
    {
        if (collected) return;
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        if (glow != null)
            glow.intensity = Mathf.Lerp(pulseIntensity.x, pulseIntensity.y, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other.GetComponentInParent<PlayerController>() == null) return;
        if (LevelSequenceController.Instance == null) return;
        collected = true;
        GetComponent<Collider2D>().enabled = false;
        LevelSequenceController.Instance.BeginHookAcquisition(this);
    }

    /// Called by LevelSequenceController when the cinematic beat starts.
    public void PlayCollected()
    {
        animator.SafeTrigger(CollectedH);
        StartCoroutine(SwellAndVanish());
    }

    IEnumerator SwellAndVanish()
    {
        float startI = glow != null ? glow.intensity : 0f;
        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            if (glow != null) glow.intensity = Mathf.Lerp(startI, collectSwellIntensity, t / 1.2f);
            yield return null;
        }
        t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            float k = 1f - t / 0.8f;
            if (glow != null) glow.intensity = collectSwellIntensity * k;
            foreach (var r in renderers) { var c = r.color; c.a = k; r.color = c; }
            yield return null;
        }
        if (ambientParticles != null) ambientParticles.Stop();
        gameObject.SetActive(false);
    }
}
