using UnityEngine;
using UnityEngine.InputSystem;

public static class InputManager
{
    private static InputAction leftStick, rightStick;
    private static InputAction southButton;
    private static InputAction leftShoulder, rightShoulder;

    public static Vector2 LeftStickInput { get; private set; }
    public static Vector2 RightStickInput { get; private set; }

    public static bool SouthButtonPressed { get; private set; }
    public static bool SouthButtonReleased { get; private set; }

    public static bool LeftShoulderPressed { get; set; }
    public static bool LeftShoulderReleased { get; private set; }
    /// <summary>
    /// This becomes true on the single frame where the left-shoulder button was pressed twice within the threshold.
    /// Clear it each frame using ResetFrameInputs().
    /// </summary>
    public static bool LeftShoulderDoublePressed { get; private set; }

    public static bool RightShoulderPressed { get; set; }
    public static bool RightShoulderReleased { get; private set; }
    /// <summary>
    /// This becomes true on the single frame where the right-shoulder button was pressed twice within the threshold.
    /// Clear it each frame using ResetFrameInputs().
    /// </summary>
    public static bool RightShoulderDoublePressed { get; private set; }

    public static int leftShoulderPressCount;

    private static bool useRawInput;
    private static float minStickMagnitude;

    // --- Double-press tracking: ---
    /// <summary>
    /// How many seconds apart the two presses can be and still count as a “double-press.”
    /// </summary>
    private static readonly float shoulderDoublePressThreshold = 0.3f;

    /// <summary>
    /// Time.time of the last left-shoulder *started* event.
    /// </summary>
    private static float lastLeftShoulderPressTime = -Mathf.Infinity;

    /// <summary>
    /// Time.time of the last right-shoulder *started* event.
    /// </summary>
    private static float lastRightShoulderPressTime = -Mathf.Infinity;
    // -------------------------------

    public static void Initialize(InputActionAsset inputAsset, bool rawInput, float stickThreshold)
    {
        useRawInput = rawInput;
        minStickMagnitude = stickThreshold;

        var map = inputAsset.FindActionMap("Player");
        leftStick = map.FindAction("DirCalculation");
        rightStick = map.FindAction("SwipeCalculation");
        southButton = map.FindAction("MovementTrigger");
        leftShoulder = map.FindAction("AdvancedMovementMode");
        rightShoulder = map.FindAction("CombatMode");

        Enable();

        // South button:
        southButton.started += ctx => SouthButtonPressed = true;
        southButton.canceled += ctx =>
        {
            SouthButtonPressed = false;
            SouthButtonReleased = true;
        };

        // Left shoulder: invoke generic CheckDoublePress for left-shoulder
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

        // Right shoulder: invoke generic CheckDoublePress for right-shoulder
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
    }

    private static void Enable()
    {
        leftStick?.Enable();
        southButton?.Enable();
        rightStick?.Enable();
        rightShoulder?.Enable();
        leftShoulder?.Enable();
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

    /// <summary>
    /// Call this once per frame (e.g. at the end of your Update) to clear
    /// any “released” flags and also clear any double-press flags so they only remain true
    /// on the single frame that the double‐press was detected.
    /// </summary>
    public static void ResetFrameInputs()
    {
        SouthButtonReleased = false;
        LeftShoulderReleased = false;
        RightShoulderReleased = false;

        LeftShoulderDoublePressed = false;
        RightShoulderDoublePressed = false;
    }

    // ================================================================
    // Generic double-press helper:
    // ================================================================
    /// <summary>
    /// If the current tap occurs within thresholdSecs of lastPressTime, this returns true
    /// (indicating a double-press), and resets lastPressTime. Otherwise, it updates
    /// lastPressTime to now and returns false.
    /// </summary>
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
    // ================================================================
}
