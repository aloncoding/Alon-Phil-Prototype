using UnityEngine;
using Unity.Cinemachine;

/// Drives a Cinemachine 3 camera via a "tracking target" empty GameObject.
/// Setup: CinemachineCamera (Follow = cameraTarget), add CinemachineImpulseListener to it,
/// CinemachineImpulseSource on this object, optional CinemachineConfiner2D for level bounds.
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] PlayerController player;
    [SerializeField] Transform cameraTarget;
    [SerializeField] CinemachineCamera vcam;
    [SerializeField] CinemachineImpulseSource impulse;

    [Header("Follow")]
    [SerializeField] float lookAheadDistance = 2.2f;
    [SerializeField] float lookAheadSpeed = 2.5f;
    [SerializeField] float verticalOffset = 0.8f;
    [SerializeField] float fallLookDown = 1.5f;
    [SerializeField] float fallLookThreshold = -8f;
    [SerializeField] float followSmoothTime = 0.12f;

    [Header("Zoom / Focus")]
    [SerializeField] float defaultOrthoSize = 6f;
    [SerializeField] float zoomSpeed = 1.5f;
    [SerializeField] float focusBlendSpeed = 0.5f;

    Transform focus;
    Vector3 focusPos, velocity;
    float focusWeight, focusTargetWeight, focusMaxWeight = 0.6f, focusSize, lookAhead;

    void Awake()
    {
        Instance = this;
        focusSize = defaultOrthoSize;
    }

    void LateUpdate()
    {
        if (player == null) return;
        float dt = Time.unscaledDeltaTime;

        float moving = Mathf.Abs(player.Velocity.x) > 0.5f ? 1f : 0.4f;
        lookAhead = Mathf.Lerp(lookAhead, player.Facing * lookAheadDistance * moving, 1f - Mathf.Exp(-lookAheadSpeed * dt));
        float down = player.Velocity.y < fallLookThreshold ? -fallLookDown : 0f;

        Vector3 desired = player.transform.position + new Vector3(lookAhead, verticalOffset + down, 0f);

        if (focus != null) focusPos = focus.position;
        focusWeight = Mathf.MoveTowards(focusWeight, focusTargetWeight, dt * focusBlendSpeed);
        float eased = Mathf.SmoothStep(0f, 1f, focusWeight) * focusMaxWeight / Mathf.Max(focusMaxWeight, 0.0001f);
        desired = Vector3.Lerp(desired, new Vector3(focusPos.x, focusPos.y, 0f), eased * focusMaxWeight);
        desired.z = 0f;

        cameraTarget.position = Vector3.SmoothDamp(cameraTarget.position, desired, ref velocity, followSmoothTime, Mathf.Infinity, dt);

        var lens = vcam.Lens;
        float targetSize = Mathf.Lerp(defaultOrthoSize, focusSize, eased);
        lens.OrthographicSize = Mathf.Lerp(lens.OrthographicSize, targetSize, 1f - Mathf.Exp(-zoomSpeed * dt));
        vcam.Lens = lens;
    }

    public void SetFocus(Transform target, float orthoSize, float weight = 0.6f)
    {
        focus = target;
        focusSize = orthoSize;
        focusMaxWeight = Mathf.Clamp01(weight);
        focusTargetWeight = 1f;
    }

    public void ClearFocus()
    {
        focus = null;
        focusTargetWeight = 0f;
    }

    public void Shake(float force)
    {
        if (impulse != null) impulse.GenerateImpulseWithForce(force);
    }

    public static void DoShake(float force)
    {
        if (Instance != null) Instance.Shake(force);
    }

    /// Call after teleporting the player so the camera doesn't glide across the level.
    public void Warp(Vector3 playerPosition)
    {
        Vector3 desired = playerPosition + new Vector3(0f, verticalOffset, 0f);
        Vector3 delta = desired - cameraTarget.position;
        cameraTarget.position = desired;
        velocity = Vector3.zero;
        vcam.OnTargetObjectWarped(cameraTarget, delta);
    }
}
