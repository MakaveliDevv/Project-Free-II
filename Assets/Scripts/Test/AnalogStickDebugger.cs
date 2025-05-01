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

    //    #region Update Handlers
    //     /// <summary>
    //     /// Handles button hold logic, auto-triggering actions if max hold time is exceeded.
    //     /// Related to: PerformAction (triggers action when conditions are met).
    //     /// </summary>
    //     private void HandleButtonHold()
    //     {
    //         if (southButtonPressed && !actionInProgress)
    //         {
    //             holdTime += Time.deltaTime;

    //             if (state != MovementState.Charging)
    //                 state = MovementState.Charging;
                
    //             // Auto-trigger if max hold time is reached
    //             if (holdTime >= maxHoldTime && lastSnappedDirection != Vector2.zero)
    //             {
    //                 PerformAction();
    //             }
    //         }
    //     }

    //     /// <summary>
    //     /// Detects and manages player's falling state.
    //     /// Related to: ApplyCustomGravity (gravity behavior based on falling).
    //     /// </summary>
    //     private void DetectFallingState()
    //     {
    //         if (!isFalling && rb.linearVelocity.y < -0.5f && currentSurfaceState == SurfaceState.Ceiling)
    //         {
    //             isFalling = true;
    //             Debug.Log("Player is falling from ceiling");
    //         }

    //         // Reset falling state when touching ground or other surface
    //         if (isFalling && rb.linearVelocity.y >= -0.1f)
    //         {
    //             isFalling = false;
    //         }
    //     }

    //     /// <summary>
    //     /// Manages buffer timing for state transitions.
    //     /// Related to: stateChanged flag (used to track transitions).
    //     /// </summary>
    //     private void HandleStateTransitionBuffer()
    //     {
    //         if(stateChanged) 
    //         {
    //             stateTimer += Time.deltaTime;
    //             if(stateTimer >= stateBuffer) 
    //             {
    //                 stateChanged = false;
    //                 stateTimer = 0f;
    //             }
    //         }
    //     }
    //     #endregion

    // private void HandleActionCompletion()
    // {
    //     if (!actionInProgress || actionCompleted) return;

    //     // Vector from start to predicted target
    //     Vector3 actionVector = predictedTargetPosition - rb.position;
    //     float remainingDistance = actionVector.magnitude;

    //     // Check if we've passed the target in the direction of movement
    //     float forwardProgress = Vector3.Dot(rb.linearVelocity .normalized, actionVector.normalized);

    //     // Diagonal boost factor
    //     float tolerance = Mathf.Max(0.15f, rb.linearVelocity .magnitude * Time.fixedDeltaTime);

    //     if (isDiagonalJump)
    //     {
    //         tolerance *= 2.0f; // Diagonal jumps need more tolerance
    //     }

    //     // If we're close OR we've passed the target directionally
    //     if (remainingDistance <= tolerance || forwardProgress < 0f)
    //     {
    //         actionCompleted = true;
    //         Invoke(nameof(ResetActionState), 0.1f);

    //         // ✅ Always re-enable gravity unless we trigger hover
    //         if (state != MovementState.AirDashing && state != MovementState.Ascending)
    //         {
    //             rb.useGravity = true;
    //         }

    //         if ((state == MovementState.AirDashing || state == MovementState.Ascending) && !hasTriggeredHover)
    //         {
    //             TryStartHover();
    //         }
            
    //         Debug.Log("✅ Action complete — reached or passed target.");
    //     }

    // }


      // void Update()
    // {
    //     HandleButtonHold();
    //     DetectFallingState();
    //     HandleStateTransitionBuffer();

    //     // Handle exiting hover state
    //     // if (state == MovementState.Hovering)
    //     // {
    //     //     hoverTimer += Time.deltaTime;
    //     //     if (hoverTimer > hoverDuration)
    //     //     {
    //     //         ExitHover();
    //     //     }
    //     // }

    //     if (state == MovementState.Hovering)
    //     {
    //         hoverTimer += Time.deltaTime;

    //         if (hoverTimer > hoverDuration)
    //         {
    //             ExitHover();
    //         }
    //         else if (useHoverWobble)
    //         {
    //             hoverWobbleTimer += Time.deltaTime;
    //             float wobbleOffset = Mathf.Sin(hoverWobbleTimer * hoverWobbleSpeed) * hoverWobbleHeight;
    //             Vector3 newPosition = originalHoverPosition + new Vector3(0f, wobbleOffset, 0f);
    //             rb.MovePosition(newPosition);
    //         }
    //     }

    //     // To contineously update the inputDirection sinc it bugs out sometimes
    //     // Need to find a way to do it differently and clean
    //     inputDirection = leftAnalogStickInput.ReadValue<Vector2>();

    //     if (!actionInProgress && inputDirection.magnitude > deadzone)
    //     {
    //         Vector2 snapped = GetSnappedDirection(inputDirection).normalized;
    //         if (snapped != Vector2.zero)
    //         {
    //             lastSnappedDirection = snapped;
    //         }
    //     }

    //     if (!actionInProgress && southButtonPressed && lastSnappedDirection != Vector2.zero)
    //     {
    //         UpdatePredictedTargetPosition();
    //     }

    //     // if(state == MovementState.Charging) 
    //     // {
    //     //     rb.useGravity = false;
    //     // }

    // }

    // void FixedUpdate()
    // {
    //     HandleActionForces();
    //     CheckAirState();
    //     HandleActionTimeout();
    //     CheckSurfaceContact();
    //     HandleActionCompletion();

    //     if(isInAir) 
    //     {
    //         currentSurfaceState = SurfaceState.Air;

    //         bool stickDown = inputDirection == Vector2.down;

    //         if (stickDown &&
    //         (
    //             !allowedToMoveInAir ||
    //             hasUsedAirDash ||
    //             state == MovementState.Ascending ||
    //             state == MovementState.Descending ||
    //             state == MovementState.AirDashing
    //         )
    //         )
    //         {
    //             DropPlayerStraightDown();
    //         }
    //     }

    //     // Apply hover logic
    //     // if (state == MovementState.Ascending && !hasTriggeredHover)
    //     // {
    //     //     hoverTimer += Time.fixedDeltaTime;
    //     //     if (hoverTimer >= hoverStartDelay)
    //     //     {
    //     //         if (CheckHoverEligibility())
    //     //         {
    //     //             state = MovementState.Hovering;
    //     //             rb.useGravity = false;
    //     //             rb.linearVelocity  = new Vector3(rb.linearVelocity .x, 0, rb.linearVelocity .z); // Neutralize vertical velocity
    //     //             hasTriggeredHover = true;
    //     //         }
    //     //     }
    //     // }

    //     // if (state == MovementState.Ascending && !hasTriggeredHover)
    //     // {
    //     //     TryStartHover();
    //     // }

    //     if (useCustomGravity) ApplyCustomGravity();

    //     if (!actionInProgress && !isInAir && rb.linearVelocity .magnitude < 0.1f)
    //     {
    //         state = MovementState.Idle;
    //     }
    // }
}
