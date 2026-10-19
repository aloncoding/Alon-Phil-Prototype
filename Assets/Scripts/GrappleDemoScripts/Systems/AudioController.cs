using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// Pooled SFX playback, looping ambience layers, music, mixer snapshots and master fades.
/// Use: AudioController.Sfx(bank, position, intensity).
public class AudioController : MonoBehaviour
{
    [Serializable]
    public class AmbientLayer
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.5f;
        [HideInInspector] public AudioSource source;
    }

    public static AudioController Instance { get; private set; }

    [Header("Mixer groups (optional)")]
    [SerializeField] AudioMixerGroup sfxGroup;
    [SerializeField] AudioMixerGroup ambienceGroup;
    [SerializeField] AudioMixerGroup musicGroup;

    [Header("SFX pool")]
    [SerializeField] int poolSize = 20;
    [SerializeField] float minDistance = 4f;
    [SerializeField] float maxDistance = 30f;

    [Header("Ambience & music")]
    [SerializeField] AmbientLayer[] ambientLayers;
    [SerializeField] AudioClip musicLoop;
    [SerializeField, Range(0f, 1f)] float musicVolume = 0.35f;

    AudioSource[] pool;
    AudioSource music;
    int next;
    float ambienceMultiplier = 1f;
    Coroutine ambienceFade;

    void Awake()
    {
        Instance = this;
        AudioListener.volume = 1f;

        pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject("SFX_" + i);
            go.transform.SetParent(transform);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.outputAudioMixerGroup = sfxGroup;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = minDistance;
            s.maxDistance = maxDistance;
            pool[i] = s;
        }

        foreach (var l in ambientLayers)
        {
            if (l.clip == null) continue;
            var go = new GameObject("Ambience_" + l.clip.name);
            go.transform.SetParent(transform);
            l.source = go.AddComponent<AudioSource>();
            l.source.clip = l.clip;
            l.source.loop = true;
            l.source.spatialBlend = 0f;
            l.source.volume = l.volume;
            l.source.outputAudioMixerGroup = ambienceGroup;
            l.source.Play();
        }

        if (musicLoop != null)
        {
            music = gameObject.AddComponent<AudioSource>();
            music.clip = musicLoop;
            music.loop = true;
            music.volume = musicVolume;
            music.spatialBlend = 0f;
            music.outputAudioMixerGroup = musicGroup;
            music.Play();
        }
    }

    public static void Sfx(SoundBank bank, Vector2 position, float intensity = 1f)
    {
        if (Instance != null && bank != null) Instance.Play(bank, position, intensity);
    }

    public void Play(SoundBank bank, Vector2 position, float intensity = 1f)
    {
        if (!bank.TryGet(out var clip, out var vol, out var pitch)) return;
        var s = pool[next];
        next = (next + 1) % pool.Length;

        s.transform.position = new Vector3(position.x, position.y, 0f);
        s.clip = clip;
        s.volume = Mathf.Clamp01(vol * intensity);
        s.pitch = pitch * Mathf.Lerp(1.08f, 0.94f, Mathf.Clamp01(intensity)); // bigger = lower
        s.spatialBlend = bank.spatialBlend;
        s.Play();
    }

    public void TransitionTo(AudioMixerSnapshot snapshot, float time)
    {
        if (snapshot != null) snapshot.TransitionTo(time);
    }

    public void SetAmbienceMultiplier(float target, float time)
    {
        if (ambienceFade != null) StopCoroutine(ambienceFade);
        ambienceFade = StartCoroutine(AmbienceFadeRoutine(target, time));
    }

    IEnumerator AmbienceFadeRoutine(float target, float time)
    {
        float start = ambienceMultiplier, t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            ambienceMultiplier = Mathf.Lerp(start, target, t / time);
            foreach (var l in ambientLayers) if (l.source != null) l.source.volume = l.volume * ambienceMultiplier;
            yield return null;
        }
    }

    public void FadeMaster(float target, float time) => StartCoroutine(MasterFadeRoutine(target, time));

    IEnumerator MasterFadeRoutine(float target, float time)
    {
        float start = AudioListener.volume, t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            AudioListener.volume = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        AudioListener.volume = target;
    }
}
