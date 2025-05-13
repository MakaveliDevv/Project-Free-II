// Assets/Editor/PlayPauseController.cs
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[InitializeOnLoad]
public static class PlayPauseController
{
    static bool startDown;
    static bool selectDown;

    static PlayPauseController()
    {
        // Hook into the Editor’s update loop
        EditorApplication.update += OnEditorUpdate;
    }

    static void OnEditorUpdate()
    {
        // Force the Input System to poll devices and clear its temp allocations
        InputSystem.Update();

        var pad = Gamepad.current;
        if (pad == null)
        {
            startDown = selectDown = false;
            return;
        }

        bool startPressed  = pad.startButton.isPressed;
        bool selectPressed = pad.selectButton.isPressed;

        // Toggle Play on rising edge of START
        if (startPressed && !startDown)
            EditorApplication.isPlaying = !EditorApplication.isPlaying;

        // Toggle Pause/Unpause on rising edge of SELECT
        if (selectPressed && !selectDown)
            EditorApplication.ExecuteMenuItem("Edit/Pause");

        startDown  = startPressed;
        selectDown = selectPressed;

        Debug.ClearDeveloperConsole();
    }
}
