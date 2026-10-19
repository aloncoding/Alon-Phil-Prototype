using UnityEngine;

/// Background/foreground depth. factor 0 = stays in the world, 1 = locked to the camera.
/// Background layers: 0.1-0.8. Foreground framing layers: negative values (e.g. -0.2).
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] Vector2 factor = new Vector2(0.5f, 0.2f);
    [SerializeField] Transform cam;

    Vector3 startPos, camStart;

    void Start()
    {
        if (cam == null) cam = Camera.main.transform;
        startPos = transform.position;
        camStart = cam.position;
    }

    void LateUpdate()
    {
        Vector3 d = cam.position - camStart;
        transform.position = new Vector3(startPos.x + d.x * factor.x, startPos.y + d.y * factor.y, startPos.z);
    }
}
