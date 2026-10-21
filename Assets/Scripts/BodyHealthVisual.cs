using System.Collections.Generic;
using UnityEngine;

/// Body-only health display. Does not change health, collisions, animation choice, or hook art.
[DisallowMultipleComponent]
public sealed class BodyHealthVisual : MonoBehaviour
{
    sealed class FrameVariants
    {
        public Sprite twoSections, oneSection, silhouette;
    }

    readonly Dictionary<Sprite, FrameVariants> frames = new Dictionary<Sprite, FrameVariants>();
    PlayerClimber player;
    SpriteRenderer body, silhouette;

    public int VisibleSections { get; private set; } = 3;
    public float PulseAlpha { get; private set; } = 1f;
    public int CachedFrameCount => frames.Count;

    public void Init(PlayerClimber p, SpriteRenderer bodyRenderer, Transform visual)
    {
        player = p;
        body = bodyRenderer;
        if (silhouette == null)
        {
            var go = new GameObject("BodyHealthSilhouette");
            go.transform.SetParent(visual, false);
            silhouette = go.AddComponent<SpriteRenderer>();
        }
        silhouette.sharedMaterial = ArtLibrary.SpriteMat;
        silhouette.sortingLayerID = body.sortingLayerID;
        silhouette.sortingOrder = body.sortingOrder - 1;
        silhouette.color = Color.white;
        silhouette.enabled = false;

        // These are the five procedural body frames. Cache once at initialization,
        // so normal animation changes never allocate textures during Update.
        foreach (var frame in ArtLibrary.PlayerRun) Cache(frame);
        Cache(ArtLibrary.PlayerJump);
        Cache(ArtLibrary.PlayerFall);
        Cache(ArtLibrary.PlayerHook);
    }

    public void SetFrame(Sprite frame, float invulnRemaining)
    {
        if (player == null || body == null || silhouette == null) return;
        body.enabled = true;
        if (frame == null)
        {
            body.sprite = null;
            silhouette.enabled = false;
            return;
        }

        VisibleSections = player.Dead ? 0 : Mathf.Clamp(player.Health, 0, 3);
        PulseAlpha = VisibleSections == 1
            ? 0.775f + 0.225f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f)
            : 1f;

        if (VisibleSections == 3)
        {
            body.sprite = frame;
            silhouette.enabled = false;
        }
        else
        {
            var cached = Cache(frame);
            body.sprite = VisibleSections == 2 ? cached.twoSections
                : VisibleSections == 1 ? cached.oneSection : null;
            silhouette.sprite = cached.silhouette;
            silhouette.enabled = true;
        }

        // A mild tint fades throughout invulnerability, without blinking or
        // restoring any missing body section. Pulse affects only the active body.
        float duration = Mathf.Max(0.001f, GameConfig.I.player.invulnerableTime);
        float hurt = Mathf.Clamp01(invulnRemaining / duration);
        Color tint = Color.Lerp(Color.white, GameConfig.I.player.lowHealthTint, hurt * 0.35f);
        tint.a = PulseAlpha;
        body.color = tint;
    }

    FrameVariants Cache(Sprite source)
    {
        if (frames.TryGetValue(source, out var cached)) return cached;

        Rect rect = source.rect;
        int width = Mathf.RoundToInt(rect.width), height = Mathf.RoundToInt(rect.height);
        int x = Mathf.RoundToInt(rect.x), y = Mathf.RoundToInt(rect.y);
        Color32[] texturePixels = source.texture.GetPixels32();
        var pixels = new Color32[width * height];
        for (int row = 0; row < height; row++)
            System.Array.Copy(texturePixels, (y + row) * source.texture.width + x, pixels, row * width, width);

        var two = (Color32[])pixels.Clone();
        var one = (Color32[])pixels.Clone();
        var ghost = (Color32[])pixels.Clone();
        int lowerThird = height / 3, lowerTwoThirds = height * 2 / 3;
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                int i = row * width + col;
                if (row < lowerThird) two[i].a = 0;
                if (row < lowerTwoThirds) one[i].a = 0;
                bool ink = pixels[i].r == 24 && pixels[i].g == 18 && pixels[i].b == 40;
                ghost[i].a = (byte)Mathf.RoundToInt(pixels[i].a * (ink ? 0.5f : 0.18f));
            }
        }

        cached = new FrameVariants {
            twoSections = MakeSprite(source, width, height, two, "Health2"),
            oneSection = MakeSprite(source, width, height, one, "Health1"),
            silhouette = MakeSprite(source, width, height, ghost, "HealthSilhouette")
        };
        frames.Add(source, cached);
        return cached;
    }

    static Sprite MakeSprite(Sprite source, int width, int height, Color32[] pixels, string suffix)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) {
            name = source.name + "_" + suffix,
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = source.texture.filterMode,
            wrapMode = source.texture.wrapMode
        };
        texture.SetPixels32(pixels);
        texture.Apply();
        Vector2 pivot = new Vector2(source.pivot.x / width, source.pivot.y / height);
        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), pivot,
            source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    void OnDestroy()
    {
        foreach (var cached in frames.Values)
        {
            Release(cached.twoSections);
            Release(cached.oneSection);
            Release(cached.silhouette);
        }
        frames.Clear();
    }

    static void Release(Sprite sprite)
    {
        if (sprite == null) return;
        Texture2D texture = sprite.texture;
        if (Application.isPlaying)
        {
            Destroy(sprite);
            Destroy(texture);
        }
        else
        {
            DestroyImmediate(sprite);
            DestroyImmediate(texture);
        }
    }
}
