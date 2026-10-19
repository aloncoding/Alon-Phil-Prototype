using UnityEngine;

/// Trigger zone that frames something important (a relic, a vista) before the player reaches it.
[RequireComponent(typeof(Collider2D))]
public class CameraFocusZone : MonoBehaviour
{
    [SerializeField] Transform focusPoint;
    [SerializeField] float orthoSize = 7f;
    [SerializeField, Range(0f, 1f)] float weight = 0.5f;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (CameraController.Instance != null && other.GetComponentInParent<PlayerController>() != null)
            CameraController.Instance.SetFocus(focusPoint != null ? focusPoint : transform, orthoSize, weight);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (CameraController.Instance != null && other.GetComponentInParent<PlayerController>() != null)
            CameraController.Instance.ClearFocus();
    }
}
