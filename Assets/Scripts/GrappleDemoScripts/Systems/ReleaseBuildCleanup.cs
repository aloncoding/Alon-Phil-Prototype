using UnityEngine;

/// Add to the first scene. Keeps debug output out of release builds (zero text/overlay rule).
/// Reminder: also avoid OnGUI, remove Gizmos-dependent visuals, and uncheck "Development Build".
public class ReleaseBuildCleanup : MonoBehaviour
{
    [SerializeField] bool hideSystemCursor = false; // if true, you need another way to show aim direction
    [SerializeField] bool confineCursor = true;

    void Awake()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        Debug.unityLogger.logEnabled = false;
        Debug.developerConsoleVisible = false;
#endif
        Cursor.visible = !hideSystemCursor;
        if (confineCursor) Cursor.lockState = CursorLockMode.Confined;
        QualitySettings.vSyncCount = 1;
    }
}
