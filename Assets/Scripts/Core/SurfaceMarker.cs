using UnityEngine;

public enum SurfaceType { Stone, Moss, Dirt, Wood, Metal, Water }

/// Put on ground colliders so footsteps/landings can pick the right sound.
public class SurfaceMarker : MonoBehaviour
{
    public SurfaceType surface = SurfaceType.Stone;
}
