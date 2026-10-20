using UnityEngine;

public static class Shaft
{
    public static float Width => GameConfig.I.world.shaftWidth;
    public static float HalfWidth => Width * 0.5f;
}

/// Two solid stone walls that follow the camera. They snap to whole units so the brick pattern looks fixed in the world.
public class ShaftWalls : MonoBehaviour
{
    Transform cam, left, right;

    public static ShaftWalls Create(Camera camera)
    {
        var go = new GameObject("ShaftWalls");
        var w = go.AddComponent<ShaftWalls>();
        w.cam = camera.transform;
        float height = Mathf.Ceil(Mathf.Max(40f, GameConfig.I.cam.orthoSize * 4f) / 2f) * 2f;
        w.left = MakeWall(go.transform, -(Shaft.HalfWidth + 3f), height);
        w.right = MakeWall(go.transform, Shaft.HalfWidth + 3f, height);
        return w;
    }

    static Transform MakeWall(Transform parent, float x, float height)
    {
        var go = new GameObject("Wall");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, 0f, 0f);

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(6f, height);
        box.sharedMaterial = GameManager.NoFriction;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ArtLibrary.Stone;
        sr.sharedMaterial = ArtLibrary.SpriteMat;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(6f, height);
        sr.color = GameConfig.I.visuals.wallTint;
        sr.sortingOrder = 2;
        return go.transform;
    }

    void LateUpdate()
    {
        float y = Mathf.Round(cam.position.y);
        left.position = new Vector3(left.position.x, y, 0f);
        right.position = new Vector3(right.position.x, y, 0f);
    }
}
