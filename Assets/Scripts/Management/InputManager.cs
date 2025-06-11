using UnityEngine;
using UnityEngine.InputSystem;

public static class InputManager
{
    private static InputAction leftStick, rightStick;
    private static InputAction buttonSouth, buttonNorth;
    private static InputAction leftShoulder, rightShoulder;
    public static InputAction leftTrigger, rightTrigger;

    public static Vector2 LeftStickInput { get; private set; }
    public static Vector2 RightStickInput { get; private set; }

    // Test purposes
    private static InputAction dPadUp;

    public static bool SouthButtonPressed { get; private set; }
    public static bool SouthButtonReleased { get; private set; }

    public static bool NorthButtonPressed { get; private set; }
    public static bool NorthButtonReleased { get; private set; }

    public static bool LeftShoulderPressed { get; set; }
    public static bool LeftShoulderReleased { get; private set; }

    public static bool LeftShoulderDoublePressed { get; private set; }

    public static bool RightShoulderPressed { get; set; }
    public static bool RightShoulderReleased { get; private set; }

    public static bool RightShoulderDoublePressed { get; private set; }

    public static bool LeftTriggerPressed { get; private set; }
    public static bool LeftTriggerReleased { get; private set; }

    public static bool UseAutoHover { get; private set; } = false;
    public static int LeftShoulderPressCount { get; private set; }

    public static bool TriggerLock { get; set; }
    private static bool useRawInput;
    private static float minStickMagnitude;


    private static readonly float shoulderDoublePressThreshold = 0.3f;


    private static float lastLeftShoulderPressTime = -Mathf.Infinity;
    private static float lastRightShoulderPressTime = -Mathf.Infinity;


    public static void Initialize(InputActionAsset inputAsset, bool rawInput, float stickThreshold)
    {
        useRawInput = rawInput;
        minStickMagnitude = stickThreshold;

        var map = inputAsset.FindActionMap("Player");
        var map2 = inputAsset.FindActionMap("ToggleMechanics");

        leftStick = map.FindAction("DirCalculation");
        rightStick = map.FindAction("SwipeCalculation");
        buttonSouth = map.FindAction("MovementTrigger");
        buttonNorth = map.FindAction("PullTrigger");

        leftShoulder = map.FindAction("AdvancedMovementMode");
        rightShoulder = map.FindAction("CombatMode");

        leftTrigger = map.FindAction("Launch");
        dPadUp = map2.FindAction("ToggleAutoHover");

        Enable();

        // South button:
        buttonSouth.started += ctx => SouthButtonPressed = true;
        buttonSouth.canceled += ctx =>
        {
            SouthButtonPressed = false;
            SouthButtonReleased = true;
        };

        buttonNorth.started += ctx => NorthButtonPressed = true;
        buttonNorth.canceled += ctx =>
        {
            NorthButtonPressed = false;
            NorthButtonReleased = true;
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

        leftTrigger.started += ctx => LeftTriggerPressed = true;
        leftTrigger.canceled += ctx =>
        {
            LeftTriggerPressed = false;
            LeftTriggerReleased = true;
        };

        // Dpad Up
        dPadUp.started += ctx =>
        {
            UseAutoHover = !UseAutoHover;
            Debug.Log("Toggle autoHover");
        };

        dPadUp.started -= ctx =>
        {
            UseAutoHover = !UseAutoHover;
            Debug.Log("Toggle autoHover");
        };
    }

    private static void Enable()
    {
        leftStick?.Enable();
        rightStick?.Enable();

        buttonSouth?.Enable();
        buttonNorth?.Enable();

        leftShoulder?.Enable();
        rightShoulder?.Enable();

        leftTrigger?.Enable();

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
        NorthButtonReleased = false;

        LeftShoulderReleased = false;
        RightShoulderReleased = false;

        LeftTriggerReleased = false;

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

    public static Vector3 GetSnappedDirection(Vector2 input, bool snapDirectionsEnabled, float directionCount)
    {
        if (input.sqrMagnitude < minStickMagnitude) { return Vector3.zero; }

        float rawAngle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        if (snapDirectionsEnabled)
        {
            float angleStep = 360f / directionCount;
            rawAngle = Mathf.Round(rawAngle / angleStep) * angleStep;
        }

        return Quaternion.Euler(0f, 0f, rawAngle) * Vector3.right;
    }

    public static float buttonHoldTimer;
    public static float minButtonPressTime;
    // public static bool ActionInputDetected()
    // {
    //     if (HasLeftStickMovement() && SouthButtonPressed && buttonHoldTimer >= minButtonPressTime) { return true; }

    //     buttonHoldTimer = 0;

    //     Debug.Log("Action Input Detected");
    //     return false;
    // }

    public static bool ActionInputDetected()
    {
        return HasLeftStickMovement()
                        && SouthButtonPressed
                        && buttonHoldTimer >= minButtonPressTime;
                        
        // if (detected)
        // {
        //     Debug.Log("Action input detected");
        //     return true;
        // }
        // else
        // {
        //     Debug.Log("No action input detected");
        //     return false;
        // }
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
