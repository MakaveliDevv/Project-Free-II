// using UnityEditor;
// using UnityEngine;
// using UnityEngine.InputSystem;

// [InitializeOnLoad]
// public static class TestInputEditMode
// {
//     static TestInputEditMode()
//     {
//         EditorApplication.update += Update;
//     }

//     static void Update()
//     {
//         if (Gamepad.current != null)
//         {
//             Debug.Log("Gamepad detected in Edit Mode.");
//             if (Gamepad.current.startButton.isPressed)
//             {
//                 EditorApplication.isPlaying = true;
//                 Debug.Log("Start button is being pressed.");
//             }
//         }
//     }
// }
