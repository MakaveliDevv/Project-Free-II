using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnalogStickDebugger : MonoBehaviour
{
    public float debugRadius = 2f;
    public Vector2[] debugDirections; // should match your jumpDirections
    public Color validColor = Color.green;
    public Color invalidColor = Color.red;

    [Range(0f, 1f)] public float stickVisualThreshold = 0.2f;
    public Vector2 currentInput;
    public bool showDebug = true;

    public InputActionAsset inputActions;
    private InputAction movementAction;


    void Start()
    {
        var map = inputActions.FindActionMap("Player");
        movementAction = map.FindAction("Movement");
        movementAction.Enable();

        Vector2[] jumpDirections = {
            new Vector2(-0.92f, 0.38f),  // WNW
            new Vector2(-0.71f, 0.71f),  // NW
            new Vector2(-0.38f, 0.92f),  // NNW
            new Vector2(0f, 1f),         // N
            new Vector2(0.38f, 0.92f),   // NNE
            new Vector2(0.71f, 0.71f),   // NE
            new Vector2(0.92f, 0.38f),   // ENE
        };

        debugDirections = jumpDirections;
    }

    void Update()
    {
        // Replace this with your actual input value
        // currentInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        currentInput = movementAction.ReadValue<Vector2>();

        if (showDebug && currentInput.magnitude > stickVisualThreshold)
        {
            Debug.DrawLine(transform.position, transform.position + (Vector3)(currentInput.normalized * debugRadius), Color.yellow, 0f, false);
        }

        if (showDebug && debugDirections != null)
        {
            foreach (var dir in debugDirections)
            {
                Debug.DrawLine(transform.position, transform.position + (Vector3)(dir.normalized * debugRadius), validColor, 0f, false);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebug || debugDirections == null) return;

        Gizmos.color = validColor;
        foreach (var dir in debugDirections)
        {
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(dir.normalized * debugRadius));
        }

        if (Application.isPlaying && currentInput.magnitude > stickVisualThreshold)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(currentInput.normalized * debugRadius));
        }
    }
}
