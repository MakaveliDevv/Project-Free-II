using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    private bool wasPressedLastFrame = false;  // Track if the button was pressed last frame

    private void Update()
    {
        if (Gamepad.current == null) return;  // Ensure Gamepad is connected

        bool isPressed = Gamepad.current.selectButton.isPressed;

        // Check if the button is pressed but wasn't pressed last frame (rising edge)
        if (isPressed && !wasPressedLastFrame && EditorApplication.isPlaying)
        {
            // Toggle the Play Mode pause state
            EditorApplication.isPaused = !EditorApplication.isPaused;
            Debug.Log("Play Mode " + (EditorApplication.isPaused ? "Paused" : "Unpaused"));
        }

        // Update the previous button state for next frame
        wasPressedLastFrame = isPressed;
    }
}
