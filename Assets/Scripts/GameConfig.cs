using System;
using UnityEngine;

/// ONE place for every gameplay number. In the Editor an asset is created automatically at
/// Assets/Resources/GameConfig.asset the first time you press Play - select it and tweak values
/// (most of them apply live while the game is running). Delete the asset to go back to defaults.
/// Menu: Tools > Hook Climber > Select Config.
[CreateAssetMenu(fileName = "GameConfig", menuName = "Hook Climber/Game Config")]
public class GameConfig : ScriptableObject
{
    // ============================ PLAYER ============================
    [Serializable]
    public class PlayerSettings
    {
        [Header("Movement")]
        public float moveSpeed = 10f;
        public float groundAccel = 80f;
        public float groundDecel = 90f;
        public float airAccel = 50f;
        public float airDecel = 22f;

        [Header("Jump")]
        public float jumpVelocity = 16f;
        public float gravityScale = 3f;
        public float fallMultiplier = 1.3f;
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.12f;
        [Range(0.1f, 1f)] public float jumpCutMultiplier = 0.5f;
        public float maxFallSpeed = 28f;

        [Header("Enemy stomp")]
        public float stompBounce = 15f;
        public float stompBounceHeld = 19f;       // bounce when Space is held
        public float stompGraceTime = 0.15f;      // can't be hurt right after a stomp
        [Range(0f, 1f)] public float hookStompHorizontalKeep = 0.5f;

        [Header("Health (no UI: shown by flicker + red tint)")]
        public int maxHealth = 3;
        public float invulnerableTime = 1.3f;
        public float hurtBounceSpeed = 9f;
        [Range(0f, 1f)] public float hurtHorizontalKeep = 0.5f;
        public bool tintByHealth = true;
        public Color lowHealthTint = new Color(1f, 0.5f, 0.5f);

        [Header("Body")]
        public Vector2 colliderSize = new Vector2(0.7f, 1.4f);
    }

    // ============================ HOOK ============================
    [Serializable]
    public class HookSettings
    {
        [Header("Aiming")]
        public float range = 14f;
        public float aimRadius = 0.45f;           // forgiveness of the aim ray
        public float cooldown = 0.12f;

        [Header("Pulling")]
        public float pullSpeed = 28f;
        public float pullAccel = 170f;
        public float startSpeedMin = 8f;
        public float maxPullTime = 1.6f;
        public float enemyStopDistance = 0.35f;
        public float platformStopDistance = 0.12f;
        public float platformEaseFactor = 12f;    // higher = brakes later when landing on a platform
        public float platformAnchorHeight = 0.75f;// how far above the platform top you end up
        [Range(0f, 1f)] public float platformExitSpeedKeep = 0.3f;
        public float releaseSpeedKeep = 0.8f;
        public float jumpOffBoost = 12f;          // extra upward speed when you press Space while hooked

        [Header("Rope look")]
        public float ropeWidth = 0.09f;
        public Color ropeColor = new Color(0.97f, 0.86f, 0.5f);
    }

    // ============================ ENEMIES ============================
    [Serializable]
    public class EnemySettings
    {
        [Header("Crawler")]
        public float crawlerSpeed = 1.6f;
        public float crawlerEdgeMargin = 0.8f;    // keeps crawlers away from platform edges

        [Header("Floater")]
        public float floaterBobX = 1f;
        public float floaterBobXSpeed = 1.3f;
        public float floaterBobY = 0.4f;
        public float floaterBobYSpeed = 2f;

        [Header("General")]
        public float animFps = 6f;
        public int killBurstCount = 16;
    }

    // ============================ WORLD GENERATION ============================
    [Serializable]
    public class WorldSettings
    {
        [Header("Tower")]
        public float shaftWidth = 24f;            // applied when the run starts
        public float platformThickness = 1f;
        public float safeStartHeight = 8f;        // only plain platforms below this height

        [Header("Vertical spacing (grows with difficulty)")]
        public float rowGapMin = 3.2f;
        public float rowGapMax = 6.2f;
        public float difficultyRampHeight = 600f;  // height at which difficulty reaches 1

        [Header("Platform width (shrinks with difficulty)")]
        public float platformWidthStart = 4.5f;
        public float platformWidthEnd = 2.2f;
        public float minPlatformWidth = 1.8f;
        public float widthJitterMin = 0.85f;
        public float widthJitterMax = 1.2f;
        public float pairWidthFactor = 0.75f;      // each platform of a two-platform row
        public float horizontalShift = 9f;         // max sideways jump between rows

        [Header("Row style weights (normalised)")]
        public float rowSingleWeight = 0.50f;
        public float rowPairWeight = 0.12f;
        public float rowFloaterStepWeight = 0.20f; // platform + a floater to bounce off below it

        [Header("Sparseness: rows with no platform at all")]
        [Range(0f, 1f)] public float platformSkipChance = 0.3f;     // chance a row is left empty (above safeStartHeight)
        public int maxConsecutiveSkips = 1;
        [Range(0f, 1f)] public float skippedRowFloaterChance = 0.7f; // an empty row often still gets a floater to bounce off

        [Header("Enemies on platforms")]
        public float crawlerChanceStart = 0.2f;
        public float crawlerChanceEnd = 0.55f;
        public float crawlerMinPlatformWidth = 2.6f;
        public float floaterAboveChanceStart = 0.2f;
        public float floaterAboveChanceEnd = 0.4f;

        [Header("Enemy-only stretches (no platforms, just floaters)")]
        public bool stretchEnabled = true;
        public float stretchMinHeight = 25f;
        [Range(0f, 1f)] public float stretchChancePerRow = 0.15f;
        public int stretchCooldownRows = 5;        // rows of normal level after a stretch
        public int stretchMinRows = 4;
        public int stretchMaxRows = 8;
        public float stretchGapMin = 2.4f;          // vertical distance between floater rows
        public float stretchGapMax = 3.4f;
        public int stretchFloatersMin = 1;
        public int stretchFloatersMax = 2;
        public float stretchHorizontalRange = 8f;
        public bool stretchEndsWithPlatform = true;

        [Header("Streaming")]
        public float generateAhead = 20f;
        public float cleanupBelow = 15f;           // world is deleted this far below the LAVA surface
    }

    // ============================ CAMERA ============================
    [Serializable]
    public class CameraSettings
    {
        public float orthoSize = 9f;
        [Range(0f, 1f)] public float playerScreenHeight = 0.45f; // camera centre sits this fraction of ortho above the player
        public float followSmooth = 6f;            // how fast the camera follows you upward
        public float followSmoothDown = 12f;       // ...and downward (faster so you never fall off screen)
        public float verticalDeadZone = 2.5f;      // camera ignores vertical movement within this distance, so hops don't bob it
        public bool canDescend = true;             // false = camera only ever moves up (then falling off the bottom of the screen kills)
        public float offscreenKillMargin = 1f;     // only used when canDescend is false
    }

    // ============================ LAVA ============================
    [Serializable]
    public class LavaSettings
    {
        public float startY = -6f;                 // starting height of the lava surface (floor top is 0)
        public float startAfterHeight = 2f;        // lava starts rising once you have climbed this high
        public float speedStart = 1.2f;            // rise speed (units/second) at the bottom...
        public float speedEnd = 4.5f;              // ...and at speedRampHeight
        public float speedRampHeight = 500f;
        public float leadSoftCap = 25f;            // if you get further ahead than this the lava speeds up to catch up
        public float catchUpFactor = 0.5f;         // extra speed per unit of lead beyond the cap
        public float killOffset = 0.3f;            // you die when your centre is below (lava surface + this)
    }

    // ============================ FLOW ============================
    [Serializable]
    public class FlowSettings
    {
        public float restartDelay = 3f;            // seconds after dying before the run restarts
        public float hurtShake = 0.3f;
        public float stompShake = 0.12f;
        public float deathShake = 0.5f;
        public int bonusPerStompCombo = 25;        // score bonus is not displayed; kept for future use
    }

    // ============================ AUDIO ============================
    [Serializable]
    public class AudioSettings
    {
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.35f;
        public float musicBpm = 140f;              // applied at startup (music is synthesised then)
        public float musicPitchBoostAtMaxHeight = 0.18f;
        public float stompPitchPerCombo = 0.06f;
        public float bellPitchPerCombo = 0.12f;
    }

    // ============================ LOOK ============================
    [Serializable]
    public class VisualSettings
    {
        public Color backgroundColor = new Color(0.12f, 0.08f, 0.3f);
        public Color skyHighTint = new Color(0.3f, 0.35f, 0.7f);
        public float skyDarkenHeight = 450f;
        public float starsStartFraction = 0.1f;
        public Color wallTint = new Color(0.6f, 0.6f, 0.8f);
    }

    public PlayerSettings player = new PlayerSettings();
    public HookSettings hook = new HookSettings();
    public EnemySettings enemies = new EnemySettings();
    public WorldSettings world = new WorldSettings();
    public CameraSettings cam = new CameraSettings();
    public LavaSettings lava = new LavaSettings();
    public FlowSettings flow = new FlowSettings();
    public AudioSettings audio = new AudioSettings();
    public VisualSettings visuals = new VisualSettings();

    static GameConfig inst;

    public static GameConfig I
    {
        get
        {
            if (inst != null) return inst;
            inst = Resources.Load<GameConfig>("GameConfig");
#if UNITY_EDITOR
            if (inst == null)
            {
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                var created = CreateInstance<GameConfig>();
                UnityEditor.AssetDatabase.CreateAsset(created, "Assets/Resources/GameConfig.asset");
                UnityEditor.AssetDatabase.SaveAssets();
                inst = created;
            }
#endif
            if (inst == null) inst = CreateInstance<GameConfig>(); // builds without an asset use the defaults above
            return inst;
        }
    }
}