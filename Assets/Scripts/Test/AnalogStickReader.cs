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

    //== Snapped Input Settings ==
    [Header("Snapped Input Settings")]
    public bool snapDirectionsEnabled = false;
    public int directionCount = 16;

    //== Direction Labels ==
    [Header("Label Settings")]
    public bool showDirectionLabels = true;
    public bool useCardinalLabels = true;
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

    //== Movement Forces ==
    [Header("Movement Settings")]
    public float jumpForce = 5f;
    public float maxJumpDistance = 5f;

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
    public Vector3 gravityDirection = Vector3.down;
    public float lowJumpMultiplier = 4.0f;
    public float fallMultiplier = 1f;
    public float dropMultiplier = 2f;
    public float maxFallSpeed = 40f;
    public ForceMode dropForceMode = ForceMode.Impulse;
    public ForceMode fallForceMode = ForceMode.Acceleration;
    // Private fall state
    private bool fastFalling = false;
    private float storedGravityValue;

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

    //========= UNITY LIFECYCLE =========//
    void Awake()
    {
        SetupInputActions();
        rb = GetComponent<Rigidbody>();
        BuildLabelToAngleMap();
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

        if (southButtonPressed)
        {
            buttonHoldTimer += Time.deltaTime;
            if (!actionInProgress)
            {
                if (buttonHoldTimer >= maxHoldTime && buttonHoldTimer >= minButtonPressTime)
                {
                    PerformMovementAction();
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
                break;
            case MovementState.AirDashing:
                break;
            case MovementState.Stucked:
                break;
            case MovementState.WallDescending:
                break;
            case MovementState.WallDashing:
                break;
        }
    }

    void FixedUpdate()
    {
        isInAir = !IsCollidingWithSurface();
        HandleActionForces();
        CheckArrivalAtTarget();

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

        if (movementState != MovementState.Hovering && movementState != MovementState.AirDashing)
        {
            ApplyCustomGravity();

            if (rb.linearVelocity.y < 0 && !IsNearGround() && !isLandingBuffered)
            {
                movementState = MovementState.Descending;
            }
        }

        if (movementState == MovementState.Jumping)
        {
            Vector3 toTarget = predictedTargetPoint - rb.position;
            float distance = toTarget.magnitude;
            TryStartHoverEffect();

            Vector3 desiredDir = toTarget.normalized;

            float easingFactor = Mathf.Clamp01(distance / targetDistance);
            float scaledForce = forceMagnitude * easingFactor;

            rb.AddForce(desiredDir * scaledForce, jumpForceMode);

            float maxJumpSpeed = 10f;
            if (rb.linearVelocity.magnitude > maxJumpSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxJumpSpeed;
            }

            float closeRange = 1.0f;
            rb.linearDamping = (distance < closeRange)
                ? Mathf.Lerp(0f, 5f, 1f - (distance / closeRange))
                : 0f;
        }

        if (movementState == MovementState.Descending && IsNearGround())
        {
            movementState = MovementState.Idle;
            isLandingBuffered = true;
            Debug.Log("✅ Landed – forced transition to Idle");
        }

        TrySetIdleState();

        if (movementState == MovementState.Idle && currentSurfaceState == SurfaceState.Ground)
        {
            isLandingBuffered = false;
        }

        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            isMoving = true;
        }

        if (isMoving)
        {
            TrySmartDecelerateIfNearSurface();
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

            if (leftStickInput.magnitude > minStickMagnitude)
            {
                stickHoldTimer += Time.deltaTime;
                if (stickHoldTimer >= minStickHoldTime)
                {
                    snappedDir = GetSnappedDirection().normalized;
                }
            }
            else
            {
                stickHoldTimer = 0f;
            }
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
            actionInProgress = false;
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

            if (buttonHoldTimer < minButtonPressTime || stickHoldTimer < minStickHoldTime)
            {
                Debug.Log("⛔ Ignoring quick input");
                ResetActionState();
                return;
            }

            if (snappedDir != Vector2.zero)
            {
                if (isInAir)
                {
                    Debug.Log("⛔ Cannot perform action in air");
                    hasAppliedForce = false;
                    buttonHoldTimer = 0;
                    stickHoldTimer = 0;
                    southButtonPressed = false;
                    return;
                }

                PerformMovementAction();
            }
            else
            {
                ResetActionState();
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
        hoverTimer = 0f;
        hoverWobbleTimer = 0f;
        rb.linearDamping  = 0f; 
        hasTriggeredHover = false; 
        gravityStrength = storedGravityValue;
        actionInProgress = false;

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
        storedGravityValue = gravityStrength;
        gravityStrength = 0f;

        hoverTimer = hoverDuration;
        hoverWobbleTimer = 0f;
        originalHoverPosition = rb.position;

        StartCoroutine(SmoothHoverTransition());
        hasTriggeredHover = true;
        Debug.Log("🛸 Hover Started — Smooth Entry");
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
        HandleSurfaceState(collision);

        if (movementState == MovementState.Descending && (currentSurfaceState == SurfaceState.Ground || currentSurfaceState == SurfaceState.Ceiling))
        {
            movementState = MovementState.Idle;
            isLandingBuffered = true;
            Debug.Log("🛬 Collision landed – set to Idle");
        }
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
    private void ApplyCustomGravity()
    {   
        if (movementState == MovementState.Hovering)
            return;

        Vector3 gravityDir = DetermineGravityDirection();
        float gravityForce = gravityStrength;

        float verticalVelocity = Vector3.Dot(rb.linearVelocity, gravityDir);
        float upwardVelocity = Vector3.Dot(rb.linearVelocity, -gravityDir);
        float jumpHeightSoFar = Vector3.Project(rb.position - startPos, -gravityDir).magnitude;
        bool isDropping = leftAnalogStickInput.ReadValue<Vector2>() == Vector2.down;

        if (verticalVelocity > maxFallSpeed) return;

        // ✅ Handle FAST-FALL as a 1-time burst
        if (isDropping && isInAir && !fastFalling)
        {
            rb.AddForce(dropMultiplier * gravityForce * gravityDir, dropForceMode);
            fastFalling = true;
            Debug.Log("⚡ Burst drop applied (Impulse)");
            return; // ⛔ Skip normal gravity this frame
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
        Vector3 gravityDir = gravityDirection.normalized;
        
        // Modify gravity direction based on surface state
        switch (currentSurfaceState)
        {
            case SurfaceState.Ground:
                // Standard down gravity when on ground
                gravityDir = Vector3.down;
                fastFalling = false;

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

    //========= STATE HANDLER =========//
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

    private void TrySetIdleState()
    {
        if (movementState == MovementState.Charging ||
            movementState == MovementState.Jumping ||
            movementState == MovementState.Dashing ||
            actionInProgress)
            return;

        Debug.Log($"linearVelocity {rb.linearVelocity.sqrMagnitude}");
        Debug.Log($"linearVelocity: X->{rb.linearVelocity.x}, Y->{rb.linearVelocity.y}");
        if (rb.linearVelocity.sqrMagnitude < 0.01f && currentSurfaceState == SurfaceState.Ground)
        {
            movementState = MovementState.Idle;
        }
    }

    private void ResetActionState()
    {
        actionInProgress = false;
        southButtonPressed = false;
        rb.linearVelocity = Vector3.zero;
        rb.linearDamping = 0;
        snappedDir = Vector2.zero;
        hasAppliedForce = false;
        buttonHoldTimer = 0;
        stickHoldTimer = 0;
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
}
