using UnityEngine;

/// Vertically scrolling background layer. factor 0 = fixed in the world, 1 = locked to the camera.
public class ParallaxLayer : MonoBehaviour
{
    Camera cam;
    float factor, tileW, tileH;

    public static ParallaxLayer Create(Transform parent, Sprite sprite, float factor, int order, Color tint)
    {
        var go = new GameObject("Parallax_" + order);
        go.transform.SetParent(parent);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = ArtLibrary.SpriteMat;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(sprite.bounds.size.x * 7f, sprite.bounds.size.y * 7f);
        sr.sortingOrder = order;
        sr.color = tint;

        var pl = go.AddComponent<ParallaxLayer>();
        pl.cam = Camera.main;
        pl.factor = factor;
        pl.tileW = sprite.bounds.size.x;
        pl.tileH = sprite.bounds.size.y;
        return pl;
    }

    void LateUpdate()
    {
        Vector3 c = cam.transform.position;
        float x = c.x - Mathf.Repeat(c.x * factor, tileW);
        float y = c.y - Mathf.Repeat(c.y * factor, tileH);
        transform.position = new Vector3(x, y, 5f);
    }
}
