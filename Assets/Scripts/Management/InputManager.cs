using UnityEngine;
using UnityEngine.InputSystem;

public static class InputManager
{
    private static InputAction leftStick;
    private static InputAction southButton;
    private static InputAction rightStick;

    public static Vector2 LeftStickInput { get; private set; }
    public static Vector2 RightStickInput { get; private set; }
    public static bool SouthButtonPressed { get; private set; }
    public static bool SouthButtonReleased { get; private set; }

    private static bool useRawInput;
    private static float minStickMagnitude;

    public static void Initialize(InputActionAsset inputAsset, bool rawInput, float stickThreshold)
    {
        useRawInput = rawInput;
        minStickMagnitude = stickThreshold;

        var playerMap = inputAsset.FindActionMap("Player");
        leftStick = playerMap.FindAction("DirCalculation");
        southButton = playerMap.FindAction("MovementTrigger");
        rightStick = playerMap.FindAction("SwipeCalculation");

        leftStick?.Enable();
        southButton?.Enable();
        rightStick?.Enable();

        southButton.started += ctx => SouthButtonPressed = true;
        southButton.canceled += ctx => {
            SouthButtonPressed = false;
            SouthButtonReleased = true;
        };

        Debug.Log("Input initialization complete");
    }

    public static void UpdateInput()
    {
        if (useRawInput && Gamepad.current != null)
        {
            LeftStickInput = Gamepad.current.leftStick.ReadUnprocessedValue();
            RightStickInput = Gamepad.current.rightStick.ReadUnprocessedValue();
        }
        else
        {
            LeftStickInput = leftStick != null ? leftStick.ReadValue<Vector2>() : Vector2.zero;
            RightStickInput = rightStick != null ? rightStick.ReadValue<Vector2>() : Vector2.zero;
        }
    }

    public static bool HasStickMovement() => LeftStickInput.magnitude > minStickMagnitude;

    public static void ResetFrameInputs()
    {
        SouthButtonReleased = false;
    }
}
