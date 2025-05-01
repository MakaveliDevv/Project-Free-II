using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class JumpTest2 : MonoBehaviour
{
   #region Enums
    public enum MovementState { Idle, Charging, Ascending, Hovering, Descending, Dashing, AirDashing, WallDashing, Stucked, WallDescending }
    public enum Direction { North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest }
    public enum SurfaceState { Ground, LeftWall, RightWall, Ceiling, Air }

    public MovementState state;
    public SurfaceState currentSurfaceState = SurfaceState.Ground;
    #endregion

    #region ░░ INSPECTOR VARIABLES ░░

        #region ➤ Jump Settings
            [Header("Jump Settings")]
            [Tooltip("Force applied to execute a jump.")]
            [Range(0, 100f)] public float jumpForce = 0.5f;

            [Tooltip("Maximum horizontal distance achievable by a jump.")]
            public float maxJumpDistance = 10f;

            [Tooltip("Maximum vertical height achievable by a jump.")]
            public float jumpHeight = 3f;

            [Tooltip("Toggle between arced and straight-line diagonal jumps.")]
            public bool useArcForDiagonalJumps = true;

            [Tooltip("Controls how pronounced the arc is (affects arc-based jumps).")]
            [Range(1, 10)] public float arcMultiplier = 5f;
        #endregion

        #region ➤ Dash Settings
            [Header("Dash Settings")]
            [Tooltip("Force applied during a dash action.")]
            [Range(0, 100f)] public float dashForce = 20f;

            [Tooltip("Maximum horizontal distance achievable by a dash.")]
            public float maxDashDistance = 8f;
        #endregion

        #region ➤ Air Movement
            [Header("Air Movement")]
            [Tooltip("Determines if the player can move while airborne.")]
            public bool allowedToMoveInAir = false;

            [Tooltip("Force applied during an air dash.")]
            [Range(0, 100f)] public float airDashForce = 15f;

            [Tooltip("Cooldown period before another air dash can be performed.")]
            public float airDashCooldown = 0.5f;

            [Tooltip("Maximum distance achievable with an air dash.")]
            public float maxAirDashDistance;

            [Tooltip("Type of force applied generally for movement.")]
            public ForceMode mode;

            [Tooltip("Buffer time for transitions between movement states.")]
            public float stateBuffer = 0.25f;
        #endregion

        #region ➤ Hover Settings
            [Header("Hover Settings")]
            [Tooltip("Duration for which the player can hover in the air.")]
            public float hoverDuration = 0.5f;

            [Tooltip("Delay before the player begins to hover after ascending.")]
            public float hoverStartDelay = 0.1f;

            [Tooltip("Minimum height gain required to trigger hover.")]
            public float minHoverHeight = 2.0f;

            [Tooltip("Minimum horizontal distance for diagonal hover eligibility.")]
            public float minDiagonalDistance = 1.5f;

            [Tooltip("Enable wobble effect during hover.")]
            public bool useHoverWobble = true;

            [Tooltip("Amplitude of the vertical wobble during hover.")]
            public float hoverWobbleHeight = 0.2f;

            [Tooltip("Speed of the vertical wobble oscillation.")]
            public float hoverWobbleSpeed = 2f;
        #endregion

        #region ➤ Gravity Settings
            [Header("Gravity Settings")]
            [Tooltip("Strength of the custom gravity applied to the player.")]
            public float customGravityStrength = 20f;

            [Tooltip("Multiplier applied to gravity when the player is falling.")]
            public float fallMultiplier = 2.5f;

            [Tooltip("Multiplier applied when performing low jumps.")]
            public float lowJumpMultiplier = 2.0f;

            [Tooltip("Multiplier applied when dropping through stick input.")]
            public float dropMultiplier;

            [Tooltip("Determines whether to use custom gravity or Unity's default.")]
            public bool useCustomGravity = true;

            [Tooltip("If true, gravity is applied. If false, gravity is ignored.")]
            public bool toggleGravity = true;

            [Tooltip("Direction in which gravity is applied.")]
            public Vector3 gravityDirection = Vector3.down;

            [Tooltip("Maximum speed at which the player can fall.")]
            public float maxFallSpeed = 40f;
        #endregion

        #region ➤ Control Settings
            [Header("Control Settings")]
            [Tooltip("Maximum duration the jump/dash button can be held down.")]
            public float maxHoldTime = 1.5f;

            [Tooltip("Minimum input threshold to consider valid input.")]
            public float deadzone = 0.2f;
        #endregion

        #region ➤ Force Settings
            [Header("Force Settings")]
            [Tooltip("Type of force applied during a jump.")]
            public ForceMode jumpForceMode = ForceMode.Impulse;

            [Tooltip("Type of force applied during a dash.")]
            public ForceMode dashForceMode = ForceMode.Impulse;

            [Tooltip("Type of force applied during an air dash.")]
            public ForceMode airDashForceMode = ForceMode.Impulse;
        #endregion

        #region ➤ Direction & Wall Settings
            [Header("Directional Settings")]
            [Tooltip("Number of possible directional inputs.")]
            public int numberOfDirections = 8; // 8 = N, NE, E, SE, S, SW, W, NW

            [Tooltip("Length of visualized direction gizmos.")]
            public float gizmosLength;

            [Tooltip("Layer mask for detecting walls.")]
            public LayerMask wallLayer;

            [Tooltip("Distance checked to detect wall collisions.")]
            public float checkDistance;
        #endregion

    #endregion

    #region ░░ PRIVATE VARIABLES ░░

        #region ➤ Input
            // private Vector2 inputDirection = Vector2.zero;
            // private Vector2 lastSnappedDirection = Vector2.zero;
            private Vector2 lastSnappedDirection = Vector2.zero;
            private Vector3 moveDirection;
            private Vector3 predictedTargetPosition;
            private bool southButtonPressed;
        #endregion

        #region ➤ References
            private Rigidbody rb;
            private Transform camTransform;
        #endregion

        #region ➤ Input System
            public InputActionAsset inputActions;
            private InputAction leftAnalogStickInput;
            private InputAction southButtonInput;
        #endregion

        #region ➤ State Control
            private float holdTime;
            private float holdRatio;
            private float angle;
            private bool actionInProgress;
            private bool actionCompleted;
            private bool isAscending;
            private bool isDashing;
            private bool isFalling;
            private bool isEastDirection;
            private bool isWestDirection;
            private bool hasUsedAirDash;
            private bool hasAppliedForce;
            private bool stateChanged;
            private bool isApplyingCustomGravity;
            private bool isDiagonalJump;
            private bool isInAir = false;
            private bool fastFalling;
        #endregion

        #region ➤ Timers
            private float lastActionTime;
            private float lastContactTime;
            private float hoverTimer = 0f;
            private float hoverWobbleTimer = 0f;
            private float stateTimer = 0f;
            public float estimatedTimeThreshold = .25f;
            private const float NO_CONTACT_THRESHOLD = 0.2f;
        #endregion

        #region ➤ Movement
            // private Vector3 moveDirection;
            // private Vector3 predictedTargetPosition;
            private Vector3 originalHoverPosition;
            private float forceMagnitude;
            private float targetDistance;
            private float currentFallMultiplier;

        #endregion

        #region ➤ Direction Calculation
            private float[] allowedAngles;
        #endregion

        #region ➤ Debug & Gizmos
            private bool showPredictedSphere = false;
            private string currentPredictionMode = "None";
            private bool hasTriggeredHover = false;
        #endregion

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
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            toggleGravity = !toggleGravity;
            Debug.Log("Gravity Toggled: " + toggleGravity);
        }
        
        Vector2 inputDirection = leftAnalogStickInput.ReadValue<Vector2>();

        if (!actionInProgress && inputDirection.magnitude > deadzone)
        {
            Vector3 snapped = GetSnappedDirection(inputDirection);
            if (snapped != Vector3.zero)
            {
                lastSnappedDirection = new Vector2(snapped.x, snapped.y);
            }
        }

        if (!actionInProgress && southButtonPressed && lastSnappedDirection != Vector2.zero)
        {
            UpdatePredictedTargetPosition();
        }

        if (southButtonPressed && !actionInProgress)
        {
            holdTime += Time.deltaTime;
            if (state != MovementState.Charging) state = MovementState.Charging;

            if (holdTime >= maxHoldTime && lastSnappedDirection != Vector2.zero)
            {
                PerformAction();
            }
        }

        // Reset Charging state if we're not allowed to act mid-air
        // if (isInAir && state == MovementState.Charging && !allowedToMoveInAir)
        // {
        //     state = MovementState.Descending;
        //     holdTime = 0f;
        //     southButtonPressed = false;
        //     lastSnappedDirection = Vector2.zero;
        //     Debug.Log("🔻 Reset Charging – airborne & blocked from acting");
        // }


        if (state == MovementState.Hovering)
        {
            hoverTimer += Time.deltaTime;
            if (hoverTimer > hoverDuration)
            {
                ExitHover();
            }
            else if (useHoverWobble)
            {
                hoverWobbleTimer += Time.deltaTime;
                float wobbleOffset = Mathf.Sin(hoverWobbleTimer * hoverWobbleSpeed) * hoverWobbleHeight;
                Vector3 newPosition = originalHoverPosition + new Vector3(0f, wobbleOffset, 0f);
                rb.MovePosition(newPosition);
            }
        }

        if(isInAir && state == MovementState.Charging) 
        {
            rb.linearVelocity = Vector2.zero;
            toggleGravity = false;
        }
        else if(isInAir && state == MovementState.AirDashing) 
        {
            toggleGravity = false;
        }
        else 
        {
            toggleGravity = true;
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

        // Falling detection
        if (!isFalling && rb.linearVelocity.y < -0.5f && currentSurfaceState == SurfaceState.Ceiling)
        {
            isFalling = true;
        }
        if (isFalling && rb.linearVelocity.y >= -0.1f)
        {
            isFalling = false;
        }
    }

    void FixedUpdate()
    {
        isInAir = !IsCollidingWithSurface();

        HandleActionForces();
        HandleActionCompletion();

        if (actionInProgress && Time.time - lastActionTime > 2f)
        {
            Debug.LogWarning("Action timeout - forcing reset");
            ForceResetAllActions();
        }

        if (Time.time - lastContactTime > NO_CONTACT_THRESHOLD && currentSurfaceState == SurfaceState.Ceiling)
        {
            isFalling = true;
        }

        if (isInAir)
        {
            currentSurfaceState = SurfaceState.Air;

            if (leftAnalogStickInput.ReadValue<Vector2>() == Vector2.down &&
                (!allowedToMoveInAir || hasUsedAirDash || state == MovementState.Ascending ||
                state == MovementState.Descending || state == MovementState.AirDashing))
            {
                DropPlayerStraightDown();
            }
        }

        if (useCustomGravity && toggleGravity)
        {
            ApplyCustomGravity();
        }

        if (!actionInProgress && !isInAir && rb.linearVelocity.magnitude < 0.1f)
        {
            state = MovementState.Idle;
        }

        // Recovery fix: if damping is still active while not hovering, reset it
        if (state != MovementState.Hovering && rb.linearDamping != 0f)
        {
            rb.linearDamping = 0f;
            Debug.LogWarning("🛠 Damping reset — was lingering after hover.");
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
        // leftAnalogStickInput.started += OnStickStarted;
        // leftAnalogStickInput.performed += OnStickPerformed;
        // leftAnalogStickInput.canceled += OnStickCanceled;

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
        // leftAnalogStickInput.started -= OnStickStarted;
        // leftAnalogStickInput.performed -= OnStickPerformed;
        // leftAnalogStickInput.canceled -= OnStickCanceled;

        southButtonInput.started -= OnSouthButtonStarted;
        southButtonInput.performed -= OnSouthButtonPerformed;
        southButtonInput.canceled -= OnSouthButtonCanceled;
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
                rb.AddForce(moveDirection.normalized * forceMagnitude, mode);

                hasAppliedForce = true;

                Debug.Log($"🔼 Force applied: {moveDirection * forceMagnitude} (Mode: {mode})");
            }
        }
    }

    /// <summary>
    /// Checks and finalizes action completion based on player's position.
    /// Related to: PerformAction (action lifecycle completion).
    /// </summary>
    private void HandleActionCompletion()
    {
        if (!actionInProgress || actionCompleted) return;

        Vector3 actionVector = predictedTargetPosition - rb.position;
        float remainingDistance = actionVector.magnitude;
        float forwardProgress = Vector3.Dot(rb.linearVelocity.normalized, actionVector.normalized);
        float tolerance = Mathf.Max(0.15f, rb.linearVelocity.magnitude * Time.fixedDeltaTime);

        if (isDiagonalJump)
            tolerance *= 2.0f;

        bool hasReachedTarget = remainingDistance <= tolerance || forwardProgress < 0f;

        if (hasReachedTarget)
        {
            actionCompleted = true;

            TryStartHover();
            
            Invoke(nameof(ResetActionState), 0.1f); // delay reset so hover has time to trigger

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
        if (input.magnitude < deadzone) return Vector3.zero;

        if (isInAir) return new Vector3(input.x, input.y, 0f).normalized;

        float inputAngle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        inputAngle = (inputAngle + 360f) % 360f;

        float sectorSize = 360f / numberOfDirections;
        float closestAngle = Mathf.Round(inputAngle / sectorSize) * sectorSize;

        // Directly return Vector3 from snapped angle
        float rad = closestAngle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
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

        float travelDistance;

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

        // Vector3 snappedDir = ((Vector3)lastSnappedDirection).normalized;
        Vector3 snappedDir = new Vector3(lastSnappedDirection.x, lastSnappedDirection.y, 0f);
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
        if ((actionInProgress && !actionCompleted) || state == MovementState.Hovering) return;

        Vector3 gravityDir = DetermineGravityDirection();
        float gravityForce = customGravityStrength;

        float verticalVelocity = Vector3.Dot(rb.linearVelocity, gravityDir);
        float upwardVelocity = Vector3.Dot(rb.linearVelocity, -gravityDir);

        float jumpHeightSoFar = Vector3.Project(rb.position - jumpStartPosition, -gravityDir).magnitude;
        bool isDropping = leftAnalogStickInput.ReadValue<Vector2>() == Vector2.down;

        if (jumpHeightSoFar < minHoverHeight && upwardVelocity > 0.1f && !southButtonPressed)
        {
            // Low Jump - released early
            gravityForce *= lowJumpMultiplier;
        }
        else if (verticalVelocity > 0.1f)
        {
            // Normal Falling
            gravityForce *= fallMultiplier;

            if (isDropping)
            {
                gravityForce *= dropMultiplier;
                fastFalling = true;
            }
        }

        float currentFallSpeed = verticalVelocity;
        if (currentFallSpeed < maxFallSpeed)
        {
            rb.AddForce(gravityDir * gravityForce, ForceMode.Acceleration);
            isApplyingCustomGravity = true;
        }
    }

    // private void ApplyCustomGravity()
    // {
    //     // Skip gravity when an action is in progress
    //     if ((actionInProgress && !actionCompleted) || state == MovementState.Hovering) return;

    //     // Determine gravity direction based on surface state
    //     Vector3 currentGravityDir = DetermineGravityDirection();
        
    //     // Calculate gravity force based on velocity and state
    //     float gravityForce = customGravityStrength;
        
    //     // Apply stronger gravity when falling
    //     if (Vector3.Dot(rb.linearVelocity , currentGravityDir) > 0)
    //     {
    //         gravityForce *= currentFallMultiplier;
    //     }
    //     // Apply weaker gravity when jumping but button released early
    //     else if (Vector3.Dot(rb.linearVelocity , -currentGravityDir) > 0 && !southButtonPressed)
    //     {
    //         gravityForce *= lowJumpMultiplier;
    //     }
        
    //     // Apply the gravity force
    //     Vector3 gravityVector = currentGravityDir * gravityForce;
        
    //     // Check if we're exceeding max fall speed
    //     float currentFallSpeed = Vector3.Project(rb.linearVelocity , currentGravityDir).magnitude;
    //     if (currentFallSpeed < maxFallSpeed)
    //     {
    //         rb.AddForce(gravityVector, ForceMode.Acceleration);
    //         isApplyingCustomGravity = true;
    //     }
        
    //     // // Debug info
    //     // if (isApplyingCustomGravity)
    //     // {
    //     //     Debug.DrawRay(transform.position, gravityVector.normalized * 2f, Color.red);
    //     // }
    // }

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

    private void TryStartHover()
    {
        float totalTravelled = Vector3.Distance(jumpStartPosition, rb.position);
        if (totalTravelled < minHoverHeight)
        {
            Debug.Log("🛑 Hover skipped – movement too small");
            return;
        }

        Vector3 toTarget = predictedTargetPosition - rb.position;
        float forwardDot = Vector3.Dot(rb.linearVelocity.normalized, toTarget.normalized);
        float distanceToTarget = toTarget.magnitude;

        float hoverTriggerRadius = 2f;       // standard success zone
        float hoverForgivenessDistance = 4.0f;  // fallback overshoot buffer

        bool isCloseEnough = distanceToTarget <= hoverTriggerRadius;
        bool hasPassedTarget = forwardDot < 0f;
        bool isInForgivenessZone = hasPassedTarget && distanceToTarget <= hoverForgivenessDistance;

        if (!(isCloseEnough || isInForgivenessZone))
        {
            Debug.Log("❌ Hover skipped – not within range or overshoot buffer");
            return;
        }

        // ✅ Passed all checks – trigger hover
        state = MovementState.Hovering;
        rb.linearVelocity = Vector3.zero;
        rb.linearDamping = 5f;

        hoverTimer = 0f;
        hoverWobbleTimer = 0f;
        originalHoverPosition = rb.position;

        hasTriggeredHover = true;
        Debug.Log("🛸 Hover Started — Triggered by " + (isCloseEnough ? "target proximity" : "forgiveness zone"));
    }


    // private void TryStartHover()
    // {
    //     Vector3 gravityDir = DetermineGravityDirection();
    //     float jumpHeightSoFar = Vector3.Project(rb.position - jumpStartPosition, -gravityDir).magnitude;
    //     float distanceToTarget = Vector3.Distance(rb.position, predictedTargetPosition);

    //     if (jumpHeightSoFar < minHoverHeight)
    //     {
    //         Debug.Log("🛑 Hover skipped – not enough height gained (low jump)");
    //         return;
    //     }

    //     float totalDistanceMoved = Vector3.Distance(rb.position, jumpStartPosition);

    //     bool isFarEnough = totalDistanceMoved >= minHoverHeight;
    //     bool isCloseEnough = distanceToTarget <= 0.9f;

    //     if (isFarEnough && isCloseEnough)
    //     {
    //         state = MovementState.Hovering;
    //         rb.linearVelocity = Vector3.zero;
    //         rb.linearDamping = 5f;

    //         hoverTimer = 0f;
    //         hoverWobbleTimer = 0f;
    //         originalHoverPosition = rb.position;

    //         hasTriggeredHover = true;
    //         Debug.Log("🛸 Hover Started — Smooth Floating");
    //     }
    //     else
    //     {
    //         Debug.Log("❌ Hover failed – not far or close enough");

    //         if (state == MovementState.Ascending)
    //         {
    //             state = MovementState.Descending;
    //             Debug.Log("⬇️ Transitioned to Descending – Hover skipped");
    //         }
    //     }
    // }

    private void ExitHover()
    {
        state = MovementState.Descending;
        hoverTimer = 0f; 
        rb.linearDamping  = 0f; 

        hasTriggeredHover = false; 
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

        southButtonPressed = false;
        rb.linearVelocity = Vector3.zero; // Stop all movement
        lastSnappedDirection = Vector2.zero;
        isApplyingCustomGravity = false;
        showPredictedSphere = false;
        hasAppliedForce = false;
        rb.linearDamping = 0f; 
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
    #endregion

    #region Action Execution
    /// <summary>
    /// Executes the player’s intended jump, dash, or air-dash.
    /// Related to: SetupMovement, HandleActionForces.
    /// </summary>
    private Vector3 jumpStartPosition;

    private void PerformAction()
    {
        if (lastSnappedDirection == Vector2.zero || fastFalling)
            return;


        if (state == MovementState.Hovering)
        {
            // Fully exit hover
            rb.linearDamping = 0f;
            hoverTimer = 0f;
            hasTriggeredHover = false;
        }

        if (state == MovementState.Descending && hasTriggeredHover && hasUsedAirDash)
        {
            Debug.Log("⛔ Ignored input during descent after hover.");
            return;
        }

        // ✅ Always allow AIR DASH if airborne and not yet used
        if (isInAir && allowedToMoveInAir && !hasUsedAirDash)
        {
            jumpStartPosition = rb.position;

            state = MovementState.AirDashing;
            SetupMovement(maxAirDashDistance, jumpHeight, 1f, airDashForce, "AirDash");
            hasUsedAirDash = true;

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
            Debug.Log("⛔ Blocked: Can't act while airborne");
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

            // newDir = moveDirection.normalized;
            forceMagnitude = forcePower;

            // Apply smoothing
            moveDirection = Vector3.Slerp(rb.linearVelocity .normalized, moveDirection, 0.85f);
        }
        else // Jump
        {
            isAscending = true;
            isDashing = false;

            // Check if diagonal
            isDiagonalJump = Mathf.Abs(lastSnappedDirection.x) > 0 && Mathf.Abs(lastSnappedDirection.y) > 0;

            if (isDiagonalJump)
            {
                if(!useArcForDiagonalJumps) 
                {
                    Vector3 directionToTarget = (predictedTargetPosition - rb.position).normalized;
                    float distance = Vector3.Distance(rb.position, predictedTargetPosition);
                    float estimatedTime = estimatedTimeThreshold; // ⚠️ Tune this to feel right

                    moveDirection = directionToTarget;
                    forceMagnitude = distance / estimatedTime * forcePower;
                    Debug.DrawLine(rb.position, predictedTargetPosition, Color.magenta, 1.5f);

                    Debug.Log("🟧 Straight-Line Diagonal Jump to Exact Target");
                }
                else
                {
                    Vector3 displacement = predictedTargetPosition - rb.position;
                    float gravity = Mathf.Abs(Physics.gravity.y) * gravityMultiplier; // Or use your customGravityStrength
                    float heightDifference = displacement.y;
                    // float arcHeight = Mathf.Max(jumpHeight, heightDifference + 0.1f); // Ensure arcHeight > heightDifference
                    float arcHeight = Mathf.Max(jumpHeight, heightDifference + 0.1f) * (arcMultiplier / 5f);

                    // ✅ Guard against division by zero or invalid values
                    if (gravity <= 0f)
                    {
                        Debug.LogWarning("⛔ Gravity must be greater than zero for arc jump.");
                        return;
                    }

                    float timeToApex = Mathf.Sqrt(2f * arcHeight / gravity);
                    float timeToDescend = Mathf.Sqrt(2f * Mathf.Max(0.1f, arcHeight - heightDifference) / gravity);
                    float totalFlightTime = timeToApex + timeToDescend;

                    if (totalFlightTime <= 0f || float.IsNaN(totalFlightTime))
                    {
                        Debug.LogWarning("⚠️ Invalid total flight time.");
                        return;
                    }

                    Vector3 horizontalDisplacement = new Vector3(displacement.x, 0f, displacement.z);
                    float horizontalDistance = horizontalDisplacement.magnitude;

                    Vector3 horizontalDir = horizontalDisplacement.normalized;

                    float vx = horizontalDistance / totalFlightTime;
                    float vy = Mathf.Sqrt(2f * gravity * arcHeight);

                    Vector3 launchVelocity = horizontalDir * vx + Vector3.up * vy;

                    moveDirection = launchVelocity.normalized;
                    forceMagnitude = launchVelocity.magnitude * forcePower;

                    Debug.Log($"🟢 Arc Setup → Velocity: {launchVelocity}, Magnitude: {forceMagnitude}");

                    // // Smooth out any janky direction change
                    moveDirection = Vector3.Slerp(rb.linearVelocity .normalized, moveDirection, 0.9f);

                    Debug.Log("🌀 Arc-Based Jump");
                }
            }
        }

        Debug.Log($"{action} ➤ Optimized Direction: {moveDirection}, Force: {forceMagnitude}");
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

        Vector2 inputDirection = leftAnalogStickInput.ReadValue<Vector2>();
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

        Vector2 inputDirection = leftAnalogStickInput.ReadValue<Vector2>();
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
            // Vector3 snappedDir = ((Vector3)lastSnappedDirection).normalized;
            Vector3 snappedDir = new Vector3(lastSnappedDirection.x, lastSnappedDirection.y, 0f).normalized;
            
            if (targetDistance > 0f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, origin + snappedDir * targetDistance);
                DrawArrowHead(origin + snappedDir * targetDistance, snappedDir);
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
                // Vector3 snappedDir = ((Vector3)lastSnappedDirection).normalized;
                Vector3 snappedDir = new Vector3(lastSnappedDirection.x, lastSnappedDirection.y, 0f).normalized;

                float distance = maxJumpDistance * holdRatio;
                Gizmos.color = Color.cyan;
                DrawJumpArc(rb.position, snappedDir, distance);
            }
            else if (currentPredictionMode == "AirDash" && lastSnappedDirection != Vector2.zero)
            {
                // Vector3 snappedDir = ((Vector3)lastSnappedDirection).normalized;
                Vector3 snappedDir = new Vector3(lastSnappedDirection.x, lastSnappedDirection.y, 0f).normalized;

                float distance = maxDashDistance * holdRatio;
                Gizmos.color = new Color(0.8f, 0f, 1f, 0.6f);
                DrawDashedLine(rb.position, rb.position + snappedDir * distance, 0.3f);
            }
            else if (currentPredictionMode == "WallDashing" && lastSnappedDirection != Vector2.zero)
            {
                // Vector3 snappedDir = ((Vector3)lastSnappedDirection).normalized;
                Vector3 snappedDir = new Vector3(lastSnappedDirection.x, lastSnappedDirection.y, 0f).normalized;

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