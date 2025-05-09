using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[InitializeOnLoad]
public static class PlayModeController
{
    static bool wasPressedLastFrame = false;

    static PlayModeController()
    {
        EditorApplication.update += Update;
    }

    static void Update()
    {
        var pad = Gamepad.current;
        if (pad == null) return;

        bool isPressed = pad.startButton.isPressed;

        if (isPressed && !wasPressedLastFrame)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("Exiting Play Mode via controller.");
                EditorApplication.isPlaying = false;
            }
            else
            {
                Debug.Log("Entering Play Mode via controller.");
                EditorApplication.isPlaying = true;
            }
        }

        wasPressedLastFrame = isPressed;
    }
}
