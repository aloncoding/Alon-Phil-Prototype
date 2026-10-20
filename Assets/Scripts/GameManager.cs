using UnityEngine;

/// Entry point. Starts itself automatically when you press Play in ANY scene (even an empty one).
/// Builds: camera, sky/stars/clouds, tower walls, audio, the player and the endless tower.
/// There is no UI or text at all. After you die the run restarts by itself (GameConfig.flow.restartDelay).
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    static PhysicsMaterial2D noFriction;
    public static PhysicsMaterial2D NoFriction
    {
        get
        {
            if (noFriction == null) noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            return noFriction;
        }
    }

    // set to false if you'd rather place this object in a scene yourself
    const bool AutoStart = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!AutoStart || FindFirstObjectByType<GameManager>() != null) return;
        new GameObject("Game").AddComponent<GameManager>();
    }

    static GameConfig Cfg => GameConfig.I;

    Camera cam;
    CameraFollow follow;
    PlayerClimber player;
    GameObject runRoot;
    SpriteRenderer skyRenderer, starsRenderer;
    Transform sky;
    LavaRise lava;

    bool dead;
    float deadTime;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        GameInput.Init();
        SoundManager.Init();

        SetupCamera();
        BuildBackground();
        ShaftWalls.Create(cam);
        StartRun();
    }

    // ---------------- setup ----------------
    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = Cfg.cam.orthoSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Cfg.visuals.backgroundColor;
        cam.transform.position = new Vector3(0f, 5f, -10f);
        cam.transform.rotation = Quaternion.identity;

        follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
    }

    void BuildBackground()
    {
        var bg = new GameObject("Background").transform;

        var skyGo = new GameObject("Sky");
        skyGo.transform.SetParent(cam.transform, false);
        skyGo.transform.localPosition = new Vector3(0f, 0f, 20f);
        skyRenderer = skyGo.AddComponent<SpriteRenderer>();
        skyRenderer.sprite = ArtLibrary.Sky;
        skyRenderer.sharedMaterial = ArtLibrary.SpriteMat;
        skyRenderer.sortingOrder = -100;
        sky = skyGo.transform;

        var stars = ParallaxLayer.Create(bg, ArtLibrary.Stars, 0.08f, -95, new Color(1f, 1f, 1f, 0f));
        starsRenderer = stars.GetComponent<SpriteRenderer>();
        ParallaxLayer.Create(bg, ArtLibrary.CloudField, 0.25f, -90, Color.white);
        ParallaxLayer.Create(bg, ArtLibrary.CloudField, 0.5f, -85, new Color(1f, 1f, 1f, 0.6f));
    }

    // ---------------- run lifecycle ----------------
    void StartRun()
    {
        if (runRoot != null) Destroy(runRoot);
        runRoot = new GameObject("Run");

        player = new GameObject().AddComponent<PlayerClimber>();
        player.transform.SetParent(runRoot.transform);
        player.transform.position = new Vector3(0f, 1f, 0f);
        player.StartY = 1f;
        player.Died += OnPlayerDied;
        player.Stomped += OnStomp;
        player.Hurt += OnHurt;

        lava = runRoot.AddComponent<LavaRise>();
        lava.Begin(player);

        var gen = runRoot.AddComponent<WorldGenerator>();
        gen.Begin(player.transform, 1f, lava);

        follow.Begin(player);
        dead = false;
        SoundManager.SetMusicPitch(1f);
        SoundManager.SetPull(false, 0f);
    }

    void OnStomp(int combo) => follow.Shake(Cfg.flow.stompShake);
    void OnHurt() => follow.Shake(Cfg.flow.hurtShake);

    void OnPlayerDied()
    {
        dead = true;
        deadTime = Time.unscaledTime;
        follow.Shake(Cfg.flow.deathShake);
        SoundManager.SetMusicPitch(0.8f);
        SoundManager.SetPull(false, 0f);
    }

    void Update()
    {
        var v = Cfg.visuals;
        cam.orthographicSize = Cfg.cam.orthoSize;
        cam.backgroundColor = v.backgroundColor;

        // keep the sky covering the whole view
        float h = cam.orthographicSize * 2f + 2f;
        sky.localScale = new Vector3(h * 2.6f / 2f, h / 128f, 1f);

        // altitude mood: sky darkens, stars fade in
        float alt = player != null ? Mathf.Clamp01(player.Height / Mathf.Max(1f, v.skyDarkenHeight)) : 0f;
        skyRenderer.color = Color.Lerp(Color.white, v.skyHighTint, alt);
        starsRenderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01((alt - v.starsStartFraction) * 1.6f));

        if (dead)
        {
            if (Time.unscaledTime >= deadTime + Cfg.flow.restartDelay) StartRun();
            return;
        }

        // die in the lava (or, if the camera can't descend, by falling off the bottom of the screen)
        float py = player.transform.position.y;
        bool inLava = py < lava.Y + Cfg.lava.killOffset;
        bool offscreen = !Cfg.cam.canDescend && py < follow.Bottom - Cfg.cam.offscreenKillMargin;
        if (inLava || offscreen) player.Kill();

        SoundManager.SetMusicPitch(1f + Cfg.audio.musicPitchBoostAtMaxHeight * alt);
    }
}