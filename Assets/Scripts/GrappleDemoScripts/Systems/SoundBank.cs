using UnityEngine;

/// A set of interchangeable clips with randomised pitch/volume so sounds never feel repetitive.
/// Create via Assets > Create > Audio > Sound Bank.
[CreateAssetMenu(menuName = "Audio/Sound Bank")]
public class SoundBank : ScriptableObject
{
    public AudioClip[] clips;
    public Vector2 volumeRange = new Vector2(0.85f, 1f);
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [Range(0f, 1f)] public float spatialBlend = 0.6f;
    public float minInterval = 0.03f;

    int lastIndex = -1;
    float lastTime = -100f;

    void OnEnable() { lastIndex = -1; lastTime = -100f; }

    public bool TryGet(out AudioClip clip, out float volume, out float pitch)
    {
        clip = null; volume = 1f; pitch = 1f;
        if (clips == null || clips.Length == 0) return false;
        if (Time.realtimeSinceStartup - lastTime < minInterval) return false;
        lastTime = Time.realtimeSinceStartup;

        int i = Random.Range(0, clips.Length);
        if (clips.Length > 1 && i == lastIndex) i = (i + 1) % clips.Length; // avoid immediate repeats
        lastIndex = i;

        clip = clips[i];
        volume = Random.Range(volumeRange.x, volumeRange.y);
        pitch = Random.Range(pitchRange.x, pitchRange.y);
        return clip != null;
    }
}
