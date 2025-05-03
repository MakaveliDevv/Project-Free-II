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

//     using UnityEngine;
// using UnityEngine.InputSystem;
// using System.Collections.Generic;

// [RequireComponent(typeof(Rigidbody))]
// public class AnalogStickReader : MonoBehaviour
// {
//     public InputAction moveAction;
//     public bool useRawInput = true;
//     private Vector2 currentInput = Vector2.zero;

//     [Header("Gizmo Settings")]
//     public float gizmoScale = 2f;
//     public Color gizmoColor = Color.green;
//     public float directionLineLength = 1.5f;

//     [Header("Jump Settings")]
//     public float jumpForce = 5f;
//     public float horizontalForceMultiplier = 3f;
//     public float maxJumpDistance = 5f;
//     public float jumpSpeed = 10f;

//     [Header("Return Settings")]
//     public bool returnToStartAfterJump = false;
//     public float returnSpeed = 5f;

//     [Header("Snapped Input Settings")]
//     public bool snapDirectionsEnabled = false;
//     public int directionCount = 16;

//     [Header("Label Settings")]
//     public bool showDirectionLabels = true;
//     public bool useCardinalLabels = true;

//     private Rigidbody rb;
//     private Vector3 startPosition;
//     private Vector3 jumpTarget;
//     private bool isJumping = false;
//     private bool isReturning = false;
//     private List<Vector3> landingPoints = new List<Vector3>();

//     void Awake()
//     {
//         rb = GetComponent<Rigidbody>();
//         rb.isKinematic = true;
//         startPosition = transform.position;
//     }

//     void OnEnable()
//     {
//         moveAction.Enable();
//     }

//     void OnDisable()
//     {
//         moveAction.Disable();
//     }

//     void Update()
//     {
//         if (useRawInput && Gamepad.current != null)
//         {
//             currentInput = Gamepad.current.leftStick.ReadUnprocessedValue();
//         }
//         else
//         {
//             currentInput = moveAction.ReadValue<Vector2>();
//         }

//         // Handle jump input
//         if (!isJumping && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
//         {
//             JumpInInputDirection();
//         }

//         // Handle jump movement
//         if (isJumping)
//         {
//             transform.position = Vector3.MoveTowards(transform.position, jumpTarget, jumpSpeed * Time.deltaTime);
//             if (Vector3.Distance(transform.position, jumpTarget) < 0.01f)
//             {
//                 isJumping = false;
//                 landingPoints.Add(jumpTarget);

//                 if (returnToStartAfterJump)
//                 {
//                     isReturning = true;
//                     rb.isKinematic = true;
//                 }
//                 else
//                 {
//                     rb.isKinematic = false;
//                 }
//             }
//         }

//         // Handle return to start
//         if (isReturning)
//         {
//             transform.position = Vector3.MoveTowards(transform.position, startPosition, returnSpeed * Time.deltaTime);
//             if (Vector3.Distance(transform.position, startPosition) < 0.01f)
//             {
//                 isReturning = false;
//                 rb.isKinematic = false;
//             }
//         }
//     }

//     void LateUpdate()
//     {
//         // Optional: force Z = 0 if you're staying in 2D
//         Vector3 pos = transform.position;
//         pos.z = 0;
//         transform.position = pos;
//     }

//     public void JumpInInputDirection()
//     {
//         Vector3 snappedDir = GetSnappedDirection();
//         if (snappedDir.sqrMagnitude < 0.01f)
//             return;

//         jumpTarget = transform.position + snappedDir.normalized * maxJumpDistance + Vector3.up * jumpForce;
//         jumpTarget = transform.position + snappedDir.normalized * maxJumpDistance * jumpForce;

//         rb.isKinematic = true;
//         isJumping = true;
//     }

//     private Vector3 GetSnappedDirection()
//     {
//         if (currentInput.sqrMagnitude < 0.01f)
//             return Vector3.zero;

//         float rawAngle = Mathf.Atan2(currentInput.y, currentInput.x) * Mathf.Rad2Deg;

//         if (snapDirectionsEnabled)
//         {
//             float angleStep = 360f / directionCount;
//             rawAngle = Mathf.Round(rawAngle / angleStep) * angleStep;
//         }

//         // ✅ Rotate around Z axis for X-Y plane movement
//         Quaternion rotation = Quaternion.Euler(0f, 0f, rawAngle);
//         return rotation * Vector3.right;
//     }

//     private string GetDirectionLabel(int index)
//     {
//         if (!useCardinalLabels)
//             return (index + 1).ToString();

//         // Standard 16-point compass
//         string[] labels = new string[]
//         {
//             "E", "ENE", "NE", "NNE",
//             "N", "NNW", "NW", "WNW",
//             "W", "WSW", "SW", "SSW",
//             "S", "SSE", "SE", "ESE"
//         };

//         return labels[index % labels.Length];
//     }

//     void OnDrawGizmos()
//     {
//         Gizmos.color = Color.gray;
//         Gizmos.DrawWireSphere(transform.position, gizmoScale);

//         // 🔵 Draw all direction segments (in editor and play mode)
//         if (snapDirectionsEnabled)
//         {
//             Gizmos.color = Color.blue;
//             float angleStep = 360f / directionCount;

//             for (int i = 0; i < directionCount; i++)
//             {
//                 float angle = i * angleStep;
//                 float angleRad = angle * Mathf.Deg2Rad;
//                 Vector3 dir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);

//                 Vector3 endPoint = transform.position + dir * directionLineLength;
//                 Gizmos.DrawLine(transform.position, endPoint);

//                 // 🏷️ Draw label
//                 if (showDirectionLabels)
//                 {
//         #if UNITY_EDITOR
//                     UnityEditor.Handles.color = Color.white;
//                     UnityEditor.Handles.Label(endPoint + Vector3.up * 0.1f, GetDirectionLabel(i));
//         #endif
//                 }
//             }
//         }

//         // 🟢 Show current snapped input (only in Play mode)
//         if (Application.isPlaying && currentInput.sqrMagnitude > 0.01f)
//         {
//             Vector3 snappedDir = GetSnappedDirection();
//             Gizmos.color = gizmoColor;
//             Gizmos.DrawLine(transform.position, transform.position + snappedDir.normalized * directionLineLength);
//         }

//         // 🔴 Draw jump target
//         if (isJumping)
//         {
//             Gizmos.color = Color.red;
//             Gizmos.DrawWireSphere(jumpTarget, 0.25f);
//         }

//         // ✅ Draw all previous landings
//         Gizmos.color = Color.green;
//         foreach (var point in landingPoints)
//         {
//             Gizmos.DrawWireSphere(point, 0.25f);
//         }
//     }
// }

}
// private void ReturnToStartPos() 
//     {
//          if (returnToStartAfterJump)
//         {
//             isReturning = true;
//             rb.isKinematic = true;
//         }
//         else
//         {
//             rb.isKinematic = false;
//         }

//         // Handle jump movement
//         if (isMoving)
//         {
//             transform.position = Vector3.MoveTowards(transform.position, moveTarget, jumpSpeed * Time.deltaTime);
//             if (Vector3.Distance(transform.position, moveTarget) < 0.01f)
//             {
//                 // Don't "snap" too early
//                 if ((transform.position - moveTarget).sqrMagnitude < 0.0001f) 
//                 {
//                     isMoving = false;
//                     transform.position = moveTarget; // ensure final precision

//                     if (returnToStartAfterJump)
//                     {
//                         isReturning = true;
//                         rb.isKinematic = true;
//                     }
//                     else
//                     {
//                         rb.isKinematic = false;
//                     }
//                 }
//             }
//         }

//         // Handle return to start
//         if (isReturning)
//         {
//             transform.position = Vector3.MoveTowards(transform.position, startPosition, returnSpeed * Time.deltaTime);
//             if (Vector3.Distance(transform.position, startPosition) < 0.01f)
//             {
//                 isReturning = false;
//                 rb.isKinematic = false;
//             }
//         }
//     }
    

    
    // private (float minAngle, float maxAngle) GetAllowedJumpRange()
    // {
    //     if (isInAir || !allowedMoveLabels.ContainsKey(currentSurfaceState))
    //         return (0f, 0f);

    //     string[] labels = allowedMoveLabels[currentSurfaceState];

    //     float min = 360f;
    //     float max = 0f;

    //     foreach (var label in labels)
    //     {
    //         if (labelToAngle.TryGetValue(label, out float angle))
    //         {
    //             min = Mathf.Min(min, angle);
    //             max = Mathf.Max(max, angle);
    //         }
    //     }

    //     if (max - min > 180f)
    //     {
    //         float temp = min;
    //         min = max;
    //         max = temp + 360f;
    //     }

    //     return (min, max);
    // }
