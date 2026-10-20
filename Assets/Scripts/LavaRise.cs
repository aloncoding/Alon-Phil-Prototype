using UnityEngine;

/// The lava is a real object in the world with its own height (Y) that rises at its own speed, independent of the camera.
/// Climb faster than it rises and you build a lead. If your lead gets bigger than lava.leadSoftCap it speeds up to catch you.
/// All numbers: GameConfig.lava.
public class LavaRise : MonoBehaviour
{
    static GameConfig.LavaSettings L => GameConfig.I.lava;

    public float Y { get; private set; }
    public float Lead => player != null ? player.transform.position.y - Y : 0f;

    PlayerClimber player;
    Transform visual;
    bool started;

    public void Begin(PlayerClimber p)
    {
        player = p;
        Y = L.startY;
        started = false;

        visual = new GameObject("Lava").transform;
        visual.SetParent(transform, false);

        var body = new GameObject("Body");
        body.transform.SetParent(visual, false);
        body.transform.localPosition = new Vector3(0f, -20f, 0f);
        var bs = body.AddComponent<SpriteRenderer>();
        bs.sprite = ArtLibrary.LavaBody;
        bs.sharedMaterial = ArtLibrary.SpriteMat;
        bs.drawMode = SpriteDrawMode.Tiled;
        bs.size = new Vector2(60f, 40f);
        bs.sortingOrder = 15;

        var top = new GameObject("Top");
        top.transform.SetParent(visual, false);
        var ts = top.AddComponent<SpriteRenderer>();
        ts.sprite = ArtLibrary.LavaTop;
        ts.sharedMaterial = ArtLibrary.SpriteMat;
        ts.drawMode = SpriteDrawMode.Tiled;
        ts.size = new Vector2(60f, 0.25f);
        ts.sortingOrder = 16;

        visual.position = new Vector3(0f, Y, 0f);
    }

    void Update()
    {
        if (player == null) return;

        if (!started && player.Height > L.startAfterHeight) started = true;
        if (started && !player.Dead)
        {
            float k = Mathf.Clamp01(player.Height / Mathf.Max(1f, L.speedRampHeight));
            float speed = Mathf.Lerp(L.speedStart, L.speedEnd, k);
            float lead = Lead;
            if (lead > L.leadSoftCap) speed += (lead - L.leadSoftCap) * L.catchUpFactor;
            Y += speed * Time.deltaTime;
        }
        visual.position = new Vector3(0f, Y + Mathf.Sin(Time.time * 2f) * 0.08f, 0f);
    }
}
