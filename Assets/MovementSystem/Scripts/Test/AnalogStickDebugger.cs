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




// void Awake()
    // {
    //     SetupInputActions();

    //     rb = GetComponent<Rigidbody>();
    //     BuildLabelToAngleMap();
    // }

    // private void SetupInputActions()
    // {
    //     var map = inputActions.FindActionMap("Player");
    //     leftAnalogStickInput = map.FindAction("Movement");
    //     southButtonInput = map.FindAction("Jump");

    //     leftAnalogStickInput.Enable();
    //     southButtonInput.Enable();
    // }

    // void OnEnable()
    // {
    //     // inputActions.Enable();
    //     RegisterInputCallbacks();
    // }

    // void OnDisable()
    // {
    //     // inputActions.Disable();
    //     UnregisterInputCallbacks();
    // }

    // private void RegisterInputCallbacks()
    // {
    //     southButtonInput.started += OnSouthButtonStarted;
    //     southButtonInput.performed += OnSouthButtonPerformed;
    //     southButtonInput.canceled += OnSouthButtonCanceled;
    // }

    // private void UnregisterInputCallbacks()
    // {
    //     southButtonInput.started -= OnSouthButtonStarted;
    //     southButtonInput.performed -= OnSouthButtonPerformed;
    //     southButtonInput.canceled -= OnSouthButtonCanceled;
    // }

    // private void LeftAnalogStickInput() 
    // {
    //     if (Gamepad.current != null)
    //     {
    //         if (useRawInput)
    //             leftStickInput = Gamepad.current.leftStick.ReadUnprocessedValue();
    //         else
    //             leftStickInput = leftAnalogStickInput.ReadValue<Vector2>();

    //         if (leftStickInput.magnitude > minStickMagnitude)
    //         {
    //             stickHoldTimer += Time.deltaTime;
    //             if (stickHoldTimer >= minStickHoldTime)
    //             {
    //                 snappedDir = GetSnappedDirection().normalized;
    //             }
    //         }
    //         else
    //         {
    //             stickHoldTimer = 0f;
    //         }
    //     }

    // }

    // public void OnSouthButtonStarted(InputAction.CallbackContext ctx)
    // {
    //     if (ctx.started) 
    //     {
    //         Debug.Log("South Button Started");

    //         if(isInAir) return; 
        
    //         movementState = MovementState.Charging;
    //         buttonHoldTimer = 0;
    //         southButtonPressed = true;
    //         actionInProgress = false;
    //     }
    // }

    // public void OnSouthButtonPerformed(InputAction.CallbackContext ctx) 
    // {
    //     // This is typically called when the button is fully pressed
    // }

    // public void OnSouthButtonCanceled(InputAction.CallbackContext ctx) 
    // {
    //     if (ctx.canceled && southButtonPressed)
    //     {
    //         southButtonPressed = false;

    //         // Block micro-taps
    //         if (buttonHoldTimer < minButtonPressTime || stickHoldTimer < minStickHoldTime)
    //         {
    //             Debug.Log("⛔ Ignoring quick input");
    //             ResetActionState();
    //             return;
    //         }

    //         if (snappedDir != Vector2.zero)
    //         {
    //             if (isInAir)
    //             {
    //                 Debug.Log("⛔ Cannot perform action in air");
    //                 hasAppliedForce = false;
    //                 buttonHoldTimer = 0;
    //                 stickHoldTimer = 0;
    //                 southButtonPressed = false;
    //                 return;
    //             }

    //             PerformMovementAction();
    //         }
    //         else
    //         {
    //             ResetActionState();
    //         }
    //     }
    // }


    // void Update()
    // {
    //     LeftAnalogStickInput();

    //     if (stateChanged)
    //     {
    //         stateTimer += Time.deltaTime;
    //         if (stateTimer >= stateBuffer)
    //         {
    //             stateChanged = false;
    //             stateTimer = 0f;
    //         }
    //     }

    //     if(isInAir) currentSurfaceState = SurfaceState.Air;

    //     if (southButtonPressed)
    //     {
    //         buttonHoldTimer += Time.deltaTime;
    //         if (!actionInProgress)
    //         {
    //             if(buttonHoldTimer >= maxHoldTime && buttonHoldTimer >= minButtonPressTime) 
    //             {
    //                 PerformMovementAction();
    //             }
    //             // else 
    //             // {
    //             //     rb.linearVelocity = Vector2.zero;
    //             //     movementState = MovementState.Idle;
    //             // }
    //         }
    //     }

    //     switch (movementState)
    //     {
    //         case MovementState.Idle:
                
    //         break;

    //         case MovementState.Charging:

    //         break;

    //         case MovementState.Dashing:

    //         break;

    //         case MovementState.Jumping:

    //         break;

    //         case MovementState.Hovering:

    //         break;

    //         case MovementState.Descending:

    //         break;

    //         case MovementState.AirDashing:

    //         break;

    //         case MovementState.Stucked:

    //         break;

    //         case MovementState.WallDescending:

    //         break;

    //         case MovementState.WallDashing:

    //         break;
    //     }
    // }

    // void FixedUpdate()
    // {
    //     isInAir = !IsCollidingWithSurface();
    //     HandleActionForces();
    //     CheckArrivalAtTarget();

    //     if (movementState == MovementState.Hovering)
    //     {
    //         if (useHoverWobble && hoverTimer < (hoverDuration - hoverStartDelay))
    //         {
    //             hoverWobbleTimer += Time.fixedDeltaTime;
    //             float wobbleFadeIn = Mathf.Clamp01(hoverWobbleTimer / wobbleFadeInFactor); 
    //             float wobbleOffset = Mathf.Sin(hoverWobbleTimer * hoverWobbleSpeed) * hoverWobbleHeight * wobbleFadeIn;
    //             Vector3 upwardForce = new Vector3(0f, wobbleOffset, 0f);

    //             rb.AddForce(upwardForce, ForceMode.Acceleration);
    //         }
            
    //         UpdateHoverTimer();
    //     }
    //     else if(movementState == MovementState.Jumping || movementState == MovementState.AirDashing)
    //     {
    //         TryStartHoverEffect();
    //     }

    //     if (movementState != MovementState.Hovering && movementState != MovementState.AirDashing)
    //     {
    //         ApplyCustomGravity();

    //         // Only enter Descending if NOT near ground and not already landing
    //         if (rb.linearVelocity.y < 0 && !IsNearGround() && !isLandingBuffered)
    //         {
    //             movementState = MovementState.Descending;
    //         }
    //     }

    //     if (movementState == MovementState.Jumping)
    //     {
    //         Vector3 toTarget = predictedTargetPoint - rb.position;
    //         float distance = toTarget.magnitude;
    //         TryStartHoverEffect();

    //         Vector3 desiredDir = toTarget.normalized;

    //         // 📉 Ease-in force based on proximity
    //         float easingFactor = Mathf.Clamp01(distance / targetDistance);
    //         float scaledForce = forceMagnitude * easingFactor;

    //         rb.AddForce(desiredDir * scaledForce, jumpForceMode);

    //         // 🧤 Clamp max speed
    //         float maxJumpSpeed = 10f;
    //         if (rb.linearVelocity.magnitude > maxJumpSpeed)
    //         {
    //             rb.linearVelocity = rb.linearVelocity.normalized * maxJumpSpeed;
    //         }

    //         // 🌫️ Add damping based on proximity to slow down
    //         float closeRange = 1.0f;
    //         rb.linearDamping = (distance < closeRange)
    //             ? Mathf.Lerp(0f, 5f, 1f - (distance / closeRange))
    //             : 0f;
    //     }

    //     if (movementState == MovementState.Descending && IsNearGround())
    //     {
    //         movementState = MovementState.Idle;
    //         isLandingBuffered = true;
    //         Debug.Log("✅ Landed – forced transition to Idle");
    //     }

    //     TrySetIdleState();

    //     // Reset landing buffer once fully idle and grounded
    //     if (movementState == MovementState.Idle && currentSurfaceState == SurfaceState.Ground)
    //     {
    //         isLandingBuffered = false;
    //     }

    //     if(rb.linearVelocity.sqrMagnitude > 0.1f) 
    //     {
    //         isMoving = true;
    //     }

    //     // FIX THIS
    //     if(isMoving) 
    //     {
    //         // Slowly reduce acceleration

    //         // Calculate the distance from the surface to the player from the moment the IsNearSurface is true

    //         // Calculate the speed of the player

    //         // Check if the player reaches the surface with the amount of speed and distance left to reach it

    //         // If the player reaches the surface with the current speed and the distance need to reach it

    //         // Then gradually reduce the speed of the player untill it's zero

    //         // The speed should be zero when the player reached the surface

    //         // If the player won't reach the surface with the current speed based on the calculations made

    //         // Then return

    //         // Do all this in another method and invoke it in this if statement
    //         TrySmartDecelerateIfNearSurface();
    //     }
    // }

    // private void TrySmartDecelerateIfNearSurface()
    // {
    //     // Skip if we're barely moving
    //     if (rb.linearVelocity.sqrMagnitude < 0.01f)
    //         return;

    //     Vector3 moveDirection = rb.linearVelocity.normalized;
    //     Vector3 origin = rb.position;

    //     if (Physics.Raycast(origin, moveDirection, out RaycastHit hit, checkDistance, surfaceLayer))
    //     {
    //         nearbySurface = hit.collider.gameObject;
    //         float distanceToSurface = hit.distance;
    //         float currentSpeed = rb.linearVelocity.magnitude;

    //         float estimatedTimeToReach = distanceToSurface / currentSpeed;

    //         if (estimatedTimeToReach <= 0.5f)
    //         {
    //             float decelerationAmount = currentSpeed / estimatedTimeToReach;
    //             Vector3 decelerationForce = -moveDirection * decelerationAmount;

    //             rb.AddForce(decelerationForce, ForceMode.Acceleration);

    //             if (distanceToSurface <= distanceToSurfaceThreshold)
    //             {
    //                 rb.linearVelocity = Vector3.zero;
    //                 Debug.Log("🛑 Reached surface – velocity clamped");
    //             }

    // #if UNITY_EDITOR
    //             Debug.DrawLine(origin, hit.point, Color.magenta); // Visual debug
    // #endif
    //         }
    //     }
    // }

    // void LateUpdate()
    // {
    //     // Optional: force Z = 0 if you're staying in 2D
    //     Vector3 pos = rb.position;
    //     pos.z = 0;
    //     rb.position = pos;
    // }

    // private void OnCollisionEnter(Collision collision)
    // {
    //     HandleSurfaceState(collision);

    //     if (movementState == MovementState.Descending && currentSurfaceState == SurfaceState.Ground || currentSurfaceState == SurfaceState.Ceiling)
    //     {
    //         movementState = MovementState.Idle;
    //         isLandingBuffered = true;
    //         Debug.Log("🛬 Collision landed – set to Idle");
    //     }
    // }

    // private void OnCollisionStay(Collision collision)
    // {
    //     HandleSurfaceState(collision);
        
    //     if (currentSurfaceState == SurfaceState.Ground && rb.position.magnitude < 0.01f)
    //     {
    //         Debug.Log("Change movement state to idle");
    //     }
    // }

    // private void OnCollisionExit(Collision collision)
    // {
    //     lastContactTime = Time.time;
    // }

    // private void CheckArrivalAtTarget()
    // {
    //     if (Vector3.Distance(rb.position, predictedTargetPoint) < arrivalRadius)
    //     {
    //         hasReachedTarget = true;
    //         Debug.Log("🏁 Reached target point, returning to start...");
    //     }
    // }

    // private void ResetActionState() 
    // {
    //     actionInProgress = false;
    //     southButtonPressed = false;
    //     rb.linearVelocity = Vector3.zero;
    //     rb.linearDamping = 0;
    //     snappedDir = Vector2.zero;
    //     hasAppliedForce = false;
    //     buttonHoldTimer = 0;
    //     stickHoldTimer = 0;

    //     // if (movementState == MovementState.Charging)
    //     // {
    //     //     // Resume proper state if in air
    //     //     if (isInAir)
    //     //     {
    //     //         if (rb.linearVelocity.y > 0)
    //     //             movementState = MovementState.Jumping;
    //     //         else if (rb.linearVelocity.y < 0)
    //     //             movementState = MovementState.Descending;
    //     //     }
    //     //     else
    //     //     {
    //     //         movementState = MovementState.Idle;
    //     //     }
    //     // }
    // }

    // private void HandleActionForces()
    // {
    //     if (!hasAppliedForce && snappedDir.sqrMagnitude > 0.01f)
    //     {
    //         rb.linearVelocity = Vector3.zero;
    //         // Vector3 appliedForce = forceMagnitude * targetDistance * snappedDir.normalized;
    //         // rb.AddForce(appliedForce * Time.fixedDeltaTime, movementForceMode);
    //         rb.AddForce(snappedDir.normalized * forceMagnitude, movementForceMode);
    //         hasAppliedForce = true;

    //         // Debug.Log($"🔼 {currentAction} applied force: {appliedForce} (Mode: {movementForceMode})");
    //     }
    // }

    // // private bool actionInProgress = false;
    // private void PerformMovementAction( ) 
    // {
    //     if(snappedDir == Vector2.zero) return;

    //     // ✅ Block input if player is in air
    //     if (isInAir)
    //     {
    //         Debug.Log("⛔ Action blocked while in air");
    //         buttonHoldTimer = 0;
    //         stickHoldTimer = 0;
    //         southButtonPressed = false;
    //         return;
    //     }

    //     string dirLabel = GetClosestDirectionLabel(snappedDir);
    //     bool isJumpAllowed = IsJumpDirectionAllowed(dirLabel);
    //     bool isDashAllowed = IsDashDirectionAllowed(dirLabel);

    //     startPos = rb.position;
        
    //     if(isDashAllowed) 
    //     {
    //         SetupMovement(maxDashDistance, dashForce, "Dash");
    //     }  
    //     else if(isJumpAllowed) 
    //     {
    //         SetupMovement(maxJumpDistance, jumpForce, "Jump");
    //     }

    //     actionInProgress = true;
    // }

    // private void SetupMovement(float maxTravelDistance, float force, string action) 
    // {
    //     targetDistance = maxTravelDistance * HoldRatio;
    //     Debug.Log($"target distance = {targetDistance}");
    //     currentAction = action;

    //     // ✅ Set the actual target here using direction and distance
    //     predictedTargetPoint = transform.position + (Vector3)snappedDir * targetDistance;
    //     hasAppliedForce = false;

    //     if(action == "Dash") 
    //     {
    //         movementState = MovementState.Dashing;
    //         isJumping = false;
    //         isDashing = true;
    //         rb.useGravity = true;

    //         movementForceMode = dashForceMode;
    //         // forceMagnitude = force;
    //         Debug.Log("Dashing");
    //     }
    //     else if(action == "Jump") 
    //     {
    //         movementState = MovementState.Jumping;
    //         isDashing = false;
    //         isJumping = true;
    //         rb.useGravity = false;
    //         movementForceMode = jumpForceMode; 
            
    //         bool isDiagonalJump = Mathf.Abs(snappedDir.x) > 0 && Mathf.Abs(snappedDir.y) > 0;
    //         Debug.Log(isDiagonalJump ? "Jumping Diagonal" : "Jumping Straight");

    //         // Vector3 directionToTarget = (predictedTargetPoint - rb.position).normalized;
    //         // snappedDir = directionToTarget;
    //         // forceMagnitude = distance / estimatedTime * force;
    //     }

    //     forceMagnitude = force;


    //     // ✅ Set the actual target here using direction and distance
    //     // predictedTargetPoint = transform.position + targetDistance * (Vector3)snappedDir.normalized;
    //     snappedDir = Vector3.Lerp(rb.linearVelocity.normalized, snappedDir.normalized, lerpAmount);

    //     Debug.Log($"{action} ➤ Direction: {snappedDir}, Force: {forceMagnitude}");
    //     Debug.Log($"📍 Set jump target to: {predictedTargetPoint}");
    // }

    // private bool IsDashDirectionAllowed(string label)
    // {
    //     if (currentSurfaceState == SurfaceState.Ground || currentSurfaceState == SurfaceState.Ceiling)
    //         return label == "W" || label == "E";

    //     if (currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall)
    //         return label == "N" || label == "S";

    //     return false;
    // }

    // private bool IsJumpDirectionAllowed(string label)
    // {
    //     if (label == "W" || label == "E")
    //         return false;

    //     if ((currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall) &&
    //         (label == "N" || label == "S"))
    //         return false;

    //     return allowedMoveLabels.TryGetValue(currentSurfaceState, out var allowed) &&
    //         System.Array.Exists(allowed, l => l == label);
    // }

    // private string GetClosestDirectionLabel(Vector2 dir)
    // {
    //     float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    //     angle = (angle + 360f) % 360f;

    //     float minDiff = float.MaxValue;
    //     string closestLabel = "N";

    //     foreach (var pair in labelToAngle)
    //     {
    //         float diff = Mathf.Abs(Mathf.DeltaAngle(angle, pair.Value));
    //         if (diff < minDiff)
    //         {
    //             minDiff = diff;
    //             closestLabel = pair.Key;
    //         }
    //     }

    //     return closestLabel;
    // }
    
    // private Vector3 GetSnappedDirection()
    // {
    //     if (leftStickInput.sqrMagnitude < 0.01f)
    //         return Vector3.zero;

    //     float rawAngle = Mathf.Atan2(leftStickInput.y, leftStickInput.x) * Mathf.Rad2Deg;

    //     if (snapDirectionsEnabled)
    //     {
    //         float angleStep = 360f / directionCount;
    //         rawAngle = Mathf.Round(rawAngle / angleStep) * angleStep;
    //     }

    //     // ✅ Rotate around Z axis for X-Y plane movement
    //     Quaternion rotation = Quaternion.Euler(0f, 0f, rawAngle);
    //     return rotation * Vector3.right;
    // }

    // private string GetDirectionLabel(int index)
    // {
    //     if (!useCardinalLabels)
    //         return (index + 1).ToString();

    //     // Standard 16-point compass
    //     string[] labels = new string[]
    //     {
    //         "E", "ENE", "NE", "NNE",
    //         "N", "NNW", "NW", "WNW",
    //         "W", "WSW", "SW", "SSW",
    //         "S", "SSE", "SE", "ESE"
    //     };

    //     return labels[index % labels.Length];
    // }
    
    // private void BuildLabelToAngleMap()
    // {
    //     labelToAngle = new Dictionary<string, float>();
    //     float angleStep = 360f / directionCount;

    //     for (int i = 0; i < directionCount; i++)
    //     {
    //         string label = GetDirectionLabel(i);
    //         float angle = (i * angleStep + 360f) % 360f;
    //         labelToAngle[label] = angle;
    //     }
    // }
    
    // private bool IsCollidingWithSurface()
    // {
    //     // Using a small overlap sphere to check for collisions
    //     Collider[] colliders = Physics.OverlapSphere(
    //         rb.position, 
    //         GetComponent<Collider>().bounds.extents.y + 0.1f // Slightly larger than collider
    //     );
        
    //     // If we have any colliders other than ourselves, we're touching something
    //     return colliders.Length > 1;
    // }

    // private void HandleSurfaceState(Collision collision)
    // {
    //     bool foundGround = false;
    //     bool foundCeiling = false;
    //     bool foundWallRight = false;
    //     bool foundWallLeft = false;
        
    //     foreach (ContactPoint contact in collision.contacts)
    //     {
    //         Vector3 normal = contact.normal;
    //         if (Vector3.Dot(normal, Vector3.up) > 0.7f)
    //         {
    //             foundGround = true;
    //         }
    //         else if (Vector3.Dot(normal, Vector3.down) > 0.7f)
    //         {
    //             foundCeiling = true;
    //         }
    //         else if (Vector3.Dot(normal, Vector3.right) > 0.7f)
    //         {
    //             foundWallLeft = true;
    //         }
    //         else if (Vector3.Dot(normal, Vector3.left) > 0.7f)
    //         {
    //             foundWallRight = true;
    //         }
    //     }
        
    //     SurfaceState previousState = currentSurfaceState;
        
    //     // Priority-based state determination with stickiness for ground and ceiling
    //     if (foundGround)
    //     {
    //         currentSurfaceState = SurfaceState.Ground;
    //     }
    //     else if (foundCeiling)
    //     {
    //         currentSurfaceState = SurfaceState.Ceiling;
    //     }
    //     else if ((foundWallLeft || foundWallRight) && currentSurfaceState == SurfaceState.Air)
    //     {
    //         // Only switch to wall state if we weren't on ground or ceiling
    //         if (foundWallLeft)
    //         {
    //             currentSurfaceState = SurfaceState.LeftWall;
    //         }
    //         else
    //         {
    //             currentSurfaceState = SurfaceState.RightWall;
    //         }
    //     }

    //     if (previousState != currentSurfaceState)
    //     {
    //         Debug.Log($"Surface State changed from {previousState} to {currentSurfaceState}");
    //         stateChanged = true;
    //     }
    // }

    // private void ApplyCustomGravity()
    // {
    //     if (movementState == MovementState.Hovering)
    //         return;

    //     Vector3 gravityDir = DetermineGravityDirection();
    //     float gravityForce = gravityStrength;

    //     float verticalVelocity = Vector3.Dot(rb.linearVelocity, gravityDir);
    //     float upwardVelocity = Vector3.Dot(rb.linearVelocity, -gravityDir);
    //     float jumpHeightSoFar = Vector3.Project(rb.position - startPos, -gravityDir).magnitude;
    //     bool isDropping = leftAnalogStickInput.ReadValue<Vector2>() == Vector2.down;

    //     if (verticalVelocity > maxFallSpeed) return;

    //     // ✅ Handle FAST-FALL as a 1-time burst
    //     if (isDropping && isInAir && !fastFalling)
    //     {
    //         rb.AddForce(dropMultiplier * gravityForce * gravityDir, dropForceMode);
    //         fastFalling = true;
    //         Debug.Log("⚡ Burst drop applied (Impulse)");
    //         return; // ⛔ Skip normal gravity this frame
    //     }

    //     // ✅ Apply modifiers for regular jump/fall
    //     if (jumpHeightSoFar < minHoverHeight && upwardVelocity > 0.1f && !southButtonPressed)
    //     {
    //         gravityForce *= lowJumpMultiplier;
    //     }
    //     else if (verticalVelocity > 0.1f)
    //     {
    //         gravityForce *= fallMultiplier;
    //     }

    //     // ✅ Apply normal gravity
    //     rb.AddForce(gravityDir * gravityForce, fallForceMode);
    // }
    
    // private bool IsNearGround()
    // {
    //     if (!TryGetComponent<Collider>(out var col) && movementState != MovementState.Descending) return false;

    //     Vector3 origin = col.bounds.center;
    //     origin.y = col.bounds.min.y; 

    //     if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundProximityCheckDistance))
    //     {
    //         Debug.DrawLine(origin, hit.point, Color.magenta);
    //         return true;
    //     }

    //     return false;
    // }

    // private Vector3 DetermineGravityDirection()
    // {
    //     // Default world gravity direction
    //     Vector3 gravityDir = gravityDirection.normalized;
        
    //     // Modify gravity direction based on surface state
    //     switch (currentSurfaceState)
    //     {
    //         case SurfaceState.Ground:
    //             // Standard down gravity when on ground
    //             gravityDir = Vector3.down;
    //             fastFalling = false;

    //             break;
                
    //         case SurfaceState.Ceiling:
    //             // Standard down gravity when on ceiling (to fall)
    //             gravityDir = Vector3.down;
    //             break;
                
    //         case SurfaceState.LeftWall:
    //             // Pull character toward left wall
    //             gravityDir = Vector3.right;
    //             break;
                
    //         case SurfaceState.RightWall:
    //             // Pull character toward right wall
    //             gravityDir = Vector3.left;
    //             break;
    //     }
        
    //     // If in mid-air and not touching any surface, use default gravity
    //     if (isInAir && Time.time - lastContactTime > NO_CONTACT_THRESHOLD)
    //     {
    //         gravityDir = gravityDirection.normalized;
    //     }
        
    //     return gravityDir;
    // }

    // private void TrySetIdleState()
    // {
    //     if (movementState == MovementState.Charging ||
    //         movementState == MovementState.Jumping ||
    //         movementState == MovementState.Dashing ||
    //         actionInProgress)
    //         return;

    //     Debug.Log($"linearVelocity {rb.linearVelocity.sqrMagnitude}");
    //     Debug.Log($"linearVelocity: X->{rb.linearVelocity.x}, Y->{rb.linearVelocity.y}");
    //     if (rb.linearVelocity.sqrMagnitude < 0.01f && currentSurfaceState == SurfaceState.Ground)
    //     {
    //         movementState = MovementState.Idle;
    //     }
    // }

    // private bool TryStartHoverEffect()
    // {
    //     if (!isInAir || movementState == MovementState.Hovering || hasTriggeredHover)
    //         return false;

    //     Vector3 toTarget = predictedTargetPoint - rb.position;
    //     float forwardDot = Vector3.Dot(rb.linearVelocity.normalized, toTarget.normalized);
    //     float distanceToTarget = toTarget.magnitude;

    //     float hoverTriggerRadius = hoverActivationRadius;
    //     float hoverForgivenessDistance = 2.5f;

    //     bool isCloseEnough = distanceToTarget <= hoverTriggerRadius;
    //     bool hasPassedTarget = forwardDot < 0f;
    //     bool isInForgivenessZone = hasPassedTarget && distanceToTarget <= hoverForgivenessDistance;

    //     if (!(isCloseEnough || isInForgivenessZone))
    //         return false;

    //     // ✅ Passed checks — start hover
    //     movementState = MovementState.Hovering;
    //     rb.linearDamping = linearDampingOnHover;
    //     storedGravityValue = gravityStrength;
    //     gravityStrength = 0f;

    //     hoverTimer = hoverDuration;
    //     hoverWobbleTimer = 0f;
    //     originalHoverPosition = rb.position;

    //     StartCoroutine(SmoothHoverTransition());
    //     hasTriggeredHover = true;
    //     Debug.Log("🛸 Hover Started — Smooth Entry");
    //     return true;
    // }

    // private IEnumerator SmoothHoverTransition()
    // {
    //     float transitionTime = 0.25f; // Time to blend in
    //     float elapsed = 0f;
    //     float initialDamping = rb.linearDamping;
    //     float targetDamping = linearDampingOnHover;

    //     float initialGravity = gravityStrength;
    //     float targetGravity = 0f;

    //     while (elapsed < transitionTime)
    //     {
    //         float t = elapsed / transitionTime;

    //         rb.linearDamping = Mathf.Lerp(initialDamping, targetDamping, t);
    //         gravityStrength = Mathf.Lerp(initialGravity, targetGravity, t);

    //         elapsed += Time.fixedDeltaTime;
    //         yield return new WaitForFixedUpdate();
    //     }

    //     rb.linearDamping = targetDamping;
    //     gravityStrength = targetGravity;
    // }

    // private void UpdateHoverTimer()
    // {
    //     if (movementState != MovementState.Hovering)
    //         return;

    //     hoverTimer -= Time.fixedDeltaTime;

    //     if (hoverTimer <= 0f)
    //     {
    //         Debug.Log("Hover timer ran out");
    //         ExitHover();
    //     }
    // }

    // private void ExitHover()
    // {
    //     movementState = MovementState.Descending; 
    //     hoverTimer = 0f;
    //     hoverWobbleTimer = 0f;
    //     rb.linearDamping  = 0f; 
    //     hasTriggeredHover = false; 
    //     gravityStrength = storedGravityValue;
    //     actionInProgress = false;

    //     Debug.Log("⬇️ Exiting Hover – Starting to Descend");
    // }


    // void OnDrawGizmos()
    // {
    //     // 🔵 Direction segments
    //     if (snapDirectionsEnabled)
    //     {
    //         Gizmos.color = baseDirectionColor;
    //         float angleStep = 360f / directionCount;

    //         for (int i = 0; i < directionCount; i++)
    //         {
    //             float angle = i * angleStep;
    //             float angleRad = angle * Mathf.Deg2Rad;
    //             Vector3 dir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);

    //             Vector3 endPoint = transform.position + dir * directionLineLength;
    //             Gizmos.DrawLine(transform.position, endPoint);

    //             #if UNITY_EDITOR
    //             if (showDirectionLabels)
    //             {
    //                 UnityEditor.Handles.color = Color.white;
    //                 UnityEditor.Handles.Label(endPoint + Vector3.up * 0.1f, GetDirectionLabel(i));
    //             }
    //             #endif
    //         }
    //     }

    //     // 🔴/🟢 Jump target visualization depending on reach status
    //     if (Application.isPlaying && predictedTargetPoint != Vector3.zero)
    //     {
    //         if (rb == null)
    //         {
    //             rb = GetComponent<Rigidbody>();
    //             if (rb == null) return; // Still null? Exit the method to avoid errors
    //         }

    //         float distanceToTarget = Vector3.Distance(rb.position, predictedTargetPoint);
    //         Gizmos.color = hasReachedTarget ? jumpTargetColor : landingPointColor;
    //         Gizmos.DrawSphere(predictedTargetPoint, 0.25f);
    //     }

    //     // 🟩 Allowed move directions
    //     if (Application.isPlaying && labelToAngle != null && allowedMoveLabels.ContainsKey(currentSurfaceState))
    //     {
    //         string[] labels = allowedMoveLabels[currentSurfaceState];

    //         foreach (var label in labels)
    //         {
    //             if (labelToAngle.TryGetValue(label, out float angle))
    //             {
    //                 float angleRad = angle * Mathf.Deg2Rad;
    //                 Vector3 dir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);
                    
    //                 Gizmos.color = allowedJumpColor;
    //                 Gizmos.DrawLine(rb.position, rb.position + dir.normalized * directionLineLength);

    //                 #if UNITY_EDITOR
    //                 if (showDirectionLabels)
    //                 {
    //                     UnityEditor.Handles.color = allowedJumpColor;
    //                     UnityEditor.Handles.Label(rb.position + dir.normalized * (directionLineLength + 0.1f), label);
    //                 }
    //                 #endif
    //             }
    //         }
    //     }

    //     // 🟦 Dash directions in cyan
    //     if (Application.isPlaying && labelToAngle != null)
    //     {
    //         foreach (var pair in labelToAngle)
    //         {
    //             string label = pair.Key;
    //             float angle = pair.Value;

    //             if (!IsDashDirectionAllowed(label)) continue;

    //             float angleRad = angle * Mathf.Deg2Rad;
    //             Vector3 dir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);

    //             Gizmos.color = dashDirectionColor;
    //             Gizmos.DrawLine(rb.position, rb.position + dir.normalized * directionLineLength);

    //     #if UNITY_EDITOR
    //             if (showDirectionLabels)
    //             {
    //                 UnityEditor.Handles.color = dashDirectionColor;
    //                 UnityEditor.Handles.Label(rb.position + dir.normalized * (directionLineLength + 0.1f), $"D:{label}");
    //             }
    //     #endif
    //         }
    //     }


    //     // 🟠 Snapped input direction – draw LAST, always on top
    //     if (Application.isPlaying && leftStickInput.sqrMagnitude > 0.01f)
    //     {
    //         Gizmos.color = snappedInputColor;
    //         Gizmos.DrawLine(rb.position, rb.position + (Vector3)snappedDir.normalized * directionLineLength);

    //     #if UNITY_EDITOR
    //         if (showDirectionLabels)
    //         {
    //             string dirLabel = GetClosestDirectionLabel(snappedDir);
    //             UnityEditor.Handles.color = snappedInputColor;
    //             UnityEditor.Handles.Label(rb.position + (Vector3)snappedDir.normalized * (directionLineLength + 0.15f), $"Input: {dirLabel}");
    //         }
    //     #endif
    //     }
    // }
// }


        // ⏳ Handle stuck freeze logic
        // if (movementState == MovementState.Stucked)
        // {
        //     // If dropping input is detected, skip the stuck timer and start wall descending immediately
        //     if (isStuckFrozen && isDropping)
        //     {
        //         isStuckFrozen = false;
        //         rb.isKinematic = false;
        //         movementState = MovementState.WallDescending;
        //         stuckCooldownTimer = stuckCooldownDuration;

        //         Debug.Log("⏬ Dropping input detected — early release from wall Stuck into WallDescending");
        //         return; // Skip rest of stuck logic
        //     }

        //     if (isStuckFrozen)
        //     {
        //         stuckTimer -= Time.fixedDeltaTime;

        //         // Freeze player during stuck time
        //         rb.isKinematic = true;
        //         gravityStrength = 0;

        //         if (stuckTimer <= 0)
        //         {
        //             isStuckFrozen = false;
        //             rb.isKinematic = false;

        //             if (currentSurfaceState != SurfaceState.Ceiling)
        //                 movementState = MovementState.WallDescending;
        //             else
        //                 movementState = MovementState.Descending;

        //             stuckCooldownTimer = stuckCooldownDuration;

        //             Debug.Log($"🔓 Released from Stuck – Resume Movement");
        //             Debug.Log($"Movement state changed to {movementState} from Handle Stuck Freeze Logic");
        //         }
        //     }

        //     droppedFromWall = true;
        // }

        // // State change after being stucked on the wall
        // if (movementState == MovementState.Stucked && stuckTimer <= 0f && !isStuckFrozen)
        // {
        //     if (currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall)
        //     {
        //         movementState = MovementState.WallDescending;
        //         Debug.Log("➡️ Entered WallDescending after stuck release");
        //     }
        //     else
        //     {
        //         movementState = MovementState.Descending;
        //     }
        // }



         // GPT
    // void FixedUpdate()
    // {
    //     TrySetIdleState();
    //     ApplyCustomGravity();
    //     GetLastCollidedSurface();

    //     isInAir = !IsCollidingWithSurface();
    //     if (useHandleActionForces) HandleActionForces();
        
    //     CheckArrivalAtTarget();
    //     if ((movementState == MovementState.Jumping || movementState == MovementState.AirDashing) 
    //         && (!isDropping || !fastFalling))
    //     {
    //         TryStartHoverEffect();
    //         Debug.Log("Can hover");
    //     }
        
    //     if (movementState != MovementState.Hovering 
    //         && movementState != MovementState.AirDashing 
    //         && movementState != MovementState.Stucked)
    //     {
    //         if (rb.linearVelocity.y < 0 
    //             && !hasReachedTarget              
    //             && !IsNearGround() 
    //             && !isLandingBuffered 
    //             && currentSurfaceState != SurfaceState.LeftWall 
    //             && currentSurfaceState != SurfaceState.RightWall)
    //         {
    //             movementState = MovementState.Descending;
    //             // Debug.Log($"rb linear vel Y -> {rb.linearVelocity.y}, isNearGround -> {IsNearGround()}, isLandingBuffered -> {isLandingBuffered}");
    //             // Debug.Log($"✅ Movement state changed to Descending");
    //         }
    //     }

    //     // else if (movementState == MovementState.Hovering && (!isDropping || !fastFalling)) 
    //     // {
    //     //     WobbleEffect();
    //     // }
    //     // ⬇️ Prevent descending override if we're about to hover

    //     // Smooth out movement
    //     SmoothMovement(); 

    //     if (rb.linearVelocity.sqrMagnitude > isMovingThreshold)
    //         isMoving = true;
    //     else 
    //         isMoving = false;
    
    //     // if (isMoving)
    //     // {
    //     //     TrySmartDecelerateIfNearSurface();
    //     // }

    //     // ⛔ Stop movement if colliding while moving
    //     StopMovementUponCollision();

    //     // // Hover
    //     // if ((movementState == MovementState.Jumping || movementState == MovementState.AirDashing) 
    //     //     && (!isDropping || !fastFalling))
    //     // {
    //     //     TryStartHoverEffect();
    //     // }
    //     // else if (movementState == MovementState.Hovering && (!isDropping || !fastFalling)) 
    //     // {
    //     //     WobbleEffect();
    //     // }

    //     // if((movementState == MovementState.Hovering || movementState == MovementState.Descending) && isDropping && !fastFalling) 
    //     // {
    //     //     Debug.Log("Applying burst drop");
    //     //     ApplyBurstDropForce(); 
    //     // } 
        
    //     // ⏳ Handle stuck freeze logic
    //     if (movementState == MovementState.Stucked)
    //     {
    //         FreezePlayer();
    //     }
        
    //     // Force to idle if near ground
    //     if (/*movementState == MovementState.Descending ||*/ movementState == MovementState.WallDescending)
    //     {
    //         if (IsNearGround() && rb.linearVelocity.y < -10f) // Adjust speed threshold as needed
    //         {
    //             float wallDescendingSpeed = 10f;
    //             // Clamp vertical speed near ground to ensure smoother landing
    //             rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallDescendingSpeed, rb.linearVelocity.z);
    //             Debug.Log("✅ Landed – forced transition to Idle");
    //         }
    //     }

    //     // Handle bounce mechanic when wall descending
    //     if (movementState == MovementState.WallDescending && IsNearGround()) 
    //     {
    //         OnGroundCollisionBounceFromWall();
    //     }
    // }

    // ORIGNAL 
    // void FixedUpdate()
    // {
    //     ApplyCustomGravity();
    //     GetLastCollidedSurface();

    //     isInAir = !IsCollidingWithSurface();
    //     if(useHandleActionForces) HandleActionForces();
        
    //     if (movementState != MovementState.Hovering && movementState != MovementState.AirDashing && movementState != MovementState.Stucked)
    //     {
    //         if (rb.linearVelocity.y < 0 
    //             && !IsNearGround() 
    //             && !isLandingBuffered 
    //             && currentSurfaceState != SurfaceState.LeftWall 
    //             && currentSurfaceState != SurfaceState.RightWall)
    //         {
    //             movementState = MovementState.Descending;
    //             // Debug.Log($"rb linear vel Y -> {rb.linearVelocity.y}, isNearGround -> {IsNearGround()}, isLandingBuffered -> {isLandingBuffered}");
    //             // Debug.Log($"✅ Movement state changed to Descending");
    //         }

    //     }

    //     // Smoooth out movement
    //     SmoothMovement(); 

    //     if (rb.linearVelocity.sqrMagnitude > isMovingThreshold)
    //     {
    //         isMoving = true;
    //     }
    //     else { isMoving = false; }
       

    //     // if (isMoving)
    //     // {
    //     //     TrySmartDecelerateIfNearSurface();
    //     // }

    //     CheckArrivalAtTarget();

    //     // Hover
    //     if (movementState == MovementState.Jumping || movementState == MovementState.AirDashing && (!isDropping || !fastFalling))
    //     {
    //         TryStartHoverEffect();
    //     }
    //     else if (movementState == MovementState.Hovering && (!isDropping || !fastFalling)) 
    //     {
    //         WobbleEffect();
    //     }

    //     // if((movementState == MovementState.Hovering || movementState == MovementState.Descending) && isDropping && !fastFalling) 
    //     // {
    //     //     Debug.Log("Applying burst drop");
    //     //     ApplyBurstDropForce(); 
    //     // } 

    //     // Force to idle if near ground
    //     if (/*movementState == MovementState.Descending ||*/ movementState == MovementState.WallDescending)
    //     {
    //         if (IsNearGround() && rb.linearVelocity.y < -10f) // Adjust speed threshold as needed
    //         {
    //             float wallDescendingSpeed = 10f;
    //             // Clamp vertical speed near ground to ensure smoother landing
               
    //             rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallDescendingSpeed, rb.linearVelocity.z);
    //             Debug.Log("✅ Landed – forced transition to Idle");
    //         }
    //     }

    //     TrySetIdleState();

    //     // ⛔ Stop movement if colliding while moving
    //     StopMovementUponCollision();

    //     // ⏳ Handle stuck freeze logic
    //     if (movementState == MovementState.Stucked)
    //     {
    //         FreezePlayer();
    //     }
       
    //     // Handle bounce mechanic when walldescending
    //     if(movementState == MovementState.WallDescending && IsNearGround()) 
    //     {
    //         OnGroundCollisionBounceFromWall();
    //     }
    // }