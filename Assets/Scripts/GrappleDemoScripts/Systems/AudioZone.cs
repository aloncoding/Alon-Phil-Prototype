using UnityEngine;
using UnityEngine.Audio;

/// Trigger zone that swaps mixer snapshots (e.g. open air <-> enclosed ruins reverb).
[RequireComponent(typeof(Collider2D))]
public class AudioZone : MonoBehaviour
{
    [SerializeField] AudioMixerSnapshot enterSnapshot;
    [SerializeField] AudioMixerSnapshot exitSnapshot;
    [SerializeField] float transitionTime = 1.5f;
    [SerializeField, Range(0f, 1f)] float ambienceOnEnter = 1f;
    [SerializeField, Range(0f, 1f)] float ambienceOnExit = 1f;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (AudioController.Instance == null || other.GetComponentInParent<PlayerController>() == null) return;
        AudioController.Instance.TransitionTo(enterSnapshot, transitionTime);
        AudioController.Instance.SetAmbienceMultiplier(ambienceOnEnter, transitionTime);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (AudioController.Instance == null || other.GetComponentInParent<PlayerController>() == null) return;
        AudioController.Instance.TransitionTo(exitSnapshot, transitionTime);
        AudioController.Instance.SetAmbienceMultiplier(ambienceOnExit, transitionTime);
    }
}
