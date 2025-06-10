using UnityEngine;
using UnityEngine.InputSystem;

public static class InputManager
{
    private static InputAction leftStick, rightStick;
    private static InputAction southButton;
    private static InputAction leftShoulder, rightShoulder;

    public static Vector2 LeftStickInput { get; private set; }
    public static Vector2 RightStickInput { get; private set; }

    // Test purposes
    private static InputAction dPadUp;

    public static bool SouthButtonPressed { get; private set; }
    public static bool SouthButtonReleased { get; private set; }

    public static bool LeftShoulderPressed { get; set; }
    public static bool LeftShoulderReleased { get; private set; }

    public static bool LeftShoulderDoublePressed { get; private set; }

    public static bool RightShoulderPressed { get; set; }
    public static bool RightShoulderReleased { get; private set; }

    public static bool RightShoulderDoublePressed { get; private set; }

    public static int leftShoulderPressCount;

    private static bool useRawInput;
    private static float minStickMagnitude;


    private static readonly float shoulderDoublePressThreshold = 0.3f;


    private static float lastLeftShoulderPressTime = -Mathf.Infinity;
    private static float lastRightShoulderPressTime = -Mathf.Infinity;

    public static bool useAutoHover = false;

    public static void Initialize(InputActionAsset inputAsset, bool rawInput, float stickThreshold)
    {
        useRawInput = rawInput;
        minStickMagnitude = stickThreshold;

        var map = inputAsset.FindActionMap("Player");
        var map2 = inputAsset.FindActionMap("ToggleMechanics");

        leftStick = map.FindAction("DirCalculation");
        rightStick = map.FindAction("SwipeCalculation");
        southButton = map.FindAction("MovementTrigger");
        leftShoulder = map.FindAction("AdvancedMovementMode");
        rightShoulder = map.FindAction("CombatMode");

        dPadUp = map2.FindAction("ToggleAutoHover");

        Enable();

        // South button:
        southButton.started += ctx => SouthButtonPressed = true;
        southButton.canceled += ctx =>
        {
            SouthButtonPressed = false;
            SouthButtonReleased = true;
        };

        // Left shoulder
        leftShoulder.started += ctx =>
        {
            LeftShoulderPressed = true;
            if (CheckDoublePress(ref lastLeftShoulderPressTime, shoulderDoublePressThreshold))
                LeftShoulderDoublePressed = true;
        };
        leftShoulder.canceled += ctx =>
        {
            LeftShoulderPressed = false;
            LeftShoulderReleased = true;
        };

        // Right shoulder
        rightShoulder.started += ctx =>
        {
            RightShoulderPressed = true;
            if (CheckDoublePress(ref lastRightShoulderPressTime, shoulderDoublePressThreshold))
                RightShoulderDoublePressed = true;
        };
        rightShoulder.canceled += ctx =>
        {
            RightShoulderPressed = false;
            RightShoulderReleased = true;
        };

        // Dpad Up
        dPadUp.started += ctx =>
        {
            useAutoHover = !useAutoHover;
            Debug.Log("Toggle autoHover");
        }; 

        dPadUp.started -= ctx =>
        {
            useAutoHover = !useAutoHover;
            Debug.Log("Toggle autoHover");
        };
    }

    private static void Enable()
    {
        leftStick?.Enable();
        southButton?.Enable();
        rightStick?.Enable();
        rightShoulder?.Enable();
        leftShoulder?.Enable();
        dPadUp?.Enable();
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

    public static bool HasLeftStickMovement() => LeftStickInput.magnitude > minStickMagnitude;
    public static bool HasRightStickMovement() => RightStickInput.magnitude > minStickMagnitude;
    public static void ResetFrameInputs()
    {
        SouthButtonReleased = false;
        LeftShoulderReleased = false;
        RightShoulderReleased = false;

        LeftShoulderDoublePressed = false;
        RightShoulderDoublePressed = false;
    }

    private static bool CheckDoublePress(ref float lastPressTime, float thresholdSecs)
    {
        float now = Time.time;
        if (now - lastPressTime <= thresholdSecs)
        {
            // Two taps occurred within the threshold → double-press!
            lastPressTime = -Mathf.Infinity;
            return true;
        }
        else
        {
            // Not close enough; record this tap’s timestamp and wait for the next one
            lastPressTime = now;
            return false;
        }
    }
    
    // private static void ToggleAutoHover(InputAction.CallbackContext ctx)
    // {
    //     if (ctx.started)
    //     {
    //         Debug.Log("Toggle Auto Hover");
    //         useAutoHover = !useAutoHover;
    //     }
    // }
}
