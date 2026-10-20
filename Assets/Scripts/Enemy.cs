using UnityEngine;

public enum EnemyType { Crawler, Floater }

/// Crawler: walks back and forth on its platform. Floater: hovers and bobs in the air.
/// Land on the head (or arrive via hook) = kill + bounce. Any other contact hurts the player.
/// All numbers live in GameConfig.enemies.
public class Enemy : MonoBehaviour
{
    public EnemyType Type { get; private set; }
    public bool IsDead { get; private set; }
    public Vector2 HeadPoint => (Vector2)transform.position + Vector2.up * headOffset;

    static GameConfig.EnemySettings E => GameConfig.I.enemies;

    SpriteRenderer sr;
    Collider2D col;
    Sprite[] frames;
    float headOffset, leftLimit, rightLimit, dir = -1f, phase;
    Vector3 home;

    public static Enemy Create(EnemyType type, Vector2 pos, float leftLimit, float rightLimit, Transform parent)
    {
        var go = new GameObject(type.ToString());
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var e = go.AddComponent<Enemy>();
        e.Init(type, pos, leftLimit, rightLimit);
        return e;
    }

    void Init(EnemyType type, Vector2 pos, float left, float right)
    {
        Type = type;
        leftLimit = left;
        rightLimit = right;
        home = pos;
        phase = Random.value * 10f;
        dir = Random.value < 0.5f ? -1f : 1f;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = ArtLibrary.SpriteMat;
        sr.sortingOrder = 5;

        var box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        col = box;

        if (type == EnemyType.Crawler)
        {
            frames = ArtLibrary.Crawler;
            box.size = new Vector2(0.9f, 0.65f);
            box.offset = new Vector2(0f, 0.35f);
            headOffset = 0.75f;
        }
        else
        {
            frames = ArtLibrary.Floater;
            box.size = new Vector2(0.8f, 0.8f);
            headOffset = 0.5f;
        }
        sr.sprite = frames[0];
    }

    void Update()
    {
        if (IsDead) return;
        phase += Time.deltaTime;
        if (frames.Length > 1) sr.sprite = frames[(int)(phase * E.animFps) % frames.Length];

        if (Type == EnemyType.Crawler)
        {
            Vector3 p = transform.position;
            p.x += dir * E.crawlerSpeed * Time.deltaTime;
            if (p.x < leftLimit) { p.x = leftLimit; dir = 1f; }
            if (p.x > rightLimit) { p.x = rightLimit; dir = -1f; }
            transform.position = p;
        }
        else
        {
            transform.position = home + new Vector3(
                Mathf.Sin(phase * E.floaterBobXSpeed) * E.floaterBobX,
                Mathf.Sin(phase * E.floaterBobYSpeed) * E.floaterBobY, 0f);
        }
    }

    public void Kill()
    {
        if (IsDead) return;
        IsDead = true;
        col.enabled = false;
        Color c = Type == EnemyType.Crawler ? new Color(0.75f, 0.45f, 1f) : new Color(0.5f, 0.9f, 1f);
        FX.Burst(transform.position + Vector3.up * 0.4f, c, E.killBurstCount, 6f);
        SoundManager.Play(Sfx.Kill, 0.9f);
        transform.localScale = new Vector3(1.4f, 0.3f, 1f);
        Destroy(gameObject, 0.12f);
    }
}
