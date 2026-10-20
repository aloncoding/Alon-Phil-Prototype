using UnityEngine;
using UnityEngine.InputSystem;

/// All input is built in code (needs the Input System package; the 2D templates include it).
/// A/D or arrows: move. Space/W/Up: jump. Left click: fire hook toward the mouse.
public static class GameInput
{
    static InputAction axis, jump, hook, aim;

    public static void Init()
    {
        Dispose();

        axis = new InputAction("Axis", InputActionType.Value);
        axis.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
        axis.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");

        jump = new InputAction("Jump", InputActionType.Button);
        jump.AddBinding("<Keyboard>/space");
        jump.AddBinding("<Keyboard>/w");
        jump.AddBinding("<Keyboard>/upArrow");

        hook = new InputAction("Hook", InputActionType.Button, "<Mouse>/leftButton");
        aim = new InputAction("Aim", InputActionType.Value, "<Mouse>/position");

        axis.Enable(); jump.Enable(); hook.Enable(); aim.Enable();
    }

    static void Dispose()
    {
        axis?.Dispose(); jump?.Dispose(); hook?.Dispose(); aim?.Dispose();
    }

    public static float Axis => axis.ReadValue<float>();
    public static bool JumpDown => jump.WasPressedThisFrame();
    public static bool JumpUp => jump.WasReleasedThisFrame();
    public static bool JumpHeld => jump.IsPressed();
    public static bool HookDown => hook.WasPressedThisFrame();

    public static Vector2 AimWorld(Camera cam)
    {
        Vector2 sp = aim.ReadValue<Vector2>();
        Vector3 w = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, Mathf.Abs(cam.transform.position.z)));
        return w;
    }
}
