using UnityEngine;

/// Code-built particle bursts (no prefabs needed).
public static class FX
{
    static ParticleSystem template;

    static void Ensure()
    {
        if (template != null) return;
        var go = new GameObject("FXTemplate");
        go.SetActive(false);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;

        var em = ps.emission; em.rateOverTime = 0f;
        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.1f;

        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var sz = ps.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var r = go.GetComponent<ParticleSystemRenderer>();
        var m = new Material(Shader.Find("Sprites/Default"));
        m.mainTexture = ArtLibrary.Dot.texture;
        r.sharedMaterial = m;
        r.sortingOrder = 20;

        template = ps;
    }

    public static void Burst(Vector2 pos, Color color, int count, float speed)
    {
        Ensure();
        var ps = Object.Instantiate(template);
        ps.transform.position = new Vector3(pos.x, pos.y, 0f);
        ps.gameObject.SetActive(true);
        var main = ps.main;
        main.startColor = color;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        ps.Play();
        ps.Emit(count);
        Object.Destroy(ps.gameObject, 1.2f);
    }

    public static void Dust(Vector2 pos, int count = 6) =>
        Burst(pos, new Color(0.8f, 0.75f, 0.7f, 0.9f), count, 2.5f);
}
