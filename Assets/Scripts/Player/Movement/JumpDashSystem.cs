// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class JumpDashSystem
// {    
//     private readonly PlayerController playerController;
//     private readonly MonoBehaviour mono;
//     private readonly InputAction movementAction;

//     [Header("Jump Settings")]
//     public float maxJumpRange = 7f;
//     public float maxHoldTime = 1f;

//     [Header("Dash Settings")]
//     public float maxDashRange = 10f;
//     public float dashForceMultiplier = 1f;
//     public float stopDashAfterDuration = .5f;

//     [Header("Physics")]
//     public float jumpForceMultiplier = 1f;
//     public float epsilon = 0.1f;
//     public float clampMagnitudeMaxLength = 20f;
//     public List<Collider> walls = new(); 

//     [Header("Hover Settings")]
//     public float hoverDelay = 0.1f;
//     public float hoverDuration = 0.5f;
//     public float minHoverHeight = 2.0f; // Minimum height from ground required to hover
//     public float minDiagonalDistance = 1.5f; // Minimum diagonal travel distance required for diagonal jumps

//     [Header("Ground Detection")]
//     public float groundCheckDistance = 0.1f;
//     public LayerMask wallLayer;
//     public float wallCheckDistance = 0.25f;

//     [Header("Gravity Settings")]
//     public float gravityScale = 1f;
//     public float fastFallMultiplier = 2f;

//     [Header("Direction Settings")]
//     public float horizontalLeftAngleThreshold = 150f;
//     public float horizontalRightAngleThreshold = 30f;

//     private Rigidbody rb;
//     private Vector2 lastDirection;
//     private float holdTime;
//     private bool actionPerformed;

//     private Vector3 startPos;
//     private float hoverTimer;
//     private float hoverStartDelay;
//     private bool isDiagonalJump;
//     private float holdRatio;
//     private Vector3 targetVelocity;
//     private float totalDistance;
//     private float distanceTravelled;
//     private bool lastGroundedState = false;
//     private bool hasFastFallenThisJump = false;

//     private const float inputDeadzone = 0.1f;
//     private float inputBufferTime = 0.15f;
//     private float inputBufferTimer = 0f;
//     private Vector2 bufferedDirection;
//     private bool hasPendingAction = false;
//     private float minimumActionTime = 0f;
//     private float actionTimeElapsed = 0f;

//     private readonly Vector2[] jumpDirections = new Vector2[]
//     {
//         new Vector2(-0.9239f,  0.3827f), // WNW
//         new Vector2(-0.7071f,  0.7071f), // NW
//         new Vector2(-0.3827f,  0.9239f), // NNW
//         new Vector2(0f,        1f),      // N
//         new Vector2(0.3827f,   0.9239f), // NNE
//         new Vector2(0.7071f,   0.7071f), // NE
//         new Vector2(0.9239f,   0.3827f), // ENE
//         // We don't include East/West as they're dash directions
//     };    

//     public JumpDashSystem(MonoBehaviour mono, InputAction movementAction) 
//     { 
//         this.mono = mono; 
//         this.movementAction = movementAction; 

//         playerController =  mono.GetComponent<PlayerController>();
//     }

//     public virtual void Start()
//     {
//         rb = mono.GetComponentInChildren<Rigidbody>();
//         if (rb != null)
//         {
//             Debug.Log($"Rigidbody settings: isKinematic={rb.isKinematic}, useGravity={rb.useGravity}, " +
//                     $"mass={rb.mass}, drag={rb.linearDamping}, constraints={rb.constraints}");
//         }
//     }

//     public void Update()
//     {
//         Debug.Log($"State: {playerController.state}");

//         // Process input buffer
//         if (inputBufferTimer > 0)
//         {
//             inputBufferTimer -= Time.deltaTime;
            
//             // If we returned to idle state and have a pending action
//             if (playerController.state == PlayerController.JumpState.Idle && hasPendingAction)
//             {
//                 // Try to execute the buffered action
//                 lastDirection = bufferedDirection;
//                 holdTime = 0f;
//                 actionPerformed = false;
//                 TransitionTo(PlayerController.JumpState.Charging);
//                 rb.useGravity = false;
//                 hasPendingAction = false;
//                 Debug.Log($"Executing buffered action with direction: {lastDirection}");
//             }
//         }

//         switch (playerController.state)
//         {
//             case PlayerController.JumpState.Charging:
//                 holdTime += Time.deltaTime;
//                 if (holdTime >= maxHoldTime)
//                 {
//                     holdTime = maxHoldTime;
//                     // BeginAction();
//                 }
//                 break;

//             case PlayerController.JumpState.Jumping:
//                 // WallCheck();
//                 // DelayedCall.Invoke(mono, () => WallCheck(), 0.2f);                
//                 break;

//             case PlayerController.JumpState.Hovering:                 
//                 hoverTimer += Time.deltaTime;
//                 if (hoverTimer >= hoverStartDelay + hoverDuration)
//                 {
//                     ExitHover();
//                 }
//                 break;
//         }
//     }
    
//     public void FixedUpdate()
//     {
//         if (playerController.state == PlayerController.JumpState.Dashing || playerController.state == PlayerController.JumpState.Jumping)
//         {
//             actionTimeElapsed += Time.fixedDeltaTime;
//         }

//         bool isGrounded = IsGrounded();

//         // if (playerController.state == PlayerController.JumpState.Jumping)
//         // {
//             WallCheck();
//             // Continuously check for the wall while jumping
//             // DelayedCall.Invoke(mono, () => WallCheck(), 0.2f);                
//         // }

//         // Handle transition to idle state - only if minimum action time has passed
//         if (isGrounded && (playerController.state == PlayerController.JumpState.Descending || 
//             (playerController.state == PlayerController.JumpState.Dashing && actionTimeElapsed >= minimumActionTime)))
//         {
//             Debug.Log("Switching to Grounded from Descending/Dashing");
//             TransitionTo(PlayerController.JumpState.Idle);
//             return; // Exit early to ensure state change takes effect immediately
//         }

//         if (playerController.state == PlayerController.JumpState.Jumping || playerController.state == PlayerController.JumpState.Dashing)
//         {
//             distanceTravelled = Vector3.Distance(rb.position, startPos);

//             // Initial push if stuck
//             if (distanceTravelled < 0.01f)
//             {
//                 rb.AddForce(targetVelocity.normalized * 5f, ForceMode.VelocityChange);
//             }

//             // For jumping, adjust velocity based on the multiplier
//             Vector3 desiredVelocity = targetVelocity;

//             if (playerController.state == PlayerController.JumpState.Dashing)
//             {
//                 rb.linearVelocity = desiredVelocity; // Apply the target velocity directly for dashing
//             }
//             else
//             {
//                 // For jumping, apply velocity change with acceleration
//                 Vector3 velocityChange = desiredVelocity - rb.linearVelocity;
//                 velocityChange = Vector3.ClampMagnitude(velocityChange, clampMagnitudeMaxLength);
//                 rb.AddForce(velocityChange, ForceMode.Acceleration);
//             }

//             // Check for the completion threshold for dashing
//             float completionThreshold = Mathf.Min(totalDistance * 0.9f, totalDistance - 0.5f);

//             Debug.Log($"Total Distance: {totalDistance}, Distance Travelled: {distanceTravelled}, Threshold: {completionThreshold}");

//             // Only allow dash to complete by distance if minimum time has elapsed
//             if (playerController.state == PlayerController.JumpState.Dashing && 
//                 distanceTravelled >= completionThreshold && 
//                 actionTimeElapsed >= minimumActionTime)
//             {
//                 Debug.Log("Dashing complete, transitioning to Idle");
//                 TransitionTo(PlayerController.JumpState.Idle);
//             }
//         }

//         CheckEnterHover();
//         ApplyCustomGravity();

//         if (isGrounded && (playerController.state == PlayerController.JumpState.Descending))
//         {
//             Debug.Log("Switching to Grounded from Descending");
//             TransitionTo(PlayerController.JumpState.Idle);
//         }
//     }

//     public void OnStickStarted(InputAction.CallbackContext ctx)
//     {
//         Vector2 input = ctx.ReadValue<Vector2>();
//         Debug.Log($"OnStickStarted: input={input}, magnitude={input.magnitude}, state={playerController.state}");

//         if (input.magnitude <= inputDeadzone) return;

//         // If we're in a state where we can't immediately act
//         if (playerController.state != PlayerController.JumpState.Idle)
//         {
//             // Buffer the input
//             bufferedDirection = input;
//             inputBufferTimer = inputBufferTime;
//             hasPendingAction = true;
//             Debug.Log($"Buffering input: {bufferedDirection}");
//             return;
//         }

//         // Normal flow
//         lastDirection = input;
//         holdTime = 0f;
//         actionPerformed = false;
//         TransitionTo(PlayerController.JumpState.Charging);
//         rb.useGravity = false;
//         Debug.Log($"Started charging with direction: {lastDirection}");
//     }

//     private bool onStickPerformed = false;

//     public void OnStickPerformed(InputAction.CallbackContext ctx)
//     {
//         if (playerController.state != PlayerController.JumpState.Charging) return;

//         Vector2 input = ctx.ReadValue<Vector2>();

//         // Skip small inputs (deadzone)
//         if (input.magnitude < inputDeadzone) return;

//         // Just use the normalized input directly - no snapping
//         lastDirection = input.normalized;
//         onStickPerformed = true;
//     }

//     public void OnStickCanceled(InputAction.CallbackContext ctx)
//     {
//         if (playerController.state == PlayerController.JumpState.Charging && !actionPerformed)
//         {
//             // BeginAction();
//             onStickPerformed = false;
//             lastDirection = Vector3.zero;
//             targetVelocity = Vector3.zero;
//         }
//     }

//     public void OnJump(InputAction.CallbackContext ctx) 
//     {
//         if(ctx.started && onStickPerformed && playerController.state == PlayerController.JumpState.Charging) 
//         {
//             BeginAction();
//         }
//     }

//     private void ApplyCustomGravity()
//     {
//         if (rb == null || playerController.state == PlayerController.JumpState.Dashing)
//             return;

//         Vector2 input = movementAction.ReadValue<Vector2>();
//         bool wantsFastFall = input.y < -0.5f && !IsGrounded();

//         float appliedGravity = gravityScale * Physics.gravity.y;

//         if (playerController.state == PlayerController.JumpState.Hovering)
//         {
//             if (wantsFastFall)
//             {
//                 Debug.Log("🔻 Fast Fall: Cancelling hover");
//                 ExitHover();
//                 hasFastFallenThisJump = true;
//                 appliedGravity *= fastFallMultiplier;
//                 rb.AddForce(Vector3.up * appliedGravity, ForceMode.Acceleration);
//             }
//             return;
//         }

//         if (playerController.state == PlayerController.JumpState.Descending)
//         {
//             if (wantsFastFall)
//             {
//                 appliedGravity *= fastFallMultiplier;
//                 hasFastFallenThisJump = true;
//             }

//             rb.AddForce(Vector3.up * appliedGravity, ForceMode.Acceleration);
//         }
//     }

//     private Vector2 GetClosestJumpDirection(Vector2 inputDir)
//     {
//         // Using epsilon for vertical check
//         bool isAlmostVertical = Mathf.Abs(inputDir.x) < 0.1f && inputDir.y > 0;
        
//         if (isAlmostVertical)
//         {
//             return Vector2.up; // Return pure North
//         }
        
//         // Find closest jump direction (excluding horizontal dash directions)
//         float bestDot = -Mathf.Infinity;
//         Vector2 bestDirection = Vector2.zero;

//         foreach (var dir in jumpDirections)
//         {
//             float dot = Vector2.Dot(inputDir, dir);
//             if (dot > bestDot)
//             {
//                 bestDot = dot;
//                 bestDirection = dir;
//             }
//         }

//         Debug.Log($"Input dir: {inputDir}, Selected direction: {bestDirection}");
//         return bestDirection.normalized;
//     }

//     private void BeginAction()
//     {
//         actionPerformed = true;
//         holdRatio = holdTime / maxHoldTime;
        
//         // Normalize the direction
//         Vector2 dir = lastDirection.normalized;
//         float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

//         Debug.Log($"BeginAction: angle={angle}, jumpRatio={holdRatio}, direction={dir}");

//         // Check if this is a horizontal dash (right or left)
//         bool isRightDash = angle >= -horizontalRightAngleThreshold && angle <= horizontalRightAngleThreshold;
//         bool isLeftDash = angle >= horizontalLeftAngleThreshold || angle <= -horizontalLeftAngleThreshold;

//         // Check if there's a wall in the direction we want to go
//         Vector2 checkDirection = new Vector2(dir.x, 0).normalized;
//         bool wallInWay = false;
        
//         // Only check for wall if we're moving horizontally
//         if (Mathf.Abs(dir.x) > 0.1f)
//         {
//             wallInWay = Physics.Raycast(rb.position, new Vector3(checkDirection.x, 0, 0), wallCheckDistance, wallLayer);
//             Debug.Log($"Wall check before action: {wallInWay}");
//         }

//         // For dashing, we allow dashing away from walls but not into them
//         if (isRightDash || isLeftDash)
//         {
//             // Allow dash if not against wall or dashing away from it
//             if (!wallInWay || (isRightDash && dir.x < 0) || (isLeftDash && dir.x > 0))
//             {
//                 Vector3 direction = isRightDash ? Vector3.right : Vector3.left;
//                 Dash(holdRatio * maxDashRange * direction);
//                 return;
//             }
//             else
//             {
//                 Debug.Log("❌ Wall in the way - No dash will occur.");
//                 TransitionTo(PlayerController.JumpState.Idle);
//                 return;
//             }
//         }

//         // If it's not a horizontal dash and y is <= 0, reject it
//         // This prevents jumping downward while allowing horizontal dashes
//         if (dir.y <= 0)
//         {
//             Debug.Log("❌ Downward direction — No jump will occur.");
//             TransitionTo(PlayerController.JumpState.Idle);
//             return;
//         }

//         // For jumps, check if there's wall in the way of the horizontal component
//         // but still allow vertical jumps even near walls
//         if (wallInWay && Mathf.Abs(dir.x) > 0.5f)
//         {
//             Debug.Log("Wall in the way for horizontal jump - adjusting to more vertical trajectory");
//             // Adjust the direction to be more vertical
//             dir = new Vector2(dir.x * 0.2f, dir.y);
//         }

//         // Get the closest jump direction
//         // Vector2 jumpDir = GetClosestJumpDirection(dir);
//         // targetVelocity = new Vector3(jumpDir.x, 0f, jumpDir.y).normalized;
        
//         targetVelocity = GetClosestJumpDirection(dir).normalized;
//         startPos = rb.position;
//         isDiagonalJump = Mathf.Abs(targetVelocity.x) > 0.01f;
        
//         // Build movement direction (XZ plane), no Y
//         Vector3 horizontal = new Vector3(targetVelocity.x, 0f, 0f).normalized;

//         // Estimate jump distance
//         float estimatedDistance = maxJumpRange * holdRatio;

//         // Calculate vertical velocity based on the estimated "jump height"
//         float verticalVelocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * (estimatedDistance / 2f));

//         // Combine horizontal distance scaled with hold ratio
//         Vector3 jumpVec = horizontal * estimatedDistance;
//         jumpVec.y = verticalVelocity;

//         // Log the actual jump direction we're using
//         Debug.Log($"Jump Horizontal: {horizontal}, Vertical: {verticalVelocity}, Combined: {jumpVec}");
//         Jump(jumpVec);

//         Debug.Log($"Stick aimed at {GetDirectionName(angle)}: Jump Horizontal: {horizontal}, Vertical: {verticalVelocity}, Combined: {jumpVec}");
//     }

//     private void CheckEnterHover()
//     {
//         if (playerController.state != PlayerController.JumpState.Jumping || hasFastFallenThisJump) return;

//         float height = rb.position.y - startPos.y;
//         float horiz = Mathf.Abs(rb.position.x - startPos.x);
//         float expectedHeight = maxJumpRange * holdRatio * (isDiagonalJump ? 0.92f : 1f);
//         float expectedHoriz = maxJumpRange * holdRatio * (isDiagonalJump ? 0.38f : 0f);

//         // Check if we're too close to the ground
//         bool tooCloseToGround = Physics.Raycast(rb.position, Vector3.down, minHoverHeight);
        
//         if (height >= expectedHeight - epsilon &&
//             (!isDiagonalJump || horiz >= expectedHoriz - epsilon))
//         {
//             if (tooCloseToGround)
//             {
//                 // Too close to ground, go straight to descending
//                 Debug.Log("Too close to ground, skipping hover state");
//                 TransitionTo(PlayerController.JumpState.Descending);
//             }
//             else
//             {
//                 // Far enough from ground, enter hover state
//                 hoverTimer = 0f;
//                 hoverStartDelay = hoverDelay;
//                 TransitionTo(PlayerController.JumpState.Hovering);
//                 rb.useGravity = false;
//                 rb.linearVelocity = Vector3.zero;
//             }
//         }
//         else if (height < 0f)
//         {
//             Debug.Log("Transition state to descending from CheckEnterHover");
//             TransitionTo(PlayerController.JumpState.Descending);
//         }
//     }

//     private void ExitHover()
//     {
//         rb.useGravity = true;
//         Debug.Log("Transition state to descending from ExitHover");
//         TransitionTo(PlayerController.JumpState.Descending);
//     }

//     private void Jump(Vector3 vec)
//     {
//         hasFastFallenThisJump = false;

//         TransitionTo(PlayerController.JumpState.Jumping);
//         rb.linearVelocity = Vector3.zero;  // Reset the velocity at the start of the jump

//         // Check if against wall and apply small push away if needed
//         Vector3 pushDirection = Vector3.zero;
//         if (CheckWallCollision(out pushDirection))
//         {
//             // Add a small push away from the wall
//             rb.AddForce(pushDirection * 2f, ForceMode.Impulse);
//             Debug.Log($"Adding wall push: {pushDirection}");
//         }

//         // Apply the jump multiplier to the velocity calculation
//         targetVelocity = vec * jumpForceMultiplier;
//         totalDistance = vec.magnitude * jumpForceMultiplier * jumpForceMultiplier;
        
//         Debug.Log("Jump: Start Position: " + rb.position + " | Target Velocity: " + targetVelocity + " | Total Distance: " + totalDistance);
//     }

//     private void Dash(Vector3 vec)
//     {
//         hasFastFallenThisJump = false;

//         TransitionTo(PlayerController.JumpState.Dashing);
//         rb.useGravity = false; // prevent gravity drag
//         rb.linearDamping = 0f; // prevent velocity decay
//         rb.linearVelocity = Vector3.zero;

//         // Check if against wall and apply small push away if needed
//         Vector3 pushDirection = Vector3.zero;
//         if (CheckWallCollision(out pushDirection))
//         {
//             // Add a small push away from the wall
//             rb.AddForce(pushDirection * 2f, ForceMode.Impulse);
//             Debug.Log($"Adding wall push: {pushDirection}");
//         }

//         Vector3 dashDirection = vec.normalized;
//         float dashSpeed = maxDashRange * holdRatio * dashForceMultiplier; 

//         // Set the target velocity based on the speed and direction
//         targetVelocity = dashDirection * dashSpeed;
//         startPos = rb.position;
//         totalDistance = maxDashRange * holdRatio;

//         Debug.Log($"Dash: Start Position: {rb.position} | Target Velocity: {targetVelocity} | Total Distance: {totalDistance}");
                
//         // Set a minimum time for dash to prevent immediate transitions
//         minimumActionTime = 0.1f;
//         actionTimeElapsed = 0f;

//         mono.StartCoroutine(StopDashAfterDuration(stopDashAfterDuration));
//     }

//     private bool CheckWallCollision(out Vector3 pushDirection)
//     {
//         pushDirection = Vector3.zero;
        
//         // Check both left and right
//         bool wallLeft = Physics.Raycast(rb.position, Vector3.left, wallCheckDistance, wallLayer);
//         bool wallRight = Physics.Raycast(rb.position, Vector3.right, wallCheckDistance, wallLayer);
        
//         if (wallLeft)
//         {
//             pushDirection = Vector3.right;
//             return true;
//         }
//         else if (wallRight)
//         {
//             pushDirection = Vector3.left;
//             return true;
//         }
        
//         return false;
//     }

//     private IEnumerator StopDashAfterDuration(float duration)
//     {
//         yield return new WaitForSeconds(duration);
        
//         // Stop any movement after the dash
//         rb.linearVelocity = Vector3.zero;
//         TransitionTo(PlayerController.JumpState.Idle); 
//         Debug.Log("Changed to idle state");
//     }

//     private void TransitionTo(PlayerController.JumpState newState)
//     {
//         PlayerController.JumpState oldState = playerController.state;
//         playerController.state = newState;
//         Debug.Log($"State transition: {oldState} -> {newState}");

//         // Reset action timer when leaving action states
//         if (playerController.state == PlayerController.JumpState.Dashing || playerController.state == PlayerController.JumpState.Jumping)
//         {
//             actionTimeElapsed = 0f;
//         }

//         if (playerController.state == PlayerController.JumpState.Idle)
//         {
//             rb.useGravity = true;
//             rb.linearDamping = 0f;
//             hasFastFallenThisJump = false;
//         }
//     }
//     Transform transform;

//     private bool IsGrounded()
//     {
//         Vector3 origin = rb.position + Vector3.down * 0.5f;
//         origin.y += 0.1f; 
//         float sphereRadius = 0.2f; 
//         float rayLength = groundCheckDistance + 0.5f;

//         // Use spherecast as the primary check method
//         bool grounded = Physics.SphereCast(origin, sphereRadius, Vector3.down, out RaycastHit hit, rayLength);
        
//         // Only do additional raycasts if the spherecast didn't hit
//         if (!grounded)
//         {
//             // Try raycasts from center and slight offsets for better coverage
//             grounded = Physics.Raycast(rb.position, Vector3.down, rayLength + 0.2f) ||
//                        Physics.Raycast(rb.position + new Vector3(0.2f, 0, 0), Vector3.down, rayLength + 0.2f) ||
//                        Physics.Raycast(rb.position + new Vector3(-0.2f, 0, 0), Vector3.down, rayLength + 0.2f);
//         }

//         // Only log if the grounded state changed
//         if (grounded != lastGroundedState)
//         {
//             lastGroundedState = grounded;
//             Debug.Log(grounded ? "✅ Grounded" : "❌ Not Grounded");
//         }

//         if(lastGroundedState) 
//         {
//             transform = hit.transform;
//         }

//         return grounded;
//     }

//     private bool WallCheck()
//     {
//         // Determine direction based on movement not position
//         Vector2 checkDirection;
        
//         // Use last movement direction or targetVelocity for more accurate direction
//         if (targetVelocity.x != 0)
//         {
//             checkDirection = new Vector2(Mathf.Sign(targetVelocity.x), 0);
//         }
//         else
//         {
//             checkDirection = rb.position.x < 0 ? Vector2.left : Vector2.right;
//             Debug.Log($"Direction: {checkDirection}");
//         }

//         // Check for wall collision
//         Vector3 raycastOrigin = rb.position;
//         bool wallInFront = Physics.Raycast(raycastOrigin, checkDirection, wallCheckDistance, wallLayer);
//         bool wallAbove = Physics.Raycast(raycastOrigin + Vector3.up * 0.5f, checkDirection, wallCheckDistance, wallLayer);
        
//         Debug.Log($"Wall check: In front: {wallInFront}, Above: {wallAbove}");
        
//         // Only transition if actually moving toward the wall (not just standing by it)
//         if (wallInFront && playerController.state == PlayerController.JumpState.Jumping && 
//             Vector3.Dot(targetVelocity, new Vector3(checkDirection.x, 0, 0)) > 0)
//         {
//             Debug.Log("Transition state to descending from WallCheck");
//             TransitionTo(PlayerController.JumpState.Descending);
//             return true;
//         }

//         return false;
//     }

//     private string GetDirectionName(float angleDegrees)
//     {
//         if (angleDegrees >= -22.5f && angleDegrees < 22.5f) return "E";
//         if (angleDegrees >= 22.5f && angleDegrees < 67.5f) return "NE";
//         if (angleDegrees >= 67.5f && angleDegrees < 112.5f) return "N";
//         if (angleDegrees >= 112.5f && angleDegrees < 157.5f) return "NW";
//         if (angleDegrees >= 157.5f || angleDegrees < -157.5f) return "W";
//         if (angleDegrees >= -157.5f && angleDegrees < -112.5f) return "SW";
//         if (angleDegrees >= -112.5f && angleDegrees < -67.5f) return "S";
//         if (angleDegrees >= -67.5f && angleDegrees < -22.5f) return "SE";
//         return "Unknown";
//     }
// }

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class JumpDashSystem
{    
    private readonly PlayerController playerController;
    private readonly MonoBehaviour mono;
    private readonly InputAction movementAction;

    [Header("Jump Settings")]
    public float maxJumpRange = 7f;
    public float maxHoldTime = 1f;

    [Header("Dash Settings")]
    public float maxDashRange = 10f;
    public float dashForceMultiplier = 1f;
    public float stopDashAfterDuration = .25f;

    [Header("Physics")]
    public float jumpForceMultiplier = 1f;
    public float epsilon = 0.1f;
    public float clampMagnitudeMaxLength = 20f;
    public List<Collider> walls = new(); 

    [Header("Hover Settings")]
    public float hoverDelay = 0.1f;
    public float hoverDuration = 0.5f;
    public float minHoverHeight = 2.0f;
    public float minDiagonalDistance = 1.5f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.1f;
    public LayerMask wallLayer;
    public float wallCheckDistance = 0.25f;

    [Header("Gravity Settings")]
    public float gravityScale = 1f;
    public float fastFallMultiplier = 2f;

    [Header("Direction Settings")]
    public float horizontalLeftAngleThreshold = 150f;
    public float horizontalRightAngleThreshold = 30f;

    private Rigidbody rb;
    private Vector2 lastDirection;
    private float holdTime;
    private bool actionPerformed;

    private Vector3 startPos;
    private float hoverTimer;
    private float hoverStartDelay;
    private bool isDiagonalJump;
    private float holdRatio;
    private Vector3 targetVelocity;
    private float totalDistance;
    private float distanceTravelled;
    private bool lastGroundedState = false;
    private bool hasFastFallenThisJump = false;
    // Physics caching
    private readonly int physicsFrameSkip = 2;
    private int currentPhysicsFrame = 0;
    private bool cachedGroundedState = false;
    private bool cachedWallState = false;
    private Vector3 cachedWallDirection = Vector3.zero;

    private const float inputDeadzone = 0.1f;
    private float inputBufferTime = 0.15f;
    private Vector2 bufferedDirection;
    private bool hasPendingAction = false;
    private Coroutine stopDashCoroutine;
    private bool onStickPerformed = false;


    // Performance-enhancing measure: pre-compute jump directions
    private readonly Vector2[] jumpDirections = new Vector2[]
    {
            // Upward directions
        new Vector2(-0.7071f,  0.7071f), // NW
        new Vector2(0f,        1f),      // N
        new Vector2(0.7071f,   0.7071f), // NE
        // new Vector2(-0.9239f,  0.3827f), // WNW
        // new Vector2(-0.3827f,  0.9239f), // NNW
        // new Vector2(0.3827f,   0.9239f), // NNE
        // new Vector2(0.9239f,   0.3827f), // ENE

           // Downward directions
        new Vector2(-0.7071f,  -0.7071f), // SW
        new Vector2(0f,        -1f),      // S
        new Vector2(0.7071f,   -0.7071f), // SE
        // new Vector2(-0.9239f,  -0.3827f), // SSW
        // new Vector2(-0.3827f,  -0.9239f), // SW
        // new Vector2(0.3827f,   -0.9239f), // SSE
        // new Vector2(0.9239f,   -0.3827f), // ESE
    };    

    public JumpDashSystem(MonoBehaviour mono, InputAction movementAction) 
    { 
        this.mono = mono; 
        this.movementAction = movementAction; 
        playerController = mono.GetComponent<PlayerController>();
    }

    public virtual void Start()
    {
        rb = mono.GetComponentInChildren<Rigidbody>();
        
        #if UNITY_EDITOR
        if (rb != null)
        {
            Debug.Log($"Rigidbody settings: isKinematic={rb.isKinematic}, useGravity={rb.useGravity}, " +
                    $"mass={rb.mass}, drag={rb.linearDamping}, constraints={rb.constraints}");
        }
        #endif
    }

    public void Update()
    {
        #if UNITY_EDITOR
        if (Time.frameCount % 60 == 0) // Only log every 60 frames
        {
            Debug.Log($"State: {playerController.state}");
        }
        #endif

        // Process input buffer
        if (inputBufferTime > 0)
        {
            inputBufferTime -= Time.deltaTime;
            
            // If we returned to idle state and have a pending action
            if (playerController.state == PlayerController.JumpState.Idle && hasPendingAction)
            {
                // Try to execute the buffered action
                lastDirection = bufferedDirection;
                holdTime = 0f;
                actionPerformed = false;
                TransitionTo(PlayerController.JumpState.Charging);
                rb.useGravity = false;
                hasPendingAction = false;
                #if UNITY_EDITOR
                Debug.Log($"Executing buffered action with direction: {lastDirection}");
                #endif
            }
        }

        switch (playerController.state)
        {
            case PlayerController.JumpState.Charging:
                holdTime += Time.deltaTime;
                if (holdTime >= maxHoldTime)
                {
                    holdTime = maxHoldTime;
                }
                break;

            case PlayerController.JumpState.Hovering:                 
                hoverTimer += Time.deltaTime;
                if (hoverTimer >= hoverStartDelay + hoverDuration)
                {
                    ExitHover();
                }
                break;
        }
    }

    private void JumpFromWall() 
    {
        if(playerController.mode == PlayerController.Mode.Default) 
        {
            // Make the player stay against the wall for a certain amount of time. So freeze the position at point of collision

            // If the timer ran out, transition to descending state to glide down the wall

            // If the player is stucked and not descending yet, transition the state to stucked

            // Allow control to jump if in stucked state

            // Allow control to jump if in descending state while being stucked against the wall
        }
    }

    private void BounceFromWall() 
    {
       
    }

    private void StuckPlayerAgainstWall() 
    {
        
    }

    // Also update the FixedUpdate method to prioritize dash handling
    public void FixedUpdate()
    {
        // Increment physics frame counter
        currentPhysicsFrame = (currentPhysicsFrame + 1) % physicsFrameSkip;
        
        // For dashing, we always want to check physics every frame
        bool shouldDoPhysicsChecks = (currentPhysicsFrame == 0) || 
                                    playerController.state == PlayerController.JumpState.Dashing;
        

        bool isGrounded = shouldDoPhysicsChecks ? IsGrounded() : cachedGroundedState;

        // Only check walls when necessary
        if (shouldDoPhysicsChecks && 
            (playerController.state == PlayerController.JumpState.Jumping || 
            playerController.state == PlayerController.JumpState.Dashing))
        {
            if(WallCheck() && playerController.state == PlayerController.JumpState.Jumping) 
            {
                // Jump from wall
                JumpFromWall();
            }
            else if(WallCheck() && playerController.state == PlayerController.JumpState.Dashing) 
            {
                // Bounce away from wall
            }
        }

        // Handle transition to idle state - only if minimum action time has passed
        if (isGrounded && playerController.state == PlayerController.JumpState.Descending)
        {
            TransitionTo(PlayerController.JumpState.Idle);
            return;
        }

        if (playerController.state == PlayerController.JumpState.Jumping || playerController.state == PlayerController.JumpState.Dashing)
        {
            distanceTravelled = Vector3.Distance(rb.position, startPos);

            // Initial push if stuck
            if (distanceTravelled < 0.01f)
            {
                rb.AddForce(targetVelocity.normalized * 5f, ForceMode.VelocityChange);
            }

            // For jumping, adjust velocity based on the multiplier
            Vector3 desiredVelocity = targetVelocity;

            if (playerController.state == PlayerController.JumpState.Jumping)
            {
                // For jumping, apply velocity change with acceleration
                Vector3 velocityChange = desiredVelocity - rb.linearVelocity;
                velocityChange = Vector3.ClampMagnitude(velocityChange, clampMagnitudeMaxLength);
                rb.AddForce(velocityChange, ForceMode.Acceleration);
            }
        }

        if (shouldDoPhysicsChecks)
        {
            CheckEnterHover();
        }
        
        ApplyCustomGravity();

        if (isGrounded && (playerController.state == PlayerController.JumpState.Descending))
        {
            TransitionTo(PlayerController.JumpState.Idle);
        }
    }

    public void OnStickStarted(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();
        
        if (input.magnitude <= inputDeadzone) return;

        // If we're in a state where we can't immediately act
        if (playerController.state != PlayerController.JumpState.Idle)
        {
            // Buffer the input
            bufferedDirection = input;
            hasPendingAction = true;
            return;
        }

        // Normal flow
        lastDirection = input;
        holdTime = 0f;
        actionPerformed = false;
        TransitionTo(PlayerController.JumpState.Charging);
        rb.useGravity = false;
    }
    public void OnStickPerformed(InputAction.CallbackContext ctx)
    {
        if (playerController.state != PlayerController.JumpState.Charging) return;

        Vector2 input = ctx.ReadValue<Vector2>();

        // Skip small inputs (deadzone)
        if (input.magnitude < inputDeadzone) return;

        // Just use the normalized input directly - no snapping
        lastDirection = input.normalized;
        onStickPerformed = true;
    }

    public void OnStickCanceled(InputAction.CallbackContext ctx)
    {
        if (playerController.state == PlayerController.JumpState.Charging && !actionPerformed)
        {
            onStickPerformed = false;
            lastDirection = Vector3.zero;
            targetVelocity = Vector3.zero;
        }
    }

    public void OnJump(InputAction.CallbackContext ctx) 
    {
        if(ctx.started && onStickPerformed && playerController.state == PlayerController.JumpState.Charging) 
        {
            BeginAction();
        }
    }

    private void ApplyCustomGravity()
    {
        if (rb == null || playerController.state == PlayerController.JumpState.Dashing)
            return;

        Vector2 input = movementAction.ReadValue<Vector2>();
        bool wantsFastFall = input.y < -0.5f && !cachedGroundedState;

        float appliedGravity = gravityScale * Physics.gravity.y;

        if (playerController.state == PlayerController.JumpState.Hovering)
        {
            if (wantsFastFall)
            {
                ExitHover();
                hasFastFallenThisJump = true;
                appliedGravity *= fastFallMultiplier;
                rb.AddForce(Vector3.up * appliedGravity, ForceMode.Acceleration);
            }
            return;
        }

        if (playerController.state == PlayerController.JumpState.Descending)
        {
            if (wantsFastFall)
            {
                appliedGravity *= fastFallMultiplier;
                hasFastFallenThisJump = true;
            }

            rb.AddForce(Vector3.up * appliedGravity, ForceMode.Acceleration);
        }
    }

    private Vector2 GetClosestJumpDirection(Vector2 inputDir)
    {
        // Using epsilon for vertical check
        bool isAlmostVertical = Mathf.Abs(inputDir.x) < 0.1f && inputDir.y > 0;
        
        if (isAlmostVertical)
        {
            return Vector2.up; // Return pure North
        }
        
        // Find closest jump direction (excluding horizontal dash directions)
        float bestDot = -Mathf.Infinity;
        Vector2 bestDirection = Vector2.zero;

        foreach (var dir in jumpDirections)
        {
            float dot = Vector2.Dot(inputDir, dir);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestDirection = dir;
            }
        }

        return bestDirection.normalized;
    }

    private void BeginAction()
    {
        actionPerformed = true;
        holdRatio = holdTime / maxHoldTime;
        
        // Normalize the direction
        Vector2 dir = lastDirection.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Check if this is a horizontal dash (right or left)
        bool isRightDash = angle >= -horizontalRightAngleThreshold && angle <= horizontalRightAngleThreshold;
        bool isLeftDash = angle >= horizontalLeftAngleThreshold || angle <= -horizontalLeftAngleThreshold;

        // Check if there's a wall in the direction we want to go
        // Vector2 checkDirection = new Vector2(dir.x, 0).normalized;
        // bool wallInWay = false;
        
        // Only check for wall if we're moving horizontally
        // if (Mathf.Abs(dir.x) > 0.1f)
        // {
        //     wallInWay = Physics.Raycast(rb.position, new Vector3(checkDirection.x, 0, 0), wallCheckDistance, wallLayer);
        // }

        // For dashing, we allow dashing away from walls but not into them
        if (isRightDash || isLeftDash)
        {
            // Allow dash if not against wall or dashing away from it
            // if (!wallInWay || (isRightDash && dir.x < 0) || (isLeftDash && dir.x > 0))
            // {
                Vector3 direction = isRightDash ? Vector3.right : Vector3.left;
                Dash(holdRatio * maxDashRange * direction);
                // return;
            // }
            // else
            // {
                // TransitionTo(PlayerController.JumpState.Idle);
                // return;
            // }
        }

        // If it's not a horizontal dash and y is <= 0, reject it
        // This prevents jumping downward while allowing horizontal dashes
        if (dir.y <= 0)
        {
            TransitionTo(PlayerController.JumpState.Idle);
            return;
        }

        // For jumps, check if there's wall in the way of the horizontal component
        // but still allow vertical jumps even near walls
        // if (wallInWay && Mathf.Abs(dir.x) > 0.5f)
        // {
        //     // Adjust the direction to be more vertical
        //     dir = new Vector2(dir.x * 0.2f, dir.y);
        // }
        
        targetVelocity = GetClosestJumpDirection(dir).normalized;
        startPos = rb.position;
        isDiagonalJump = Mathf.Abs(targetVelocity.x) > 0.01f;
        
        Vector3 horizontal = new Vector3(targetVelocity.x, 0f, 0f).normalized;
        float estimatedDistance = maxJumpRange * holdRatio;
        float verticalVelocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * (estimatedDistance / 2f));
        Vector3 jumpVec = horizontal * estimatedDistance;
        jumpVec.y = verticalVelocity;

        Jump(jumpVec);
    }

    private void CheckEnterHover()
    {
        if (playerController.state != PlayerController.JumpState.Jumping || hasFastFallenThisJump) return;

        float height = rb.position.y - startPos.y;
        float horiz = Mathf.Abs(rb.position.x - startPos.x);
        float expectedHeight = maxJumpRange * holdRatio * (isDiagonalJump ? 0.92f : 1f);
        float expectedHoriz = maxJumpRange * holdRatio * (isDiagonalJump ? 0.38f : 0f);

        // Check if we're too close to the ground
        bool tooCloseToGround = Physics.Raycast(rb.position, Vector3.down, minHoverHeight);
        
        if (height >= expectedHeight - epsilon &&
            (!isDiagonalJump || horiz >= expectedHoriz - epsilon))
        {
            if (tooCloseToGround)
            {
                // Too close to ground, go straight to descending
                TransitionTo(PlayerController.JumpState.Descending);
            }
            else
            {
                // Far enough from ground, enter hover state
                hoverTimer = 0f;
                hoverStartDelay = hoverDelay;
                TransitionTo(PlayerController.JumpState.Hovering);
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
            }
        }
        else if (height < 0f)
        {
            TransitionTo(PlayerController.JumpState.Descending);
        }
    }

    private void ExitHover()
    {
        rb.useGravity = true;
        TransitionTo(PlayerController.JumpState.Descending);
    }

    private void Jump(Vector3 vec)
    {
        hasFastFallenThisJump = false;

        TransitionTo(PlayerController.JumpState.Jumping);
        rb.linearVelocity = Vector3.zero;  // Reset the velocity at the start of the jump

        // Check if against wall and apply small push away if needed
        Vector3 pushDirection = Vector3.zero;
        if (CheckWallCollision(out pushDirection))
        {
            // Add a small push away from the wall
            rb.AddForce(pushDirection * 2f, ForceMode.Impulse);
        }

        // Apply the jump multiplier to the velocity calculation
        targetVelocity = vec * jumpForceMultiplier;
        totalDistance = vec.magnitude * jumpForceMultiplier * jumpForceMultiplier;
    }

    private void Dash(Vector3 vec)
    {
        hasFastFallenThisJump = false;

        TransitionTo(PlayerController.JumpState.Dashing);
        rb.useGravity = false; // prevent gravity drag
        rb.linearDamping = 0f; // prevent velocity decay
        rb.linearVelocity = Vector3.zero;

        // Check if against wall and apply small push away if needed
        Vector3 pushDirection = Vector3.zero;
        if (CheckWallCollision(out pushDirection))
        {
            // Add a small push away from the wall
            rb.AddForce(pushDirection * 2f, ForceMode.Impulse);
            #if UNITY_EDITOR
            Debug.Log($"Adding wall push: {pushDirection}");
            #endif
        }

        Vector3 dashDirection = vec.normalized;
        float dashSpeed = maxDashRange * holdRatio * dashForceMultiplier; 

        // Set the target velocity based on the speed and direction
        targetVelocity = dashDirection * dashSpeed;
        startPos = rb.position;
        totalDistance = maxDashRange * holdRatio;
        
        #if UNITY_EDITOR
        Debug.Log($"Dash: Start Position: {rb.position} | Target Velocity: {targetVelocity} | Total Distance: {totalDistance}");
        #endif
                
        // Set a minimum time for dash to prevent immediate transitions
        // minimumActionTime = 0.1f;
        // actionTimeElapsed = 0f;

        // Stop previous coroutines before starting a new one
        if (stopDashCoroutine != null)
        {
            mono.StopCoroutine(stopDashCoroutine);
        }
        stopDashCoroutine = mono.StartCoroutine(StopDashAfterDuration(stopDashAfterDuration));
    }

    private bool CheckWallCollision(out Vector3 pushDirection)
    {
        pushDirection = Vector3.zero;
        
        // Check both left and right
        bool wallLeft = Physics.Raycast(rb.position, Vector3.left, wallCheckDistance, wallLayer);
        bool wallRight = Physics.Raycast(rb.position, Vector3.right, wallCheckDistance, wallLayer);
        
        if (wallLeft)
        {
            pushDirection = Vector3.right;
            return true;
        }
        else if (wallRight)
        {
            pushDirection = Vector3.left;
            return true;
        }
        
        return false;
    }

    private IEnumerator StopDashAfterDuration(float duration)
    {
        float elapsedTime = 0f;
        float initialSpeed = targetVelocity.magnitude;
        
        // First phase: maintain full dash speed
        while (elapsedTime < duration * 0.7f)
        {
            // Make sure we're still dashing
            if (playerController.state != PlayerController.JumpState.Dashing)
            {
                yield break;
            }
            
            // Apply full velocity
            rb.linearVelocity = targetVelocity;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Second phase: gradual slowdown for smooth ending
        float slowdownDuration = duration * 0.3f;
        float slowdownStartTime = elapsedTime;
        
        while (elapsedTime < duration)
        {
            // Make sure we're still dashing
            if (playerController.state != PlayerController.JumpState.Dashing)
            {
                yield break;
            }
            
            // Calculate slowdown factor (1.0 -> 0.1)
            float slowdownFactor = Mathf.Lerp(1.0f, 0.1f, (elapsedTime - slowdownStartTime) / slowdownDuration);
            
            // Apply reduced velocity for smooth stop
            rb.linearVelocity = targetVelocity * slowdownFactor;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Only stop the dash if we're still in dashing state
        if (playerController.state == PlayerController.JumpState.Dashing)
        {
            // One final slowdown before stopping
            rb.linearVelocity = targetVelocity * 0.05f;
            yield return new WaitForSeconds(0.05f);
            
            // Stop any movement after the dash
            rb.linearVelocity = Vector3.zero;
            TransitionTo(PlayerController.JumpState.Idle);
            #if UNITY_EDITOR
            Debug.Log("Dash completed, changed to idle state");
            #endif
        }
        
        stopDashCoroutine = null;
    }


    private void TransitionTo(PlayerController.JumpState newState)
    {
        PlayerController.JumpState oldState = playerController.state;
        playerController.state = newState;
        
        #if UNITY_EDITOR
        Debug.Log($"State transition: {oldState} -> {newState}");
        #endif

        if (playerController.state == PlayerController.JumpState.Idle)
        {
            rb.useGravity = true;
            rb.linearDamping = 0f;
            hasFastFallenThisJump = false;
        }
    }

    private bool IsGrounded()
    {
        Vector3 origin = rb.position + Vector3.down * 0.5f;
        origin.y += 0.1f; 
        float sphereRadius = 0.2f; 
        float rayLength = groundCheckDistance + 0.5f;

        // Use spherecast as the primary check method
        bool grounded = Physics.SphereCast(origin, sphereRadius, Vector3.down, out RaycastHit hit, rayLength);
        
        // Only do additional raycasts if the spherecast didn't hit
        if (!grounded)
        {
            // Try raycasts from center and slight offsets for better coverage
            grounded = Physics.Raycast(rb.position, Vector3.down, rayLength + 0.2f) ||
                       Physics.Raycast(rb.position + new Vector3(0.2f, 0, 0), Vector3.down, rayLength + 0.2f) ||
                       Physics.Raycast(rb.position + new Vector3(-0.2f, 0, 0), Vector3.down, rayLength + 0.2f);
        }

        #if UNITY_EDITOR
        // Only log if the grounded state changed
        if (grounded != lastGroundedState)
        {
            lastGroundedState = grounded;
            Debug.Log(grounded ? "✅ Grounded" : "❌ Not Grounded");
        }
        #endif

        // Update the cached state
        cachedGroundedState = grounded;
        
        return grounded;
    }

    private bool WallCheck()
    {
        // Determine direction based on movement not position
        Vector2 checkDirection;
        
        // Use last movement direction or targetVelocity for more accurate direction
        if (targetVelocity.x != 0)
        {
            checkDirection = new Vector2(Mathf.Sign(targetVelocity.x), 0);
        }
        else
        {
            checkDirection = rb.position.x < 0 ? Vector2.left : Vector2.right;
        }

        // Check for wall collision
        Vector3 raycastOrigin = rb.position;
        bool wallInFront = Physics.Raycast(raycastOrigin, checkDirection, wallCheckDistance, wallLayer);
        bool wallAbove = Physics.Raycast(raycastOrigin + Vector3.up * 0.5f, checkDirection, wallCheckDistance, wallLayer);
        
        // Cache the wall state
        cachedWallState = wallInFront;
        cachedWallDirection = new Vector3(checkDirection.x, 0, 0);
        
        // Only transition if actually moving toward the wall (not just standing by it)
        if (wallInFront && playerController.state == PlayerController.JumpState.Jumping && 
            Vector3.Dot(targetVelocity, new Vector3(checkDirection.x, 0, 0)) > 0)
        {
            // TransitionTo(PlayerController.JumpState.Descending);
            if(playerController.mode == PlayerController.Mode.Default) 
            {
                StuckPlayerAgainstWall();
            }

            return true;
        }

        return false;
    }

    private string GetDirectionName(float angleDegrees)
    {
        if (angleDegrees >= -22.5f && angleDegrees < 22.5f) return "E";
        if (angleDegrees >= 22.5f && angleDegrees < 67.5f) return "NE";
        if (angleDegrees >= 67.5f && angleDegrees < 112.5f) return "N";
        if (angleDegrees >= 112.5f && angleDegrees < 157.5f) return "NW";
        if (angleDegrees >= 157.5f || angleDegrees < -157.5f) return "W";
        if (angleDegrees >= -157.5f && angleDegrees < -112.5f) return "SW";
        if (angleDegrees >= -112.5f && angleDegrees < -67.5f) return "S";
        if (angleDegrees >= -67.5f && angleDegrees < -22.5f) return "SE";
        return "Unknown";
    }

    // Clean up resources
    public void OnDestroy()
    {
        if (stopDashCoroutine != null)
        {
            mono.StopCoroutine(stopDashCoroutine);
            stopDashCoroutine = null;
        }
    }
}