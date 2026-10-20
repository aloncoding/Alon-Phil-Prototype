using UnityEngine;

/// Vertical camera. Follows the player up AND down (with a dead zone so small hops don't bob it).
/// Set cam.canDescend = false for a camera that only moves up. It has nothing to do with the lava
/// (the lava rises on its own, see LavaRise). All numbers: GameConfig.cam.
public class CameraFollow : MonoBehaviour
{
    Camera cam;
    PlayerClimber player;
    float camY, shake;

    static GameConfig.CameraSettings C => GameConfig.I.cam;

    public float Bottom => camY - cam.orthographicSize;

    void Awake() => cam = GetComponent<Camera>();

    public void Begin(PlayerClimber p)
    {
        player = p;
        shake = 0f;
        camY = p.transform.position.y + cam.orthographicSize * C.playerScreenHeight;
        Apply(Vector2.zero);
    }

    public void Shake(float amount) => shake = Mathf.Max(shake, amount);

    void LateUpdate()
    {
        if (player == null) return;
        float dt = Time.deltaTime;

        float follow = player.transform.position.y + cam.orthographicSize * C.playerScreenHeight;
        float dz = Mathf.Max(0f, C.verticalDeadZone);
        float goal = Mathf.Clamp(camY, follow - dz, follow + dz); // stay put while the player is inside the dead zone
        float rate = goal < camY ? C.followSmoothDown : C.followSmooth;
        float next = Mathf.Lerp(camY, goal, 1f - Mathf.Exp(-rate * dt));
        camY = C.canDescend ? next : Mathf.Max(camY, next);

        shake = Mathf.MoveTowards(shake, 0f, dt * 1.5f);
        Apply(Random.insideUnitCircle * shake);
    }

    void Apply(Vector2 offset) => transform.position = new Vector3(offset.x, camY + offset.y, -10f);
}