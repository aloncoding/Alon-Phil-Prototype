using UnityEngine;
using static PixelCanvas;

/// Every sprite in the game is drawn in code here. Edit colours/shapes freely.
public static class ArtLibrary
{
    static Material mat;
    public static Material SpriteMat
    {
        get
        {
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
            return mat;
        }
    }

    static readonly Color32 Ink = C(24, 18, 40);

    // ---------- Player ----------
    static Sprite[] playerRun;
    static Sprite playerJump, playerFall, playerHook;
    public static Sprite[] PlayerRun => playerRun ??= new[] { MakePlayer(0, 0f, false), MakePlayer(1, 1.6f, false) };
    public static Sprite PlayerJump => playerJump != null ? playerJump : playerJump = MakePlayer(2, 0.8f, false);
    public static Sprite PlayerFall => playerFall != null ? playerFall : playerFall = MakePlayer(3, 2.4f, false);
    public static Sprite PlayerHook => playerHook != null ? playerHook : playerHook = MakePlayer(2, 0.4f, true);

    static Sprite MakePlayer(int legMode, float scarfPhase, bool armUp)
    {
        var c = new PixelCanvas(16, 24);
        Color32 pants = C(52, 63, 120), boots = C(120, 76, 52), tunic = C(46, 170, 160), tunicD = C(30, 120, 120);
        Color32 belt = C(240, 200, 80), skin = C(255, 214, 170), cap = C(70, 58, 110), white = C(255, 255, 255);
        Color32 scarfA = C(235, 70, 70), scarfB = C(170, 40, 60);

        for (int i = 0; i < 6; i++) // scarf behind the neck
        {
            int y = 14 + Mathf.RoundToInt(Mathf.Sin(i * 1.1f + scarfPhase) * 1.4f);
            c.Rect(5 - i, y, 1, 2, i % 2 == 0 ? scarfA : scarfB);
        }

        switch (legMode)
        {
            case 0: // run A
                c.Rect(5, 2, 3, 7, pants); c.Rect(5, 2, 3, 2, boots);
                c.Rect(9, 5, 3, 4, pants); c.Rect(9, 5, 4, 2, boots); break;
            case 1: // run B
                c.Rect(9, 2, 3, 7, pants); c.Rect(9, 2, 3, 2, boots);
                c.Rect(5, 5, 3, 4, pants); c.Rect(4, 5, 4, 2, boots); break;
            case 2: // jump (tucked)
                c.Rect(5, 5, 3, 4, pants); c.Rect(5, 5, 3, 2, boots);
                c.Rect(9, 5, 3, 4, pants); c.Rect(9, 5, 4, 2, boots); break;
            default: // fall (spread)
                c.Rect(4, 3, 3, 6, pants); c.Rect(4, 3, 3, 2, boots);
                c.Rect(10, 3, 3, 6, pants); c.Rect(10, 3, 3, 2, boots); break;
        }

        c.Rect(4, 9, 8, 6, tunic);
        c.Rect(4, 9, 8, 1, tunicD);
        c.Rect(4, 11, 8, 1, belt);
        c.Rect(2, 9, 2, 5, tunicD); // back arm
        if (armUp) { c.Rect(12, 14, 2, 6, tunic); c.Rect(12, 20, 2, 2, skin); }
        else { c.Rect(12, 9, 2, 5, tunic); c.Rect(12, 9, 2, 1, skin); }

        c.Ellipse(8, 18.5f, 5f, 5f, skin);
        for (int y = 19; y < 24; y++)
            for (int x = 0; x < 16; x++)
                if (c.Get(x, y).r == skin.r && c.Get(x, y).g == skin.g && c.Get(x, y).a != 0) c.Set(x, y, cap);
        c.Rect(10, 18, 2, 2, white);
        c.Set(11, 18, Ink);
        c.Set(11, 19, Ink);

        c.Outline(Ink);
        return c.ToSprite(16, new Vector2(0.5f, 0.5f));
    }

    // ---------- Enemies ----------
    static Sprite[] crawler, floater;
    public static Sprite[] Crawler => crawler ??= new[] { MakeCrawler(0), MakeCrawler(1) };
    public static Sprite[] Floater => floater ??= new[] { MakeFloater(0), MakeFloater(1) };

    static Sprite MakeCrawler(int frame)
    {
        var c = new PixelCanvas(16, 12);
        Color32 body = C(150, 80, 200), light = C(195, 135, 240), dark = C(70, 30, 100), white = C(255, 255, 255);
        c.Ellipse(8, 6, 7.5f, 5.5f, body);
        c.Rect(0, 0, 16, 2, C(0, 0, 0, 0));
        c.Ellipse(6, 9, 3f, 1.5f, light);
        c.Rect(4, 5, 3, 3, white); c.Rect(9, 5, 3, 3, white);
        c.Rect(5, 5, 1, 2, dark); c.Rect(10, 5, 1, 2, dark);
        c.Line(3, 10, 7, 8, dark); c.Line(12, 10, 8, 8, dark);
        if (frame == 0) { c.Rect(3, 0, 3, 2, dark); c.Rect(10, 1, 3, 1, dark); }
        else { c.Rect(3, 1, 3, 1, dark); c.Rect(10, 0, 3, 2, dark); }
        c.Outline(Ink);
        return c.ToSprite(16, new Vector2(0.5f, 0f));
    }

    static Sprite MakeFloater(int frame)
    {
        var c = new PixelCanvas(16, 16);
        Color32 body = C(90, 200, 230), belly = C(160, 235, 250), wing = C(235, 248, 255), dark = C(20, 60, 90), white = C(255, 255, 255);
        if (frame == 0) { c.Ellipse(2.5f, 12, 2.5f, 4f, wing); c.Ellipse(13.5f, 12, 2.5f, 4f, wing); }
        else { c.Ellipse(2.5f, 6, 2.5f, 3.5f, wing); c.Ellipse(13.5f, 6, 2.5f, 3.5f, wing); }
        c.Ellipse(8, 8, 5.5f, 5.5f, body);
        c.Ellipse(8, 6, 3.5f, 2.5f, belly);
        c.Ellipse(8, 9, 3f, 3f, white);
        c.Rect(8, 8, 2, 2, dark);
        c.Outline(Ink);
        return c.ToSprite(16, new Vector2(0.5f, 0.5f));
    }

    // ---------- Hook, particles ----------
    static Sprite hookHead, dot;
    public static Sprite HookHead
    {
        get
        {
            if (hookHead != null) return hookHead;
            var c = new PixelCanvas(8, 8);
            Color32 metal = C(220, 225, 240), dark = C(120, 125, 150);
            c.Rect(0, 3, 3, 2, dark);
            for (int x = 3; x < 8; x++)
            {
                int half = Mathf.Max(1, Mathf.RoundToInt(3.2f - (x - 3) * 0.7f));
                c.Rect(x, 4 - half, 1, half * 2, metal);
            }
            c.Outline(Ink);
            return hookHead = c.ToSprite(16, new Vector2(0.5f, 0.5f));
        }
    }

    public static Sprite Dot
    {
        get
        {
            if (dot != null) return dot;
            var c = new PixelCanvas(4, 4);
            c.Ellipse(2, 2, 2, 2, C(255, 255, 255));
            return dot = c.ToSprite(16, new Vector2(0.5f, 0.5f));
        }
    }

    // ---------- Terrain ----------
    static Sprite stone, grass;
    public static Sprite Stone
    {
        get
        {
            if (stone != null) return stone;
            var c = new PixelCanvas(16, 16);
            Color32 baseC = C(78, 84, 112), mortar = C(52, 56, 82), hi = C(98, 106, 136), lo = C(64, 70, 96);
            c.Rect(0, 0, 16, 16, baseC);
            c.Rect(0, 0, 16, 1, mortar); c.Rect(0, 8, 16, 1, mortar);
            c.Rect(0, 1, 1, 7, mortar); c.Rect(8, 9, 1, 7, mortar);
            var rng = new System.Random(3);
            for (int i = 0; i < 26; i++)
            {
                int x = rng.Next(16), y = rng.Next(16);
                if (c.Get(x, y).r == mortar.r) continue;
                c.Set(x, y, rng.Next(2) == 0 ? hi : lo);
            }
            return stone = c.ToSprite(16, new Vector2(0.5f, 0.5f));
        }
    }

    public static Sprite Grass
    {
        get
        {
            if (grass != null) return grass;
            var c = new PixelCanvas(16, 4);
            c.Rect(0, 3, 16, 1, C(140, 220, 100));
            c.Rect(0, 2, 16, 1, C(88, 170, 72));
            c.Rect(0, 1, 16, 1, C(66, 126, 66));
            c.Rect(0, 0, 16, 1, C(54, 90, 70));
            var rng = new System.Random(9);
            for (int i = 0; i < 6; i++) c.Set(rng.Next(16), 3, C(190, 245, 130));
            return grass = c.ToSprite(16, new Vector2(0.5f, 0.5f));
        }
    }

    // ---------- Background ----------
    static Sprite sky, cloudField, stars, lavaTop, lavaBody;

    public static Sprite Sky
    {
        get
        {
            if (sky != null) return sky;
            var c = new PixelCanvas(2, 128);
            Color top = new Color(0.12f, 0.08f, 0.30f), mid = new Color(0.55f, 0.25f, 0.55f), bot = new Color(1f, 0.62f, 0.45f);
            for (int y = 0; y < 128; y++)
            {
                float k = y / 127f; // 0 bottom, 1 top
                Color col = k < 0.5f ? Color.Lerp(bot, mid, k * 2f) : Color.Lerp(mid, top, (k - 0.5f) * 2f);
                for (int x = 0; x < 2; x++) c.Set(x, y, col);
            }
            return sky = c.ToSprite(1, new Vector2(0.5f, 0.5f), true);
        }
    }

    /// 8x8 world-unit tile of clouds (clouds stay inside the tile so it tiles seamlessly).
    public static Sprite CloudField
    {
        get
        {
            if (cloudField != null) return cloudField;
            var c = new PixelCanvas(128, 128);
            Color32 col = C(255, 230, 240, 140);
            float[,] pos = { { 30, 28 }, { 92, 58 }, { 38, 98 }, { 96, 112 } };
            for (int i = 0; i < 4; i++)
            {
                float cx = pos[i, 0], cy = pos[i, 1];
                c.Ellipse(cx, cy, 14, 5, col);
                c.Ellipse(cx - 9, cy - 1, 8, 4, col);
                c.Ellipse(cx + 10, cy - 1, 9, 4, col);
                c.Ellipse(cx + 1, cy + 3, 8, 4, col);
            }
            return cloudField = c.ToSprite(16, new Vector2(0.5f, 0.5f));
        }
    }

    public static Sprite Stars
    {
        get
        {
            if (stars != null) return stars;
            var c = new PixelCanvas(128, 128);
            var rng = new System.Random(77);
            for (int i = 0; i < 45; i++)
            {
                int x = rng.Next(1, 127), y = rng.Next(1, 127);
                int a = rng.Next(120, 255);
                c.Set(x, y, C(255, 255, 255, a));
                if (rng.Next(4) == 0) { c.Set(x + 1, y, C(255, 255, 255, a / 2)); c.Set(x, y + 1, C(255, 255, 255, a / 2)); }
            }
            return stars = c.ToSprite(16, new Vector2(0.5f, 0.5f));
        }
    }

    // ---------- Lava ----------
    public static Sprite LavaTop
    {
        get
        {
            if (lavaTop != null) return lavaTop;
            var c = new PixelCanvas(16, 4);
            c.Rect(0, 3, 16, 1, C(255, 240, 150));
            c.Rect(0, 2, 16, 1, C(255, 190, 70));
            c.Rect(0, 1, 16, 1, C(250, 120, 40));
            c.Rect(0, 0, 16, 1, C(220, 70, 40));
            for (int x = 0; x < 16; x += 3) c.Set(x, 3, C(255, 255, 210));
            return lavaTop = c.ToSprite(16, new Vector2(0.5f, 0.5f));
        }
    }

    public static Sprite LavaBody
    {
        get
        {
            if (lavaBody != null) return lavaBody;
            var c = new PixelCanvas(16, 16);
            c.Rect(0, 0, 16, 16, C(210, 60, 40));
            var rng = new System.Random(5);
            for (int i = 0; i < 24; i++) c.Set(rng.Next(16), rng.Next(16), rng.Next(2) == 0 ? C(245, 110, 50) : C(160, 35, 40));
            return lavaBody = c.ToSprite(16, new Vector2(0.5f, 0.5f));
        }
    }
}
