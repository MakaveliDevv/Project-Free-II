// using UnityEngine;
// using UnityEngine.InputSystem;

// public class JumpControllerAdvanced : MonoBehaviour
// {
//     public InputActionAsset inputActions;
//     private InputAction movementAction;

//     [Header("Jump Settings")]
//     public float jumpSpeed = 10f;
//     public float maxJumpHeight = 5f;
//     public float maxJumpDistance = 7f;
//     public float fallSpeed = -10f;
//     public float sideMoveSpeed = 5f;
//     public float jumpChargeRate = 1f;

//     [Header("Hover Settings")]
//     public float hoverDuration = 0.2f;
//     public float hoverWobbleAmplitude = 0.2f;
//     public float hoverWobbleFrequency = 2f;

//     [Header("Hover Trigger Thresholds")]
//     public float minHoverHeight = 1f;
//     public float minHoverDistance = 1f;
//     public float minDiagonalHoverHeight = 1f; // Minimum height required to hover after diagonal jump

//     [Header("Dash Settings")]
//     public float dashSpeed = 12f;
//     public float dashAngleThreshold = 20f;
//     public float maxDashDistance = 10f;
//     public float dashCooldownTime = 1f;
//     public float maxHoldTime = 0.5f;

//     public float dashMomentumDuration = 0.5f;
//     public float dashMomentumSpeed = 3f;

//     private float dashCooldownTimer = 0f;
//     private Vector3 dashStartPos;
//     private float dashDistanceTravelled = 0f;
//     private float dashHoldTime = 0f;
//     private float dashTargetDistance = 0f;
//     private bool isPreparingDash = false;
//     private Vector2 dashDirection;

//     private bool isDashingMomentum = false;
//     private float dashMomentumTimer = 0f;
//     private Vector3 dashMomentumDirection = Vector3.zero;

//     private bool IsFullDashReady => dashHoldTime >= maxHoldTime;

//     [Header("Jump Direction Threshold")]
//     public float jumpDirectionMinUpwardAngle = 70f;
//     public float sideAngleDeadZone = 15f;
//     public float diagonalJumpAngleMin = 20f; // Minimum angle to consider a jump diagonal
//     public float diagonalJumpAngleMax = 70f; // Maximum angle to consider a jump diagonal

//     [Header("Ground Detection")]
//     public LayerMask groundLayer;
//     public float groundCheckDistance = 0.1f;

//     private Rigidbody rb;
//     private float hoverTimer = 0f;
//     private float jumpCharge = 0f;
//     private Vector2 lastHeldDirection = Vector2.zero;
//     private bool isCharging = false;
//     private Vector3 jumpStartPos;
//     private bool isDiagonalJump = false;

//     public enum JumpState { Idle, Charging, Jumping, Hovering, Descending, Dashing }
//     public JumpState state = JumpState.Idle;

//     private Vector2 cachedInputDir = Vector2.zero;
//     public float hoverThresholdTolerance = 0.1f;
    
//     // Add this to track if user deliberately canceled hovering
//     private bool hoverCanceledByInput = false;

//     void Start()
//     {
//         rb = GetComponentInChildren<Rigidbody>();
//         rb.useGravity = true;

//         var playerMap = inputActions.FindActionMap("Player");
//         playerMap.Enable();
//         movementAction = playerMap.FindAction("Movement");
//         movementAction.Enable();
//     }

//     void Update()
//     {
//         if (dashCooldownTimer > 0)
//             dashCooldownTimer -= Time.deltaTime;

//         Vector2 rawInput = movementAction.ReadValue<Vector2>();
//         cachedInputDir = rawInput.sqrMagnitude < 0.01f ? Vector2.zero : rawInput.normalized;
//         cachedInputDir = SnapDirectionToClosestAngle(cachedInputDir);

//         HandleInput(cachedInputDir);

//         Debug.DrawRay(transform.position, new Vector3(cachedInputDir.x, cachedInputDir.y, 0f), Color.cyan);

//         if (isPreparingDash)
//         {
//             dashHoldTime += Time.deltaTime;
//             Vector2 currentInput = movementAction.ReadValue<Vector2>();

//             if (currentInput.sqrMagnitude < 0.1f && !IsFullDashReady)
//             {
//                 PerformDash(dashDirection);
//                 isPreparingDash = false;
//                 state = JumpState.Dashing;
//             }
//             else if (IsFullDashReady)
//             {
//                 PerformDash(dashDirection);
//                 isPreparingDash = false;
//                 state = JumpState.Dashing;
//             }
//         }
//     }

//     void FixedUpdate()
//     {
//         if (state == JumpState.Dashing)
//         {
//             dashDistanceTravelled = Vector3.Distance(dashStartPos, rb.position);

//             if (dashDistanceTravelled >= dashTargetDistance || rb.linearVelocity.magnitude < 0.1f)
//             {
//                 rb.linearVelocity = Vector3.zero;
//                 state = JumpState.Idle;

//                 isDashingMomentum = true;
//                 dashMomentumDirection = rb.transform.right * Mathf.Sign(dashDirection.x);
//                 dashMomentumTimer = 0f;

//                 dashCooldownTimer = dashTargetDistance < maxDashDistance ? dashCooldownTime / 2f : dashCooldownTime;
//             }
//         }

//         if (isDashingMomentum)
//         {
//             dashMomentumTimer += Time.deltaTime;
//             if (dashMomentumTimer < dashMomentumDuration)
//             {
//                 rb.linearVelocity = new Vector3(dashMomentumDirection.x * dashMomentumSpeed, rb.linearVelocity.y, 0f);
//             }
//             else
//             {
//                 isDashingMomentum = false;
//                 rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
//             }
//         }
//     }

//     private void HandleInput(Vector2 inputDir)
//     {
//         CheckDashInput(inputDir);

//         switch (state)
//         {
//             case JumpState.Idle:
//                 hoverCanceledByInput = false; // Reset this when we're idle
//                 isDiagonalJump = false; // Reset diagonal jump flag
//                 if (IsGrounded() && inputDir != Vector2.zero && inputDir.y > 0.1f)
//                 {
//                     isCharging = true;
//                     jumpCharge = 0f;
//                     lastHeldDirection = inputDir;
//                     state = JumpState.Charging;
//                 }
//                 break;

//             case JumpState.Charging:
//                 jumpCharge += Time.deltaTime;
//                 if (inputDir == Vector2.zero || jumpCharge >= (1f / jumpChargeRate))
//                 {
//                     // Check if this will be a diagonal jump
//                     CheckIfDiagonalJump(lastHeldDirection);
//                     PerformJump(lastHeldDirection);
//                     state = JumpState.Jumping;
//                 }
//                 else if (inputDir.y < -0.5f)
//                 {
//                     ForceDrop();
//                 }
//                 else
//                 {
//                     lastHeldDirection = inputDir;
//                 }
//                 break;

//             case JumpState.Jumping:
//                 float currentHeight = rb.transform.position.y - jumpStartPos.y;
//                 float jumpDistance = Vector3.Distance(new Vector3(rb.transform.position.x, 0, 0), new Vector3(jumpStartPos.x, 0, 0));
//                 bool reachedMaxJump = currentHeight >= (maxJumpHeight - hoverThresholdTolerance) ||
//                                     jumpDistance >= (maxJumpDistance - hoverThresholdTolerance);

//                 Debug.Log($"[Jumping] Height: {currentHeight:F2}/{maxJumpHeight}, Distance: {jumpDistance:F2}/{maxJumpDistance}, isDiagonal: {isDiagonalJump}");

//                 if (inputDir.y < -0.5f)
//                 {
//                     ForceDrop(); // Allow manual drop
//                 }
//                 else if (CanEnterHover() && (reachedMaxJump || inputDir == Vector2.zero))
//                 {
//                     // For diagonal jumps, we need an additional height check
//                     if (isDiagonalJump)
//                     {
//                         float groundDistancee = DistanceToGround();
//                         if (groundDistancee >= minDiagonalHoverHeight)
//                         {
//                             EnterHover();
//                         }
//                         else
//                         {
//                             // Too close to ground for hovering on diagonal jump
//                             Debug.Log($"[DiagonalJump] Too close to ground: {groundDistancee:F2} < {minDiagonalHoverHeight}");

//                             // Transition to descending state
//                             rb.useGravity = true;
//                             state = JumpState.Descending;
//                         }
//                     }
//                     else
//                     {
//                         // Non-diagonal jumps use normal hover logic
//                         EnterHover();
//                     }
//                 }
//                 else
//                 {
//                     Vector2 moveDirection = lastHeldDirection.normalized;
//                     rb.linearVelocity = new Vector3(moveDirection.x * sideMoveSpeed, rb.linearVelocity.y, 0f);
//                 }
//                 break;

//             case JumpState.Hovering:
//                 hoverTimer += Time.deltaTime;
//                 float wobble = Mathf.Sin(hoverTimer * hoverWobbleFrequency) * hoverWobbleAmplitude;
//                 rb.linearVelocity = new Vector3(0f, wobble, 0f);

//                 float groundDistance = DistanceToGround();
//                 float distanceFromStart = Vector3.Distance(new Vector3(rb.transform.position.x, 0, 0), new Vector3(jumpStartPos.x, 0, 0));

//                 bool stillAboveHoverThreshold = groundDistance >= (minHoverHeight - hoverThresholdTolerance) ||
//                                                 distanceFromStart >= (minHoverDistance - hoverThresholdTolerance);

//                 // If pointing down, set our flag and cancel hover
//                 if (inputDir.y < -0.5f)
//                 {
//                     hoverCanceledByInput = true;
//                     rb.useGravity = true;
//                     state = JumpState.Descending;
//                 }
//                 // Time out or not above threshold anymore
//                 else if (hoverTimer >= hoverDuration || !stillAboveHoverThreshold)
//                 {
//                     rb.useGravity = true;
//                     state = JumpState.Descending;
//                 }
//                 break;

//             case JumpState.Descending:
//                 // Only try to re-enter hover if we didn't explicitly cancel with input
//                 // if (!hoverCanceledByInput && CanEnterHover())
//                 // {
//                 //     EnterHover();
//                 //     return;
//                 // }
                
//                 // Reset the flag when stick is released or pointing up
//                 if (inputDir == Vector2.zero || inputDir.y > 0.1f)
//                 {
//                     hoverCanceledByInput = false;
//                 }

//                 if (IsGrounded())
//                 {
//                     state = JumpState.Idle;
//                 }
//                 break;
//         }
//     }

//     private void CheckIfDiagonalJump(Vector2 direction)
//     {
//         // Calculate angle from horizontal
//         float angle = Vector2.Angle(Vector2.right, new Vector2(direction.x, 0));
        
//         // Check if this is a diagonal jump based on direction (between min and max angles)
//         float verticalAngle = Vector2.Angle(Vector2.up, direction);
//         isDiagonalJump = Mathf.Abs(direction.x) > 0.3f && 
//                          verticalAngle >= diagonalJumpAngleMin && 
//                          verticalAngle <= diagonalJumpAngleMax;
        
//         Debug.Log($"[DiagonalCheck] Direction: {direction}, Angle: {verticalAngle:F2}, IsDiagonal: {isDiagonalJump}");
//     }

//     private bool CanEnterHover()
//     {
//         float groundDistance = DistanceToGround();
//         float currentHeight = rb.transform.position.y - jumpStartPos.y;
//         float jumpDistance = Vector3.Distance(new Vector3(rb.transform.position.x, 0, 0), new Vector3(jumpStartPos.x, 0, 0));

//         bool highEnough = groundDistance >= (minHoverHeight - hoverThresholdTolerance);
//         bool farEnough = jumpDistance >= (minHoverDistance - hoverThresholdTolerance);

//         Debug.Log($"[HoverCheck] GroundDistance: {groundDistance:F2}, JumpHeight: {currentHeight:F2}, JumpDistance: {jumpDistance:F2}, HighEnough: {highEnough}, FarEnough: {farEnough}");

//         return highEnough || farEnough;
//     }

//     private float DistanceToGround()
//     {
//         Collider col = rb.GetComponent<Collider>();
//         Vector3 origin = col.bounds.center;
//         origin.y = col.bounds.min.y + 0.01f;
//         Ray ray = new Ray(origin, Vector3.down);
//         if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
//         {
//             return hit.distance;
//         }
//         return Mathf.Infinity;
//     }

//     private void CheckDashInput(Vector2 inputDir)
//     {
//         bool isDashDirection = Mathf.Abs(inputDir.x) > 0.4f && Mathf.Abs(inputDir.y) < 0.4f;
//         bool canDashFromState = state == JumpState.Idle || state == JumpState.Hovering || state == JumpState.Jumping || state == JumpState.Descending;
//         bool inputValid = isDashDirection && dashCooldownTimer <= 0;

//         if (canDashFromState && inputValid && !isPreparingDash)
//         {
//             isPreparingDash = true;
//             dashHoldTime = 0f;
//             dashDirection = inputDir.normalized;
//         }
//     }

//     private void PerformDash(Vector2 direction)
//     {
//         rb.useGravity = true;
//         dashStartPos = rb.position;
//         Vector3 dashDir = new Vector3(direction.x, 0f, 0f).normalized;
//         float holdRatio = Mathf.Clamp01(dashHoldTime / maxHoldTime);
//         dashTargetDistance = Mathf.Lerp(0.5f * maxDashDistance, maxDashDistance, holdRatio);

//         rb.linearVelocity = dashDir * dashSpeed;
//         dashDistanceTravelled = 0f;
//         isDashingMomentum = false;
//     }

//     private void PerformJump(Vector2 direction)
//     {
//         Vector2 jumpVec = direction.normalized;

//         float verticalRatio = Mathf.Clamp01(jumpVec.y);
//         float horizontalRatio = Mathf.Clamp01(Mathf.Abs(jumpVec.x));

//         // Scale jump height and horizontal movement by charge amount
//         float chargeRatio = Mathf.Clamp01(jumpCharge * jumpChargeRate);

//         float dynamicMaxJumpHeight = maxJumpHeight * verticalRatio;
//         float maxVerticalSpeed = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * dynamicMaxJumpHeight);
//         float yVelocity = Mathf.Max(maxVerticalSpeed, 0.1f) * chargeRatio;

//         float xVelocity = horizontalRatio * jumpSpeed * Mathf.Sign(jumpVec.x) * chargeRatio;

//         rb.linearVelocity = new Vector3(xVelocity, yVelocity, 0f);
//         rb.useGravity = true;
//         jumpStartPos = rb.transform.position;
//     }

//     private void EnterHover()
//     {
//         hoverTimer = 0f;
//         rb.useGravity = false;
//         rb.linearVelocity = Vector3.zero;
//         state = JumpState.Hovering;
//     }

//     private void ForceDrop()
//     {
//         rb.useGravity = true;
//         rb.linearVelocity = new Vector3(0f, fallSpeed, 0f);
//         state = JumpState.Descending;
//         hoverCanceledByInput = true; // Set this when we force drop
//     }

//     private bool IsGrounded()
//     {
//         Collider col = rb.GetComponent<Collider>();
//         Vector3 origin = col.bounds.center;
//         origin.y = col.bounds.min.y + 0.01f;
//         float distance = groundCheckDistance + 0.1f;

//         bool grounded = Physics.Raycast(origin, Vector3.down, distance, groundLayer);
//         Debug.DrawRay(origin, Vector3.down * distance, grounded ? Color.green : Color.red);
//         return grounded;
//     }

//     private Vector2 SnapDirectionToClosestAngle(Vector2 inputDir)
//     {
//         if (inputDir.sqrMagnitude < 0.1f) return inputDir;

//         Vector2[] snapAngles = new Vector2[]
//         {
//             new Vector2(1, 0),
//             new Vector2(0.7071f, 0.7071f),
//             new Vector2(0, 1),
//             new Vector2(-0.7071f, 0.7071f),
//             new Vector2(-1, 0),
//             new Vector2(-0.7071f, -0.7071f),
//             new Vector2(0, -1),
//             new Vector2(0.7071f, -0.7071f),
//         };

//         float closestAngle = float.MaxValue;
//         Vector2 closestDirection = inputDir;

//         foreach (var direction in snapAngles)
//         {
//             float angle = Vector2.Angle(inputDir, direction);
//             if (angle < closestAngle && angle <= 45f)
//             {
//                 closestAngle = angle;
//                 closestDirection = direction;
//             }
//         }

//         return closestDirection.normalized;
//     }
// }


using UnityEngine;
using UnityEngine.InputSystem;

public class JumpControllerAdvanced : MonoBehaviour
{
    public InputActionAsset inputActions;
    private InputAction movementAction;

    [Header("Jump Settings")]
    public float jumpSpeed = 10f;
    public float maxJumpHeight = 5f;
    public float maxJumpDistance = 7f;
    public float fallSpeed = -10f;
    public float sideMoveSpeed = 5f;
    public float jumpChargeRate = 1f;

    [Header("Hover Settings")]
    public float hoverDuration = 0.2f;
    public float hoverWobbleAmplitude = 0.2f;
    public float hoverWobbleFrequency = 2f;

    [Header("Hover Trigger Thresholds")]
    public float minHoverHeight = 1f;
    public float minHoverDistance = 1f;
    public float minDiagonalHoverHeight = 1f; // Minimum height required to hover after diagonal jump

    [Header("Dash Settings")]
    public float dashSpeed = 12f;
    public float dashAngleThreshold = 20f;
    public float maxDashDistance = 10f;
    public float dashCooldownTime = 1f;
    public float maxHoldTime = 0.5f;

    public float dashMomentumDuration = 0.5f;
    public float dashMomentumSpeed = 3f;

    private float dashCooldownTimer = 0f;
    private Vector3 dashStartPos;
    private float dashDistanceTravelled = 0f;
    private float dashHoldTime = 0f;
    private float dashTargetDistance = 0f;
    private bool isPreparingDash = false;
    private Vector2 dashDirection;

    private bool isDashingMomentum = false;
    private float dashMomentumTimer = 0f;
    private Vector3 dashMomentumDirection = Vector3.zero;

    private bool IsFullDashReady => dashHoldTime >= maxHoldTime;

    [Header("Jump Direction Settings")]
    public float diagonalJumpAngleMin = 20f; // Minimum angle to consider a jump diagonal
    public float diagonalJumpAngleMax = 70f; // Maximum angle to consider a jump diagonal
    
    // Directional jump settings
    [Header("Directional Jump Settings")]
    [Tooltip("Jump directions in clockwise order")]
    public string[] jumpDirectionNames = new string[] { "W", "WNW", "NW", "NNW", "N", "NNE", "NE", "ENE", "E" };
    
    // A multiplier for each direction that affects jump height and distance
    [Tooltip("Height multiplier for each jump direction")]
    public float[] heightMultipliers = new float[] { 0.1f, 0.3f, 0.5f, 0.8f, 1.0f, 0.8f, 0.5f, 0.3f, 0.1f };
    
    [Tooltip("Distance multiplier for each jump direction")]
    public float[] distanceMultipliers = new float[] { 1.0f, 0.9f, 0.8f, 0.6f, 0.1f, 0.6f, 0.8f, 0.9f, 1.0f };

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;

    private Rigidbody rb;
    private float hoverTimer = 0f;
    private float jumpCharge = 0f;
    private Vector2 lastHeldDirection = Vector2.zero;
    private bool isCharging = false;
    private Vector3 jumpStartPos;
    private bool isDiagonalJump = false;
    private int currentJumpDirectionIndex = -1;

    public enum JumpState { Idle, Charging, Jumping, Hovering, Descending, Dashing }
    public JumpState state = JumpState.Idle;

    private Vector2 cachedInputDir = Vector2.zero;
    public float hoverThresholdTolerance = 0.1f;
    
    // Add this to track if user deliberately canceled hovering
    private bool hoverCanceledByInput = false;

    void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();
        rb.useGravity = true;

        var playerMap = inputActions.FindActionMap("Player");
        playerMap.Enable();
        movementAction = playerMap.FindAction("Movement");
        movementAction.Enable();
    }

    void Update()
    {
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        Vector2 rawInput = movementAction.ReadValue<Vector2>();
        cachedInputDir = rawInput.sqrMagnitude < 0.01f ? Vector2.zero : rawInput.normalized;
        
        // Debug visualization for the raw input direction
        DrawDirectionDebug(cachedInputDir);
        
        // Handle input based on the current state
        HandleInput(cachedInputDir);

        if (isPreparingDash)
        {
            dashHoldTime += Time.deltaTime;
            Vector2 currentInput = movementAction.ReadValue<Vector2>();

            if (currentInput.sqrMagnitude < 0.1f && !IsFullDashReady)
            {
                PerformDash(dashDirection);
                isPreparingDash = false;
                state = JumpState.Dashing;
            }
            else if (IsFullDashReady)
            {
                PerformDash(dashDirection);
                isPreparingDash = false;
                state = JumpState.Dashing;
            }
        }
    }

    void FixedUpdate()
    {
        if (state == JumpState.Dashing)
        {
            dashDistanceTravelled = Vector3.Distance(dashStartPos, rb.position);

            if (dashDistanceTravelled >= dashTargetDistance || rb.linearVelocity.magnitude < 0.1f)
            {
                rb.linearVelocity = Vector3.zero;
                state = JumpState.Idle;

                isDashingMomentum = true;
                dashMomentumDirection = rb.transform.right * Mathf.Sign(dashDirection.x);
                dashMomentumTimer = 0f;

                dashCooldownTimer = dashTargetDistance < maxDashDistance ? dashCooldownTime / 2f : dashCooldownTime;
            }
        }

        if (isDashingMomentum)
        {
            dashMomentumTimer += Time.deltaTime;
            if (dashMomentumTimer < dashMomentumDuration)
            {
                rb.linearVelocity = new Vector3(dashMomentumDirection.x * dashMomentumSpeed, rb.linearVelocity.y, 0f);
            }
            else
            {
                isDashingMomentum = false;
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }
    }

    private void DrawDirectionDebug(Vector2 inputDir)
    {
        // Draw the actual input direction
        Debug.DrawRay(transform.position, new Vector3(inputDir.x, inputDir.y, 0f) * 2f, Color.cyan);
        
        // Draw current state as text in scene view
        Debug.DrawLine(transform.position, transform.position + Vector3.up * 2f, Color.white);
        
        // If we have a jump direction, draw it
        if (currentJumpDirectionIndex >= 0 && currentJumpDirectionIndex < jumpDirectionNames.Length)
        {
            // Calculate the appropriate direction vector based on the direction index
            Vector2 dirVector = GetDirectionVector(currentJumpDirectionIndex);
            Debug.DrawRay(transform.position, new Vector3(dirVector.x, dirVector.y, 0f) * 1.5f, Color.yellow);
        }
    }

    private Vector2 GetDirectionVector(int directionIndex)
    {
        // Return a normalized direction vector for each of our direction indices
        switch (directionIndex)
        {
            case 0: return new Vector2(-1f, 0f); // W
            case 1: return new Vector2(-0.9f, 0.4f).normalized; // WNW
            case 2: return new Vector2(-0.7f, 0.7f).normalized; // NW
            case 3: return new Vector2(-0.4f, 0.9f).normalized; // NNW
            case 4: return new Vector2(0f, 1f); // N
            case 5: return new Vector2(0.4f, 0.9f).normalized; // NNE
            case 6: return new Vector2(0.7f, 0.7f).normalized; // NE
            case 7: return new Vector2(0.9f, 0.4f).normalized; // ENE
            case 8: return new Vector2(1f, 0f); // E
            default: return new Vector2(0f, 1f); // Default to North
        }
    }

    private void HandleInput(Vector2 inputDir)
    {
        CheckDashInput(inputDir);

        switch (state)
        {
            case JumpState.Idle:
                hoverCanceledByInput = false; // Reset this when we're idle
                isDiagonalJump = false; // Reset diagonal jump flag
                currentJumpDirectionIndex = -1; // Reset current direction
                
                if (IsGrounded() && inputDir != Vector2.zero && inputDir.y > 0.1f)
                {
                    isCharging = true;
                    jumpCharge = 0f;
                    lastHeldDirection = inputDir;
                    currentJumpDirectionIndex = DetermineJumpDirection(inputDir);
                    
                    Debug.Log($"[Charging] Started charge with direction: {GetCurrentDirectionName()}, Index: {currentJumpDirectionIndex}");
                    
                    state = JumpState.Charging;
                }
                break;

            case JumpState.Charging:
                jumpCharge += Time.deltaTime;
                
                // Update the jump direction if input changes significantly
                if (inputDir.sqrMagnitude > 0.1f)
                {
                    int newDirectionIndex = DetermineJumpDirection(inputDir);
                    if (newDirectionIndex != currentJumpDirectionIndex)
                    {
                        Debug.Log($"[Charging] Direction changed: {GetCurrentDirectionName()} -> {jumpDirectionNames[newDirectionIndex]}");
                        currentJumpDirectionIndex = newDirectionIndex;
                        lastHeldDirection = inputDir;
                    }
                }
                
                if (inputDir == Vector2.zero || jumpCharge >= (1f / jumpChargeRate))
                {
                    // Check if this will be a diagonal jump
                    isDiagonalJump = IsDirectionDiagonal(currentJumpDirectionIndex);
                    Debug.Log($"[Jump] Performing jump in direction: {GetCurrentDirectionName()}, IsDiagonal: {isDiagonalJump}");
                    PerformJump(currentJumpDirectionIndex);
                    state = JumpState.Jumping;
                }
                else if (inputDir.y < -0.5f)
                {
                    ForceDrop();
                }
                break;

            case JumpState.Jumping:
                float currentHeight = rb.transform.position.y - jumpStartPos.y;
                float jumpDistance = Vector3.Distance(new Vector3(rb.transform.position.x, 0, 0), new Vector3(jumpStartPos.x, 0, 0));
                bool reachedMaxJump = currentHeight >= (maxJumpHeight - hoverThresholdTolerance) ||
                                    jumpDistance >= (maxJumpDistance - hoverThresholdTolerance);

                Debug.Log($"[Jumping] Height: {currentHeight:F2}/{maxJumpHeight}, Distance: {jumpDistance:F2}/{maxJumpDistance}, isDiagonal: {isDiagonalJump}, Direction: {GetCurrentDirectionName()}");

                if (inputDir.y < -0.5f)
                {
                    ForceDrop(); // Allow manual drop
                }
                else if (CanEnterHover() && (reachedMaxJump || inputDir == Vector2.zero))
                {
                    // For diagonal jumps, we need an additional height check
                    if (isDiagonalJump)
                    {
                        float groundDistancee = DistanceToGround();
                        if (groundDistancee >= minDiagonalHoverHeight)
                        {
                            EnterHover();
                        }
                        else
                        {
                            // Too close to ground for hovering on diagonal jump - transition to descending
                            Debug.Log($"[DiagonalJump] Too close to ground: {groundDistancee:F2} < {minDiagonalHoverHeight}");
                            rb.useGravity = true;
                            state = JumpState.Descending;
                        }
                    }
                    else
                    {
                        // Non-diagonal jumps use normal hover logic
                        EnterHover();
                    }
                }
                else
                {
                    Vector2 moveDirection = lastHeldDirection.normalized;
                    rb.linearVelocity = new Vector3(moveDirection.x * sideMoveSpeed, rb.linearVelocity.y, 0f);
                }
                break;

            case JumpState.Hovering:
                hoverTimer += Time.deltaTime;
                float wobble = Mathf.Sin(hoverTimer * hoverWobbleFrequency) * hoverWobbleAmplitude;
                rb.linearVelocity = new Vector3(0f, wobble, 0f);

                float groundDistance = DistanceToGround();
                float distanceFromStart = Vector3.Distance(new Vector3(rb.transform.position.x, 0, 0), new Vector3(jumpStartPos.x, 0, 0));

                bool stillAboveHoverThreshold = groundDistance >= (minHoverHeight - hoverThresholdTolerance) ||
                                                distanceFromStart >= (minHoverDistance - hoverThresholdTolerance);

                // If pointing down, set our flag and cancel hover
                if (inputDir.y < -0.5f)
                {
                    hoverCanceledByInput = true;
                    rb.useGravity = true;
                    state = JumpState.Descending;
                }
                // Time out or not above threshold anymore
                else if (hoverTimer >= hoverDuration || !stillAboveHoverThreshold)
                {
                    rb.useGravity = true;
                    state = JumpState.Descending;
                }
                break;

            case JumpState.Descending:
                // Reset the flag when stick is released or pointing up
                if (inputDir == Vector2.zero || inputDir.y > 0.1f)
                {
                    hoverCanceledByInput = false;
                }

                if (IsGrounded())
                {
                    state = JumpState.Idle;
                }
                break;
        }
    }

    private int DetermineJumpDirection(Vector2 inputDir)
    {
        if (inputDir.sqrMagnitude < 0.1f) return 4; // Default to North if no real input
        
        // Normalize input
        Vector2 normalizedInput = inputDir.normalized;
        
        // Calculate angle in degrees from positive X axis (East), counter-clockwise
        float inputAngle = Mathf.Atan2(normalizedInput.y, normalizedInput.x) * Mathf.Rad2Deg;
        
        // Normalize angle to 0-360 range
        if (inputAngle < 0) inputAngle += 360f;
        
        // Simple 8-direction detection based on 45-degree sectors
        // 0-22.5 or 337.5-360: East
        if ((inputAngle >= 0 && inputAngle < 22.5f) || (inputAngle >= 337.5f && inputAngle <= 360f))
            return 8; // East
        // 22.5-67.5: ENE
        else if (inputAngle >= 22.5f && inputAngle < 67.5f)
            return 7; // ENE
        // 67.5-112.5: North
        else if (inputAngle >= 67.5f && inputAngle < 112.5f)
            return 4; // North
        // 112.5-157.5: NNW
        else if (inputAngle >= 112.5f && inputAngle < 157.5f)
            return 3; // NNW
        // 157.5-202.5: West
        else if (inputAngle >= 157.5f && inputAngle < 202.5f)
            return 0; // West
        // 202.5-247.5: WSW (not in your list, will default to West)
        else if (inputAngle >= 202.5f && inputAngle < 247.5f)
            return 0; // West (since there's no WSW in your array)
        // 247.5-292.5: South (not in your list, will default to idle/no jump)
        else if (inputAngle >= 247.5f && inputAngle < 292.5f)
            return 4; // Default North (you might want to block jumps in this direction)
        // 292.5-337.5: ESE (not in your list, will default to East)
        else
            return 8; // East (since there's no ESE in your array)
    }
    
    private bool IsDirectionDiagonal(int directionIndex)
    {
        // Check if the direction is diagonal (not straight up, left, or right)
        return directionIndex == 1 || directionIndex == 2 || directionIndex == 3 || 
               directionIndex == 5 || directionIndex == 6 || directionIndex == 7;
    }
    
    private string GetCurrentDirectionName()
    {
        if (currentJumpDirectionIndex >= 0 && currentJumpDirectionIndex < jumpDirectionNames.Length)
            return jumpDirectionNames[currentJumpDirectionIndex];
        return "Unknown";
    }

    private float DistanceToGround()
    {
        Collider col = rb.GetComponent<Collider>();
        Vector3 origin = col.bounds.center;
        origin.y = col.bounds.min.y + 0.01f;
        Ray ray = new Ray(origin, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            return hit.distance;
        }
        return Mathf.Infinity;
    }

    private void CheckDashInput(Vector2 inputDir)
    {
        // Calculate the angle between the input direction and the x-axis
        float inputAngle = Mathf.Abs(Vector2.Angle(new Vector2(Mathf.Sign(inputDir.x), 0), inputDir));
        
        // Only consider as dash if the angle is within our very strict threshold (2-3 degrees)
        bool isDashDirection = inputDir.sqrMagnitude > 0.5f && inputAngle == 0f;
        
        bool canDashFromState = state == JumpState.Idle || state == JumpState.Hovering || state == JumpState.Descending;
        bool inputValid = isDashDirection && dashCooldownTimer <= 0;

        if (canDashFromState && inputValid && !isPreparingDash)
        {
            Debug.Log($"[DashCheck] Starting dash prep, angle: {inputAngle:F1}°");
            isPreparingDash = true;
            dashHoldTime = 0f;
            dashDirection = new Vector2(Mathf.Sign(inputDir.x), 0).normalized; // Perfectly horizontal
        }
    }
    
    // private void CheckDashInput(Vector2 inputDir)
    // {
    //     bool isDashDirection = Mathf.Abs(inputDir.x) > 0.4f && Mathf.Abs(inputDir.y) < 0.4f;
    //     bool canDashFromState = state == JumpState.Idle || state == JumpState.Hovering || state == JumpState.Jumping || state == JumpState.Descending;
    //     bool inputValid = isDashDirection && dashCooldownTimer <= 0;

    //     if (canDashFromState && inputValid && !isPreparingDash)
    //     {
    //         isPreparingDash = true;
    //         dashHoldTime = 0f;
    //         dashDirection = inputDir.normalized;
    //     }
    // }

    private void PerformDash(Vector2 direction)
    {
        rb.useGravity = true;
        dashStartPos = rb.position;
        Vector3 dashDir = new Vector3(direction.x, 0f, 0f).normalized;
        float holdRatio = Mathf.Clamp01(dashHoldTime / maxHoldTime);
        dashTargetDistance = Mathf.Lerp(0.5f * maxDashDistance, maxDashDistance, holdRatio);

        rb.linearVelocity = dashDir * dashSpeed;
        dashDistanceTravelled = 0f;
        isDashingMomentum = false;
    }

    private void PerformJump(int directionIndex)
    {
        // Make sure we have a valid direction index, default to North (4) if not
        if (directionIndex < 0 || directionIndex >= jumpDirectionNames.Length)
            directionIndex = 4;  // Default to North
        
        // Get the direction vector for this jump
        Vector2 jumpVec = GetDirectionVector(directionIndex);
        
        // Apply the multipliers for the specific direction
        float heightMult = heightMultipliers[directionIndex];
        float distanceMult = distanceMultipliers[directionIndex];
        
        // Scale jump height and horizontal movement by charge amount
        float chargeRatio = Mathf.Clamp01(jumpCharge * jumpChargeRate);
        
        // Calculate vertical velocity
        float dynamicMaxJumpHeight = maxJumpHeight * heightMult;
        float verticalSpeed = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * dynamicMaxJumpHeight);
        float yVelocity = Mathf.Max(verticalSpeed, 0.1f) * chargeRatio;
        
        // Calculate horizontal velocity
        float xVelocity = jumpSpeed * jumpVec.x * distanceMult * chargeRatio;
        
        // Set velocity (adjust the multiplier for y if needed based on the direction)
        rb.linearVelocity = new Vector3(xVelocity, yVelocity * jumpVec.y, 0f);
        rb.useGravity = true;
        jumpStartPos = rb.transform.position;
        
        Debug.Log($"[Jump] Direction: {GetCurrentDirectionName()}, HeightMult: {heightMult:F1}, " +
                  $"DistanceMult: {distanceMult:F1}, Velocity: ({xVelocity:F1}, {yVelocity * jumpVec.y:F1})");
    }

    private bool CanEnterHover()
    {
        float groundDistance = DistanceToGround();
        float currentHeight = rb.transform.position.y - jumpStartPos.y;
        float jumpDistance = Vector3.Distance(new Vector3(rb.transform.position.x, 0, 0), new Vector3(jumpStartPos.x, 0, 0));

        bool highEnough = groundDistance >= (minHoverHeight - hoverThresholdTolerance);
        bool farEnough = jumpDistance >= (minHoverDistance - hoverThresholdTolerance);

        Debug.Log($"[HoverCheck] GroundDistance: {groundDistance:F2}, JumpHeight: {currentHeight:F2}, JumpDistance: {jumpDistance:F2}, HighEnough: {highEnough}, FarEnough: {farEnough}");

        return highEnough || farEnough;
    }

    private void EnterHover()
    {
        hoverTimer = 0f;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        state = JumpState.Hovering;
    }

    private void ForceDrop()
    {
        rb.useGravity = true;
        rb.linearVelocity = new Vector3(0f, fallSpeed, 0f);
        state = JumpState.Descending;
        hoverCanceledByInput = true; // Set this when we force drop
    }

    private bool IsGrounded()
    {
        Collider col = rb.GetComponent<Collider>();
        Vector3 origin = col.bounds.center;
        origin.y = col.bounds.min.y + 0.01f;
        float distance = groundCheckDistance + 0.1f;

        bool grounded = Physics.Raycast(origin, Vector3.down, distance, groundLayer);
        Debug.DrawRay(origin, Vector3.down * distance, grounded ? Color.green : Color.red);
        return grounded;
    }
}


