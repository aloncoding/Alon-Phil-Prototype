using UnityEngine;

/// Tiny software canvas used to draw all game art at runtime.
public class PixelCanvas
{
    public readonly int w, h;
    readonly Color32[] px;

    public PixelCanvas(int w, int h)
    {
        this.w = w; this.h = h;
        px = new Color32[w * h];
    }

    public static Color32 C(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);

    public void Set(int x, int y, Color32 c)
    {
        if (x < 0 || y < 0 || x >= w || y >= h) return;
        px[y * w + x] = c;
    }

    public Color32 Get(int x, int y)
    {
        if (x < 0 || y < 0 || x >= w || y >= h) return new Color32(0, 0, 0, 0);
        return px[y * w + x];
    }

    public void Rect(int x0, int y0, int rw, int rh, Color32 c)
    {
        for (int y = y0; y < y0 + rh; y++)
            for (int x = x0; x < x0 + rw; x++) Set(x, y, c);
    }

    public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
    {
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Set(x, y, c);
            }
    }

    public void Line(int x0, int y0, int x1, int y1, Color32 c)
    {
        int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            Set(x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    public void Outline(Color32 c)
    {
        var copy = (Color32[])px.Clone();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (copy[y * w + x].a != 0) continue;
                if (Opaque(copy, x - 1, y) || Opaque(copy, x + 1, y) || Opaque(copy, x, y - 1) || Opaque(copy, x, y + 1))
                    px[y * w + x] = c;
            }
    }

    bool Opaque(Color32[] a, int x, int y) => x >= 0 && y >= 0 && x < w && y < h && a[y * w + x].a != 0;

    public Sprite ToSprite(float ppu, Vector2 pivot, bool smooth = false)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point;
        tex.wrapMode = smooth ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
    }
}
