using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class JumpTest2 : MonoBehaviour
{
    public enum MovementState { Idle, Charging, Ascending, Hovering, Descending, Dashing, AirDashing, WallDashing, Stucked, WallDescending }
    public MovementState state;

    #region Enums
    public enum Direction
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    public enum SurfaceState
    {
        Ground,
        LeftWall,
        RightWall,
        Ceiling,
        Air
    }
    #endregion

    #region Inspector Variables
    // Jump Settings
    [Header("Jump Settings")]
    [Tooltip("Force applied to execute a jump.")]
    [Range(0, 100f)]
    public float jumpForce = .5f;

    [Tooltip("Maximum horizontal distance achievable by a jump.")]
    public float maxJumpDistance = 10f;

    [Tooltip("Maximum vertical height achievable by a jump.")]
    public float jumpHeight = 3f;

    // Dash Settings
    [Header("Dash Settings")]
    [Tooltip("Force applied during a dash action.")]
    [Range(0, 100f)]
    public float dashForce = 20f;

    [Tooltip("Maximum horizontal distance achievable by a dash.")]
    public float maxDashDistance = 8f;

    // Control Settings
    [Header("Control Settings")]
    [Tooltip("Maximum duration the jump/dash button can be held down.")]
    public float maxHoldTime = 1.5f;

    [Tooltip("Minimum input threshold to consider valid input.")]
    public float deadzone = 0.2f;

    // Force Settings
    [Header("Force Settings")]
    [Tooltip("Type of force applied during a jump.")]
    public ForceMode jumpForceMode = ForceMode.Impulse;

    [Tooltip("Type of force applied during a dash.")]
    public ForceMode dashForceMode = ForceMode.Impulse;

    [Tooltip("Type of force applied during an air dash.")]
    public ForceMode airDashForceMode = ForceMode.Impulse;

    // Direction Settings
    [Header("Directional Settings")]
    [Tooltip("Number of possible directional inputs.")]
    public int numberOfDirections = 8; // 8 = N, NE, E, SE, S, SW, W, NW

    [Tooltip("Length of visualized direction gizmos.")]
    public float gizmosLength;

    [Tooltip("Current surface state the player is interacting with.")]
    public SurfaceState currentSurfaceState = SurfaceState.Ground;

    [Tooltip("Layer mask for detecting walls.")]
    public LayerMask wallLayer;

    [Tooltip("Distance checked to detect wall collisions.")]
    public float checkDistance;

    // Air Movement
    [Header("Air Movement")]
    [Tooltip("Determines if the player can move while airborne.")]
    public bool allowedToMoveInAir = false;
    private bool isInAir = false;

    [Tooltip("Force applied during an air dash.")]
    [Range(0, 100f)]
    public float airDashForce = 15f;

    [Tooltip("Cooldown period before another air dash can be performed.")]
    public float airDashCooldown = 0.5f;
    
    [Tooltip("Maximum distance achievable with an air dash.")]
    public float maxAirDashDistance;

    [Tooltip("Type of force applied generally for movement.")]
    public ForceMode mode;

    [Tooltip("Buffer time for transitions between movement states.")]
    public float stateBuffer = 0.25f;

    // Hover Settings
    [Header("Hover Settings")]
    [Tooltip("Duration for which the player can hover in the air.")]
    public float hoverDuration = 0.5f;

    [Tooltip("Delay before the player begins to hover after ascending.")]
    public float hoverStartDelay = 0.1f;
    private float hoverTimer = 0f;
    public float minHoverHeight = 2.0f;
    public float minDiagonalDistance = 1.5f;
    private bool hasTriggeredHover = false;

    private float jumpStartHeight;

    // Gravity Settings
    [Header("Gravity Settings")]
    [Tooltip("Strength of the custom gravity applied to the player.")]
    public float customGravityStrength = 20f;

    [Tooltip("Multiplier applied to gravity when the player is falling.")]
    public float fallMultiplier = 2.5f;
    private float currentFallMultiplier;

    [Tooltip("Multiplier applied when performing low jumps.")]
    public float lowJumpMultiplier = 2.0f;

    [Tooltip("Multiplier applied when dropping through stick input")]
    public float dropMultiplier;

    [Tooltip("Determines whether to use custom gravity or Unity's default.")]
    public bool useCustomGravity = true;

    [Tooltip("Direction in which gravity is applied.")]
    public Vector3 gravityDirection = Vector3.down;

    [Tooltip("Maximum speed at which the player can fall.")]
    public float maxFallSpeed = 40f;
    #endregion

    #region Private Variables
    // Input variables
    private Vector2 inputDirection = Vector2.zero;
    private Vector2 lastSnappedDirection = Vector2.zero;
    private Vector2 newDir = Vector2.zero;
    private Vector3 predictedTargetPosition;
    private bool southButtonPressed;
    private bool stickStarted = false;

    // Component references
    private Rigidbody rb;
    private Transform camTransform;
    
    // Action state
    private bool actionInProgress;
    private bool actionCompleted;
    private float holdTime;
    private float holdRatio;
    private float lastActionTime;
    
    // Movement state
    private float angle;
    private bool isEastDirection;
    private bool isWestDirection;
    private bool isAscending;
    private bool isDashing;
    private bool isFalling = false;
    private bool hasUsedAirDash = false;
    private float lastAirDashTime = 0f;
    private float lastContactTime;
    
    // Target values
    private Vector3 moveDirection;
    private float forceMagnitude;
    private float targetDistance;

    // Direction calculation
    private float[] allowedAngles;
    private bool isDiagonalJump;

    // Gravity
    private bool isApplyingCustomGravity = false;
    private bool hasAppliedForce = false;

    // Input system
    public InputActionAsset inputActions;
    private InputAction leftAnalogStickInput;
    private InputAction southButtonInput;

    // Timers and thresholds
    private const float NO_CONTACT_THRESHOLD = 0.2f;
    private float inputWaitTimer = 0f;
    private const float baseInputWaitTime = 0.05f;
    private float stateTimer = 0f;
    private bool stateChanged = false;

    private bool showPredictedSphere = false;
    private string currentPredictionMode = "None";
    private bool fastFalling;

    #endregion

    #region Unity Lifecycle Methods
    void Awake()
    {
        InitializeDirections();
        SetupInputActions();
        rb = GetComponent<Rigidbody>();
        camTransform = Camera.main != null ? Camera.main.transform : null;
    }

    void Start()
    {
        currentFallMultiplier = fallMultiplier;
    }

    void OnEnable()
    {
        RegisterInputCallbacks();
    }

    void OnDisable()
    {
        UnregisterInputCallbacks();
    }

    void Update()
    {
        HandleButtonHold();
        DetectFallingState();
        HandleStateTransitionBuffer();

        // Handle exiting hover state
        // if (state == MovementState.Hovering)
        // {
        //     hoverTimer += Time.deltaTime;
        //     if (hoverTimer > hoverDuration)
        //     {
        //         ExitHover();
        //     }
        // }

        // To contineously update the inputDirection sinc it bugs out sometimes
        // Need to find a way to do it differently and clean
        inputDirection = leftAnalogStickInput.ReadValue<Vector2>();

        if (!actionInProgress && inputDirection.magnitude > deadzone)
        {
            Vector2 snapped = GetSnappedDirection(inputDirection).normalized;
            if (snapped != Vector2.zero)
            {
                lastSnappedDirection = snapped;
            }
        }

        if (!actionInProgress && southButtonPressed && lastSnappedDirection != Vector2.zero)
        {
            UpdatePredictedTargetPosition();
        }

        if(state == MovementState.Charging) 
        {
            rb.useGravity = false;
        }

    }

    void FixedUpdate()
    {
        HandleActionForces();
        CheckAirState();
        HandleActionTimeout();
        CheckSurfaceContact();
        HandleActionCompletion();

        if(isInAir) 
        {
            currentSurfaceState = SurfaceState.Air;

            bool stickDown = inputDirection == Vector2.down;

            if (stickDown &&
            (
                !allowedToMoveInAir ||
                hasUsedAirDash ||
                state == MovementState.Ascending ||
                state == MovementState.Descending ||
                state == MovementState.AirDashing
            )
            )
            {
                DropPlayerStraightDown();
            }
        }

        // Apply hover logic
        // if (state == MovementState.Ascending && !hasTriggeredHover)
        // {
        //     hoverTimer += Time.fixedDeltaTime;
        //     if (hoverTimer >= hoverStartDelay)
        //     {
        //         if (CheckHoverEligibility())
        //         {
        //             state = MovementState.Hovering;
        //             rb.useGravity = false;
        //             rb.linearVelocity  = new Vector3(rb.linearVelocity .x, 0, rb.linearVelocity .z); // Neutralize vertical velocity
        //             hasTriggeredHover = true;
        //         }
        //     }
        // }

        // if (state == MovementState.Ascending && !hasTriggeredHover)
        // {
        //     TryStartHover();
        // }

        if (useCustomGravity) ApplyCustomGravity();

        if (!actionInProgress && !isInAir && rb.linearVelocity .magnitude < 0.1f)
        {
            state = MovementState.Idle;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleSurfaceState(collision);
        ResetAirState();

        if (currentSurfaceState == SurfaceState.Ground && !actionInProgress)
        {
            fastFalling = false;
            state = MovementState.Idle;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleSurfaceState(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        lastContactTime = Time.time;
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes directional snap angles for input.
    /// Related to: GetSnappedDirection (utilizes these angles).
    /// </summary>
    private void InitializeDirections()
    {
        allowedAngles = new float[numberOfDirections];
        float step = 360f / numberOfDirections;
        for (int i = 0; i < numberOfDirections; i++)
        {
            allowedAngles[i] = i * step;
        }
    }

    /// <summary>
    /// Configures input actions for joystick and button controls.
    /// Related to: RegisterInputCallbacks, UnregisterInputCallbacks (setup input bindings).
    /// </summary>
    private void SetupInputActions()
    {
        var map = inputActions.FindActionMap("Player");
        leftAnalogStickInput = map.FindAction("Movement");
        southButtonInput = map.FindAction("Jump");

        leftAnalogStickInput.Enable();
        southButtonInput.Enable();
    }

    /// <summary>
    /// Registers input callback methods to Unity's Input System.
    /// Related to: SetupInputActions (called after action configuration).
    /// </summary>
    private void RegisterInputCallbacks()
    {
        leftAnalogStickInput.started += OnStickStarted;
        leftAnalogStickInput.performed += OnStickPerformed;
        leftAnalogStickInput.canceled += OnStickCanceled;

        southButtonInput.started += OnSouthButtonStarted;
        southButtonInput.performed += OnSouthButtonPerformed;
        southButtonInput.canceled += OnSouthButtonCanceled;
    }

    /// <summary>
    /// Unregisters input callback methods to clean up.
    /// Related to: RegisterInputCallbacks (reverses action).
    /// </summary>
    private void UnregisterInputCallbacks()
    {
        leftAnalogStickInput.started -= OnStickStarted;
        leftAnalogStickInput.performed -= OnStickPerformed;
        leftAnalogStickInput.canceled -= OnStickCanceled;

        southButtonInput.started -= OnSouthButtonStarted;
        southButtonInput.performed -= OnSouthButtonPerformed;
        southButtonInput.canceled -= OnSouthButtonCanceled;
    }
    #endregion

    #region Update Handlers
    /// <summary>
    /// Handles button hold logic, auto-triggering actions if max hold time is exceeded.
    /// Related to: PerformAction (triggers action when conditions are met).
    /// </summary>
    private void HandleButtonHold()
    {
        if (southButtonPressed && !actionInProgress)
        {
            holdTime += Time.deltaTime;

            if (state != MovementState.Charging)
                state = MovementState.Charging;
            
            // Auto-trigger if max hold time is reached
            if (holdTime >= maxHoldTime && lastSnappedDirection != Vector2.zero)
            {
                PerformAction();
            }
        }
    }

    /// <summary>
    /// Detects and manages player's falling state.
    /// Related to: ApplyCustomGravity (gravity behavior based on falling).
    /// </summary>
    private void DetectFallingState()
    {
        if (!isFalling && rb.linearVelocity.y < -0.5f && currentSurfaceState == SurfaceState.Ceiling)
        {
            isFalling = true;
            Debug.Log("Player is falling from ceiling");
        }

        // Reset falling state when touching ground or other surface
        if (isFalling && rb.linearVelocity.y >= -0.1f)
        {
            isFalling = false;
        }
    }

    /// <summary>
    /// Manages buffer timing for state transitions.
    /// Related to: stateChanged flag (used to track transitions).
    /// </summary>
    private void HandleStateTransitionBuffer()
    {
        if(stateChanged) 
        {
            stateTimer += Time.deltaTime;
            if(stateTimer >= stateBuffer) 
            {
                stateChanged = false;
                stateTimer = 0f;
            }
        }
    }
    #endregion

    #region FixedUpdate Handlers
    /// <summary>
    /// Applies calculated forces for jump and dash actions.
    /// Related to: PerformAction, SetupMovement (executes set parameters).
    /// </summary>
    
    private void HandleActionForces()
    {
        if (!actionInProgress || actionCompleted) return;

        if (isAscending || isDashing)
        {
            if (!hasAppliedForce)
            {
                // Determine the force mode based on action type
                mode = isAscending ? (isInAir ? airDashForceMode : jumpForceMode) : dashForceMode;

                rb.linearVelocity  = Vector3.zero; 
                rb.AddForce(newDir * forceMagnitude, mode);

                hasAppliedForce = true;

                Debug.Log($"🔼 Force applied: {newDir * forceMagnitude} (Mode: {mode})");
            }
        }
    }

    // private void HandleActionForces()
    // {
    //     if (actionInProgress && !actionCompleted)
    //     {
    //         // rb.useGravity = false;

    //         if (isAscending || isDashing)
    //         {
    //             if (rb.linearVelocity.magnitude < 0.1f)
    //             {
    //                 // Determine the force mode based on action type
    //                 if(isAscending && isInAir) 
    //                 {
    //                     mode = airDashForceMode;
    //                 }
    //                 else if(isAscending) 
    //                 {
    //                     mode = jumpForceMode;
    //                 }
    //                 else if (isDashing) 
    //                 {
    //                     mode = dashForceMode;
    //                 }

    //                 // rb.AddForce(moveDirection * forceMagnitude, mode);
    //                 rb.AddForce(newDir * forceMagnitude, mode);

    //                 Debug.Log($"Applied {(isAscending ? "jump" : "dash")} force: {moveDirection * forceMagnitude}");
    //             }
    //         }
    //     }
    // }

    /// <summary>
    /// Checks and finalizes action completion based on player's position.
    /// Related to: PerformAction (action lifecycle completion).
    /// </summary>
    private void HandleActionCompletion()
    {
        if (!actionInProgress || actionCompleted) return;

        // Vector from start to predicted target
        Vector3 actionVector = predictedTargetPosition - rb.position;
        float remainingDistance = actionVector.magnitude;

        // Check if we've passed the target in the direction of movement
        float forwardProgress = Vector3.Dot(rb.linearVelocity .normalized, actionVector.normalized);

        // Diagonal boost factor
        float tolerance = Mathf.Max(0.15f, rb.linearVelocity .magnitude * Time.fixedDeltaTime);
        if (Mathf.Abs(lastSnappedDirection.x) > 0 && Mathf.Abs(lastSnappedDirection.y) > 0)
        {
            tolerance *= 2.0f; // Diagonal jumps need more tolerance
        }

        // If we're close OR we've passed the target directionally
        if (remainingDistance <= tolerance || forwardProgress < 0f)
        {
            rb.useGravity = true;

            actionCompleted = true;
            Invoke(nameof(ResetActionState), 0.1f);

            Debug.Log("✅ Action complete — reached or passed target.");
        }
    }

    /// <summary>
    /// Determines whether the player is airborne.
    /// Related to: CheckSurfaceContact (validates surface contact).
    /// </summary>
    private void CheckAirState()
    {
        isInAir = !IsCollidingWithSurface();
    }

    /// <summary>
    /// Handles forced reset when action times out.
    /// Related to: ForceResetAllActions (forced reset logic).
    /// </summary>
    private void HandleActionTimeout()
    {
        // Add a failsafe timer to prevent permanent stuck state
        if (actionInProgress && Time.time - lastActionTime > 2f)
        {
            Debug.LogWarning("Action timeout - forcing reset");
            ForceResetAllActions();
        }
    }

    /// <summary>
    /// Verifies continuous surface contact for correct player state.
    /// Related to: IsCollidingWithSurface (surface detection logic).
    /// </summary>
    private void CheckSurfaceContact()
    {
        // Check if we've lost contact with surfaces
        if (Time.time - lastContactTime > NO_CONTACT_THRESHOLD)
        {
            // If we're in ceiling state but not touching anything for a while, temporarily
            // disable actions until we land on something
            if (currentSurfaceState == SurfaceState.Ceiling)
            {
                isFalling = true;
            }
        }
    }

    /// <summary>
    /// Resets airborne state upon contact with surface.
    /// Related to: HandleSurfaceState (resets upon detecting new collision).
    /// </summary>
    private void ResetAirState()
    {
        // Reset air dash when touching any surface
        hasUsedAirDash = false;
        isInAir = false;
    }
    #endregion

    #region Surface Detection
    /// <summary>
    /// Detects collision with surrounding surfaces.
    /// Related to: HandleSurfaceState (utilizes collision data).
    /// </summary>
    private bool IsCollidingWithSurface()
    {
        // Using a small overlap sphere to check for collisions
        Collider[] colliders = Physics.OverlapSphere(
            rb.position, 
            GetComponent<Collider>().bounds.extents.y + 0.1f // Slightly larger than collider
        );
        
        // If we have any colliders other than ourselves, we're touching something
        return colliders.Length > 1; // > 1 because we'll detect our own collider
    }

    /// <summary>
    /// Updates player's surface state based on collision.
    /// Related to: CheckSurfaceContact, ResetAirState (collision-based state management).
    /// </summary>
    private void HandleSurfaceState(Collision collision)
    {
        lastContactTime = Time.time;
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
            // Always prioritize ground
            currentSurfaceState = SurfaceState.Ground;
        }
        else if (foundCeiling)
        {
            // Always prioritize ceiling second
            currentSurfaceState = SurfaceState.Ceiling;
        }
        else if ((foundWallLeft || foundWallRight) && currentSurfaceState == SurfaceState.Air 
                /*previousState != SurfaceState.Ground && previousState != SurfaceState.Ceiling*/)
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
        // Keep previous state if nothing else detected and still in contact
        else if (collision.contactCount == 0)
        {
            // No surface contact logic
            // You might want to handle this case differently
        }
        
        if (previousState != currentSurfaceState)
        {
            Debug.Log($"Surface State changed from {previousState} to {currentSurfaceState}");
            ForceResetAllActions();
            stateChanged = true;
        }

        if (isFalling)
        {
            state = MovementState.Descending;
        }
    }

    /// <summary>
    /// Detects collision with walls 
    /// Related to: HandleSurfaceState (part of collision handling logic).
    /// </summary>
    private GameObject CheckWallCollision(float checkDistance) 
    {
        Vector2 checkDirection = Vector2.zero;

        if(isEastDirection) 
        {
            checkDirection = Vector2.right;
        }
        else if(isWestDirection) 
        {
            checkDirection = Vector2.left;
        }

        Debug.DrawRay(rb.position, checkDirection * checkDistance, Color.red, 0.1f);

        if (Physics.Raycast(rb.position, checkDirection, out RaycastHit hit, checkDistance, wallLayer))
        {
            Debug.Log($"Collision with wall: {hit.transform.gameObject.name}");
            return hit.collider.gameObject;
        }
        
        return null;
    }
    #endregion

    #region Direction & Angle Calculations
    /// <summary>
    /// Determines major directional category based on input angle.
    /// Related to: GetSnappedDirection (directional categorization).
    /// </summary>
    private Direction GetMajorDirection(float angle)
    {
        if (angle >= 337.5f || angle < 22.5f)
            return Direction.East;
        else if (angle >= 22.5f && angle < 67.5f)
            return Direction.NorthEast;
        else if (angle >= 67.5f && angle < 112.5f)
            return Direction.North;
        else if (angle >= 112.5f && angle < 157.5f)
            return Direction.NorthWest;
        else if (angle >= 157.5f && angle < 202.5f)
            return Direction.West;
        else if (angle >= 202.5f && angle < 247.5f)
            return Direction.SouthWest;
        else if (angle >= 247.5f && angle < 292.5f)
            return Direction.South;
        else // (angle >= 292.5f && angle < 337.5f)
            return Direction.SouthEast;
    }

    /// <summary>
    /// Calculates valid jump angle range based on current surface.
    /// Related to: IsAngleWithinRange (angle validation).
    /// </summary>
    private (float minAngle, float maxAngle) GetAllowedJumpRange()
    {
        // If in air, return an impossible range to prevent jumping
        if (isInAir)
        {
            return (0f, 0f); // No valid jump angles
        }

        return currentSurfaceState switch
        {
            SurfaceState.Ground => (45f, 135f),     // Upward cone
            SurfaceState.LeftWall => (315f, 45f),   // Rightward
            SurfaceState.RightWall => (135f, 225f), // Leftward
            SurfaceState.Ceiling => (225f, 315f),   // Downward
            _ => (0f, 360f),                        // All directions
        };
    }

    /// <summary>
    /// Calculates valid dash angle range based on current surface.
    /// Related to: IsAngleWithinRange (angle validation).
    /// </summary>
    private (float minAngle, float maxAngle) GetAllowedDashRange()
    {
        // If in air and allowed to air dash, allow 360 degrees
        if (isInAir && allowedToMoveInAir && !hasUsedAirDash)
        {
            return (0f, 360f); // Full 360-degree range for air dash
        }

        return currentSurfaceState switch
        {
            SurfaceState.Ground => (0f, 180f),      // Left to Right (West to East)
            SurfaceState.LeftWall => (90f, 270f),   // Up and Down (North and South)
            SurfaceState.RightWall => (90f, 270f),  // Up and Down (North and South)
            SurfaceState.Ceiling => (0f, 180f),     // Left to Right (West to East)
            _ => (0f, 360f),                        // All directions
        };
    }
    
    /// <summary>
    /// Validates whether input angle is within allowable range.
    /// Related to: GetAllowedJumpRange, GetAllowedDashRange.
    /// </summary>
    private bool IsAngleWithinRange(float angle, float minAngle, float maxAngle)
    {
        if (minAngle < maxAngle)
            return angle >= minAngle && angle <= maxAngle;
        else
            return angle >= minAngle || angle <= maxAngle; // handle wrap-around (like 270–90)
    }

    /// <summary>
    /// Snaps joystick input direction to closest valid direction.
    /// Related to: InitializeDirections (uses initialized angles).
    /// </summary>
    private Vector3 GetSnappedDirection(Vector2 input)
    {
        if (input.magnitude < deadzone)
            return Vector3.zero;

        // When airborne, allow free movement (no snapping to allowed angles)
        if (isInAir)
        {
            return new Vector3(input.normalized.x, input.normalized.y, 0f);
        }

        // On ground, continue to snap to allowed angles
        Vector2 normalizedInput = input.normalized;
        float inputAngle = Mathf.Atan2(normalizedInput.y, normalizedInput.x) * Mathf.Rad2Deg;
        inputAngle = (inputAngle + 360f) % 360f;

        // Snap based on sectors instead of closest angle
        float sectorSize = 360f / allowedAngles.Length;
        float halfSector = sectorSize / 2f;

        foreach (float allowedAngle in allowedAngles)
        {
            float lowerBound = (allowedAngle - halfSector + 360f) % 360f;
            float upperBound = (allowedAngle + halfSector) % 360f;

            bool inSector = lowerBound < upperBound
                ? inputAngle >= lowerBound && inputAngle < upperBound
                : inputAngle >= lowerBound || inputAngle < upperBound;

            if (inSector)
            {
                Vector2 snapped2D = new Vector2(
                    Mathf.Cos(allowedAngle * Mathf.Deg2Rad),
                    Mathf.Sin(allowedAngle * Mathf.Deg2Rad)
                );
                return new Vector3(snapped2D.x, snapped2D.y, 0f);
            }
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Predicts target position based on current input and state.
    /// Related to: PerformAction, SetupMovement.
    /// </summary>
    private void UpdatePredictedTargetPosition()
    {
        if (lastSnappedDirection == Vector2.zero || !southButtonPressed)
        {
            showPredictedSphere = false;
            currentPredictionMode = "None";
            return;
        }

        holdRatio = Mathf.Clamp01(holdTime / maxHoldTime);
        if (holdRatio < 0.1f) holdRatio = 0.1f;

        currentPredictionMode = GetPredictedActionType(lastSnappedDirection);

        float travelDistance = 0f;

        switch (currentPredictionMode)
        {
            case "Jump":
                travelDistance = maxJumpDistance * holdRatio;
                break;
            case "Dash":
            case "AirDash":
                travelDistance = maxDashDistance * holdRatio;
                break;
            default:
                showPredictedSphere = false;
                return;
        }

        Vector3 snappedDir = ((Vector3)lastSnappedDirection).normalized;
        predictedTargetPosition = rb.position + snappedDir * travelDistance;
        showPredictedSphere = true;
    }
    #endregion

    #region Gravity Management
    /// <summary>
    /// Manages gravity effects on the player.
    /// Related to: DetermineGravityDirection.
    /// </summary>
    private void ApplyCustomGravity()
    {
        // Skip gravity when an action is in progress
        if ((actionInProgress && !actionCompleted) || state == MovementState.Hovering) return;

        // Determine gravity direction based on surface state
        Vector3 currentGravityDir = DetermineGravityDirection();
        
        // Calculate gravity force based on velocity and state
        float gravityForce = customGravityStrength;
        
        // Apply stronger gravity when falling
        if (Vector3.Dot(rb.linearVelocity , currentGravityDir) > 0)
        {
            gravityForce *= currentFallMultiplier;
        }
        // Apply weaker gravity when jumping but button released early
        else if (Vector3.Dot(rb.linearVelocity , -currentGravityDir) > 0 && !southButtonPressed)
        {
            gravityForce *= lowJumpMultiplier;
        }
        
        // Apply the gravity force
        Vector3 gravityVector = currentGravityDir * gravityForce;
        
        // Check if we're exceeding max fall speed
        float currentFallSpeed = Vector3.Project(rb.linearVelocity , currentGravityDir).magnitude;
        if (currentFallSpeed < maxFallSpeed)
        {
            rb.AddForce(gravityVector, ForceMode.Acceleration);
            isApplyingCustomGravity = true;
        }
        
        // // Debug info
        // if (isApplyingCustomGravity)
        // {
        //     Debug.DrawRay(transform.position, gravityVector.normalized * 2f, Color.red);
        // }
    }

    /// <summary>
    /// Determines correct gravity direction based on surface state.
    /// Related to: ApplyCustomGravity.
    /// </summary>
    private Vector3 DetermineGravityDirection()
    {
        // Default world gravity direction
        Vector3 gravityDir = gravityDirection.normalized;
        
        // Modify gravity direction based on surface state
        switch (currentSurfaceState)
        {
            case SurfaceState.Ground:
                // Standard down gravity when on ground
                gravityDir = Vector3.down;
                break;
                
            case SurfaceState.Ceiling:
                // Standard down gravity when on ceiling (to fall)
                gravityDir = Vector3.down;
                break;
                
            case SurfaceState.LeftWall:
                // Pull character toward left wall
                gravityDir = Vector3.right;
                break;
                
            case SurfaceState.RightWall:
                // Pull character toward right wall
                gravityDir = Vector3.left;
                break;
        }
        
        // If in mid-air and not touching any surface, use default gravity
        if (isInAir && Time.time - lastContactTime > NO_CONTACT_THRESHOLD)
        {
            gravityDir = gravityDirection.normalized;
        }
        
        return gravityDir;
    }

    // Check eligibility for hover
    // private bool CheckHoverEligibility()
    // {
    //     float height = rb.position.y - jumpStartHeight;
    //     bool isHighEnough = height > minHoverHeight;
    //     bool isFarEnough = Mathf.Abs(rb.position.x - (jumpStartHeight + minDiagonalDistance)) > 0; 
    //     return isHighEnough && isFarEnough;
    // }

    private void TryStartHover()
    {
        float height = rb.position.y - jumpStartHeight;
        bool isHighEnough = height > minHoverHeight;
        bool isFarEnough = Mathf.Abs(rb.position.x - (jumpStartHeight + minDiagonalDistance)) > 0; // adjust as needed

        if (isHighEnough && isFarEnough)
        {
            state = MovementState.Hovering;
            rb.useGravity = false;
            rb.linearVelocity  = new Vector3(rb.linearVelocity .x, 0, rb.linearVelocity .z); // stop vertical motion
            hoverTimer = 0f;
            hasTriggeredHover = true;
            Debug.Log("🛸 Entered Hover State");
        }
    }

    private void ExitHover()
    {
        state = MovementState.Descending;
        rb.useGravity = true;
        hoverTimer = 0f; // Reset hover timer
        hasTriggeredHover = false; // Reset hover trigger for next jump
        Debug.Log("⬇️ Exiting Hover – Starting to Descend");
    }

    private void DropPlayerStraightDown()
    {
        // if (rb.linearVelocity.y <= -0.5f) return; // Already falling fast

        fastFalling = true;
        // Reset horizontal velocity, keep only downward force
        Vector3 downward = Vector3.down * Mathf.Max(customGravityStrength, 10f);
        rb.linearVelocity = new Vector3(0f, -downward.magnitude * dropMultiplier, 0f);

        state = MovementState.Descending;
        Debug.Log("⬇️ Manual Drop Triggered — Falling straight down");
    }

    #endregion

    #region Action State Management
    /// <summary>
    /// Immediately resets all current actions.
    /// Related to: HandleActionTimeout, HandleSurfaceState.
    /// </summary>
    private void ForceResetAllActions()
    {
        actionInProgress = false;
        actionCompleted = false;
        isAscending = false;
        isDashing = false;
        isFalling = false; 
        holdTime = 0f;
        rb.linearVelocity = Vector3.zero; // Stop all movement
        
        // Clear any pending invokes
        CancelInvoke(nameof(ResetActionState));
        
        southButtonPressed = false;
        lastSnappedDirection = Vector2.zero;
        showPredictedSphere = false;
        hasAppliedForce = false;

        state = MovementState.Idle;
        
        Debug.Log("Forced complete action reset due to surface change");
    }

    /// <summary>
    /// Resets state after action completion.
    /// Related to: HandleActionCompletion.
    /// </summary>
    private void ResetActionState()
    {
        actionInProgress = false;
        actionCompleted = false;
        isAscending = false;
        isDashing = false;
        holdTime = 0f;

        isApplyingCustomGravity = false;
        showPredictedSphere = false;
        hasAppliedForce = false;
    }
    #endregion

    #region Input Handlers
    public void OnSouthButtonStarted(InputAction.CallbackContext ctx)
    {
        if (ctx.started && !actionInProgress) 
        {
            Debug.Log("South Button Started");
            southButtonPressed = true;
            holdTime = 0f;
            actionCompleted = false;
        }
    }

    public void OnSouthButtonPerformed(InputAction.CallbackContext ctx) 
    {
        // This is typically called when the button is fully pressed
    }

    public void OnSouthButtonCanceled(InputAction.CallbackContext ctx) 
    {
        if (ctx.canceled && southButtonPressed) 
        {
            Debug.Log("South Button Released");
            southButtonPressed = false;
            
            // If we have a valid direction, perform the action
            if (lastSnappedDirection != Vector2.zero && !actionInProgress)
            {
                PerformAction();
            }
        }
    }
    
    public void OnStickStarted(InputAction.CallbackContext ctx)
    {
        if(stickStarted) return;

        // Set a small timer
        inputWaitTimer = baseInputWaitTime;

        if (inputWaitTimer > 0f)
        {
            inputWaitTimer -= Time.deltaTime;
        }

        inputDirection = ctx.ReadValue<Vector2>();
    }

    public void OnStickPerformed(InputAction.CallbackContext ctx)
    {
        inputDirection = ctx.ReadValue<Vector2>();

        // Wait for the timer to finish before proceeding
        // if (inputWaitTimer > 0f)
        // {
        //     inputWaitTimer = 0f;
        //     return;
        // } 
        // else if(inputWaitTimer <= 0) 
        // {
            if (inputDirection.magnitude < deadzone) inputDirection = Vector2.zero;

            stickStarted = true;

            if (!actionInProgress && stickStarted)
            {
                // Vector2 inputDirection = ctx.ReadValue<Vector2>();
                lastSnappedDirection = GetSnappedDirection(inputDirection).normalized;

                if (lastSnappedDirection != Vector2.zero)
                {
                    Debug.Log($"Snapped Direction: {lastSnappedDirection}");
                }
      
                // Reset action state if we have a new direction
                if (lastSnappedDirection != Vector2.zero && actionCompleted)
                {
                    ResetActionState();
                }
            }
        // }
    }
    
    public void OnStickCanceled(InputAction.CallbackContext ctx)
    {
        if (!actionInProgress)
        {
            stickStarted = false;
        }
    }
    #endregion

    #region Action Execution
    /// <summary>
    /// Executes the player’s intended jump, dash, or air-dash.
    /// Related to: SetupMovement, HandleActionForces.
    /// </summary>
    
    private void PerformAction()
    {
        if (lastSnappedDirection == Vector2.zero || fastFalling)
            return;

        // ✅ Always allow AIR DASH if airborne and not yet used
        if (isInAir && allowedToMoveInAir && !hasUsedAirDash)
        {
            state = MovementState.AirDashing;
            SetupMovement(maxAirDashDistance, 0f, 1f, airDashForce, "AirDash");
            hasUsedAirDash = true;
            lastAirDashTime = Time.time;

            actionInProgress = true;
            hasAppliedForce = false;
            southButtonPressed = false;
            lastActionTime = Time.time;

            Debug.Log("🟣 Air Dash performed mid-air!");
            return;
        }

        // 🚫 Block ALL other actions unless player is touching a surface
        if (isInAir)
        {
            Debug.Log("⛔ Blocked: Can't act while airborne (except AirDash)");
            return;
        }

        // Clamp & calculate
        holdRatio = Mathf.Clamp01(holdTime / maxHoldTime);
        if (holdRatio < 0.1f) holdRatio = 0.1f;

        angle = Mathf.Atan2(lastSnappedDirection.y, lastSnappedDirection.x) * Mathf.Rad2Deg;
        angle = (angle + 360f) % 360f;
        Direction majorDirection = GetMajorDirection(angle);

        var (jumpMin, jumpMax) = GetAllowedJumpRange();
        var (dashMin, dashMax) = GetAllowedDashRange();

        bool isJumpAllowed = IsAngleWithinRange(angle, jumpMin, jumpMax);
        bool isDashAllowed = IsAngleWithinRange(angle, dashMin, dashMax);

        if (isDashAllowed &&
            (currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall) &&
            (majorDirection == Direction.North || majorDirection == Direction.South))
        {
            state = MovementState.WallDashing;
            isEastDirection = false;
            isWestDirection = false;
            SetupMovement(maxDashDistance, 0f, 1f, dashForce, "WallDash");
        }
        else if (isDashAllowed && (majorDirection == Direction.East || majorDirection == Direction.West))
        {
            state = MovementState.Dashing;
            isEastDirection = majorDirection == Direction.East;
            isWestDirection = majorDirection == Direction.West;
            SetupMovement(maxDashDistance, 0f, 1f, dashForce, "Dash");
        }
        else if (isJumpAllowed)
        {
            state = MovementState.Ascending;
            SetupMovement(maxJumpDistance, jumpHeight, 1f, jumpForce, "Jump");
        }
        else
        {
            Debug.LogWarning("❌ No valid action performed");
            return;
        }

        actionInProgress = true;
        hasAppliedForce = false;
        southButtonPressed = false;
        lastActionTime = Time.time;

        Debug.Log($"▶️ Action Started: {state}, Dir: {lastSnappedDirection}, Angle: {angle:F1}°");
    }

    /// <summary>
    /// Configures movement parameters for actions.
    /// Related to: PerformAction, HandleActionForces.
    /// </summary>
    
    private void SetupMovement(float maxTravelDistance, float forceHeight, float gravityMultiplier, float forcePower, string action)
    {
        targetDistance = maxTravelDistance * holdRatio;

        if (action == "Dash" || action == "WallDash" || action == "AirDash")
        {
            isAscending = false;
            isDashing = true;

            // Correct direction based on surface and current input
            if (currentSurfaceState == SurfaceState.Ground || currentSurfaceState == SurfaceState.Ceiling)
            {
                moveDirection = isEastDirection ? Vector3.right : Vector3.left;
            }
            else if (currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall)
            {
                moveDirection = lastSnappedDirection.y > 0 ? Vector3.up : Vector3.down;
            }
            else
            {
                moveDirection = new Vector3(lastSnappedDirection.x, lastSnappedDirection.y, 0f).normalized;
            }

            newDir = moveDirection.normalized;
            forceMagnitude = forcePower;

            // Apply smoothing
            newDir = Vector3.Slerp(rb.linearVelocity .normalized, newDir, 0.85f);
        }
        else // Jump
        {
            isAscending = true;
            isDashing = false;

            // --- Optimized Jump Trajectory ---
            float angleDegrees = 45f;
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            float gravity = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;

            float initialVelocity = Mathf.Sqrt(gravity * targetDistance / Mathf.Sin(2 * angleRadians));
            Vector3 horizontalDir = new Vector3(lastSnappedDirection.x, 0f, 0f).normalized;

            float vx = initialVelocity * Mathf.Cos(angleRadians);
            float vy = initialVelocity * Mathf.Sin(angleRadians);

            moveDirection = horizontalDir * vx;
            lastSnappedDirection.y = vy;

            newDir = new Vector3(moveDirection.x, lastSnappedDirection.y, 0f).normalized;
            forceMagnitude = forcePower;

            // Smooth out any janky direction change
            newDir = Vector3.Slerp(rb.linearVelocity .normalized, newDir, 0.9f);
        }

        Debug.Log($"{action} ➤ Optimized Direction: {newDir}, Force: {forceMagnitude}");
        showPredictedSphere = true;
    }

    private string GetPredictedActionType(Vector2 dir)
    {
        if (dir == Vector2.zero)
            return "None";

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        angle = (angle + 360f) % 360f;
        Direction majorDirection = GetMajorDirection(angle);

        (float jumpMin, float jumpMax) = GetAllowedJumpRange();
        (float dashMin, float dashMax) = GetAllowedDashRange();

        bool canJump = IsAngleWithinRange(angle, jumpMin, jumpMax);
        bool canDash = IsAngleWithinRange(angle, dashMin, dashMax);

        // Special case: air dash
        if (isInAir && allowedToMoveInAir && !hasUsedAirDash)
            return "AirDash";

        // Prioritize dash only for horizontal ground/ceiling movement
        if (canDash && (currentSurfaceState == SurfaceState.Ground || currentSurfaceState == SurfaceState.Ceiling))
        {
            if (majorDirection == Direction.East || majorDirection == Direction.West)
                return "Dash";
        }

        // Wall dash: vertical dash from wall surface
        if (canDash &&
            (currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall) &&
            (majorDirection == Direction.North || majorDirection == Direction.South))
        {
            return "WallDashing";
        }

        // Default to jump if allowed
        if (canJump)
            return "Jump";

        return "None";
    }
    #endregion
    
    #region Gizmos Visualization
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if(allowedAngles == null || allowedAngles.Length <= 0) return;

        // If in editor mode and not playing, initialize the angles for gizmo drawing
        if (!Application.isPlaying)
        {
            // Initialize allowedAngles for editor visualization
            InitializeAllowedAngles();
        }
        else
        {
            return; // If in play mode but array is invalid, return
        }
        
        Gizmos.color = Color.cyan;
        Vector3 origin = rb.position;

        foreach (float angle in allowedAngles)
        {
            Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.right; // rotate around Z axis to stay in X/Y
            Gizmos.DrawLine(origin, origin + dir * gizmosLength);
            DrawArrowHead(origin + dir * gizmosLength, dir);
        }

        // Optional: show last target
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + (Vector3)lastSnappedDirection * gizmosLength, 0.15f);

        if (lastSnappedDirection != Vector2.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position + (Vector3)lastSnappedDirection * gizmosLength, 0.15f);
        }

        Gizmos.color = Color.green;
        Vector3 inputDir = GetSnappedDirection(inputDirection);
        if (inputDir != Vector3.zero)
        {
            Gizmos.DrawLine(transform.position, transform.position + inputDir * gizmosLength);
        }
    }

    private void InitializeAllowedAngles()
    {
        allowedAngles = new float[numberOfDirections];
        float step = 360f / numberOfDirections;
        for (int i = 0; i < numberOfDirections; i++)
        {
            allowedAngles[i] = i * step;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

         Gizmos.color = Color.green;
        (float minAngle, float maxAngle) = GetAllowedJumpRange();
        Vector3 center = rb.position;
        DrawAngleArc(center, minAngle, maxAngle, 2f); // 2f is the radius

        Vector3 origin = rb.position;

        // 🟢 Green Arrow – Current stick aim direction
        Vector3 liveStickDir = GetSnappedDirection(inputDirection);
        if (liveStickDir != Vector3.zero)
        {
            Gizmos.color = Color.green;
            float previewLength = 1.5f;
            Gizmos.DrawLine(origin, origin + liveStickDir.normalized * previewLength);
            DrawArrowHead(origin + liveStickDir.normalized * previewLength, liveStickDir);
        }

        // 🔴 Red Arrow – Predicted move distance
        if ((southButtonPressed || holdTime > 0f) && lastSnappedDirection != Vector2.zero)
        {
            Vector3 snappedDir = ((Vector3)lastSnappedDirection).normalized;

            float travelDistance = 0f;
            if (currentPredictionMode == "Dash")
                travelDistance = maxDashDistance * holdRatio;
            else if (currentPredictionMode == "Jump")
                travelDistance = maxJumpDistance * holdRatio;

            if (travelDistance > 0f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, origin + snappedDir * travelDistance);
                DrawArrowHead(origin + snappedDir * travelDistance, snappedDir);
            }
        }

        // 🔵 Predicted Sphere – Actual target position
        if (showPredictedSphere && predictedTargetPosition != Vector3.zero)
        {
            Gizmos.color = currentPredictionMode switch
            {
                "Jump" => Color.blue,
                "Dash" => Color.red,
                "AirDash" => new Color(0.6f, 0f, 1f), // Purple-ish
                // "WallDashing" => Color.magenta,
                _ => Color.gray
            };
            Gizmos.DrawSphere(predictedTargetPosition, 0.2f);

        #if UNITY_EDITOR
            string label = $"{currentPredictionMode} → {Vector3.Distance(rb.position, predictedTargetPosition):F1}m";
            UnityEditor.Handles.Label(predictedTargetPosition + Vector3.up * 0.3f, label);
        #endif

            if (currentPredictionMode == "Jump" && lastSnappedDirection != Vector2.zero)
            {
                Vector3 snappedDir = ((Vector3)lastSnappedDirection).normalized;
                float distance = maxJumpDistance * holdRatio;
                Gizmos.color = Color.cyan;
                DrawJumpArc(rb.position, snappedDir, distance);
            }
            else if (currentPredictionMode == "AirDash" && lastSnappedDirection != Vector2.zero)
            {
                Vector3 snappedDir = ((Vector3)lastSnappedDirection).normalized;
                float distance = maxDashDistance * holdRatio;
                Gizmos.color = new Color(0.8f, 0f, 1f, 0.6f);
                DrawDashedLine(rb.position, rb.position + snappedDir * distance, 0.3f);
            }
            else if (currentPredictionMode == "WallDashing" && lastSnappedDirection != Vector2.zero)
            {
                Vector3 snappedDir = ((Vector3)lastSnappedDirection).normalized;
                float distance = maxDashDistance * holdRatio;
                Gizmos.color = Color.magenta;
                DrawDashedLine(rb.position, rb.position + snappedDir * distance, 0.25f);
            }

            if (actionInProgress && !actionCompleted)
            {
                Gizmos.color = Color.white;
                float debugAcceptableRadius = Mathf.Max(0.1f, rb.linearVelocity.magnitude * Time.fixedDeltaTime);
                Gizmos.DrawWireSphere(predictedTargetPosition, debugAcceptableRadius);
            }
        }

        // 🟦 Draw Hover Eligibility Box (min hover height & distance)
        // if (state == MovementState.Ascending && !hasTriggeredHover)
        // {
        //     Gizmos.color = Color.cyan;

        //     Vector3 start = new Vector3(jumpStartHeight + minDiagonalDistance, jumpStartHeight + minHoverHeight, 0);
        //     Vector3 newCenter = new Vector3(rb.position.x, jumpStartHeight + minHoverHeight / 2f, rb.position.z);
        //     Vector3 size = new Vector3(minDiagonalDistance * 2, minHoverHeight, 1f);

        //     Gizmos.DrawWireCube(newCenter, size);

        // #if UNITY_EDITOR
        //     UnityEditor.Handles.Label(newCenter + Vector3.up * 0.5f, "🛸 Hover Zone");
        // #endif
        // }
    }

    private void DrawAngleArc(Vector3 center, float startAngle, float endAngle, float radius)
    {
        int segments = 30;
        float angleStep = (endAngle > startAngle)
            ? (endAngle - startAngle) / segments
            : (360f - startAngle + endAngle) / segments;

        Vector3 previousPoint = center + (Vector3)AngleToVector2(startAngle) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = (startAngle + angleStep * i) % 360f;
            Vector3 nextPoint = center + (Vector3)AngleToVector2(angle) * radius;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        // Draw center lines
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center, center + (Vector3)AngleToVector2(startAngle) * radius);
        Gizmos.DrawLine(center, center + (Vector3)AngleToVector2(endAngle) * radius);
    }

    private Vector2 AngleToVector2(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
    #endif
    #endregion

    private void DrawArrowHead(Vector3 position, Vector3 direction)
    {
        float arrowHeadAngle = 20.0f;
        float arrowHeadLength = 0.3f;

        Vector3 right = Quaternion.AngleAxis(180 + arrowHeadAngle, Vector3.forward) * direction.normalized;
        Vector3 left = Quaternion.AngleAxis(180 - arrowHeadAngle, Vector3.forward) * direction.normalized;

        Gizmos.DrawLine(position, position + right * arrowHeadLength);
        Gizmos.DrawLine(position, position + left * arrowHeadLength);
    }

    private void DrawJumpArc(Vector3 origin, Vector3 direction, float distance, int steps = 20)
    {
        _ = Mathf.Abs(Physics.gravity.y);
        float jumpForceLocal = jumpForce;
        float totalTime = 2f * jumpHeight / jumpForceLocal;

        Vector3 prevPoint = origin;

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps * totalTime;
            Vector3 point = origin + direction * (distance * (t / totalTime));
            point.y += jumpHeight * 4 * t / totalTime * (1 - t / totalTime); // Parabola approximation

            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }

    private void DrawDashedLine(Vector3 start, Vector3 end, float dashLength)
    {
        float distance = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;

        int dashCount = Mathf.CeilToInt(distance / dashLength);

        for (int i = 0; i < dashCount; i += 2)
        {
            Vector3 segmentStart = start + direction * (i * dashLength);
            Vector3 segmentEnd = start + direction * (Mathf.Min(i + 1, dashCount) * dashLength);
            Gizmos.DrawLine(segmentStart, segmentEnd);
        }
    }
}