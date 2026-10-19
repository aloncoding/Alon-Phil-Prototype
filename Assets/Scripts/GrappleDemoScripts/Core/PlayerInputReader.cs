using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// Builds all input actions in code (no .inputactions asset needed).
/// A/D move, Space jump, LMB hook (tap/hold), RMB pull, mouse position aims.
public class PlayerInputReader : MonoBehaviour
{
    public static PlayerInputReader Instance { get; private set; }

    public event Action JumpPressed, JumpReleased, HookPressed, HookReleased, PullPressed;

    public float Move { get; private set; }
    public bool InputEnabled { get; private set; } = true;

    InputAction move, jump, hook, pull, aim;
    Camera cam;

    void Awake()
    {
        Instance = this;

        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");

        jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        hook = new InputAction("Hook", InputActionType.Button, "<Mouse>/leftButton");
        pull = new InputAction("Pull", InputActionType.Button, "<Mouse>/rightButton");
        aim = new InputAction("Aim", InputActionType.Value, "<Mouse>/position");

        jump.performed += _ => { if (InputEnabled) JumpPressed?.Invoke(); };
        jump.canceled += _ => JumpReleased?.Invoke();
        hook.performed += _ => { if (InputEnabled) HookPressed?.Invoke(); };
        hook.canceled += _ => HookReleased?.Invoke();
        pull.performed += _ => { if (InputEnabled) PullPressed?.Invoke(); };
    }

    void OnEnable() { move.Enable(); jump.Enable(); hook.Enable(); pull.Enable(); aim.Enable(); }
    void OnDisable() { move.Disable(); jump.Disable(); hook.Disable(); pull.Disable(); aim.Disable(); }

    void Update() => Move = InputEnabled ? move.ReadValue<float>() : 0f;

    public void SetInputEnabled(bool enabled) => InputEnabled = enabled;

    public Vector2 AimWorldPoint()
    {
        if (cam == null) cam = Camera.main;
        Vector2 sp = aim.ReadValue<Vector2>();
        Vector3 w = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, Mathf.Abs(cam.transform.position.z)));
        return w;
    }
}
