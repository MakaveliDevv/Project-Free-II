using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class MovementSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC VARIABLES
    // ─────────────────────────────────────────────────────────────────────────

    // ─ Movement Enums
    public enum MovementState { Idle, Charging, Jumping, WallJump, Hovering, Descending, Dashing, AirDashing, WallDashing, Stucked, WallDescending, NOTHING }
    public enum SurfaceState { Ground, LeftWall, RightWall, Ceiling, Air }
    public enum GravityDirection { Down, Up, Left, Right }

    // ─ Movement State
    [Header("Movement State")]
    [Tooltip("Current movement state of the character")]
    public MovementState movementState = MovementState.Idle;

    [Tooltip("Surface the character is currently on")]
    public SurfaceState currentSurfaceState = SurfaceState.Ground;

    // ─ Input Settings
    [Header("Input Settings")]
    [Tooltip("Reference to the InputActionAsset defining stick and button actions")]
    public InputActionAsset inputActions;

    [Tooltip("If true, bypasses Unity’s smoothing to use raw input values from inputActions")]
    public bool useRawInput = true;

    // ─ Direction Snapping
    [Header("Direction Snapping")]
    [Tooltip("Enable snapping of stick input to discrete directions; if false, full analog input is used")]
    public bool snapDirectionsEnabled = false;

    [Tooltip("Number of directions to snap to when snapDirectionsEnabled is true; used in angle quantization")]
    public int directionCount = 16;

    // ─ Stuck Mechanics
    [Header("Stuck Mechanics")]
    [Tooltip("Time in seconds the character remains stuck when hitting a wall")]
    public float stuckDurationWall = 1.0f;

    [Tooltip("Time in seconds the character remains stuck when hitting the ceiling")]
    public float stuckDurationCeiling = 1.5f;

    [Tooltip("Cooldown time after being stuck before stuck state can trigger again; ensures brief recovery")]
    public float stuckCooldownDuration = 1f;

    // ─ Display Settings
    [Header("Display Settings")]
    [Tooltip("Toggle the rendering of direction labels in gizmos; if false, useCardinalLabels is ignored")]
    public bool showDirectionLabels = true;

    [Tooltip("When true, uses only N/E/S/W labels instead of full 16 directions; requires showDirectionLabels")]
    public bool useCardinalLabels = true;

    // ─ Jump & Dash Parameters
    [Header("Jump & Dash Parameters")]
    [Tooltip("Upward force applied when initiating a jump")]
    public float jumpForce = 5f;

    [Tooltip("Maximum horizontal distance allowed during a jump; used to validate jump targets")]
    public float maxJumpDistance = 5f;

    [Tooltip("Speed threshold below which landing is considered safe; used with jumpForceMode")]
    public float maxJumpSpeed = 10f;

    [Tooltip("A safeguard when moving too fast")]
    public float bounceSpeed = 5f;

    [Tooltip("Force applied when starting a dash; used with dashForceMode")]
    public float dashForce = 5f;

    [Tooltip("Maximum distance allowed for a dash; used to calculate dash endpoints")]
    public float maxDashDistance = 5f;

    [Tooltip("Force applied when starting an air dash; used with airDashForceMode")]
    public float airDashForce = 5f;

    [Tooltip("Maximum distance allowed for a air dash; used to calculate air dash endpoints")]
    public float maxAirDashDistance = 5f;

    [Tooltip("Default linear damping applied to the Rigidbody when no jump or dash is active")]
    public float defaultDamping = 0f;

    // ─ Force Modes
    [Header("Force Modes")]
    [Tooltip("ForceMode used for standard movement forces")]
    public ForceMode movementForceMode;

    [Tooltip("ForceMode applied to jumpForce; defaults to VelocityChange for instant velocity change")]
    public ForceMode jumpForceMode = ForceMode.VelocityChange;

    [Tooltip("ForceMode applied to dashForce; defaults to Impulse for a quick burst")]
    public ForceMode dashForceMode = ForceMode.Impulse;

    [Tooltip("ForceMode applied to airDashForce; defaults to Impulse for a quick burst")]
    public ForceMode airDashForceMode = ForceMode.Impulse;

    // ─ Gravity Settings
    [Header("Gravity Settings")]
    [Tooltip("Magnitude of gravitational pull applied each physics frame")]
    public float gravityStrength = 9.81f;
    public bool useDynamicGravityStrenght = false;

    [Tooltip("Percentage of gravityStrength applied when on walls; reduces slip along walls")]
    public int wallGravityPercent = 10;

    [Tooltip("Vector direction of gravity; set automatically based on gravityDirection* enums")]
    public Vector3 gravityDir = Vector3.down;

    [Tooltip("GravityDirection enum used when on ground; updates gravityDir accordingly")]
    public GravityDirection gravityDirectionGround = GravityDirection.Down;

    [Tooltip("GravityDirection enum used when on ceiling; updates gravityDir accordingly")]
    public GravityDirection gravityDirectionCeiling = GravityDirection.Down;

    [Tooltip("GravityDirection enum used when on left wall; updates gravityDir accordingly")]
    public GravityDirection gravityDirectionLeftWall = GravityDirection.Right;
    
    [Tooltip("GravityDirection enum used when on right wall; updates gravityDir accordingly")]
    public GravityDirection gravityDirectionRightWall = GravityDirection.Left;

    // ─ Jump / Fall Multipliers
    [Header("Jump / Fall Multipliers")]
    [Tooltip("Gravity multiplier when performing a low jump (button released early); increases gravity to shorten jump")]
    public float lowJumpMultiplier = 4.0f;

    [Tooltip("Gravity multiplier when falling normally; makes descent faster or slower")]
    public float fallMultiplier = 1f;

    [Tooltip("Gravity multiplier for a fast-drop action when in the air;")]
    public float defaultDropMultiplier = 2f;

    [Tooltip("Gravity multiplier for a fast-drop action when stucked on the wall;")]
    public float wallDropMultiplier = 2f;

    [Tooltip("Maximum allowed downward speed when falling; caps fall velocity")]
    [Range(1f, 100f)]
    public float maxFallSpeed = 40f;

    [Header("Drop Settings")]
    [Tooltip("Half‐angle (in degrees) around straight down in which a stick flick counts as a drop")]
    [Range(0f, 45f)]
    public float dropAngleTolerance = 15f;

    [Tooltip("ForceMode applied when dropMultiplier is active; defaults to Impulse")]
    public ForceMode dropForceMode = ForceMode.Impulse;

    [Tooltip("ForceMode applied when fallMultiplier is active; defaults to Acceleration")]
    public ForceMode fallForceMode = ForceMode.Acceleration;

    // ─ Hover Settings
    [Header("Hover Settings")]
    [Tooltip("Enable vertical wobble effect during hover")]
    public bool useHoverWobble = true;

    [Tooltip("Speed of vertical wobble when hovering; only used if useHoverWobble is true")]
    public float hoverWobbleSpeed = 2f;

    [Tooltip("Height amplitude of vertical wobble during hover; only used if useHoverWobble is true")]
    public float hoverWobbleHeight = 0.2f;

    [Tooltip("Delay before allowing hover")]
    public float hoverStartDelay = 0.1f;

    [Tooltip("Fade-in factor for wobble effect at hover start; between 0 (no fade) and 1 (full fade)")]
    public float wobbleFadeInFactor = 0.25f;

    [Tooltip("Radius around calculated flight target in which hover activates; ensures hover only near peak")]
    public float hoverActivationRadius = 1.5f;

    [Tooltip("Total time allowed for hover state before forcing descent")]
    public float hoverDuration = 2f;

    [Tooltip("Minimum height above ground required to initiate hover; prevents hover too close to surface")]
    public float minHoverHeight = 2.0f;

    [Tooltip("Minimum linear drag applied during hover; smooths out motion")]
    public float minHoverLinearDamping = 0f;

    [Tooltip("Linear drag applied while hovering to dampen movement")]
    public float hoverLinearDamping = 5f;

    // ─ Surface Detection
    [Header("Surface Detection")]
    [Tooltip("Distance to check below character for ground proximity; used in raycasts")]
    public float groundProximityCheckDistance = 1.0f;

    [Tooltip("Minimum time the character must be airborne before allowing Idle state; avoids flicker")]
    public float minAirborneTimeBeforeIdle = 0.15f;

    [Tooltip("Raycast distance threshold to detect any surface; used for landing and sticking")]
    public float distanceToSurfaceThreshold = 0.05f;

    [Tooltip("Runtime-adjustable check distance, initialized from distanceToSurfaceThreshold")]
    public float checkDistance;
    [Tooltip("LayerMask defining which layers count as surfaces for detection raycasts")]
    public LayerMask surfaceLayer;

    // ─ Input Hold Timing
    [Header("Input Hold Timing")]
    [Tooltip("Minimum duration the stick must be held before registering a direction")]
    public float minStickHoldTime = 0.1f;

    [Tooltip("Minimum stick tilt magnitude to consider as valid input")]
    public float minStickMagnitude = 0.2f;

    [Tooltip("Minimum duration a button must be held to register a press")]
    public float minButtonPressTime = 0.1f;

    [Tooltip("Maximum duration to hold a button for charge actions; used in HoldRatio")]
    public float maxHoldTime = 0.5f;

    // ─ Gizmo Visualization
    [Header("Gizmo Visualization")]
    [Tooltip("Overall scale factor for all debug gizmos")]
    public float gizmoScale = 2f;

    [Tooltip("Length of directional lines drawn for stick input in gizmos")]
    public float directionLineLength = 1.5f;

    [Tooltip("Color of the base direction line when no action is active")]
    public Color baseDirectionColor = Color.blue;

    [Tooltip("Color indicating valid jump directions in gizmos")]
    public Color allowedJumpColor = Color.green;

    [Tooltip("Color used for dash direction lines in gizmos")]
    public Color dashDirectionColor = Color.cyan;

    [Tooltip("Color used to show snapped input direction when snapping is enabled")]
    public Color snappedInputColor = Color.yellow;

    [Tooltip("Color marking the jump target point in gizmos")]
    public Color jumpTargetColor = Color.red;

    [Tooltip("Color marking the landing point in gizmos")]
    public Color landingPointColor = Color.green;

    [Tooltip("Color of the ground check ray in gizmos")]
    public Color groundCheckDistanceColor;

    // ─ Miscellaneous
    [Header("Miscellaneous")]
    [Tooltip("Radius around a target position to consider arrival complete")]
    public float arrivalRadius = 0.05f;

    // [Tooltip("Buffer time between state transitions to prevent rapid toggling")]
    // public float stateBuffer = 0.25f;

    [Tooltip("Interpolation factor (0–1) used when smoothing movement toward a target")]
    public float lerpAmount = 0.85f;

    [Tooltip("When false, LateUpdate will not force Z=0 (allows small Z offsets)")]
    public bool enableZLock = true;

    [Tooltip("If true, uses InputSystem action callbacks to apply forces instead of manual polling")]
    public bool useHandleActionForces = true;

    [Tooltip("Velocity magnitude threshold above which the object is considered 'moving'")]
    public float isMovingThreshold = 0.2f;

    [Tooltip("Exponent applied to input magnitude for custom response curves; shapes force application")]
    public float forceCurveExponent = 1.0f;
    
    [Tooltip("If true, player can perform an air dash")]
    public bool allowAirDash = false;
    public float landingBuffer = 0.1f;

    [Header("Test")]
    public float fallTimer = .25f;
    public bool useAutoHover = false;
    private bool isTimerRunning = false;

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC HIDDEN VARIABLES
    // ─────────────────────────────────────────────────────────────────────────

    // ─ Movement Flags
    // ground jump
    [HideInInspector] public bool isStraightJump = false;
    [HideInInspector] public bool isDiagonalJumpRight = false;

    // air dash

    // To right
    [HideInInspector] public bool isVerticalAirDash = false;
    [HideInInspector] public bool isHorizontalAirDash = false;

    [HideInInspector] public bool isRightAirDash = false;
    
    [HideInInspector] public bool isRightDiagonalAirDash = false;
    [HideInInspector] public bool isAirDashAscend = false;


    // wall jump
    [HideInInspector] public bool isWallJumpRight = false;
    [HideInInspector] public bool isWallJumpAscend = false;

    // ─ Movement Calculations
    [HideInInspector] public Vector2 snappedDir = Vector2.zero;
    [HideInInspector] public Rigidbody rb;

    // ─ Surface Memory
    [HideInInspector] public GameObject lastSurfaceObject;

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE VARIABLES
    // ─────────────────────────────────────────────────────────────────────────

    // ─ Movement Flags
    private bool isJumping = false;
    private bool isWallJumping = false;
    private bool isDashing = false;
    private bool isAirDashing = false;
    private bool allowedToMove = false;
    private bool isInAir = false;
    private bool fastFalling = false;
    private bool isDropping = false;
    private bool hasBurstDropped = false;
    private bool prevStickDownDrop = false;
    // private bool stateChanged = false;
    private bool actionInProgress = false;
    private bool hasTriggeredHover = false;
    private bool hasAppliedForce = false;
    private bool hasReachedTarget = false;
    private bool isMoving = false;
    private bool isStuckFrozen = false;
    private bool isLandingBuffered = false;
    private bool hasBounced = false;
    private bool buttonPressedLongEnough = false;

    // ─ Timers & Counters
    // private float stateTimer = 0f;
    private float lastContactTime;
    private float surfaceContactTime; 
    private float stuckTimer = 0f;
    private float hoverTimer = 0f;
    private float hoverWobbleTimer = 0f;
    private float buttonHoldTimer = 0f;
    

    // ─ Input Tracking & Actions
    private string currentAction = "";
    private string fetchedAction = "";
    private InputAction leftStick;
    private InputAction southButton;
    private Vector2 leftStickInput = Vector2.zero;
    private bool southButtonPressed;
    private bool leftStickMovement = false;
    private bool actionReady = true;

    // ─ Physics & Gravity State
    private float initialGravityStrength = 0;
    private float wallDescendingGravityStrength = 0f;
    private Vector3 ConvertToVector(GravityDirection dir)
    {
        return dir switch
        {
            GravityDirection.Down => Vector3.down,
            GravityDirection.Up => Vector3.up,
            GravityDirection.Left => Vector3.left,
            GravityDirection.Right => Vector3.right,
            _ => Vector3.down
        };
    }

    // ─ Movement Calculations
    private float targetDistance = 0f;
    private float forceMagnitude = 0f;
    private readonly float TravelEpsilon = .1f;

    private readonly Dictionary<SurfaceState, string[]> allowedMoveLabels = new()
    {
        { SurfaceState.Ground, new[] { "W", "WNW", "NW", "NNW", "N", "NNE", "NE", "ENE", "E" } },
        { SurfaceState.Ceiling, new[] { "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W" } },
        { SurfaceState.LeftWall, new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S" } },
        { SurfaceState.RightWall, new[] { "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW", "N" } },
        { SurfaceState.Air, new[] {
            "E", "ENE", "NE", "NNE", "N", "NNW", "NW", "WNW",
            "W", "WSW", "SW", "SSW", "S", "SSE", "SE", "ESE" }
        }
    };
    

    private Vector3 jumpStartPos = Vector3.zero;
    private Vector3 dashStartPos = Vector3.zero;
    private Vector3 airDashStartPos = Vector3.zero;

    // ─ References
    private Collider col;

    private Vector3 startPos;
    private Vector3 originalHoverPosition;
    private Vector3 predictedTargetPoint;

    // ─ Surface Memory
    private GameObject nearbySurface = null;
    private float lastSurfaceCheckTime;
    private const float surfaceMemoryDuration = 0.2f;

    // ─ Label Mapping
    private Dictionary<string, float> labelToAngle;

    // ─ Computed Properties
    private float HoldRatio => Mathf.Clamp01(buttonHoldTimer / maxHoldTime);

    // ─ Constants
    private const float NO_CONTACT_THRESHOLD = 0.2f;

    // ─ Miscellaneous
    private SurfaceState lastWallSide;
    private Coroutine landingResetCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE METHODS
    // ─────────────────────────────────────────────────────────────────────────
    #region UNITY LIFECYCLE
    void Awake()
    {
        InputSystem.settings.maxEventBytesPerUpdate = 0;

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        SetupInputActions();
        BuildLabelToAngleMap();
    }

    void Start()
    {
        initialGravityStrength = gravityStrength;
    }

    void OnEnable()
    {
        RegisterInputCallbacks();
    }

    void OnDisable()
    {
        UnregisterInputCallbacks();
    }

    private void OnValidate()
    {
        wallDescendingGravityStrength = gravityStrength * (wallGravityPercent * 0.1f);
    }
    
    private IEnumerator Timer(float timer) 
    {
        isTimerRunning = true;
        yield return new WaitForSeconds(timer);
        isTimerRunning = false;
        yield break;
    }

    void Update()
    {
        LeftAnalogStickInput();
        FetchActionType();

        if (southButtonPressed && buttonHoldTimer >= minButtonPressTime && !buttonPressedLongEnough)
        {
            buttonPressedLongEnough = true;
            if (movementState != MovementState.Charging && ActionInputDetected() && allowedToMove) { movementState = MovementState.Charging; }
        }
        
        if(movementState == MovementState.Jumping || movementState == MovementState.WallJump || movementState == MovementState.Dashing 
            || movementState == MovementState.AirDashing) { CheckArrivalAtTarget(); } 

        if(!useAutoHover && movementState == MovementState.Jumping || 
            movementState == MovementState.WallJump || 
            movementState == MovementState.AirDashing) 
            {

                if(hasReachedTarget)
                {                  
                    movementState = MovementState.NOTHING;

                    Debug.Log("Player reached target point");
                    gravityStrength = gravityStrength / 100f * 10f;
                    StartCoroutine(Timer(fallTimer));
                    ResetActionState();
                    ResetPhysicsSettings(false, true);

                    if(isTimerRunning) 
                    {
                        Debug.Log("Timer running..."); 
                        return; 
                    } 
                    
                    movementState = MovementState.Descending;
                
                }
            }
            else if(movementState == MovementState.Dashing) 
            {
                float traveled = Vector3.Distance(rb.position, dashStartPos);

                if(hasReachedTarget) 
                {
                    ResetActionState();
                    ResetPhysicsSettings(true, true);
                    movementState = MovementState.Idle;
                }
                else if(!hasReachedTarget && traveled > TravelEpsilon && rb.linearVelocity.magnitude <= 0.01f) 
                {
                    ResetActionState();
                    ResetPhysicsSettings(true, true);
                    movementState = MovementState.Idle;
                }
            }

        // if (stateChanged)
        // {
        //     stateTimer += Time.deltaTime;
        //     if (stateTimer >= stateBuffer)
        //     {
        //         stateChanged = false;
        //         stateTimer = 0f;
        //     }
        // }

        if (isInAir) { currentSurfaceState = SurfaceState.Air; }

        if (southButtonPressed)
        {
            allowAirDash = true;

            buttonHoldTimer += Time.deltaTime;
            if (actionReady && !actionInProgress && buttonHoldTimer >= maxHoldTime && buttonPressedLongEnough)
            {
                if(currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall) 
                {
                    fetchedAction = "WallJump";
                    allowedToMove = true;
                }

                PerformMovementAction();
                actionReady = false;
            }
        }
        
        switch (movementState)
        {
            case MovementState.Idle:
                currentSurfaceState = SurfaceState.Ground;
                hasTriggeredHover = false;
                break;
            case MovementState.Descending:
                hasReachedTarget = false;
                actionInProgress = false;
                break;
            case MovementState.WallDescending:
                actionInProgress = false;
                gravityStrength = wallDescendingGravityStrength;
                break;
            case MovementState.Hovering:
                isJumping = false;
                break;
            case MovementState.Stucked:
                actionInProgress = false;
                allowAirDash = false;
                break;
        }
    }

    void LateUpdate()
    {
        if(enableZLock) 
        {
            Vector3 pos = rb.position;
            pos.z = 0;
            rb.position = pos;
        }
    }

    void FixedUpdate()
    {
        ApplyCustomGravity();
        GetLastCollidedSurface();

        isInAir = !IsCollidingWithSurface();
        if (useHandleActionForces) { HandleActionForces(); }
        if (isDropping && !prevStickDownDrop  && !hasBurstDropped) { ApplyBurstDropForce(); }
        prevStickDownDrop = isDropping;
        
        SmoothMovement();

        isMoving = rb.linearVelocity.sqrMagnitude > isMovingThreshold;

        if ((movementState == MovementState.Jumping || 
            movementState == MovementState.WallJump || 
            movementState == MovementState.AirDashing) && 
            (!isDropping || !fastFalling)) { TryStartHoverEffect(); }
        else if (movementState == MovementState.Hovering && (!isDropping || !fastFalling)) { WobbleEffect(); }

        ForceIdleState();
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleSurfaceState(collision, out _);
        StopMovementUponCollision();
        
        Invoke(nameof(ResetActionState), .1f);

        if (movementState == MovementState.WallDescending && 
            currentSurfaceState == SurfaceState.Ground && !hasBounced) { OnWallDescendingBounce(); }

        if (currentSurfaceState == SurfaceState.Ground)
        {
            var contact = collision.contacts[0];
            SnapToGround(contact.point);
            surfaceContactTime = Time.time;
            if (landingResetCoroutine != null) { StopCoroutine(landingResetCoroutine); }
            landingResetCoroutine = StartCoroutine(DelayedLandingReset());
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (landingResetCoroutine != null)
        {
            StopCoroutine(landingResetCoroutine);
            landingResetCoroutine = null;
        }

        lastContactTime = Time.time;
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // INPUT SETUP & CALLBACK REGISTRATION
    // ─────────────────────────────────────────────────────────────────────────

    #region INPUT SETUP & CALLBACK REGISTRATION
    /// <summary>
    /// Initializes and enables the InputActionAsset maps for movement and jump.
    /// These actions are then subscribed in RegisterInputCallbacks and read each frame in LeftAnalogStickInput.
    /// </summary>
    private void SetupInputActions()
    {
        var map = inputActions.FindActionMap("Player");
        leftStick = map.FindAction("Movement");
        southButton = map.FindAction("Jump");

        leftStick.Enable();
        southButton.Enable();
    }

    /// <summary>
    /// Subscribes south-button input events (started, performed, canceled).
    /// Paired with UnregisterInputCallbacks to manage event handlers’ lifecycle.
    /// </summary>
    private void RegisterInputCallbacks()
    {
        southButton.started += OnSouthButtonStarted;
        southButton.performed += OnSouthButtonPerformed;
        southButton.canceled += OnSouthButtonCanceled;
    }

    /// <summary>
    /// Unsubscribes south-button input events to prevent stale callbacks.
    /// Paired with RegisterInputCallbacks and called in OnDisable.
    /// </summary>
    private void UnregisterInputCallbacks()
    {
        southButton.started -= OnSouthButtonStarted;
        southButton.performed -= OnSouthButtonPerformed;
        southButton.canceled -= OnSouthButtonCanceled;
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // INPUT PROCESSING
    // ─────────────────────────────────────────────────────────────────────────

    #region INPUT PROCESSING
    /// <summary>
    /// Reads the left analog stick value (raw or processed), updates movement flags (leftStickMovement, isDropping),
    /// and computes snappedDir. Feeds into ActionInputDetected, PerformMovementAction, and dash/jump logic in Update.
    /// </summary>
    private void LeftAnalogStickInput()
    {
        if (Gamepad.current == null) { return; }

        if (useRawInput) { leftStickInput = Gamepad.current.leftStick.ReadUnprocessedValue(); }
        else { leftStickInput = leftStick.ReadValue<Vector2>(); }

        leftStickMovement = leftStickInput.magnitude > minStickMagnitude;
        
        if (leftStickMovement) { snappedDir = GetSnappedDirection(leftStickInput).normalized; }
        else { leftStickInput = snappedDir = Vector2.zero; }

        // bool inAir = currentSurfaceState == SurfaceState.Air;
        bool canDrop = currentSurfaceState == SurfaceState.Air || movementState == MovementState.Stucked;
        bool rawDown = false;

        if (leftStickMovement)
        {
            float rawAngle = Mathf.Atan2(leftStickInput.y, leftStickInput.x) * Mathf.Rad2Deg;
            rawAngle = (rawAngle + 360f) % 360f;
            float angleDiff = Mathf.DeltaAngle(rawAngle, 270f);
            rawDown = Mathf.Abs(angleDiff) <= dropAngleTolerance;
        }

        isDropping = Gamepad.current.buttonEast.isPressed && rawDown && canDrop && 
            movementState != MovementState.Idle && 
            movementState != MovementState.Charging && 
            !ActionInputDetected();
    }

    /// <summary>
    /// Handler for when the south (“jump”) button is pressed.
    /// Resets hold timers and flags southButtonPressed; paired with OnSouthButtonCanceled.
    /// </summary>
    public void OnSouthButtonStarted(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            buttonHoldTimer = 0;
            southButtonPressed = true;
        }
    }

    public void OnSouthButtonPerformed(InputAction.CallbackContext ctx) { }

    /// <summary>
    /// Handler for when the south (“jump”) button is released.
    /// Clears input flags and either triggers PerformMovementAction (if charging) or calls ResetActionState.
    /// </summary>
    public void OnSouthButtonCanceled(InputAction.CallbackContext ctx)
    {
        if (ctx.canceled)
        {
            southButtonPressed = false;
            buttonPressedLongEnough = false;

            if (snappedDir != Vector2.zero && movementState == MovementState.Charging) 
            {
                // if(currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall) 
                // {
                //     fetchedAction = "WallJump";
                //     allowedToMove = true;
                // }

                PerformMovementAction();
            } 
            else { ResetActionState(); }

            actionReady = true;
        }
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // ACTION DETECTION & HANDLING
    // ─────────────────────────────────────────────────────────────────────────
    #region ACTION DETECTION & HANDLING
    /// <summary>
    /// Determines if the stick movement and button hold satisfy the criteria for an action input.
    /// Used by Update to transition into the Charging state.
    /// </summary>
    private bool ActionInputDetected()
    {
        if (leftStickMovement && southButtonPressed && buttonHoldTimer >= minButtonPressTime) { return true; }

        buttonHoldTimer = 0;
        southButtonPressed = false;
        return false;
    }

    private void FetchActionType()
    {
        allowedToMove = false;      
        fetchedAction = "";         

        string dirLabel = GetClosestDirectionLabel(snappedDir);

        bool isJumpAllowed = currentSurfaceState != SurfaceState.LeftWall 
            && currentSurfaceState != SurfaceState.RightWall 
            && IsJumpDirectionAllowed(dirLabel);

        bool isWallJumpAllowed = (movementState == MovementState.Stucked || movementState == MovementState.WallDescending || movementState == MovementState.Charging)
            && (currentSurfaceState == SurfaceState.LeftWall 
            || currentSurfaceState == SurfaceState.RightWall)
            && IsJumpDirectionAllowed(dirLabel);
            
        bool isDashAllowed = IsDashDirectionAllowed(dirLabel);
        bool isAirDashAllowed = isInAir && allowAirDash && IsAirDashDirectionAllowed(dirLabel);

        if (!isInAir)
        {
            if (isWallJumpAllowed)
            {
                fetchedAction = "WallJump";
                allowedToMove = true;
            }
            else if (isDashAllowed)
            {
                fetchedAction = "Dash";
                allowedToMove = true;
            }
            else if (isJumpAllowed)
            {
                fetchedAction = "Jump";
                allowedToMove = true;
            }
        }
        else if (isAirDashAllowed)
        {
            fetchedAction = "AirDash";
            allowedToMove = true;
        }
    }
    
    public void PerformMovementAction() 
    {
        if (!allowedToMove || string.IsNullOrEmpty(fetchedAction)) { return; }

        float maxTravelDistance;
        float force;

        ResetPhysicsSettings(true, true);
        startPos = rb.position;

        if(fetchedAction == "Dash") 
        {
            maxTravelDistance = maxDashDistance;
            force = dashForce;
        }
        else if(fetchedAction == "Jump") 
        {
            maxTravelDistance = maxJumpDistance;
            force = jumpForce;
        }
        else if(fetchedAction == "AirDash") 
        {
            maxTravelDistance = maxAirDashDistance;
            force = airDashForce;
        }
        else if(fetchedAction == "WallJump") 
        {
            maxTravelDistance = maxJumpDistance;
            force = jumpForce;
        }
        else { Debug.LogWarning($"Unhandled action type: {fetchedAction}"); return; }

        if (landingResetCoroutine != null)
        {
            StopCoroutine(landingResetCoroutine);
            landingResetCoroutine = null;
        }

        SetupMovement(maxTravelDistance, force, fetchedAction);

        actionInProgress = true;
        hasTriggeredHover = false;
        hoverTimer = 0;
    }

    /// <summary>
    /// Called by BoxController right after pulling the player onto a box.
    /// </summary>
    public void LaunchOffBox(Vector3 direction)
    {
        // map 3D→2D stick dir
        snappedDir = new Vector2(direction.x, direction.y);
        fetchedAction = "Jump";
        allowedToMove = true;
        PerformMovementAction();
    }

    /// <summary>
    /// Configures targetDistance, forceMagnitude, predictedTargetPoint, and movementForceMode for a jump or dash.
    /// Called by PerformMovementAction and consumed by HandleActionForces and SmoothMovement.
    /// </summary>
    private void SetupMovement(float maxTravelDistance, float force, string action)
    {
        float hold = HoldRatio;
        targetDistance = maxTravelDistance * hold;
        forceMagnitude = force * Mathf.Pow(hold, forceCurveExponent);
        predictedTargetPoint = transform.position + (Vector3)snappedDir * targetDistance;
        hasAppliedForce = false;
        currentAction = action;

        string dirLabel = GetClosestDirectionLabel(snappedDir);

        if (action == "Dash")
        {
            movementState = MovementState.Dashing;
            
            isJumping = false;
            isWallJumping = false;
            isAirDashing = false;
            isDashing = true;
            
            rb.useGravity = true;
            dashStartPos = rb.position;
            movementForceMode = dashForceMode;
        }
        else if (action == "Jump")
        {
            movementState = MovementState.Jumping;

            // Movement flags
            isDashing = false;
            isWallJumping = false;
            isAirDashing = false;
            isJumping = true;

            rb.useGravity = false;
            jumpStartPos = rb.position;
            movementForceMode = jumpForceMode;

            if(dirLabel == "N") { isStraightJump = true;}
            else { isStraightJump = false; }

            bool isDiagonalJump = !isStraightJump && Mathf.Abs(snappedDir.x) > 0 && Mathf.Abs(snappedDir.y) > 0; 
            if(isDiagonalJump && snappedDir.x > 0) { isDiagonalJumpRight = true; }
            else if (isDiagonalJump && snappedDir.x < 0) { isDiagonalJumpRight = false; }
        }
        else if(action == "WallJump") 
        {
            movementState = MovementState.WallJump;

            isJumping = false;
            isDashing = false;
            isAirDashing = false;
            isWallJumping = true;
            
            rb.useGravity = false;
            jumpStartPos = rb.position;
            movementForceMode = jumpForceMode;

            if(currentSurfaceState == SurfaceState.LeftWall) { isWallJumpRight = true; }
            else if(currentSurfaceState == SurfaceState.RightWall) { isWallJumpRight = false; }

            if(snappedDir.y > 0) { isWallJumpAscend = true; }
            else if (snappedDir.y < 0) { isWallJumpAscend = false; }
        }
        else if(action == "AirDash") 
        {
            movementState = MovementState.AirDashing;
            
            isJumping = false;
            isDashing = false;
            isWallJumping = false;
            isAirDashing = true;
            
            rb.useGravity = false;
            airDashStartPos = rb.position;
            movementForceMode = airDashForceMode;
            
            if(dirLabel == "N" || dirLabel == "S") { isVerticalAirDash = true;}
            else { isVerticalAirDash = false;}

            if(dirLabel == "E" || dirLabel == "W") { isHorizontalAirDash = true; }
            else { isHorizontalAirDash = false; }

            bool isDiagonalAirDash = !isVerticalAirDash && !isHorizontalAirDash && Mathf.Abs(snappedDir.x) > 0 && Mathf.Abs(snappedDir.y) > 0; 
            
            if(isHorizontalAirDash) 
            {
                if(snappedDir.x > 0)  { isRightAirDash = true; }
                else if(snappedDir.x < 0) { isRightAirDash = false;}
            }
            else if(isDiagonalAirDash) 
            { 
                if(snappedDir.x > 0) { isRightDiagonalAirDash = true; }
                else if(snappedDir.x < 0) { isRightDiagonalAirDash = false; }
            }

            if(snappedDir.y > 0) { isAirDashAscend = true; }
            else if(snappedDir.y < 0) { isAirDashAscend = false; }	
        }

        snappedDir = Vector3.Lerp(rb.linearVelocity.normalized, snappedDir.normalized, lerpAmount);
    }

    /// <summary>
    /// Applies the initial impulse for a dash exactly once.
    /// Invoked in FixedUpdate when useHandleActionForces is true, immediately after SetupMovement for dashes.
    /// </summary>
    private void HandleActionForces()
    {
        if (movementState == MovementState.Dashing &&
            !hasAppliedForce && snappedDir.sqrMagnitude > minStickMagnitude)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(snappedDir.normalized * forceMagnitude, movementForceMode);
            hasAppliedForce = true;
        }
    }
    
    // HELPER METHODS
    /// <summary>
    /// Marks hasReachedTarget true when the Rigidbody is within arrivalRadius of predictedTargetPoint.
    /// Used by hover-triggering logic and SmoothMovement.
    /// </summary>
    private void CheckArrivalAtTarget()
    {
        if (Vector3.Distance(rb.position, predictedTargetPoint) < arrivalRadius && !isDropping)
            hasReachedTarget = true;
    }

    private void SnapToGround(Vector3 contactPoint)
    {
        float halfHeight = col.bounds.extents.y;
        Vector3 pos = rb.position;
        pos.y = contactPoint.y + halfHeight;
        rb.position = pos;
        rb.linearVelocity  = Vector3.zero;
    }

    private IEnumerator DelayedLandingReset()
    {
        // Wait exactly landingBuffer seconds
        yield return new WaitForSeconds(landingBuffer);

        // Only reset if we’re still grounded and not performing an action
        if (currentSurfaceState == SurfaceState.Ground && !actionInProgress)
        {
            movementState = MovementState.Idle;
            isLandingBuffered = true;
            hasBounced = false;
            hasBurstDropped = false;
            ResetPhysicsSettings(false, true);
            ResetActionState();
            Debug.Log("DelayedLandingReset...");
        }

        landingResetCoroutine = null;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // MOVEMENT SMOOTHING
    // ─────────────────────────────────────────────────────────────────────────
    #region MOVEMENT SMOOTHING
    /// <summary>
    /// Applies braking and forward force towards the target during Jump state, capping speed and adjusting damping.
    /// Driven by FixedUpdate and uses parameters set in SetupMovement.
    /// </summary>
    private void SmoothMovement()
    {
        if (movementState != MovementState.Jumping && movementState != MovementState.WallJump && movementState != MovementState.AirDashing) return;

        Vector3 toTarget = predictedTargetPoint - rb.position;
        float remaining = toTarget.magnitude;

        if (remaining <= arrivalRadius && !isDropping)
        {
            rb.position = predictedTargetPoint;
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 dir = toTarget / remaining;
        float brakeZone = 1.0f;
        float velAlong = Vector3.Dot(rb.linearVelocity, dir);

        if (remaining < brakeZone && velAlong > 0f)
        {
            float brakeStrength = (1f - remaining / brakeZone) * forceMagnitude;
            rb.AddForce(-dir * brakeStrength, ForceMode.Acceleration);
        }

        float ratio = Mathf.Clamp01(remaining / targetDistance);
        float dynamicForce = forceMagnitude * ratio;
        rb.AddForce(dir * dynamicForce, movementForceMode);

        if (rb.linearVelocity.magnitude > maxJumpSpeed) { rb.linearVelocity = rb.linearVelocity.normalized * maxJumpSpeed; }

        float closeRange = 1.0f;

        rb.linearDamping = (remaining < closeRange)
            ? Mathf.Lerp(0f, 5f, 1f - (remaining / closeRange))
            : defaultDamping;

        float dampingRatio = Mathf.Clamp01(remaining / targetDistance);
        rb.linearDamping = Mathf.Lerp(minHoverLinearDamping, hoverLinearDamping, dampingRatio);
    }

    /// <summary>
    /// Coroutine that interpolates linearDamping and gravityStrength over ~0.25s for a smooth hover transition.
    /// Started by TryStartHoverEffect.
    /// </summary>
    private IEnumerator SmoothHoverTransition()
    {
        float transitionTime = 0.25f;
        float elapsed = 0f;
        float initialDamping = rb.linearDamping;
        float targetDamping = hoverLinearDamping;
        float initialGravity = gravityStrength;
        float targetGravity = 0f;

        while (elapsed < transitionTime)
        {
            float t = elapsed / transitionTime;
            rb.linearDamping = Mathf.Lerp(initialDamping, targetDamping, t);
            gravityStrength = Mathf.Lerp(initialGravity, targetGravity, t);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearDamping = targetDamping;
        gravityStrength = targetGravity;
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // HOVER MECHANICS
    // ─────────────────────────────────────────────────────────────────────────
    #region HOVER MECHANICS
    /// <summary>
    /// Evaluates proximity and trajectory to decide when to enter Hover state.
    /// Zeroes gravity, sets damping, starts SmoothHoverTransition and WobbleEffect.
    /// Invoked each physics step in FixedUpdate after jump or air-dash.
    /// </summary>
    private bool TryStartHoverEffect()
    {
        if (!isInAir || movementState == MovementState.Hovering ||
            hasTriggeredHover || isDropping || fastFalling) { return false; }

        Vector3 toTarget = predictedTargetPoint - rb.position;
        float forwardDot = Vector3.Dot(rb.linearVelocity.normalized, toTarget.normalized);
        float distanceToTarget = toTarget.magnitude;
        float hoverTriggerRadius = hoverActivationRadius;
        float hoverForgivenessDistance = 2.5f;

        bool isCloseEnough = distanceToTarget <= hoverTriggerRadius;
        bool hasPassedTarget = forwardDot < 0f;
        bool isInForgivenessZone = hasPassedTarget && distanceToTarget <= hoverForgivenessDistance;

        if (!(isCloseEnough || isInForgivenessZone)) { return false; }

        if(Gamepad.current.buttonWest.IsPressed() && !useAutoHover) 
        {
            Hover();
            return true;
        }
        else if(useAutoHover) 
        {
            Debug.Log("Auto hover.."); 
            Hover(); 
            return true; 
        }

        return false;
    }

    private void Hover() 
    {
        movementState = MovementState.Hovering;
        gravityStrength = 0;
        rb.linearDamping = hoverLinearDamping;
        hoverTimer = hoverDuration;
        hoverWobbleTimer = 0f;
        originalHoverPosition = rb.position;
        hasTriggeredHover = true;

        StartCoroutine(SmoothHoverTransition());
        WobbleEffect();
    }

    /// <summary>
    /// Applies a vertical sine-wave force during Hover to create a wobble effect.
    /// Called repeatedly during hover and invokes UpdateHoverTimer to manage hover duration.
    /// </summary>
    private void WobbleEffect()
    {
        if (useHoverWobble && hoverTimer < (hoverDuration - hoverStartDelay) && hasReachedTarget)
        {
            rb.linearDamping = 0f;
            hoverWobbleTimer += Time.fixedDeltaTime;
            float wobbleFadeIn = Mathf.Clamp01(hoverWobbleTimer / wobbleFadeInFactor);
            float wobbleOffset = Mathf.Sin(hoverWobbleTimer * hoverWobbleSpeed) * hoverWobbleHeight * wobbleFadeIn;
            Vector3 velChange = new Vector3(0f, wobbleOffset / Time.fixedDeltaTime, 0f);
            rb.AddForce(velChange, ForceMode.Acceleration);
        }

        UpdateHoverTimer();
    }

    /// <summary>
    /// Decrements hoverTimer each physics step and calls ExitHover when the timer elapses.
    /// Ensures hover state ends correctly.
    /// </summary>
    private void UpdateHoverTimer()
    {
        if (movementState != MovementState.Hovering) return;
        hoverTimer -= Time.fixedDeltaTime;
        if (hoverTimer <= 0f) ExitHover();
    }

    /// <summary>
    /// Exits the hover state by transitioning to Descending, resetting timers, damping, gravity, and hover flags.
    /// Invoked by UpdateHoverTimer.
    /// </summary>    
    private void ExitHover()
    {
        movementState = MovementState.Descending;
        hoverTimer = 0f;
        hoverWobbleTimer = 0f;
        rb.linearDamping = defaultDamping;
        hasTriggeredHover = false;
        gravityStrength = initialGravityStrength;
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // GRAVITY & DROP FORCES
    // ─────────────────────────────────────────────────────────────────────────
    #region GRAVITY & DROP FORCES
    /// <summary>
    /// Applies gravity each physics step based on currentSurfaceState (via DetermineGravityDirection),
    /// and adjusts for jump/fall multipliers (lowJumpMultiplier, fallMultiplier), clamping by maxFallSpeed.
    /// </summary>
    private void ApplyCustomGravity()
    {
        Vector3 dir = DetermineGravityDirection().normalized;
        float verticalVelocity = Vector3.Dot(rb.linearVelocity, dir);
        float dropGravity = 0;
        if (fastFalling)
        {
            if(currentSurfaceState == SurfaceState.Air) dropGravity = initialGravityStrength * defaultDropMultiplier;
            else if(currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall) dropGravity = initialGravityStrength * wallDescendingGravityStrength;
            
            rb.AddForce(dir * dropGravity, fallForceMode);

            float dropCap = maxFallSpeed * defaultDropMultiplier;
            if (verticalVelocity > dropCap)
                rb.linearVelocity -= dir * (verticalVelocity - dropCap);

            // Debug.Log($"[Fast-fall] speed={verticalVelocity:F2}  gravity={dropGravity:F2}");
            return;
        }

        float heightAboveStart = Vector3.Project(rb.position - startPos, -dir).magnitude;
        float dynamicGravity = useDynamicGravityStrenght ? initialGravityStrength * (1f + heightAboveStart / maxJumpDistance) : initialGravityStrength;

        if (verticalVelocity > 0.1f) { dynamicGravity *= fallMultiplier; }
        rb.AddForce(dir * dynamicGravity, fallForceMode);
        if (verticalVelocity > maxFallSpeed) { rb.linearVelocity -= dir * (verticalVelocity - maxFallSpeed); }

        // Debug.Log($"[Fall] speed={verticalVelocity:F2}  height={heightAboveStart:F2}  gravity={dynamicGravity:F2}");
    }


    /// <summary>
    /// Applies a burst downward force for fast-fall, sets fastFalling flag.
    /// Connected to drop input logic in Update and subsequent physics behavior.
    /// </summary>
    private void ApplyBurstDropForce()
    {
        if (landingResetCoroutine != null)
        {
            StopCoroutine(landingResetCoroutine);
            landingResetCoroutine = null;
        }

        float burstStrength;

        if (movementState == MovementState.Stucked && isStuckFrozen) { ExitStuckState(); }
        if (movementState == MovementState.Hovering) { ExitHover(); StopAllCoroutines(); }

        if (currentSurfaceState == SurfaceState.LeftWall || 
            currentSurfaceState == SurfaceState.RightWall) { burstStrength = initialGravityStrength * wallDropMultiplier; }
        else { burstStrength = initialGravityStrength * defaultDropMultiplier; }
            
        rb.linearDamping = defaultDamping;
        Vector3 gDir = DetermineGravityDirection();

        float velAlongDown  = Vector3.Dot(rb.linearVelocity, gDir);
        rb.linearVelocity -= gDir * velAlongDown;
        rb.AddForce(gDir * burstStrength, ForceMode.VelocityChange);

        fastFalling = true;
        hasBurstDropped = true;
        predictedTargetPoint = rb.position;
        ResetActionState();

        // switch to the appropriate descending state
        if (currentSurfaceState == SurfaceState.LeftWall || 
            currentSurfaceState == SurfaceState.RightWall) { movementState = MovementState.WallDescending; }
        else {movementState = MovementState.Descending; }
    }

    /// <summary>
    /// Calculates the correct gravity direction vector from currentSurfaceState and contact time.
    /// Used by ApplyCustomGravity and ApplyBurstDropForce to orient gravity forces.
    /// </summary>
    private Vector3 DetermineGravityDirection()
    {
        Vector3 finalDir = gravityDir.normalized;
        switch (currentSurfaceState)
        {
            case SurfaceState.Ground:
                finalDir = ConvertToVector(gravityDirectionGround);
                fastFalling = false;
                break;
            case SurfaceState.Ceiling:
                finalDir = ConvertToVector(gravityDirectionCeiling);
                break;
            case SurfaceState.LeftWall:
                finalDir = ConvertToVector(gravityDirectionLeftWall);
                break;
            case SurfaceState.RightWall:
                finalDir = ConvertToVector(gravityDirectionRightWall);
                break;
        }
        if (isInAir && Time.time - lastContactTime > NO_CONTACT_THRESHOLD)
            finalDir = gravityDir.normalized;
        return finalDir;
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // COLLISION & SURFACE DETECTION
    // ─────────────────────────────────────────────────────────────────────────
    #region COLLISION & SRUFACE DETECTION
    /// <summary>
    /// Processes collision contacts to identify the surface type (ground, ceiling, left/right wall),
    /// updates currentSurfaceState, lastSurfaceObject, and flags stateChanged.
    /// Called in OnCollisionEnter.
    /// </summary>
    private void HandleSurfaceState(Collision collision, out GameObject surfaceObject)
    {
        surfaceObject = null;
        currentSurfaceState = SurfaceState.Air;
        SurfaceState detectedState = SurfaceState.Air;
        float bestDot = -1f;

        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 n = contact.normal;
            float dotUp = Vector3.Dot(n, Vector3.up);
            float dotDown = Vector3.Dot(n, Vector3.down);
            float dotRight = Vector3.Dot(n, Vector3.right);
            float dotLeft = Vector3.Dot(n, Vector3.left);

            if (dotUp > 0.7f && dotUp > bestDot)
            {
                detectedState = SurfaceState.Ground;
                surfaceObject = contact.otherCollider.gameObject;
                bestDot = dotUp;
            }
            else if (dotDown > 0.7f && dotDown > bestDot)
            {
                detectedState = SurfaceState.Ceiling;
                surfaceObject = contact.otherCollider.gameObject;
                bestDot = dotDown;
            }
            else if (dotLeft > 0.7f && dotLeft > bestDot)
            {                
                detectedState = SurfaceState.RightWall;
                surfaceObject = contact.otherCollider.gameObject;
                bestDot = dotLeft;
            }
            else if (dotRight > 0.7f && dotRight > bestDot)
            {
                detectedState = SurfaceState.LeftWall;
                surfaceObject = contact.otherCollider.gameObject;
                bestDot = dotRight;
            }
        }

        if (surfaceObject != null)
        {
            lastSurfaceObject = surfaceObject;
            lastSurfaceCheckTime = Time.time;
        }

        if (detectedState != currentSurfaceState) { currentSurfaceState = detectedState; }
    }

    /// <summary>
    /// Checks for nearby colliders via Physics.OverlapSphere to determine if the Rigidbody is in contact with any surface.
    /// Drives isInAir logic and collision-based state transitions in StopMovementUponCollision.
    /// </summary>    
    private bool IsCollidingWithSurface()
    {
        Collider[] cols = Physics.OverlapSphere(
            rb.position,
            GetComponent<Collider>().bounds.extents.y + 0.1f
        );
        return cols.Length > 1;
    }

    /// <summary>
    /// Returns the last collided surface object if within surfaceMemoryDuration; otherwise null.
    /// Provides brief memory of the last surface post-collision.
    /// </summary>
    private GameObject GetLastCollidedSurface()
    {
        if (Time.time - lastSurfaceCheckTime <= surfaceMemoryDuration) { return lastSurfaceObject; }
        return null;
    }

    /// <summary>
    /// Raycasts downward to detect ground proximity within groundProximityCheckDistance.
    /// Used in FixedUpdate for bounce logic and in OnGroundCollisionBounceFromWall.
    /// </summary>
    private bool IsNearGround()
    {
        Debug.Log("IsNearGround activated");
        Vector3 origin = col.bounds.center;
        origin.y = col.bounds.min.y + 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
            groundProximityCheckDistance + 0.25f, surfaceLayer))
        {
            #if UNITY_EDITOR
            Debug.DrawLine(origin, hit.point, Color.magenta);
            Debug.Log($"surface->{hit.transform.gameObject.name}");
            #endif
            return true;
        }
        
        return false;
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // STATE MANAGEMENT
    // ─────────────────────────────────────────────────────────────────────────

    #region STATE MANAGEMENT
    /// <summary>
    /// Stops movement and transitions to Stucked state upon collision during movement,
    /// setting stuckTimer and isStuckFrozen. Unlocked later by FreezePlayer.
    /// </summary>
    private void StopMovementUponCollision()
    {
        if(isMoving && movementState == MovementState.Dashing && 
            (currentSurfaceState == SurfaceState.LeftWall || 
            currentSurfaceState == SurfaceState.RightWall)) { movementState = MovementState.Idle; return; }


        if (isMoving && (movementState == MovementState.Jumping || movementState == MovementState.WallJump || movementState == MovementState.AirDashing))
        {
            if (movementState == MovementState.WallDescending) return;

            if ((currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall)
                && currentSurfaceState != SurfaceState.Ground)
            {
                movementState = MovementState.Stucked;
                stuckTimer = stuckDurationWall;
                isStuckFrozen = true;
            }
            else if (currentSurfaceState == SurfaceState.Ceiling)
            {
                movementState = MovementState.Stucked;
                stuckTimer = stuckDurationCeiling;
                isStuckFrozen = true;
            }

            FreezePlayer();
            isMoving = false;
        }
    }

    /// <summary>
    /// Manages the stuck freeze duration: while stuck, makes Rigidbody kinematic and zero gravity;
    /// upon timer expiry transitions to WallDescending or Descending.
    /// Invoked when movementState is Stucked in FixedUpdate.
    /// </summary>    
    private void FreezePlayer()
    {
        stuckTimer -= Time.fixedDeltaTime;

        if (stuckTimer <= 0f)
        {
            isStuckFrozen = false;
            rb.isKinematic = false;

            if (currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall)
            {
                lastWallSide = currentSurfaceState;
                movementState = MovementState.WallDescending;
            }
            else { movementState = MovementState.Descending; }
        }
        else
        {
            isStuckFrozen = true;
            rb.isKinematic = true;
            gravityStrength = 0;
        }
    }

    /// <summary>
    /// Applies a one-time bounce impulse when wall descending and near ground.
    /// Triggered in FixedUpdate if conditions are met.
    /// </summary>
    private void OnWallDescendingBounce()
    {
        Vector3 bounceDir = (lastWallSide == SurfaceState.LeftWall) ? Vector3.right : Vector3.left;
        rb.AddForce(bounceDir * bounceSpeed, ForceMode.Impulse);
        rb.linearVelocity = new Vector3(bounceDir.x * bounceSpeed, 0f, 0f);
        hasBurstDropped = false;
        hasBounced = false;
        Debug.Log("Bounced from wall after descending");
    }


    /// <summary>
    /// Transitions to Idle when no action is in progress and velocity is near zero on the ground,
    /// and calls ResetPhysicsSettings to restore default physics parameters.
    /// </summary>
    private void ForceIdleState()
    {
        if (movementState == MovementState.Charging ||
            movementState == MovementState.Jumping ||
            movementState == MovementState.WallJump ||
            movementState == MovementState.Dashing ||
            actionInProgress)
            return;

        if (rb.linearVelocity.sqrMagnitude < 0.01f && currentSurfaceState == SurfaceState.Ground)
        {
            movementState = MovementState.Idle;
            ResetPhysicsSettings(false, true);
        }

        if (movementState == MovementState.Idle && currentSurfaceState == SurfaceState.Ground) { isLandingBuffered = false; }
    }

    /// <summary>
    /// Clears input-related flags and timers (southButtonPressed, buttonHoldTimer, stickHoldTimer, snappedDir).
    /// Used by OnSouthButtonCanceled to abort actions cleanly.
    /// </summary>
    private void ResetActionState()
    {
        southButtonPressed = false;
        buttonHoldTimer = 0;
        actionInProgress = false;
        hasReachedTarget = false;
        isJumping = false;
        isDashing = false;
        isAirDashing = false;
        isWallJumping = false;
        snappedDir = Vector2.zero;
    }

    /// <summary>
    /// Restores gravityStrength, damping, velocity, and fastFalling. Optionally resets hasAppliedForce.
    /// Called by PerformMovementAction before new actions and by TrySetIdleState when entering Idle.
    /// </summary>
    private void ResetPhysicsSettings(bool resetAppliedForce, bool resetDamping)
    {
        gravityStrength = initialGravityStrength;
        rb.isKinematic = false;
        rb.linearDamping = defaultDamping;
        fastFalling = false;
        rb.linearVelocity = Vector3.zero;

        if (resetAppliedForce) hasAppliedForce = false;
        if (resetDamping) rb.linearDamping = defaultDamping;
    }

    private void ExitStuckState()
    {
        stuckTimer = 0f;
        isStuckFrozen = false;
        rb.isKinematic = false;
        movementState = MovementState.WallDescending;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // DIRECTION & LABEL MAPPING
    // ─────────────────────────────────────────────────────────────────────────
    #region DIRECTION & LABEL MAPPING
    /// <summary>
    /// Converts raw 2D stick input into a world-space Vector3 direction,
    /// optionally snapping to discrete increments based on directionCount.
    /// Used by LeftAnalogStickInput.
    /// </summary>
    private Vector3 GetSnappedDirection(Vector2 input)
    {
        if (input.sqrMagnitude < minStickMagnitude) { return Vector3.zero; }

        float rawAngle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        if (snapDirectionsEnabled)
        {
            float angleStep = 360f / directionCount;
            rawAngle = Mathf.Round(rawAngle / angleStep) * angleStep;
        }

        return Quaternion.Euler(0f, 0f, rawAngle) * Vector3.right;
    }

    /// <summary>
    /// Finds the nearest direction label (e.g. "NNE") for a given Vector2 direction
    /// based on the labelToAngle map. Used by PerformMovementAction and permission checks.
    /// </summary>
    public string GetClosestDirectionLabel(Vector2 dir)
    {
        float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 360f) % 360f;
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

    /// <summary>
    /// Determines if the specified direction label is permitted for a dash given the current surface state.
    /// On ground or ceiling only “W” (left) and “E” (right) are allowed; on walls only “N” (up) and “S” (down) are allowed.
    /// Used in PerformMovementAction to decide whether to execute a dash.
    /// </summary>
    private bool IsDashDirectionAllowed(string label)
    {
        if (currentSurfaceState == SurfaceState.Ground || currentSurfaceState == SurfaceState.Ceiling)
            { return label == "W" || label == "E"; }

        if (currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall)
            { return label == "N" || label == "S"; }

        return false;
    }

    /// <summary>
    /// Determines if the specified direction label is permitted for a jump given the current surface state.
    /// Disallows pure horizontal labels (“W”/“E”) and labels parallel to a wall when on that wall, then checks the allowedMoveLabels map.
    /// Used in PerformMovementAction to decide whether to execute a jump.
    /// </summary>
    private bool IsJumpDirectionAllowed(string label)
    {
        if (label == "W" || label == "E") { return false; }

        if ((currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall) &&
            (label == "N" || label == "S")) { return false; }

        return allowedMoveLabels.TryGetValue(currentSurfaceState, out var allowed) &&
            System.Array.Exists(allowed, l => l == label);
    }

    private bool IsAirDashDirectionAllowed(string label)
    {
        return allowedMoveLabels.TryGetValue(currentSurfaceState, out var allowed) &&
            System.Array.Exists(allowed, l => l == label);
    }

    /// <summary>
    /// Generates a direction label string for a given index, supporting cardinal or full 16-way labels.
    /// Used by BuildLabelToAngleMap.
    /// </summary>
    private string GetDirectionLabel(int index)
    {
        if (!useCardinalLabels) return (index + 1).ToString();
        string[] labels = new[]
        {
            "E","ENE","NE","NNE","N","NNW","NW","WNW",
            "W","WSW","SW","SSW","S","SSE","SE","ESE"
        };
        return labels[index % labels.Length];
    }

    /// <summary>
    /// Populates the labelToAngle dictionary by iterating through directionCount and calling GetDirectionLabel.
    /// Executed in Awake to initialize direction-label mappings.
    /// </summary>
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
    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // DIRECTION & LABEL MAPPING
    // ─────────────────────────────────────────────────────────────────────────
    #region GIZMOS
    void OnDrawGizmos()
    {
        
        if (snapDirectionsEnabled)
        {
            Gizmos.color = baseDirectionColor;
            float angleStep = 360f / directionCount;

            for (int i = 0; i < directionCount; i++)
            {
                float angle    = i * angleStep;
                float angleRad = angle * Mathf.Deg2Rad;
                Vector3 dir    = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);
                Vector3 endPt  = transform.position + dir * directionLineLength;

                Gizmos.DrawLine(transform.position, endPt);

        #if UNITY_EDITOR
                
                if (showDirectionLabels && !Application.isPlaying)
                {
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.Label(endPt + Vector3.up * 0.1f, GetDirectionLabel(i));
                }
        #endif
            }
        }

        
        if (Application.isPlaying && predictedTargetPoint != Vector3.zero)
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
                if (rb == null) return; 
            }

            float distanceToTarget = Vector3.Distance(rb.position, predictedTargetPoint);
            Gizmos.color = hasReachedTarget ? jumpTargetColor : landingPointColor;
            Gizmos.DrawSphere(predictedTargetPoint, 0.25f);
        }

        
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

        
        if (Application.isPlaying && leftStickInput.sqrMagnitude > 0.01f)
        {
            Gizmos.color = snappedInputColor;
            Vector3 start = rb.position;
            Vector3 end   = start + (Vector3)snappedDir.normalized * directionLineLength;

            
            Gizmos.DrawLine(start, end);

            
            float headAng = 20f, headLen = 0.25f;
            Quaternion look = Quaternion.LookRotation(Vector3.forward, end - start);
            Vector3 right = look * Quaternion.Euler(0,0, headAng)  * Vector3.up;
            Vector3 left  = look * Quaternion.Euler(0,0, -headAng) * Vector3.up;
            Gizmos.DrawLine(end, end - right * headLen);
            Gizmos.DrawLine(end, end - left  * headLen);

        #if UNITY_EDITOR
            if (showDirectionLabels)
            {
                
                var style = new GUIStyle();
                style.normal.textColor = Color.red;
                

                string lbl = GetClosestDirectionLabel(snappedDir);
                UnityEditor.Handles.Label(end + Vector3.up * 0.1f, $"Input: {lbl}", style);
            }
        #endif
        }
    }
    #endregion
}