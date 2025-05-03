using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class AnalogStickReader : MonoBehaviour
{
    public enum SurfaceState { Ground, LeftWall, RightWall, Ceiling, Air }
    public SurfaceState currentSurfaceState = SurfaceState.Ground;

    public InputAction moveAction;
    public bool useRawInput = true;
    private Vector2 currentInput = Vector2.zero;
    private Vector2 snappedDir = Vector2.zero;

    [Header("Gizmo Settings")]
    public float gizmoScale = 2f;
    public float directionLineLength = 1.5f;

    [Header("Gizmo Colors")]
    public Color baseDirectionColor = Color.blue;    // 🔵 All snapped directions
    public Color allowedJumpColor = Color.green;     // 🟩 Allowed jump directions
    public Color dashDirectionColor = Color.cyan;    // 🟦 Dash-only directions
    public Color snappedInputColor = Color.yellow;   // 🟠 Current snapped input
    public Color jumpTargetColor = Color.red;        // 🔴 Jump target
    public Color landingPointColor = Color.green;    // ✅ Landed positions

    [Header("Movment Settings")]
    public float jumpForce = 5f;
    public float maxJumpDistance = 5f;

    [Header("Dash Settings")]
    public float dashForce = 5f;
    public float maxDashDistance = 5f;

    [Header("Snapped Input Settings")]
    public bool snapDirectionsEnabled = false;
    public int directionCount = 16;

    [Header("Label Settings")]
    public bool showDirectionLabels = true;
    public bool useCardinalLabels = true;

    private Vector3 lastJumpTarget;

    private Rigidbody rb;
    private Vector3 startPosition;
    private Vector3 moveTarget;
    private List<Vector3> landingPoints = new List<Vector3>();
    private bool isInAir = false;
    private bool stateChanged = false;
    private float stateTimer = 0f;
    public float stateBuffer = 0.25f;
    private float lastContactTime;
    private const float NO_CONTACT_THRESHOLD = 0.2f;
    private float holdRatio = 0;
    private float holdTime = 0;
    public float maxHoldTime = .5f;
    private readonly Dictionary<SurfaceState, string[]> allowedMoveLabels = new()
    {
        { SurfaceState.Ground, new[] { "W", "WNW", "NW", "NNW", "N", "NNE", "NE", "ENE", "E" } },
        { SurfaceState.Ceiling, new[] { "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W" } },
        { SurfaceState.LeftWall, new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S" } },
        { SurfaceState.RightWall, new[] { "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW", "N" } },
        { SurfaceState.Air, new[] { 
            "E", "ENE", "NE", "NNE", "N", "NNW", "NW", "WNW", 
            "W", "WSW", "SW", "SSW", "S", "SSE", "SE", "ESE" } } // all 16 directions
    };

    private Dictionary<string, float> labelToAngle;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = rb.position;

        BuildLabelToAngleMap();
    }

    void OnEnable()
    {
        moveAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
    }

    void Update()
    {
        if (useRawInput && Gamepad.current != null)
        {
            currentInput = Gamepad.current.leftStick.ReadUnprocessedValue();
        }
        else
        {
            currentInput = moveAction.ReadValue<Vector2>();
        }
        
        if(currentInput.magnitude > 0.1f) 
        {
            snappedDir = GetSnappedDirection().normalized;
            Debug.Log("Movement detected, snapping direction");
        }

        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            PerformMovementAction();
        }

        if (stateChanged)
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= stateBuffer)
            {
                stateChanged = false;
                stateTimer = 0f;
            }
        }

        if(isInAir) currentSurfaceState = SurfaceState.Air;
    }

    void FixedUpdate()
    {
        isInAir = !IsCollidingWithSurface();
        HandleActionForces();
        CheckArrivalAtTarget();
    }

    void LateUpdate()
    {
        // Optional: force Z = 0 if you're staying in 2D
        Vector3 pos = rb.position;
        pos.z = 0;
        rb.position = pos;
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleSurfaceState(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleSurfaceState(collision);
        
        if (currentSurfaceState == SurfaceState.Ground && rb.position.magnitude < 0.01f)
        {
            Debug.Log("Change movement state to idle");
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        lastContactTime = Time.time;
    }

    private bool hasReachedTarget = false;
    private void CheckArrivalAtTarget()
    {
        if (Vector3.Distance(rb.position, lastJumpTarget) < 0.25f)
        {
            hasReachedTarget = true;
            Debug.Log("🏁 Reached target point, returning to start...");
        }
    }


    private bool hasAppliedForce = false;
    public ForceMode mode;
    [Tooltip("Type of force applied during a jump.")]
    public ForceMode jumpForceMode = ForceMode.VelocityChange;

    [Tooltip("Type of force applied during a dash.")]
    public ForceMode dashForceMode = ForceMode.Impulse;
    private void HandleActionForces()
    {
        // if (!hasAppliedForce && snappedDir.sqrMagnitude > 0.01f)
        // {
        //     rb.linearVelocity  = Vector3.zero; // Optional: reset for cleaner force response

        //     Vector3 appliedForce = snappedDir * forceMagnitude;

        //     rb.AddForce(appliedForce, mode);

        //     hasAppliedForce = true;

        // }

        if (!hasAppliedForce && snappedDir.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity = Vector3.zero;
            Vector3 appliedForce = snappedDir.normalized * forceMagnitude;
            rb.AddForce(appliedForce, mode);
            hasAppliedForce = true;

            Debug.Log($"🔼 {currentAction} applied force: {appliedForce} (Mode: {mode})");
        }
    }

    private void PerformMovementAction( ) 
    {
        if(snappedDir == Vector2.zero) return;

        holdRatio = Mathf.Clamp01(holdTime / maxHoldTime);
        if(holdRatio < 0.01f) holdRatio = 0;

        string dirLabel = GetClosestDirectionLabel(snappedDir);
        bool isJumpAllowed = IsJumpDirectionAllowed(dirLabel);
        bool isDashAllowed = IsDashDirectionAllowed(dirLabel);

        if(isDashAllowed) 
        {
            SetupMovement(maxDashDistance, dashForce, "Dash");
        }  
        else if(isJumpAllowed) 
        {
            SetupMovement(maxJumpDistance, jumpForce, "Jump");
        }
    }

    private string currentAction = "";
    private float targetDistance = 0;
    private float forceMagnitude = 0;
    public float lerpAmount = 0.85f;

    private void SetupMovement(float maxTravelDistance, float force, string action) 
    {
        targetDistance = maxTravelDistance * holdRatio;
        forceMagnitude = force * targetDistance;
        currentAction = action;

        snappedDir = Vector3.Lerp(rb.linearVelocity.normalized, snappedDir.normalized, lerpAmount);

        // ✅ Set the actual target here using direction and distance
        // Old: lastJumpTarget = rb.position + (Vector3)snappedDir.normalized * targetDistance;
        float predictedDistance = force / rb.mass * Time.fixedDeltaTime;
        // lastJumpTarget = rb.position + (Vector3)snappedDir.normalized * Mathf.Min(predictedDistance * 60f, targetDistance);  
        lastJumpTarget = transform.position + maxJumpDistance * (Vector3)snappedDir.normalized;


        if(action == "Dash") 
        {
            mode = dashForceMode;
            Debug.Log("Dashing");
        }
        else if(action == "Jump") 
        {
            mode = jumpForceMode; 

            bool isDiagonalJump = Mathf.Abs(snappedDir.x) > 0 && Mathf.Abs(snappedDir.y) > 0;
            Debug.Log(isDiagonalJump ? "Jumping Diagonal" : "Jumping Straight");
        }

        hasAppliedForce = false;
        forceMagnitude = force;

        Debug.Log($"{action} ➤ Direction: {snappedDir}, Force: {forceMagnitude}");
        Debug.Log($"📍 Set jump target to: {lastJumpTarget}");
    }

    private bool IsDashDirectionAllowed(string label)
    {
        if (currentSurfaceState == SurfaceState.Ground || currentSurfaceState == SurfaceState.Ceiling)
            return label == "W" || label == "E";

        if (currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall)
            return label == "N" || label == "S";

        return false;
    }

    private bool IsJumpDirectionAllowed(string label)
    {
        if (label == "W" || label == "E")
            return false;

        if ((currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall) &&
            (label == "N" || label == "S"))
            return false;

        return allowedMoveLabels.TryGetValue(currentSurfaceState, out var allowed) &&
            System.Array.Exists(allowed, l => l == label);
    }

    private string GetClosestDirectionLabel(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        angle = (angle + 360f) % 360f;

        float minDiff = float.MaxValue;
        string closestLabel = "N";

        foreach (var pair in labelToAngle)
        {
            float diff = Mathf.Abs(Mathf.DeltaAngle(angle, pair.Value));
            if (diff < minDiff)
            {
                minDiff = diff;
                closestLabel = pair.Key;
            }
        }

        return closestLabel;
    }
    
    private Vector3 GetSnappedDirection()
    {
        if (currentInput.sqrMagnitude < 0.01f)
            return Vector3.zero;

        float rawAngle = Mathf.Atan2(currentInput.y, currentInput.x) * Mathf.Rad2Deg;

        if (snapDirectionsEnabled)
        {
            float angleStep = 360f / directionCount;
            rawAngle = Mathf.Round(rawAngle / angleStep) * angleStep;
        }

        // ✅ Rotate around Z axis for X-Y plane movement
        Quaternion rotation = Quaternion.Euler(0f, 0f, rawAngle);
        return rotation * Vector3.right;
    }

    private string GetDirectionLabel(int index)
    {
        if (!useCardinalLabels)
            return (index + 1).ToString();

        // Standard 16-point compass
        string[] labels = new string[]
        {
            "E", "ENE", "NE", "NNE",
            "N", "NNW", "NW", "WNW",
            "W", "WSW", "SW", "SSW",
            "S", "SSE", "SE", "ESE"
        };

        return labels[index % labels.Length];
    }
    
    private void BuildLabelToAngleMap()
    {
        labelToAngle = new Dictionary<string, float>();
        float angleStep = 360f / directionCount;

        for (int i = 0; i < directionCount; i++)
        {
            string label = GetDirectionLabel(i);
            float angle = (i * angleStep + 360f) % 360f;
            labelToAngle[label] = angle;
        }
    }
    
    private bool IsCollidingWithSurface()
    {
        // Using a small overlap sphere to check for collisions
        Collider[] colliders = Physics.OverlapSphere(
            rb.position, 
            GetComponent<Collider>().bounds.extents.y + 0.1f // Slightly larger than collider
        );
        
        // If we have any colliders other than ourselves, we're touching something
        return colliders.Length > 1;
    }

    private void HandleSurfaceState(Collision collision)
    {
        bool foundGround = false;
        bool foundCeiling = false;
        bool foundWallRight = false;
        bool foundWallLeft = false;
        
        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 normal = contact.normal;
            if (Vector3.Dot(normal, Vector3.up) > 0.7f)
            {
                foundGround = true;
            }
            else if (Vector3.Dot(normal, Vector3.down) > 0.7f)
            {
                foundCeiling = true;
            }
            else if (Vector3.Dot(normal, Vector3.right) > 0.7f)
            {
                foundWallLeft = true;
            }
            else if (Vector3.Dot(normal, Vector3.left) > 0.7f)
            {
                foundWallRight = true;
            }
        }
        
        SurfaceState previousState = currentSurfaceState;
        
        // Priority-based state determination with stickiness for ground and ceiling
        if (foundGround)
        {
            currentSurfaceState = SurfaceState.Ground;
        }
        else if (foundCeiling)
        {
            currentSurfaceState = SurfaceState.Ceiling;
        }
        else if ((foundWallLeft || foundWallRight) && currentSurfaceState == SurfaceState.Air)
        {
            // Only switch to wall state if we weren't on ground or ceiling
            if (foundWallLeft)
            {
                currentSurfaceState = SurfaceState.LeftWall;
            }
            else
            {
                currentSurfaceState = SurfaceState.RightWall;
            }
        }

        if (previousState != currentSurfaceState)
        {
            Debug.Log($"Surface State changed from {previousState} to {currentSurfaceState}");
            stateChanged = true;
        }
    }

    void OnDrawGizmos()
    {
        // 🔵 Direction segments
        if (snapDirectionsEnabled)
        {
            Gizmos.color = baseDirectionColor;
            float angleStep = 360f / directionCount;

            for (int i = 0; i < directionCount; i++)
            {
                float angle = i * angleStep;
                float angleRad = angle * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);

                Vector3 endPoint = rb.position + dir * directionLineLength;
                Gizmos.DrawLine(rb.position, endPoint);

                #if UNITY_EDITOR
                if (showDirectionLabels)
                {
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.Label(endPoint + Vector3.up * 0.1f, GetDirectionLabel(i));
                }
                #endif
            }
        }

        // 🔴/🟢 Jump target visualization depending on reach status
        if (Application.isPlaying && lastJumpTarget != Vector3.zero)
        {
            float distanceToTarget = Vector3.Distance(rb.position, lastJumpTarget);
            Gizmos.color = hasReachedTarget ? jumpTargetColor : landingPointColor;
            Gizmos.DrawSphere(lastJumpTarget, 0.25f);
        }

        // 🟩 Allowed move directions
        if (Application.isPlaying && labelToAngle != null && allowedMoveLabels.ContainsKey(currentSurfaceState))
        {
            string[] labels = allowedMoveLabels[currentSurfaceState];

            foreach (var label in labels)
            {
                if (labelToAngle.TryGetValue(label, out float angle))
                {
                    float angleRad = angle * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);
                    
                    Gizmos.color = allowedJumpColor;
                    Gizmos.DrawLine(rb.position, rb.position + dir.normalized * directionLineLength);

                    #if UNITY_EDITOR
                    if (showDirectionLabels)
                    {
                        UnityEditor.Handles.color = allowedJumpColor;
                        UnityEditor.Handles.Label(rb.position + dir.normalized * (directionLineLength + 0.1f), label);
                    }
                    #endif
                }
            }
        }

        // 🟦 Dash directions in cyan
        if (Application.isPlaying && labelToAngle != null)
        {
            foreach (var pair in labelToAngle)
            {
                string label = pair.Key;
                float angle = pair.Value;

                if (!IsDashDirectionAllowed(label)) continue;

                float angleRad = angle * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);

                Gizmos.color = dashDirectionColor;
                Gizmos.DrawLine(rb.position, rb.position + dir.normalized * directionLineLength);

        #if UNITY_EDITOR
                if (showDirectionLabels)
                {
                    UnityEditor.Handles.color = dashDirectionColor;
                    UnityEditor.Handles.Label(rb.position + dir.normalized * (directionLineLength + 0.1f), $"D:{label}");
                }
        #endif
            }
        }


        // 🟠 Snapped input direction – draw LAST, always on top
        if (Application.isPlaying && currentInput.sqrMagnitude > 0.01f)
        {
            Gizmos.color = snappedInputColor;
            Gizmos.DrawLine(rb.position, rb.position + (Vector3)snappedDir.normalized * directionLineLength);

        #if UNITY_EDITOR
            if (showDirectionLabels)
            {
                string dirLabel = GetClosestDirectionLabel(snappedDir);
                UnityEditor.Handles.color = snappedInputColor;
                UnityEditor.Handles.Label(rb.position + (Vector3)snappedDir.normalized * (directionLineLength + 0.15f), $"Input: {dirLabel}");
            }
        #endif
        }
    }
}
