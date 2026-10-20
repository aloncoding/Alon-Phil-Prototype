using UnityEngine;

/// One-way platform: you jump up through it and land on top. Hooks can attach to it from any direction.
public class Platform : MonoBehaviour
{
    public BoxCollider2D Col { get; private set; }

    public static Platform Create(Transform parent, float centerX, float width, float topY, float thickness = 1f)
    {
        var go = new GameObject("Platform");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(centerX, topY - thickness * 0.5f, 0f);

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, thickness);
        box.sharedMaterial = GameManager.NoFriction;
        box.usedByEffector = true;

        var eff = go.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.surfaceArc = 170f;
        eff.useSideFriction = false;
        eff.useSideBounce = false;

        var body = new GameObject("Body");
        body.transform.SetParent(go.transform, false);
        var sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = ArtLibrary.Stone;
        sr.sharedMaterial = ArtLibrary.SpriteMat;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(width, thickness);
        sr.sortingOrder = 0;

        var cap = new GameObject("Cap");
        cap.transform.SetParent(go.transform, false);
        cap.transform.localPosition = new Vector3(0f, thickness * 0.5f - 0.1f, 0f);
        var cs = cap.AddComponent<SpriteRenderer>();
        cs.sprite = ArtLibrary.Grass;
        cs.sharedMaterial = ArtLibrary.SpriteMat;
        cs.drawMode = SpriteDrawMode.Tiled;
        cs.size = new Vector2(width, 0.25f);
        cs.sortingOrder = 1;

        var p = go.AddComponent<Platform>();
        p.Col = box;
        return p;
    }
}
