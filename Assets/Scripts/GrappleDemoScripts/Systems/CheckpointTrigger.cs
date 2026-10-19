using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] Transform respawnPoint;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (LevelSequenceController.Instance != null && other.GetComponentInParent<PlayerController>() != null)
            LevelSequenceController.Instance.SetCheckpoint(respawnPoint != null ? respawnPoint : transform);
    }
}
