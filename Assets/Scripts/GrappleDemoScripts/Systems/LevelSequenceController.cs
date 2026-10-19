using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// Demo flow: hook relic acquisition, checkpoints/respawn, and the scenic ending (fade, no text).
public class LevelSequenceController : MonoBehaviour
{
    public static LevelSequenceController Instance { get; private set; }

    [Header("References")]
    [SerializeField] PlayerController player;
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] GrapplingHookController hook;
    [SerializeField] Light2D globalLight;
    [SerializeField] SpriteRenderer fadeRenderer; // black full-screen sprite parented to the camera (sorting order 1000)
    [SerializeField] Transform startPoint;

    [Header("Hook acquisition")]
    [SerializeField] float acquirePreDelay = 0.4f;
    [SerializeField] float acquireFocusSize = 4.5f;
    [SerializeField] float acquireDuration = 3f;
    [SerializeField] Color acquireLightColor = new Color(0.6f, 0.85f, 1f);
    [SerializeField] float acquireLightBoost = 0.25f;
    [SerializeField] ParticleSystem acquireBurst;
    [SerializeField] SoundBank acquireSound;

    [Header("Ending")]
    [SerializeField] float endFocusSize = 8f;
    [SerializeField] float endHoldTime = 6f;
    [SerializeField] float endFadeTime = 3f;
    [SerializeField] bool reloadSceneAfterEnding = true;

    public event Action AcquisitionStarted, HookAcquired;
    public bool HookHasBeenAcquired { get; private set; }

    Transform checkpoint;
    bool ending, respawning;

    void Awake()
    {
        Instance = this;
        SetFade(1f);
    }

    void Start()
    {
        if (startPoint != null) checkpoint = startPoint;
        StartCoroutine(Fade(0f, 2f)); // gentle fade-in
    }

    public void SetCheckpoint(Transform t) => checkpoint = t;

    // ---------------- Hook acquisition ----------------
    public void BeginHookAcquisition(HookRelic relic)
    {
        if (HookHasBeenAcquired) return;
        HookHasBeenAcquired = true;
        StartCoroutine(AcquireRoutine(relic));
    }

    IEnumerator AcquireRoutine(HookRelic relic)
    {
        player.SetControlLocked(true);
        yield return new WaitForSeconds(acquirePreDelay);

        AcquisitionStarted?.Invoke();
        if (CameraController.Instance != null) CameraController.Instance.SetFocus(relic.transform, acquireFocusSize, 0.85f);
        relic.PlayCollected();
        AudioController.Sfx(acquireSound, relic.transform.position);
        VFXController.Spawn(acquireBurst, relic.transform.position);

        Color baseColor = globalLight != null ? globalLight.color : Color.white;
        float baseIntensity = globalLight != null ? globalLight.intensity : 1f;

        float shiftTime = acquireDuration * 0.4f;
        yield return LightShift(baseColor, baseIntensity, acquireLightColor, baseIntensity + acquireLightBoost, shiftTime);

        hook.Unlock();           // character visibly changes via PlayerAnimationController
        HookAcquired?.Invoke();
        yield return new WaitForSeconds(acquireDuration * 0.2f);

        yield return LightShift(acquireLightColor, baseIntensity + acquireLightBoost, baseColor, baseIntensity, acquireDuration * 0.4f);
        if (CameraController.Instance != null) CameraController.Instance.ClearFocus();
        player.SetControlLocked(false);
    }

    IEnumerator LightShift(Color fromC, float fromI, Color toC, float toI, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / time);
            if (globalLight != null)
            {
                globalLight.color = Color.Lerp(fromC, toC, k);
                globalLight.intensity = Mathf.Lerp(fromI, toI, k);
            }
            yield return null;
        }
    }

    // ---------------- Respawn ----------------
    public void Respawn()
    {
        if (respawning) return;
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        respawning = true;
        player.SetControlLocked(true);
        yield return new WaitForSeconds(0.5f);
        yield return Fade(1f, 0.4f);

        hook.ForceCancel();
        Vector2 pos = checkpoint != null ? (Vector2)checkpoint.position : (Vector2)player.transform.position;
        player.Teleport(pos);
        if (CameraController.Instance != null) CameraController.Instance.Warp(pos);
        playerHealth.ResetHealth();
        yield return new WaitForSeconds(0.2f);

        player.SetControlLocked(false);
        respawning = false;
        yield return Fade(0f, 0.6f);
    }

    // ---------------- Ending ----------------
    public void BeginEnding(Transform vista, float focusSizeOverride = -1f)
    {
        if (ending) return;
        ending = true;
        StartCoroutine(EndRoutine(vista, focusSizeOverride > 0f ? focusSizeOverride : endFocusSize));
    }

    IEnumerator EndRoutine(Transform vista, float size)
    {
        if (CameraController.Instance != null) CameraController.Instance.SetFocus(vista, size, 0.7f);
        yield return new WaitForSeconds(endHoldTime); // player may roam and soak it in

        player.SetControlLocked(true);
        if (AudioController.Instance != null) AudioController.Instance.FadeMaster(0f, endFadeTime);
        yield return Fade(1f, endFadeTime);
        yield return new WaitForSecondsRealtime(1.5f);

        if (reloadSceneAfterEnding) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        else Application.Quit();
    }

    // ---------------- Fade ----------------
    void SetFade(float a)
    {
        if (fadeRenderer == null) return;
        var c = fadeRenderer.color; c.a = a; fadeRenderer.color = c;
    }

    IEnumerator Fade(float target, float time)
    {
        if (fadeRenderer == null) yield break;
        float start = fadeRenderer.color.a, t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            SetFade(Mathf.Lerp(start, target, t / time));
            yield return null;
        }
        SetFade(target);
    }
}
