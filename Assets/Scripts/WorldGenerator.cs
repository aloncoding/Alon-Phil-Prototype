using System.Collections.Generic;
using UnityEngine;

/// Endless tower generator. All numbers live in GameConfig.world.
/// Normal rows: Single, Pair, FloaterStep (a floater to bounce off just below a platform).
/// Enemy-only stretches: several rows of nothing but floaters (no platforms), optionally ending in a landing platform.
/// Difficulty (0..1) rises with height: bigger vertical gaps (need hook/bounce), narrower platforms, more crawlers.
public class WorldGenerator : MonoBehaviour
{
    static GameConfig.WorldSettings W => GameConfig.I.world;
    static GameConfig.EnemySettings E => GameConfig.I.enemies;

    Transform player, root;
    LavaRise lava;
    Camera cam;
    float startY, cursorY, lastX;
    int stretchLeft, cooldownRows, skipsInARow;
    bool landNext;
    System.Random rng;
    readonly List<KeyValuePair<GameObject, float>> items = new List<KeyValuePair<GameObject, float>>();

    float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
    bool Chance(float p) => rng.NextDouble() < p;

    public void Begin(Transform playerT, float startY, LavaRise lavaRise)
    {
        player = playerT;
        lava = lavaRise;
        this.startY = startY;
        cam = Camera.main;
        rng = new System.Random(System.Environment.TickCount);
        root = new GameObject("World").transform;
        root.SetParent(transform);

        var floor = Platform.Create(root, 0f, Shaft.Width, 0f, W.platformThickness);
        Reg(floor.gameObject, 0f);
        cursorY = 0f;
        lastX = 0f;
        stretchLeft = 0;
        cooldownRows = 0;
        landNext = false;
        skipsInARow = 0;
    }

    void Update()
    {
        if (player == null) return;
        float h = Mathf.Max(0f, player.position.y - startY);
        float d = Mathf.Clamp01(h / Mathf.Max(1f, W.difficultyRampHeight));

        float top = cam.transform.position.y + cam.orthographicSize;
        while (cursorY < top + W.generateAhead) Row(d);

        // delete only what is far below the lava (it can't be reached any more), so the camera can follow you down
        float bottom = lava.Y - W.cleanupBelow;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].Key == null) { items.RemoveAt(i); continue; }
            if (items[i].Value < bottom) { Destroy(items[i].Key); items.RemoveAt(i); }
        }
    }

    // ---------------- helpers ----------------
    void Reg(GameObject go, float y) => items.Add(new KeyValuePair<GameObject, float>(go, y));

    Platform Place(float cx, float y, float w)
    {
        var p = Platform.Create(root, cx, w, y, W.platformThickness);
        Reg(p.gameObject, y);
        return p;
    }

    Enemy Spawn(EnemyType t, float x, float y, float left, float right)
    {
        float lim = Shaft.HalfWidth - 1f;
        x = Mathf.Clamp(x, -lim, lim);
        var e = Enemy.Create(t, new Vector2(x, y), left, right, root);
        Reg(e.gameObject, y);
        return e;
    }

    float Width(float d) =>
        Mathf.Max(W.minPlatformWidth, Mathf.Lerp(W.platformWidthStart, W.platformWidthEnd, d) * R(W.widthJitterMin, W.widthJitterMax));

    float PlaceX(float w)
    {
        float lim = Shaft.HalfWidth - w * 0.5f - 0.8f;
        return Mathf.Clamp(lastX + R(-W.horizontalShift, W.horizontalShift), -lim, lim);
    }

    void CrawlerOn(float x, float y, float w, float d)
    {
        if (w < W.crawlerMinPlatformWidth) return;
        if (!Chance(Mathf.Lerp(W.crawlerChanceStart, W.crawlerChanceEnd, d))) return;
        float half = Mathf.Max(0.2f, w * 0.5f - E.crawlerEdgeMargin);
        Spawn(EnemyType.Crawler, x, y, x - half, x + half);
    }

    void FloaterAbove(float x, float y, float d)
    {
        if (Chance(Mathf.Lerp(W.floaterAboveChanceStart, W.floaterAboveChanceEnd, d)))
            Spawn(EnemyType.Floater, x + R(-2f, 2f), y + R(2.5f, 3.5f), 0f, 0f);
    }

    // ---------------- rows ----------------
    void Row(float d)
    {
        if (stretchLeft > 0)
        {
            StretchRow();
            stretchLeft--;
            if (stretchLeft == 0 && W.stretchEndsWithPlatform) landNext = true;
            return;
        }
        if (landNext)
        {
            landNext = false;
            Landing(d);
            return;
        }

        cooldownRows--;
        if (W.stretchEnabled && cursorY >= W.stretchMinHeight && cooldownRows <= 0 && Chance(W.stretchChancePerRow))
        {
            int minRows = Mathf.Max(1, W.stretchMinRows);
            stretchLeft = rng.Next(minRows, Mathf.Max(minRows, W.stretchMaxRows) + 1);
            cooldownRows = W.stretchCooldownRows;
            StretchRow();
            stretchLeft--;
            if (stretchLeft == 0 && W.stretchEndsWithPlatform) landNext = true;
            return;
        }

        NormalRow(d);
    }

    void NormalRow(float d)
    {
        float gap = Mathf.Lerp(W.rowGapMin, W.rowGapMax, d) * R(0.85f, 1.1f);
        float y = cursorY + gap;

        // sparse mode: sometimes leave the row empty (maybe just a floater), which doubles the climb to the next platform
        if (cursorY >= W.safeStartHeight && skipsInARow < W.maxConsecutiveSkips && Chance(W.platformSkipChance))
        {
            skipsInARow++;
            if (Chance(W.skippedRowFloaterChance))
            {
                float lim = Shaft.HalfWidth - 1.5f;
                float fx = Mathf.Clamp(lastX + R(-W.horizontalShift * 0.6f, W.horizontalShift * 0.6f), -lim, lim);
                Spawn(EnemyType.Floater, fx, y + R(-0.4f, 0.4f), 0f, 0f);
                lastX = fx;
            }
            cursorY = y;
            return;
        }
        skipsInARow = 0;

        float w = Width(d);

        float total = Mathf.Max(0.0001f, W.rowSingleWeight + W.rowPairWeight + W.rowFloaterStepWeight);
        float roll = (float)rng.NextDouble() * total;
        if (cursorY < W.safeStartHeight) roll = 0f; // gentle start: plain platforms only

        if (roll < W.rowSingleWeight) Single(y, w, d);
        else if (roll < W.rowSingleWeight + W.rowPairWeight) Pair(y, w, d);
        else FloaterStep(y, w, d, gap);

        cursorY = y;
    }

    void Single(float y, float w, float d)
    {
        float x = PlaceX(w);
        Place(x, y, w);
        CrawlerOn(x, y, w, d);
        FloaterAbove(x, y, d);
        lastX = x;
    }

    void Pair(float y, float w, float d)
    {
        float w2 = Mathf.Max(W.minPlatformWidth, w * W.pairWidthFactor);
        float lim = Shaft.HalfWidth - w2 * 0.5f - 0.8f;
        float xa = Mathf.Clamp(-R(4f, 8f), -lim, lim);
        float xb = Mathf.Clamp(R(4f, 8f), -lim, lim);
        Place(xa, y, w2);
        Place(xb, y, w2);
        if (Chance(0.5f)) CrawlerOn(xa, y, w2, d); else CrawlerOn(xb, y, w2, d);
        lastX = Chance(0.5f) ? xa : xb;
    }

    void FloaterStep(float y, float w, float d, float gap)
    {
        float x = PlaceX(w);
        Place(x, y, w);
        float fx = (lastX + x) * 0.5f + R(-1.5f, 1.5f);
        Spawn(EnemyType.Floater, fx, y - gap * 0.5f + R(-0.4f, 0.4f), 0f, 0f);
        CrawlerOn(x, y, w, d);
        lastX = x;
    }

    // ---------------- enemy-only stretches ----------------
    void StretchRow()
    {
        float gap = R(W.stretchGapMin, W.stretchGapMax);
        float y = cursorY + gap;
        int minN = Mathf.Max(1, W.stretchFloatersMin);
        int n = rng.Next(minN, Mathf.Max(minN, W.stretchFloatersMax) + 1);

        float lim = Shaft.HalfWidth - 1.5f;
        float x = Mathf.Clamp(lastX + R(-W.stretchHorizontalRange, W.stretchHorizontalRange), -lim, lim);
        float chosen = x;
        for (int i = 0; i < n; i++)
        {
            if (i > 0)
            {
                float step = R(5f, 9f) * (Chance(0.5f) ? 1f : -1f);
                float nx = x + step;
                if (Mathf.Abs(nx) > lim) nx = x - step;
                x = Mathf.Clamp(nx, -lim, lim);
            }
            Spawn(EnemyType.Floater, x, y + R(-0.3f, 0.3f), 0f, 0f);
            if (i == 0 || Chance(0.5f)) chosen = x;
        }
        lastX = chosen;
        cursorY = y;
    }

    void Landing(float d)
    {
        float y = cursorY + R(W.stretchGapMin, W.stretchGapMax);
        float w = Width(d);
        float x = PlaceX(w);
        Place(x, y, w);
        CrawlerOn(x, y, w, d);
        lastX = x;
        cursorY = y;
    }
}