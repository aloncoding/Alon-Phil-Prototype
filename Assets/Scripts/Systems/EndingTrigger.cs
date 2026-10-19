using UnityEngine;

/// Place at the scenic vista. Starts the slow reveal, then the fade-out.
[RequireComponent(typeof(Collider2D))]
public class EndingTrigger : MonoBehaviour
{
    [SerializeField] Transform vistaFocus;
    [SerializeField] float focusSize = 8f;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (LevelSequenceController.Instance != null && other.GetComponentInParent<PlayerController>() != null)
            LevelSequenceController.Instance.BeginEnding(vistaFocus != null ? vistaFocus : transform, focusSize);
    }
}
