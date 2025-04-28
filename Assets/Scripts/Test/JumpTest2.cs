using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class JumpTest2 : MonoBehaviour
{
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
        Ceiling
    }


    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float maxJumpDistance = 10f;
    public float jumpHeight = 3f;

    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float maxDashDistance = 8f;

    [Header("Control Settings")]
    public float maxHoldTime = 1.5f;
    public float deadzone = 0.2f;
    public float horizontalThreshold = 30f;

    [Header("Force Settings")]
    public ForceMode jumpForceMode = ForceMode.Impulse;
    public ForceMode dashForceMode = ForceMode.Impulse;
    public ForceMode airDashForceMode = ForceMode.Impulse;

    [Header("Allowed Jump Angles (degrees)")]
    public int numberOfDirections = 8; // 8 = N, NE, E, SE, S, SW, W, NW
    private float[] allowedAngles;

    private Vector2 inputDirection = Vector2.zero;
    private Vector2 lastSnappedDirection = Vector2.zero;
    private Vector2 storedDirection = Vector2.zero;
    
    private bool southButtonPressed;
    private Rigidbody rb;
    private Transform camTransform;
    
    private bool actionInProgress;
    private bool actionCompleted;
    private float holdTime;
    private float holdRatio;
    
    private float angle;
    private bool isEastDirection;
    private bool isWestDirection;
    private bool isJumping;
    private bool isDashing;
    
    // Target direction and force
    private Vector3 moveDirection;
    private float forceMagnitude;

    public InputActionAsset inputActions;
    private InputAction leftAnalogStickInput;
    private InputAction southButtonInput;

    public float gizmosLength;
    public SurfaceState currentSurfaceState = SurfaceState.Ground; // Default is standing on ground
    public Direction directionState;
    private float lastActionTime;
    private bool isFalling = false;
    public float lastContactTime;
    private const float NO_CONTACT_THRESHOLD = 0.2f; // Check for no contact after 0.2 seconds

   [Header("Air Movement")]
    public bool allowedToMoveInAir = false;
    public bool isInAir = false;
    public float airDashForce = 15f;
    public float airDashCooldown = 0.5f;
    private float lastAirDashTime = 0f;
    private bool hasUsedAirDash = false;
    public float maxAirDashDistance;
    public ForceMode mode;
    private float inputWaitTimer = 0f;
    private const float baseInputWaitTime = 0.05f; 
    public float stateBuffer = 0.25f;
    private float stateTimer = 0f;
    private bool stateChanged = false;

    void Awake()
    {
        allowedAngles = new float[numberOfDirections];
        float step = 360f / numberOfDirections;
        for (int i = 0; i < numberOfDirections; i++)
        {
            allowedAngles[i] = i * step;
        }

        var map = inputActions.FindActionMap("Player");
        leftAnalogStickInput = map.FindAction("Movement");
        southButtonInput = map.FindAction("Jump");

        leftAnalogStickInput.Enable();
        southButtonInput.Enable();

        rb = GetComponent<Rigidbody>();
        camTransform = Camera.main?.transform;
    }

    void OnEnable()
    {
        leftAnalogStickInput.started += OnStickStarted;
        leftAnalogStickInput.performed += OnStickPerformed;
        leftAnalogStickInput.canceled += OnStickCanceled;

        southButtonInput.started += OnSouthButtonStarted;
        southButtonInput.performed += OnSouthButtonPerformed;
        southButtonInput.canceled += OnSouthButtonCanceled;
    }

    void OnDisable()
    {
        leftAnalogStickInput.started -= OnStickStarted;
        leftAnalogStickInput.performed -= OnStickPerformed;
        leftAnalogStickInput.canceled -= OnStickCanceled;

        southButtonInput.started -= OnSouthButtonStarted;
        southButtonInput.performed -= OnSouthButtonPerformed;
        southButtonInput.canceled -= OnSouthButtonCanceled;
    }

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

    private (float minAngle, float maxAngle) GetAllowedJumpRange()
    {

        // If in air, return an impossible range to prevent jumping
        if (isInAir)
        {
            return (0f, 0f); // No valid jump angles
        }

        return currentSurfaceState switch
        {
            SurfaceState.Ground => (45f, 135f),// Upward cone
            SurfaceState.LeftWall => (315f, 45f),// Rightward
            SurfaceState.RightWall => (135f, 225f),// Leftward
            SurfaceState.Ceiling => (225f, 315f),// Downward
            _ => (0f, 360f),// All directions
        };
    }

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
    
    private bool IsAngleWithinRange(float angle, float minAngle, float maxAngle)
    {
        if (minAngle < maxAngle)
            return angle >= minAngle && angle <= maxAngle;
        else
            return angle >= minAngle || angle <= maxAngle; // handle wrap-around (like 270–90)
    }

    void Update()
    {
        // Handle button hold logic
        if (southButtonPressed && !actionInProgress)
        {
            holdTime += Time.deltaTime;
            
            // Auto-trigger if max hold time is reached
            if (holdTime >= maxHoldTime && lastSnappedDirection != Vector2.zero)
            {
                PerformAction();
            }
        }

        // Detect falling state
        if (!isFalling && rb.linearVelocity .y < -0.5f && currentSurfaceState == SurfaceState.Ceiling)
        {
            isFalling = true;
            Debug.Log("Player is falling from ceiling");
        }

        // Reset falling state when touching ground or other surface
        if (isFalling && rb.linearVelocity.y >= -0.1f)
        {
            isFalling = false;
        }

        // Prevent state transition bug
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

    void FixedUpdate()
    {
        if (actionInProgress && !actionCompleted)
        {
            if (isJumping || isDashing)
            {
                if (rb.linearVelocity .magnitude < 0.1f)
                {
                    // ForceMode mode = isJumping ? jumpForceMode : dashForceMode;
                    if(isJumping && isInAir) 
                    {
                        mode = airDashForceMode;
                    }
                    else if(isJumping) 
                    {
                        mode = jumpForceMode;
                    }
                    else if (isDashing) 
                    {
                        mode = dashForceMode;
                    }

                    rb.AddForce(moveDirection * forceMagnitude, mode);

                    Debug.Log($"Applied {(isJumping ? "jump" : "dash")} force: {moveDirection * forceMagnitude}");

                    actionCompleted = true;

                    // Reset everything after force applied
                    Invoke(nameof(ResetActionState), 0.1f); // delay to allow physics to process
                }
            }
        }

        // Debug.Log($"Time: {Time.time - lastContactTime} + No Contact Threshold: {NO_CONTACT_THRESHOLD}");
        // isInAir = Time.time - lastContactTime > NO_CONTACT_THRESHOLD;
        
        isInAir = !IsCollidingWithSurface();

        // Add a failsafe timer to prevent permanent stuck state
        if (actionInProgress && Time.time - lastActionTime > 2f)
        {
            Debug.LogWarning("Action timeout - forcing reset");
            ForceResetAllActions();
        }

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

    private bool IsCollidingWithSurface()
    {
        // Using a small overlap sphere to check for collisions
        Collider[] colliders = Physics.OverlapSphere(
            transform.position, 
            GetComponent<Collider>().bounds.extents.y + 0.1f // Slightly larger than collider
            // LayerMask.GetMask("Default") // Adjust to your layer masks
        );
        
        // If we have any colliders other than ourselves, we're touching something
        return colliders.Length > 1; // > 1 because we'll detect our own collider
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleSurfaceState(collision);

        // Reset air dash when touching any surface
        hasUsedAirDash = false;
        isInAir = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleSurfaceState(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        // Set lastContactTime to track when we last touched a surface
        lastContactTime = Time.time;
    }

    private void HandleSurfaceState(Collision collision)
    {
        // Reset contact timer
        lastContactTime = Time.time;
        
        // Get the most vertical normal from all contact points
        Vector3 mostSignificantNormal = Vector3.zero;
        float bestDot = -1f;

        foreach (ContactPoint contact in collision.contacts)
        {
            // Find the most significant alignment with any axis
            float upDot = Mathf.Abs(Vector3.Dot(contact.normal, Vector3.up));
            float rightDot = Mathf.Abs(Vector3.Dot(contact.normal, Vector3.right));
            float forwardDot = Mathf.Abs(Vector3.Dot(contact.normal, Vector3.forward));
            
            float maxDot = Mathf.Max(upDot, rightDot, forwardDot);
            
            if (maxDot > bestDot)
            {
                bestDot = maxDot;
                mostSignificantNormal = contact.normal;
            }
        }
        
        // Determine surface state from the most significant normal
        if (bestDot > 0.7f) // Threshold for alignment
        {
            if(!stateChanged) 
            {
                SurfaceState previousState = currentSurfaceState;
                
                if (Vector3.Dot(mostSignificantNormal, Vector3.up) > 0.7f)
                {
                    currentSurfaceState = SurfaceState.Ground;
                }
                else if (Vector3.Dot(mostSignificantNormal, Vector3.down) > 0.7f)
                {
                    currentSurfaceState = SurfaceState.Ceiling;
                }
                else if (Vector3.Dot(mostSignificantNormal, Vector3.left) > 0.7f)
                {
                    currentSurfaceState = SurfaceState.RightWall;
                }
                else if (Vector3.Dot(mostSignificantNormal, Vector3.right) > 0.7f)
                {
                    currentSurfaceState = SurfaceState.LeftWall;
                }
                else if((Vector3.Dot(mostSignificantNormal, Vector3.up) > 0.7f || (Vector3.Dot(mostSignificantNormal, Vector3.right) > 0.7f) || Vector3.Dot(mostSignificantNormal, Vector3.left) > 0.7f) && rb.linearVelocity.y == 0) 
                {
                    Debug.Log("Player is against wall but also grounded");
                    currentSurfaceState = SurfaceState.Ground;
                }
                
                // If state changed, force action reset
                if (previousState != currentSurfaceState)
                {
                    Debug.Log($"Surface State changed from {previousState} to {currentSurfaceState}");
                    ForceResetAllActions();
                    stateChanged = true;
                }
            }
        }
    }

    private void ForceResetAllActions()
    {
        actionInProgress = false;
        actionCompleted = false;
        isJumping = false;
        isDashing = false;
        isFalling = false; 
        holdTime = 0f;
        rb.linearVelocity  = Vector3.zero; // Stop all movement
        
        // Clear any pending invokes
        CancelInvoke(nameof(ResetActionState));
        
        southButtonPressed = false;
        lastSnappedDirection = Vector2.zero;
        
        Debug.Log("Forced complete action reset due to surface change");
    }

    private void PerformAction()
    {
        if (lastSnappedDirection == Vector2.zero || actionInProgress)
            return;

        // Check if we're in air and can air dash
        if (isInAir)
        {
            if (!allowedToMoveInAir)
            {
                Debug.Log("Air movement not allowed");
                return;
            }
            
            if (hasUsedAirDash)
            {
                Debug.Log("Air dash already used");
                return;
            }
            
            if (Time.time - lastAirDashTime < airDashCooldown)
            {
                Debug.Log("Air dash on cooldown");
                return;
            }
        }

        // Prevent actions while falling from ceiling
        if (isFalling /* && currentSurfaceState == SurfaceState.Ceiling*/)
        {
            Debug.Log("Ignoring action input while falling from ceiling");
            return;
        }

        holdRatio = Mathf.Clamp01(holdTime / maxHoldTime);
        if (holdRatio < 0.1f)
            holdRatio = 0.1f; // Minimum hold ratio

        // Calculate angle
        angle = Mathf.Atan2(lastSnappedDirection.y, lastSnappedDirection.x) * Mathf.Rad2Deg;
        angle = (angle + 360f) % 360f; // Normalize angle between 0–360

        Direction majorDirection = GetMajorDirection(angle);
        
        // Get allowed ranges for both jump and dash
        (float jumpMinAngle, float jumpMaxAngle) = GetAllowedJumpRange();
        (float dashMinAngle, float dashMaxAngle) = GetAllowedDashRange();
        
        // Check if the angle is within jump or dash range
        bool isJumpAllowed = IsAngleWithinRange(angle, jumpMinAngle, jumpMaxAngle);
        bool isDashAllowed = IsAngleWithinRange(angle, dashMinAngle, dashMaxAngle);

        // Special handling for air dash
        if (isInAir && allowedToMoveInAir && !hasUsedAirDash)
        {
            // Override to force dash in air
            isDashAllowed = true;
            isJumpAllowed = false;
        }

        Debug.Log($"PerformAction -> Angle: {angle:F1}°, Major Direction: {majorDirection}, " +
                $"Jump Range: {jumpMinAngle}-{jumpMaxAngle}, Dash Range: {dashMinAngle}-{dashMaxAngle}, " +
                $"Jump Allowed: {isJumpAllowed}, Dash Allowed: {isDashAllowed}");

        // If neither is allowed, exit
        if (!isJumpAllowed && !isDashAllowed)
        {
            Debug.LogWarning($"Direction {angle:F1}° is not allowed for jump or dash on {currentSurfaceState}.");
            return;
        }

        // Stop current movement to start fresh
        rb.linearVelocity = Vector3.zero;
        
        // Air dash takes priority when in air
        if (isInAir && allowedToMoveInAir && !hasUsedAirDash)
        {
            SetupForce(maxDashDistance, jumpHeight, 1f, airDashForce, "Dash");

            hasUsedAirDash = true;
            lastAirDashTime = Time.time;
        }
        // Prioritize dash for horizontal movement (East/West) if it's allowed
        else if (isDashAllowed && (majorDirection == Direction.East || majorDirection == Direction.West))
        {
            isEastDirection = majorDirection == Direction.East;
            isWestDirection = majorDirection == Direction.West;
            SetupDash();
        }
        // On walls, check for vertical dash (North/South)
        else if (isDashAllowed && 
                (currentSurfaceState == SurfaceState.LeftWall || currentSurfaceState == SurfaceState.RightWall) &&
                (majorDirection == Direction.North || majorDirection == Direction.South))
        {
            // For vertical dash on walls
            isEastDirection = false;
            isWestDirection = false;
            SetupDash();
        }
        // Default to jump if dash is not applicable but jump is allowed
        else if (isJumpAllowed)
        {
            // SetupJump(jumpForce);
            SetupForce(maxJumpDistance, jumpHeight, 1f, jumpForce, "Jump");
        }

        actionInProgress = true;
        southButtonPressed = false;
        lastActionTime = Time.time; // Track when action started
    }

    private void SetupForce(float maxTravelDistance, float forceHeight, float gravityMultiplier, float forcePower, string action)
    {
        if(action.Contains("Jump")) 
        {
            isJumping = true;
            isDashing = false;
        }
        else if(action.Contains("Dash")) 
        {
            isJumping = false;
            isDashing = true;
        }

        float targetDistance = maxTravelDistance * holdRatio;
        
        // // Calculate jump physics
        float gravity = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
        float verticalVelocity = Mathf.Sqrt(2 * gravity * forceHeight);

        float horizontalSpeed = Mathf.Sqrt(targetDistance * gravity / Mathf.Sin(2 * Mathf.Deg2Rad * 45));

        moveDirection = lastSnappedDirection * horizontalSpeed;
        lastSnappedDirection.y = verticalVelocity;

        // Set force magnitude (you can tune airDashForce separately if you want)
        forceMagnitude = forcePower * holdRatio;

        Debug.Log($"Air Dash setup - Direction: {moveDirection}, Force: {forceMagnitude}");
    }

    private void SetupDash()
    {
        isJumping = false;
        isDashing = true;
        
        // Calculate the distance based on hold ratio
        float targetDistance = maxDashDistance * holdRatio;
        
        // Determine dash direction based on surface state and major direction
        Direction majorDirection = GetMajorDirection(angle);

        moveDirection = currentSurfaceState switch
        {
            SurfaceState.Ground or SurfaceState.Ceiling => isEastDirection ? Vector3.right : Vector3.left,// Horizontal dash on ground or ceiling
            SurfaceState.LeftWall or SurfaceState.RightWall => majorDirection == Direction.North ? Vector3.up : Vector3.down,// Vertical dash on walls
            _ => isEastDirection ? Vector3.right : Vector3.left,// Fallback to horizontal
        };

        // Set force magnitude
        forceMagnitude = dashForce * targetDistance;
        
        Debug.Log($"Dash setup - Direction: {moveDirection}, Force: {forceMagnitude}, Surface: {currentSurfaceState}");
    }

    private void ResetActionState()
    {
        actionInProgress = false;
        actionCompleted = false;
        isJumping = false;
        isDashing = false;
        holdTime = 0f;
    }

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
        // Set a small timer
        inputWaitTimer = baseInputWaitTime;

        // If in air, increase timer by 2
        if (isInAir)
        {
            inputWaitTimer += 2f;
        }

        if (inputWaitTimer > 0f)
        {
            inputWaitTimer -= Time.deltaTime;
        }

        inputDirection = ctx.ReadValue<Vector2>();
    }

    public void OnStickPerformed(InputAction.CallbackContext ctx)
    {
        // Wait for the timer to finish before proceeding
        if (inputWaitTimer > 0f)
        {
            inputWaitTimer = 0f;
            return;
        }
        else if(inputWaitTimer <= 0) 
        {
            if (inputDirection.magnitude < deadzone) inputDirection = Vector2.zero;

            if (!actionInProgress)
            {
                if (!isInAir)
                {
                    lastSnappedDirection = GetSnappedDirection(inputDirection).normalized;

                    if (lastSnappedDirection != Vector2.zero)
                    {
                        Debug.Log($"Snapped Direction: {lastSnappedDirection}");
                    }
                }
                else
                {
                    lastSnappedDirection = GetSnappedDirection(inputDirection).normalized;

                    if (lastSnappedDirection != Vector2.zero)
                    {
                        Debug.Log($"In Air Free Direction: {lastSnappedDirection}");
                    }
                }

                // Reset action state if we have a new direction
                if (lastSnappedDirection != Vector2.zero && actionCompleted)
                {
                    ResetActionState();
                }
            }
        }
    }
    
    public void OnStickCanceled(InputAction.CallbackContext ctx)
    {
        if (!actionInProgress)
        {
            storedDirection = lastSnappedDirection;
        }
    }

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
        Vector3 origin = transform.position;

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

    private void DrawArrowHead(Vector3 position, Vector3 direction)
    {
        float arrowHeadAngle = 20.0f;
        float arrowHeadLength = 0.3f;

        Vector3 right = Quaternion.AngleAxis(180 + arrowHeadAngle, Vector3.forward) * direction.normalized;
        Vector3 left = Quaternion.AngleAxis(180 - arrowHeadAngle, Vector3.forward) * direction.normalized;

        Gizmos.DrawLine(position, position + right * arrowHeadLength);
        Gizmos.DrawLine(position, position + left * arrowHeadLength);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;
        
        (float minAngle, float maxAngle) = GetAllowedJumpRange();

        Vector3 center = transform.position;

        DrawAngleArc(center, minAngle, maxAngle, 2f); // 2f is the radius
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
}