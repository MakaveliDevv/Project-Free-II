// Assets/Scripts/ExitPlayMode.cs
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExitPlayMode : MonoBehaviour
{
    void Update()
    {
#if UNITY_EDITOR
        if(EditorApplication.isPlaying) 
        {
            bool isPressed = Gamepad.current.startButton.isPressed;
            if (isPressed) 
            {
                EditorApplication.isPlaying = false;
            }
        }
#endif
    }
}
