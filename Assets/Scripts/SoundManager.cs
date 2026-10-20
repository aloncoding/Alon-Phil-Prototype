using UnityEngine;

public enum Sfx { Jump, Land, Stomp, Kill, Hurt, HookFire, HookHit, HookMiss, Death, Bell }

/// Builds all clips from Synth, pools AudioSources, loops music and the rope-tension sound.
public class SoundManager : MonoBehaviour
{
    static SoundManager inst;
    AudioClip[] clips;
    AudioSource[] pool;
    AudioSource music, pull;
    int next;

    public static void Init()
    {
        if (inst != null) return;
        var go = new GameObject("Sound");
        inst = go.AddComponent<SoundManager>();
        inst.Build();
    }

    void Build()
    {
        clips = new AudioClip[10];
        clips[(int)Sfx.Jump] = Synth.Jump();
        clips[(int)Sfx.Land] = Synth.Land();
        clips[(int)Sfx.Stomp] = Synth.Stomp();
        clips[(int)Sfx.Kill] = Synth.Kill();
        clips[(int)Sfx.Hurt] = Synth.Hurt();
        clips[(int)Sfx.HookFire] = Synth.HookFire();
        clips[(int)Sfx.HookHit] = Synth.HookHit();
        clips[(int)Sfx.HookMiss] = Synth.HookMiss();
        clips[(int)Sfx.Death] = Synth.Death();
        clips[(int)Sfx.Bell] = Synth.Bell();

        pool = new AudioSource[14];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
            pool[i].spatialBlend = 0f;
        }

        music = gameObject.AddComponent<AudioSource>();
        music.clip = Synth.Music();
        music.loop = true;
        music.volume = GameConfig.I.audio.masterVolume * GameConfig.I.audio.musicVolume;
        music.spatialBlend = 0f;
        music.Play();

        pull = gameObject.AddComponent<AudioSource>();
        pull.clip = Synth.PullLoop();
        pull.loop = true;
        pull.volume = 0f;
        pull.spatialBlend = 0f;
        pull.Play();
    }

    public static void Play(Sfx s, float volume = 1f, float pitch = 1f)
    {
        if (inst == null) return;
        var src = inst.pool[inst.next];
        inst.next = (inst.next + 1) % inst.pool.Length;
        src.clip = inst.clips[(int)s];
        src.volume = Mathf.Clamp01(volume * GameConfig.I.audio.sfxVolume * GameConfig.I.audio.masterVolume);
        src.pitch = pitch * Random.Range(0.97f, 1.03f);
        src.Play();
    }

    public static void SetPull(bool on, float intensity)
    {
        if (inst == null) return;
        inst.pull.volume = on ? 0.5f * GameConfig.I.audio.sfxVolume * GameConfig.I.audio.masterVolume : 0f;
        inst.pull.pitch = 0.8f + intensity * 0.8f;
    }

    /// Music speeds up a little as the run gets faster.
    public static void SetMusicPitch(float p)
    {
        if (inst == null) return;
        inst.music.pitch = p;
        inst.music.volume = GameConfig.I.audio.masterVolume * GameConfig.I.audio.musicVolume;
    }
}
