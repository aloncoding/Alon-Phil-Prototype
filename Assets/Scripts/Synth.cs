using UnityEngine;

/// Procedural audio: every sound effect and the music loop are synthesised from maths at startup.
public static class Synth
{
    public const int SR = 44100;
    static readonly System.Random rng = new System.Random(7);

    static float N() => (float)(rng.NextDouble() * 2.0 - 1.0);
    static float Sin(float p) => Mathf.Sin(p * Mathf.PI * 2f);
    static float Sq(float p, float duty = 0.5f) { p -= Mathf.Floor(p); return p < duty ? 1f : -1f; }
    static float Saw(float p) { p -= Mathf.Floor(p); return p * 2f - 1f; }
    static float Tri(float p) { p -= Mathf.Floor(p); return Mathf.Abs(p * 4f - 2f) - 1f; }

    static float Env(float t, float dur, float attack = 0.004f, float power = 2f)
    {
        float a = Mathf.Min(1f, t / attack);
        return a * Mathf.Pow(Mathf.Max(1f - t / dur, 0f), power);
    }

    static AudioClip Make(string name, float dur, System.Func<float, float> gen)
    {
        int n = (int)(dur * SR);
        var d = new float[n];
        for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(gen(i / (float)SR), -1f, 1f);
        var c = AudioClip.Create(name, n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    // ---------------- Sound effects ----------------
    public static AudioClip Jump()
    {
        float ph = 0; const float D = 0.18f;
        return Make("jump", D, t => { ph += Mathf.Lerp(330f, 820f, t / D) / SR; return Sq(ph, 0.4f) * 0.28f * Env(t, D); });
    }

    public static AudioClip Stomp()
    {
        float ph = 0; const float D = 0.24f;
        return Make("stomp", D, t =>
        {
            ph += Mathf.Lerp(180f, 520f, t / D) / SR;
            return Sin(ph) * 0.65f * Env(t, D, 0.002f, 1.5f) + N() * 0.3f * Env(t, 0.03f, 0.001f, 1f);
        });
    }

    public static AudioClip Kill()
    {
        float ph = 0; const float D = 0.3f;
        return Make("kill", D, t =>
        {
            ph += Mathf.Lerp(700f, 120f, t / D) / SR;
            return (Sq(ph, 0.25f) * 0.25f + N() * 0.2f * Env(t, 0.12f, 0.001f, 1f)) * Env(t, D, 0.002f, 1.2f);
        });
    }

    public static AudioClip Hurt()
    {
        float ph = 0; const float D = 0.4f;
        return Make("hurt", D, t =>
        {
            ph += Mathf.Lerp(260f, 70f, t / D) / SR;
            return Saw(ph) * 0.35f * Env(t, D, 0.002f, 1f) + N() * 0.25f * Env(t, 0.15f, 0.001f, 1.5f);
        });
    }

    public static AudioClip Land()
    {
        float lp = 0; const float D = 0.12f;
        return Make("land", D, t =>
        {
            lp += (N() - lp) * 0.08f;
            return lp * 2.2f * Env(t, D, 0.001f, 2f) + Sin(t * 85f) * 0.5f * Env(t, D, 0.001f, 2f);
        });
    }

    public static AudioClip HookFire()
    {
        float ph = 0; const float D = 0.12f;
        return Make("hookfire", D, t =>
        {
            ph += Mathf.Lerp(500f, 1800f, t / D) / SR;
            return Sin(ph) * 0.25f * Env(t, D, 0.003f, 1f) + N() * 0.12f * Env(t, D, 0.003f, 1f);
        });
    }

    public static AudioClip HookHit()
    {
        const float D = 0.22f;
        return Make("hookhit", D, t =>
            (Sin(t * 880f) * 0.3f + Sin(t * 1320f) * 0.25f) * Env(t, D, 0.001f, 3f) + N() * 0.3f * Env(t, 0.02f, 0.0005f, 1f));
    }

    public static AudioClip HookMiss()
    {
        float ph = 0; const float D = 0.14f;
        return Make("hookmiss", D, t => { ph += Mathf.Lerp(520f, 230f, t / D) / SR; return Sin(ph) * 0.18f * Env(t, D); });
    }

    public static AudioClip Death()
    {
        float ph = 0, ph2 = 0; const float D = 0.9f;
        return Make("death", D, t =>
        {
            float f = Mathf.Lerp(420f, 35f, t / D);
            ph += f / SR; ph2 += f * 0.5f / SR;
            return (Saw(ph) * 0.25f + Sq(ph2) * 0.2f) * Env(t, D, 0.003f, 1f);
        });
    }

    public static AudioClip Bell()
    {
        const float D = 0.4f;
        return Make("bell", D, t =>
            (Sin(t * 660f) * 0.3f + Sin(t * 1320f) * 0.15f + Sin(t * 1980f) * 0.07f) * Env(t, D, 0.002f, 3f));
    }

    /// 1 second seamless loop (integer cycle counts) used while the rope is under tension.
    public static AudioClip PullLoop()
    {
        return Make("pull", 1f, t => (Tri(t * 120f) * 0.5f + Sq(t * 240f, 0.3f) * 0.12f) * (0.6f + 0.4f * Sin(t * 6f)) * 0.22f);
    }

    // ---------------- Music ----------------
    const int Wave_Sine = 0, Wave_Tri = 1, Wave_Sq25 = 2, Wave_Sq50 = 3;
    const int Rest = -99;

    static void AddTone(float[] buf, float start, float dur, float freq, int wave, float amp)
    {
        int s0 = (int)(start * SR), n = (int)(dur * SR);
        for (int j = 0; j < n; j++)
        {
            float t = j / (float)SR;
            float env = Mathf.Min(1f, t / 0.003f) * Mathf.Pow(1f - (float)j / n, 1.5f);
            float p = freq * t;
            float v = wave == Wave_Sine ? Sin(p) : wave == Wave_Tri ? Tri(p) : wave == Wave_Sq25 ? Sq(p, 0.25f) : Sq(p, 0.5f);
            buf[(s0 + j) % buf.Length] += v * amp * env;
        }
    }

    static void AddKick(float[] buf, float start)
    {
        int s0 = (int)(start * SR), n = (int)(0.14f * SR);
        float ph = 0;
        for (int j = 0; j < n; j++)
        {
            float k = (float)j / n;
            ph += Mathf.Lerp(150f, 45f, k) / SR;
            buf[(s0 + j) % buf.Length] += Sin(ph) * 0.55f * (1f - k) * (1f - k);
        }
    }

    static void AddNoise(float[] buf, float start, float dur, float amp)
    {
        int s0 = (int)(start * SR), n = (int)(dur * SR);
        for (int j = 0; j < n; j++)
        {
            float k = (float)j / n;
            buf[(s0 + j) % buf.Length] += N() * amp * Mathf.Pow(1f - k, 3f);
        }
    }

    public static AudioClip Music()
    {
        float bpm = GameConfig.I.audio.musicBpm;
        float step = 60f / bpm / 4f;          // one 16th note
        const int steps = 128;                // 8 bars
        int len = (int)(steps * step * SR);
        var buf = new float[len];

        float[] roots = { 110f, 87.31f, 130.81f, 98f }; // Am F C G
        float[][] chords =
        {
            new[] { 220f, 261.63f, 329.63f },
            new[] { 174.61f, 220f, 261.63f },
            new[] { 261.63f, 329.63f, 392f },
            new[] { 196f, 246.94f, 293.66f }
        };
        int[] arp = { 0, 1, 2, 1 };
        int[] motifA = { 0, Rest, 3, Rest, 5, Rest, 7, 5, 3, Rest, 5, Rest, 0, Rest, Rest, Rest };
        int[] motifB = { 7, Rest, 10, Rest, 12, Rest, 10, 7, 5, Rest, 7, Rest, 3, Rest, Rest, Rest };

        for (int s = 0; s < steps; s++)
        {
            int bar = s / 16, st = s % 16, ci = bar % 4;
            float t0 = s * step;

            if (st % 2 == 0) AddTone(buf, t0, step * 1.8f, roots[ci] * (st % 8 == 6 ? 2f : 1f), Wave_Tri, 0.30f);

            float af = chords[ci][arp[st % 4]] * (st % 8 >= 4 ? 2f : 1f);
            AddTone(buf, t0, step * 0.9f, af, Wave_Sq25, 0.06f);

            if (bar >= 4)
            {
                int semi = (bar % 2 == 0 ? motifA : motifB)[st];
                if (semi != Rest) AddTone(buf, t0, step * 1.6f, 440f * Mathf.Pow(2f, semi / 12f), Wave_Sq50, 0.08f);
            }

            if (st % 4 == 0) AddKick(buf, t0);
            if (st == 4 || st == 12) AddNoise(buf, t0, 0.12f, 0.22f);
            if (st % 2 == 0) AddNoise(buf, t0, 0.04f, 0.07f);
        }

        float max = 0.001f;
        for (int i = 0; i < len; i++) max = Mathf.Max(max, Mathf.Abs(buf[i]));
        float scale = 0.85f / max;
        for (int i = 0; i < len; i++) buf[i] *= scale;

        var clip = AudioClip.Create("music", len, 1, SR, false);
        clip.SetData(buf, 0);
        return clip;
    }
}
