using UnityEngine;

/// Animated rope: wavy and slack when loose, straight and thin when under tension.
[RequireComponent(typeof(LineRenderer))]
public class TetherRenderer : MonoBehaviour
{
    [SerializeField] int segments = 24;
    [SerializeField] float slackAmplitude = 0.35f;
    [SerializeField] float sag = 0.25f;
    [SerializeField] float waveFrequency = 2.5f;
    [SerializeField] float waveSpeed = 14f;
    [SerializeField] float baseWidth = 0.09f;

    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.enabled = false;
    }

    public void Show() => lr.enabled = true;
    public void Hide() => lr.enabled = false;

    /// tension 0 = loose/wavy, 1 = taut.
    public void Draw(Vector2 a, Vector2 b, float tension)
    {
        if (!lr.enabled) return;
        Vector2 d = b - a;
        float len = d.magnitude;
        Vector2 perp = len > 0.001f ? new Vector2(-d.y, d.x) / len : Vector2.up;
        float slack = 1f - Mathf.Clamp01(tension);

        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float envelope = Mathf.Sin(t * Mathf.PI);
            float wave = Mathf.Sin(t * waveFrequency * Mathf.PI * 2f - Time.time * waveSpeed);
            Vector2 p = Vector2.Lerp(a, b, t)
                        + perp * wave * envelope * slackAmplitude * slack
                        + Vector2.down * envelope * sag * slack;
            lr.SetPosition(i, new Vector3(p.x, p.y, 0f));
        }

        float pulse = 1f + Mathf.Sin(Time.time * 40f) * 0.1f * tension;
        lr.widthMultiplier = Mathf.Lerp(baseWidth, baseWidth * 0.6f, tension) * pulse;
    }
}
