using UnityEngine;
using UnityEngine.InputSystem;

public static class InputController
{
    private static InputActionAsset inputAsset;
    private static InputAction leftStick;
    private static InputAction sButton;

    public static Vector2 LeftStickValue { get; private set; }
    public static bool MoveTriggerPressed { get; private set; }
    public static bool MoveTriggerHeld { get; private set; }
    public static bool MoveTriggerReleased { get; private set; }

    private static bool initialized = false;

    public static void Initialize(InputActionAsset asset)
    {
        if (initialized) return;

        inputAsset = asset;
        var playerMap = inputAsset.FindActionMap("Player");

        leftStick = playerMap.FindAction("MovementTrigger");
        sButton = playerMap.FindAction("DirCalculation");

        Enable();
        
        sButton.started += ctx => { MoveTriggerPressed = true; MoveTriggerHeld = true; };
        sButton.canceled += ctx => { MoveTriggerReleased = true; MoveTriggerHeld = false; };

        initialized = true;
    }

    private static void Enable()
    {
        leftStick.Enable();
        sButton.Enable();
    }

    public static void Update()
    {
        if (!initialized) return;

        LeftStickValue = leftStick.ReadValue<Vector2>();
    }

    public static void LateUpdate()
    {
        // Reset button press flags for next frame
        MoveTriggerPressed = false;
        MoveTriggerReleased = false;
    }
}
