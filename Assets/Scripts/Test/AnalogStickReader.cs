using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class AnalogStickReader : MonoBehaviour
{
    public enum MovementState { Idle, Charging, Jumping, Hovering, Descending, Dashing, AirDashing, WallDashing, Stucked, WallDescending }
    public enum SurfaceState { Ground, LeftWall, RightWall, Ceiling, Air }

    //== Movement & Surface State ==
    [Header("Character State")]
    public MovementState movementState = MovementState.Idle;
    public SurfaceState currentSurfaceState = SurfaceState.Ground;
    // Private state-related tracking
    private bool isJumping = false;
    private bool isDashing = false;
    private bool isInAir = false;
    private bool stateChanged = false;
    private float stateTimer = 0f;
    private float lastContactTime;
    private float airborneTime = 0f;
    private bool actionInProgress = false;
    private string currentAction = "";

    //== Input Settings ==
    [Header("Input Settings")]
    public InputActionAsset inputActions;
    public bool useRawInput = true;
    // Private input values
    private InputAction leftAnalogStickInput;
    private InputAction southButtonInput;
    private Vector2 leftStickInput = Vector2.zero;
    private Vector2 snappedDir = Vector2.zero;
    private bool southButtonPressed;
    private bool leftStickMovement = false;

    //== Snapped Input Settings ==
    [Header("Snapped Input Settings")]
    public bool snapDirectionsEnabled = false;
    public int directionCount = 16;

    //== Stuck Settings ==
    [Header("Stuck Settings")]
    public float stuckDurationWall = 1.0f;
    public float stuckDurationCeiling = 1.5f;
    private float stuckTimer = 0f;
    private bool isStuckFrozen = false;
    private float stuckCooldownTimer = 0f;
    public float stuckCooldownDuration = 1f; // how long the player is immune to re-stuck

    //== Direction Labels ==
    [Header("Label Settings")]
    public bool showDirectionLabels = true;
    public bool useCardinalLabels = true;

    //== Movement Forces ==
    [Header("Movement Settings")]
    public float jumpForce = 5f;
    public float maxJumpDistance = 5f;
    public float maxSafeSpeed = 10f;
    private float jumpGraceTimer = 0f;
    public float jumpGraceDuration = 0.15f; // adjust as needed


    [Header("Dash Settings")]
    public float dashForce = 5f;
    public float maxDashDistance = 5f;

    //== Force Modes ==
    [Header("Force Modes")]
    public ForceMode movementForceMode;
    [Tooltip("Type of force applied during a jump.")]
    public ForceMode jumpForceMode = ForceMode.VelocityChange;
    [Tooltip("Type of force applied during a dash.")]
    public ForceMode dashForceMode = ForceMode.Impulse;

    //== Fall & Gravity Settings ==
    [Header("Gravity & Fall Settings")]
    public float gravityStrength = 9.81f;
    private float storedGravityStrength = 0;
    public enum GravityDirection { Down, Up, Left, Right }
    public Vector3 gravityDir = Vector3.down;

    [Header("Wall Descending Settings")]
    [Tooltip("Multiplier in 10% steps of gravityStrength (e.g., 1 = 10%, 10 = 100%, 15 = 150%)")]
    public int wallGravityPercent = 10; // Default = 100%
    private float wallDescendingGravityStrength = 0f; // Computed automatically

    [Header("Custom Gravity Directions")]
    public GravityDirection gravityDirectionLeftWall = GravityDirection.Right;
    public GravityDirection gravityDirectionRightWall = GravityDirection.Left;
    public GravityDirection gravityDirectionCeiling = GravityDirection.Down;
    public GravityDirection gravityDirectionGround = GravityDirection.Down;

    public float lowJumpMultiplier = 4.0f;
    public float fallMultiplier = 1f;
    public float dropMultiplier = 2f;
    public float maxFallSpeed = 40f;
    public ForceMode dropForceMode = ForceMode.Impulse;
    public ForceMode fallForceMode = ForceMode.Acceleration;
    // Private fall state
    private bool fastFalling = false;    

    //== Hover Settings ==
    [Header("Hover Settings")]
    public bool useHoverWobble = true;
    public float hoverWobbleSpeed = 2f;
    public float hoverWobbleHeight = 0.2f;
    public float hoverStartDelay = 0.1f;
    public float wobbleFadeInFactor = 0.25f;
    public float hoverActivationRadius = 1.5f;
    public float hoverDuration = 2f;
    public float linearDampingOnHover = 5f;
    public float minHoverHeight = 2.0f;

    // Private hover tracking
    private float hoverTimer = 0f;
    private float hoverWobbleTimer = 0f;
    private bool hasTriggeredHover = false;
    private Vector3 originalHoverPosition;
    private bool isLandingBuffered = false;

    //== Proximity Checks ==
    [Header("Proximity Settings")]
    public float groundProximityCheckDistance = 1.0f;
    public float minAirborneTimeBeforeIdle = 0.15f;
    public float distanceToSurfaceThreshold = 0.05f;
    public float checkDistance;
    public LayerMask surfaceLayer;
    // Private proximity detection
    private GameObject nearbySurface = null;

    //== Input Debounce Settings ==
    [Header("Input Debounce Settings")]
    public float minStickHoldTime = 0.1f;
    public float minStickMagnitude = 0.2f;
    public float minButtonPressTime = 0.1f;
    public float maxHoldTime = 0.5f;
    // Private timers
    private float stickHoldTimer = 0f;
    private float buttonHoldTimer = 0f;
    private bool hasExecutedActionThisHold = false;
    private float HoldRatio => Mathf.Clamp01(buttonHoldTimer / maxHoldTime);

    //== Gizmo Settings ==
    [Header("Gizmo Settings")]
    public float gizmoScale = 2f;
    public float directionLineLength = 1.5f;

    [Header("Gizmo Colors")]
    public Color baseDirectionColor = Color.blue;
    public Color allowedJumpColor = Color.green;
    public Color dashDirectionColor = Color.cyan;
    public Color snappedInputColor = Color.yellow;
    public Color jumpTargetColor = Color.red;
    public Color landingPointColor = Color.green;
    public Color groundCheckDistanceColor;

    //== Arrival Prediction ==
    [Header("Target Prediction Settings")]
    public float arrivalRadius = 0.05f;
    public float stateBuffer = 0.25f;
    public float lerpAmount = 0.85f;
    public float estimatedTimeThreshold = 0.25f;
    // Private arrival logic
    private bool isMoving = false;
    private bool hasReachedTarget = false;
    private bool hasAppliedForce = false;
    private float targetDistance = 0f;
    private float forceMagnitude = 0f;
    private Vector3 predictedTargetPoint;

    //== Physics References ==
    private Rigidbody rb;
    private Vector3 startPos;
    private const float NO_CONTACT_THRESHOLD = 0.2f;

    // Private label data
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
    
    private Dictionary<string, float> labelToAngle;

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

    // -----------------------
    private bool droppedFromWall = false;
    private GameObject lastSurfaceObject;
    private float lastSurfaceCheckTime;
    private const float surfaceMemoryDuration = 0.2f; // how long the "collision" is considered valid
    public bool useHandleActionForces = true;

    //========= UNITY LIFECYCLE =========//
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        SetupInputActions();
        BuildLabelToAngleMap();
    }

    void Start()
    {
        storedGravityStrength = gravityStrength;
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
        // wallGravityPercent = Mathf.Max(0, wallGravityPercent);
    }

    void Update()
    {
        LeftAnalogStickInput();

        if (stateChanged)
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= stateBuffer)
            {
                stateChanged = false;
                stateTimer = 0f;
            }
        }

        if (isInAir) currentSurfaceState = SurfaceState.Air;

        if(actionInProgress && IsCollidingWithSurface()) 
        {
            actionInProgress = false;
        }

        if (southButtonPressed)
        {
            buttonHoldTimer += Time.deltaTime;
            if (!actionInProgress && !hasExecutedActionThisHold)
            {
                if (buttonHoldTimer >= maxHoldTime && buttonHoldTimer >= minButtonPressTime)
                {
                    PerformMovementAction();
                    hasExecutedActionThisHold = true;
                    Debug.Log("Invoke PerformMovementAction from Update");
                }
            }
        }

        switch (movementState)
        {
            case MovementState.Idle:
                break;
            case MovementState.Charging:
                break;
            case MovementState.Dashing:
                break;
            case MovementState.Jumping:
                break;
            case MovementState.Hovering:
                break;
            case MovementState.Descending:
                gravityStrength = storedGravityStrength;
                break;
            case MovementState.AirDashing:
                break;
            case MovementState.Stucked:
                break;
            case MovementState.WallDescending:
                gravityStrength = wallDescendingGravityStrength;
                break;
            case MovementState.WallDashing:
                break;
        }
    }

    void FixedUpdate()
    {
        isInAir = !IsCollidingWithSurface();
        GetLastCollidedSurface();
   
        if(useHandleActionForces) 
        {
            HandleActionForces();
        }

        if (jumpGraceTimer > 0f)
            jumpGraceTimer -= Time.fixedDeltaTime;
     
        if (movementState != MovementState.Hovering && movementState != MovementState.AirDashing && movementState != MovementState.Stucked)
        {
            ApplyCustomGravity();

            if (rb.linearVelocity.y < 0 
                && !IsNearGround() 
                && !isLandingBuffered 
                && currentSurfaceState != SurfaceState.LeftWall 
                && currentSurfaceState != SurfaceState.RightWall)
            {
                movementState = MovementState.Descending;
                Debug.Log($"rb linear vel Y -> {rb.linearVelocity.y}, isNearGround -> {IsNearGround()}, isLandingBuffered -> {isLandingBuffered}");
                Debug.Log($"✅ Movement state changed to Descending");
            }

        }

        // Smoooth out movement
        SmoothMovement();

        if (rb.linearVelocity.sqrMagnitude > 0.05f)
        {
            isMoving = true;
        }
        else { isMoving = false; }
       

        // if (isMoving)
        // {
        //     TrySmartDecelerateIfNearSurface();
        // }

        CheckArrivalAtTarget();

        // Hover 
        HandleHoverStatus();

        // Force to idle if near ground
        if ((movementState == MovementState.Descending || movementState == MovementState.WallDescending) && IsNearGround())
        {
            movementState = MovementState.Idle;
            isLandingBuffered = true;
            Debug.Log("✅ Landed – forced transition to Idle");
        }

        TrySetIdleState();

        if (stuckCooldownTimer > 0f)
        {
            stuckCooldownTimer -= Time.fixedDeltaTime;
        }

        // ⛔ Stop movement if colliding while moving
        StopMovementUponCollision();

        // ⏳ Handle stuck freeze logic
        FreezePlayer();
       
        // Handle bounce mechanic when walldescending
        OnGroundCollisionBounce();
    }

    private bool ActionInputDetected() 
    {
        if(leftStickMovement && southButtonPressed && buttonHoldTimer >= maxHoldTime && buttonHoldTimer >= minButtonPressTime) 
        {
            return true;
        }

        return false;
    }

    private void StopMovementUponCollision() 
    {
        if (isMoving && IsCollidingWithSurface() && stuckCooldownTimer <= 0f)
        {
            // Get the surface type & respond
            if (currentSurfaceState == SurfaceState.Ground)
            {
                movementState = MovementState.Idle;
                Debug.Log("🟢 Transition to Idle – Landed while moving");
            }
            else if (currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall)
            {
                movementState = MovementState.Stucked;
                stuckTimer = stuckDurationWall;
                isStuckFrozen = true;
                Debug.Log("🟥 Stuck on Wall");
            }
            else if (currentSurfaceState == SurfaceState.Ceiling)
            {
                movementState = MovementState.Stucked;
                stuckTimer = stuckDurationCeiling;
                isStuckFrozen = true;
                Debug.Log("🟥 Stuck on Ceiling");
            }

            isMoving = false;
        }
    }

    private void FreezePlayer() 
    {
        if (movementState == MovementState.Stucked)
        {
            // ✅ Allow immediate jump/dash input
            if (ActionInputDetected())
            {
                Debug.Log("✅ Action input detected while stuck – force jump");
                isStuckFrozen = false;
                rb.isKinematic = false;

                // Force movement
                PerformMovementAction();
                return;
            }

            if (isStuckFrozen && isDropping)
            {
                isStuckFrozen = false;
                rb.isKinematic = false;
                movementState = MovementState.WallDescending;
                stuckCooldownTimer = stuckCooldownDuration;

                Debug.Log("⏬ Dropping input detected — early release from wall Stuck into WallDescending");
                return;
            }

            if (isStuckFrozen)
            {
                stuckTimer -= Time.fixedDeltaTime;
                rb.isKinematic = true;
                gravityStrength = 0;

                if (stuckTimer <= 0f)
                {
                    isStuckFrozen = false;
                    rb.isKinematic = false;

                    movementState = (currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall)
                        ? MovementState.WallDescending
                        : MovementState.Descending;

                    stuckCooldownTimer = stuckCooldownDuration;
                    Debug.Log("🔓 Released from Stuck – Resume Movement");
                }
            }

            droppedFromWall = true;
        }
    }

    private void OnGroundCollisionBounce() 
    {
        if((currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.LeftWall) && droppedFromWall && rb.linearVelocity.y < 0 && IsNearGround()) 
        {
            Debug.Log("Player is near ground");
            Vector2 bounceDir = Vector2.zero;

            // Fetch the directions
            if(currentSurfaceState == SurfaceState.LeftWall) 
            {
                bounceDir = Vector2.right;
            }
            else if(currentSurfaceState == SurfaceState.RightWall) 
            {
                bounceDir = Vector2.left;
            }

            float bounceForce = 15f;
            float maxBounceSpeed = 5f;

            rb.AddForce(bounceDir * bounceForce, jumpForceMode);

            if (rb.linearVelocity.magnitude > maxBounceSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxBounceSpeed;
            }
        } 
    }

    private void HandleHoverStatus() 
    {
        if (movementState == MovementState.Hovering)
        {
            if (useHoverWobble && hoverTimer < (hoverDuration - hoverStartDelay))
            {
                hoverWobbleTimer += Time.fixedDeltaTime;
                float wobbleFadeIn = Mathf.Clamp01(hoverWobbleTimer / wobbleFadeInFactor);
                float wobbleOffset = Mathf.Sin(hoverWobbleTimer * hoverWobbleSpeed) * hoverWobbleHeight * wobbleFadeIn;
                Vector3 upwardForce = new Vector3(0f, wobbleOffset, 0f);

                rb.AddForce(upwardForce, ForceMode.Acceleration);
            }

            UpdateHoverTimer();
        }
        else if (movementState == MovementState.Jumping || movementState == MovementState.AirDashing)
        {
            TryStartHoverEffect();
        }
    }

    private void SmoothMovement() 
    {
        if (movementState == MovementState.Jumping)
        {
            TryStartHoverEffect();
            
            Vector3 toTarget = predictedTargetPoint - rb.position;
            float distance = toTarget.magnitude;

            Vector3 desiredDir = toTarget.normalized;

            float easingFactor = Mathf.Clamp01(distance / targetDistance);
            float scaledForce = forceMagnitude * easingFactor;

            rb.AddForce(desiredDir * scaledForce, jumpForceMode);

            if (rb.linearVelocity.magnitude > maxSafeSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSafeSpeed;
            }

            float closeRange = 1.0f;
            rb.linearDamping = (distance < closeRange)
                ? Mathf.Lerp(0f, 5f, 1f - (distance / closeRange))
                : 0f;
        }
    }

    void LateUpdate()
    {
        Vector3 pos = rb.position;
        pos.z = 0;
        rb.position = pos;
    }

    //========= INPUT SETUP & HANDLING =========//
    private void SetupInputActions()
    {
        var map = inputActions.FindActionMap("Player");
        leftAnalogStickInput = map.FindAction("Movement");
        southButtonInput = map.FindAction("Jump");

        leftAnalogStickInput.Enable();
        southButtonInput.Enable();
    }

    private void RegisterInputCallbacks()
    {
        southButtonInput.started += OnSouthButtonStarted;
        southButtonInput.performed += OnSouthButtonPerformed;
        southButtonInput.canceled += OnSouthButtonCanceled;
    }

    private void UnregisterInputCallbacks()
    {
        southButtonInput.started -= OnSouthButtonStarted;
        southButtonInput.performed -= OnSouthButtonPerformed;
        southButtonInput.canceled -= OnSouthButtonCanceled;
    }

    private void LeftAnalogStickInput()
    {
        if (Gamepad.current != null)
        {
            if (useRawInput)
                leftStickInput = Gamepad.current.leftStick.ReadUnprocessedValue();
            else
                leftStickInput = leftAnalogStickInput.ReadValue<Vector2>();

            leftStickMovement = leftStickInput.magnitude > minStickMagnitude;
            if (leftStickMovement)
            {
                snappedDir = GetSnappedDirection().normalized;
            }

            isDropping = leftAnalogStickInput.ReadValue<Vector2>() == Vector2.down;
        }
    }

    public void OnSouthButtonStarted(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            Debug.Log("South Button Started");

            if (isInAir) return;

            movementState = MovementState.Charging;
            buttonHoldTimer = 0;
            southButtonPressed = true;
        }
    }

    public void OnSouthButtonPerformed(InputAction.CallbackContext ctx)
    {
        // Typically called when the button is fully pressed
    }

    public void OnSouthButtonCanceled(InputAction.CallbackContext ctx)
    {
        if (ctx.canceled && southButtonPressed)
        {
            southButtonPressed = false;

            if (!hasExecutedActionThisHold && snappedDir != Vector2.zero)
            {
                if (isInAir)
                {
                    Debug.Log("⛔ Cannot perform action in air");
                    ResetActionState(true, true);
                    return;
                }

                Debug.Log("Invoke PerformMovementAction from OnSouthButtonCanceled");
                PerformMovementAction();
            }
            else if (!ActionInputDetected())
            {
                ResetActionState(false, false);
            }
        }
    }

    //========= MOVEMENT LOGIC =========//
    private void PerformMovementAction()
    {
        if (snappedDir == Vector2.zero) return;

        if (isInAir)
        {
            Debug.Log("⛔ Action blocked while in air");
            buttonHoldTimer = 0;
            stickHoldTimer = 0;
            southButtonPressed = false;
            return;
        }

        string dirLabel = GetClosestDirectionLabel(snappedDir);
        bool isJumpAllowed = IsJumpDirectionAllowed(dirLabel);
        bool isDashAllowed = IsDashDirectionAllowed(dirLabel);

        startPos = rb.position;

        if (isDashAllowed)
        {
            SetupMovement(maxDashDistance, dashForce, "Dash");
        }
        else if (isJumpAllowed)
        {
            SetupMovement(maxJumpDistance, jumpForce, "Jump");
        }

        actionInProgress = true;
    }

    private void SetupMovement(float maxTravelDistance, float force, string action)
    {
        targetDistance = maxTravelDistance * HoldRatio;
        Debug.Log($"target distance = {targetDistance}");
        currentAction = action;

        predictedTargetPoint = transform.position + (Vector3)snappedDir * targetDistance;
        hasAppliedForce = false;

        if (action == "Dash")
        {
            movementState = MovementState.Dashing;
            isJumping = false;
            isDashing = true;
            rb.useGravity = true;

            movementForceMode = dashForceMode;
            Debug.Log("Dashing");
        }
        else if (action == "Jump")
        {
            movementState = MovementState.Jumping;
            isDashing = false;
            isJumping = true;
            rb.useGravity = false;
            movementForceMode = jumpForceMode;

            jumpGraceTimer = jumpGraceDuration; // ⬅️ start grace period

            bool isDiagonalJump = Mathf.Abs(snappedDir.x) > 0 && Mathf.Abs(snappedDir.y) > 0;
            Debug.Log(isDiagonalJump ? "Jumping Diagonal" : "Jumping Straight");
        }

        forceMagnitude = force;
        snappedDir = Vector3.Lerp(rb.linearVelocity.normalized, snappedDir.normalized, lerpAmount);

        Debug.Log($"{action} ➤ Direction: {snappedDir}, Force: {forceMagnitude}");
        Debug.Log($"📍 Set jump target to: {predictedTargetPoint}");
    }

    private void HandleActionForces()
    {
        if (!hasAppliedForce && snappedDir.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(snappedDir.normalized * forceMagnitude, movementForceMode);
            hasAppliedForce = true;
        }
    }

    private void CheckArrivalAtTarget()
    {
        if (Vector3.Distance(rb.position, predictedTargetPoint) < arrivalRadius)
        {
            hasReachedTarget = true;
            Debug.Log("🏁 Reached target point, returning to start...");
        }
    }

    private void TrySmartDecelerateIfNearSurface()
    {
        // Skip if we're barely moving
        if (rb.linearVelocity.sqrMagnitude < 0.01f)
            return;

        Vector3 moveDirection = rb.linearVelocity.normalized;
        Vector3 origin = rb.position;

        if (Physics.Raycast(origin, moveDirection, out RaycastHit hit, checkDistance, surfaceLayer))
        {
            nearbySurface = hit.collider.gameObject;
            float distanceToSurface = hit.distance;
            float currentSpeed = rb.linearVelocity.magnitude;

            float estimatedTimeToReach = distanceToSurface / currentSpeed;

            if (estimatedTimeToReach <= 0.5f)
            {
                float decelerationAmount = currentSpeed / estimatedTimeToReach;
                Vector3 decelerationForce = -moveDirection * decelerationAmount;

                rb.AddForce(decelerationForce, ForceMode.Acceleration);
                Debug.Log($"📏 Distance to surface: {distanceToSurface} vs threshold {distanceToSurfaceThreshold}");


                if (distanceToSurface <= distanceToSurfaceThreshold)
                {
                    rb.linearVelocity = Vector3.zero;
                    Debug.Log("🛑 Reached surface – velocity clamped");
                }

    #if UNITY_EDITOR
                Debug.DrawLine(origin, hit.point, Color.magenta); // Visual debug
    #endif
            }
        }
    }

    //========= JUMP / DASH VALIDATION =========//
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

    //========= HOVER MANAGEMENT =========//
    private void UpdateHoverTimer()
    {
        if (movementState != MovementState.Hovering)
            return;

        hoverTimer -= Time.fixedDeltaTime;

        if (hoverTimer <= 0f)
        {
            Debug.Log("Hover timer ran out");
            ExitHover();
        }
    }

    private void ExitHover()
    {
        movementState = MovementState.Descending; 
        Debug.Log($"Movement state changed to {movementState} in ExitHover()");
        hoverTimer = 0f;
        hoverWobbleTimer = 0f;
        rb.linearDamping  = 0f; 
        hasTriggeredHover = false; 
        gravityStrength = storedGravityStrength;

        Debug.Log("⬇️ Exiting Hover – Starting to Descend");
    }

    private bool TryStartHoverEffect()
    {
        if (!isInAir || movementState == MovementState.Hovering || hasTriggeredHover)
            return false;

        Vector3 toTarget = predictedTargetPoint - rb.position;
        float forwardDot = Vector3.Dot(rb.linearVelocity.normalized, toTarget.normalized);
        float distanceToTarget = toTarget.magnitude;

        float hoverTriggerRadius = hoverActivationRadius;
        float hoverForgivenessDistance = 2.5f;

        bool isCloseEnough = distanceToTarget <= hoverTriggerRadius;
        bool hasPassedTarget = forwardDot < 0f;
        bool isInForgivenessZone = hasPassedTarget && distanceToTarget <= hoverForgivenessDistance;

        if (!(isCloseEnough || isInForgivenessZone))
            return false;

        movementState = MovementState.Hovering;
        rb.linearDamping = linearDampingOnHover;
        storedGravityStrength = gravityStrength;
        gravityStrength = 0f;

        hoverTimer = hoverDuration;
        hoverWobbleTimer = 0f;
        originalHoverPosition = rb.position;

        StartCoroutine(SmoothHoverTransition());
        hasTriggeredHover = true;

        return true;
    }

    private IEnumerator SmoothHoverTransition()
    {
        float transitionTime = 0.25f;
        float elapsed = 0f;
        float initialDamping = rb.linearDamping;
        float targetDamping = linearDampingOnHover;

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

    //========= COLLISION HANDLING =========//
    private void OnCollisionEnter(Collision collision)
    {
        HandleSurfaceState(collision, out lastSurfaceObject);

        // Force state change if Jumping and hit wall
        if (/*movementState == MovementState.Jumping &&*/
            currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall)
        {
            movementState = MovementState.Stucked;
            stuckTimer = stuckDurationWall;
            isStuckFrozen = true;
            rb.linearVelocity = Vector3.zero;
            hasAppliedForce = false;
            Debug.Log("🟥 Forced transition from Jumping to Stucked on Wall");
            return;
        }
        else 
        {
            Debug.Log("⏳ Jump grace timer active – skipping Stucked override");
        }

        if (movementState == MovementState.Descending && 
            (currentSurfaceState == SurfaceState.Ground || currentSurfaceState == SurfaceState.Ceiling))
        {
            movementState = MovementState.Idle;
            isLandingBuffered = true;
            Debug.Log("🛬 Collision landed – set to Idle");
        }
    }


    private void OnCollisionStay(Collision collision)
    {
        HandleSurfaceState(collision, out lastSurfaceObject);

        if (currentSurfaceState == SurfaceState.Ground && rb.position.magnitude < 0.01f)
        {
            Debug.Log("Change movement state to idle");
        }
        
        // OPTIONAL: FORCE TO JUMP STATE 
        if(ActionInputDetected() && movementState == MovementState.Charging) 
        {
            movementState = MovementState.Jumping;
            ResetActionState(false, false);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        lastContactTime = Time.time;
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

    // private bool IsCollidingWithSurface()
    // {
    //     return Time.time - lastSurfaceCheckTime <= surfaceMemoryDuration && lastSurfaceObject != null;
    // }

    private GameObject GetLastCollidedSurface()
    {
        if (Time.time - lastSurfaceCheckTime <= surfaceMemoryDuration)
        {
            Debug.Log($"last surface object: {lastSurfaceObject}");
            return lastSurfaceObject;
        }

        return null;
    }
    
    private bool IsNearGround()
    {
        if (!TryGetComponent<Collider>(out var col) && movementState != MovementState.Descending) return false;

        Vector3 origin = col.bounds.center;
        origin.y = col.bounds.min.y; 

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundProximityCheckDistance))
        {
            Debug.DrawLine(origin, hit.point, Color.magenta);
            return true;
        }

        return false;
    }

    private bool SurfaceIsGround()
    {
        return currentSurfaceState == SurfaceState.Ground;
    }
    
    //========= DIRECTION CALCULATIONS =========//
    private Vector3 GetSnappedDirection()
    {
        if (leftStickInput.sqrMagnitude < 0.01f)
            return Vector3.zero;

        float rawAngle = Mathf.Atan2(leftStickInput.y, leftStickInput.x) * Mathf.Rad2Deg;

        if (snapDirectionsEnabled)
        {
            float angleStep = 360f / directionCount;
            rawAngle = Mathf.Round(rawAngle / angleStep) * angleStep;
        }

        Quaternion rotation = Quaternion.Euler(0f, 0f, rawAngle);
        return rotation * Vector3.right;
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

    private string GetDirectionLabel(int index)
    {
        if (!useCardinalLabels)
            return (index + 1).ToString();

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

    //========= GRAVITY & FALLING =========//
    private bool isDropping = false;
    private void ApplyCustomGravity()
    {   
        if (movementState == MovementState.Hovering)
            return;

        Vector3 gravityDir = DetermineGravityDirection();
        float gravityForce = gravityStrength;

        float verticalVelocity = Vector3.Dot(rb.linearVelocity, gravityDir);
        float upwardVelocity = Vector3.Dot(rb.linearVelocity, -gravityDir);
        float jumpHeightSoFar = Vector3.Project(rb.position - startPos, -gravityDir).magnitude;
        // bool isDropping = leftAnalogStickInput.ReadValue<Vector2>() == Vector2.down;
        if (verticalVelocity > maxFallSpeed) return;

        // ✅ Handle FAST-FALL as a 1-time burst
        if (isDropping && isInAir && !fastFalling)
        {
            rb.AddForce(dropMultiplier * gravityForce * gravityDir, dropForceMode);
            fastFalling = true;
            Debug.Log("⚡ Burst drop applied (Impulse)");
            return; 
        }

        // ✅ Apply modifiers for regular jump/fall
        if (jumpHeightSoFar < minHoverHeight && upwardVelocity > 0.1f && !southButtonPressed)
        {
            gravityForce *= lowJumpMultiplier;
        }
        else if (verticalVelocity > 0.1f)
        {
            gravityForce *= fallMultiplier;
        }

        // ✅ Apply normal gravity
        rb.AddForce(gravityDir * gravityForce, fallForceMode);
    }

    private Vector3 DetermineGravityDirection()
    {
        // Default world gravity direction
        Vector3 finalGravityDir = gravityDir.normalized;

        // Modify gravity direction based on surface state
        switch (currentSurfaceState)
        {
            case SurfaceState.Ground:
                // Standard down gravity when on ground
                finalGravityDir = ConvertToVector(gravityDirectionGround);
                fastFalling = false;

                break;
                
            case SurfaceState.Ceiling:
                // Standard down gravity when on ceiling (to fall)
                finalGravityDir = ConvertToVector(gravityDirectionCeiling);
                break;
                
            case SurfaceState.LeftWall:
                // Pull character toward left wall
                finalGravityDir = ConvertToVector(gravityDirectionLeftWall);
                break;
                
            case SurfaceState.RightWall:
                // Pull character toward right wall
                finalGravityDir = ConvertToVector(gravityDirectionRightWall);
                break;
        }
        
        // If in mid-air and not touching any surface, use default gravity
        if (isInAir && Time.time - lastContactTime > NO_CONTACT_THRESHOLD)
        {
            finalGravityDir = gravityDir.normalized;
        }
        
        return finalGravityDir;
    }

    private void HandleSurfaceState(Collision collision, out GameObject surfaceObject)
    {
        surfaceObject = null;
        currentSurfaceState = SurfaceState.Air;

        SurfaceState detectedState = SurfaceState.Air;
        float bestDot = -1f;

        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 normal = contact.normal;
            float dotUp = Vector3.Dot(normal, Vector3.up);
            float dotDown = Vector3.Dot(normal, Vector3.down);
            float dotRight = Vector3.Dot(normal, Vector3.right);
            float dotLeft = Vector3.Dot(normal, Vector3.left);

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

        if (detectedState != currentSurfaceState)
        {
            Debug.Log($"Surface State changed from {currentSurfaceState} to {detectedState}");
            currentSurfaceState = detectedState;
            stateChanged = true;
        }
    }

    private void TrySetIdleState()
    {
        if (movementState == MovementState.Charging ||
            movementState == MovementState.Jumping ||
            movementState == MovementState.Dashing ||
            actionInProgress)
            return;

        if (rb.linearVelocity.sqrMagnitude < 0.01f && currentSurfaceState == SurfaceState.Ground)
        {
            movementState = MovementState.Idle;
            rb.linearVelocity = Vector3.zero;
        }

        if (movementState == MovementState.Idle && currentSurfaceState == SurfaceState.Ground)
        {
            isLandingBuffered = false;
        }
    }

    private void ResetActionState(bool _bool_1, bool _bool_2)
    {
        actionInProgress = false;
        southButtonPressed = false;
        hasExecutedActionThisHold = false;  // ✅ Reset flag
        rb.linearVelocity = Vector3.zero;
        snappedDir = Vector2.zero;
        buttonHoldTimer = 0;
        stickHoldTimer = 0;

        if(_bool_1) 
        {
            hasAppliedForce = false;
        }

        if(_bool_2) 
        {
            rb.linearDamping = 0;
        }
    }
    
    //========= GIZMOS =========//
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

                Vector3 endPoint = transform.position + dir * directionLineLength;
                Gizmos.DrawLine(transform.position, endPoint);

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
        if (Application.isPlaying && predictedTargetPoint != Vector3.zero)
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
                if (rb == null) return; // Still null? Exit the method to avoid errors
            }

            float distanceToTarget = Vector3.Distance(rb.position, predictedTargetPoint);
            Gizmos.color = hasReachedTarget ? jumpTargetColor : landingPointColor;
            Gizmos.DrawSphere(predictedTargetPoint, 0.25f);
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
        if (Application.isPlaying && leftStickInput.sqrMagnitude > 0.01f)
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