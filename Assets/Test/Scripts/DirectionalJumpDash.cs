using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DirectionalJumpDash : MonoBehaviour
{
    public enum JumpState { Idle, Charging, Jumping, Hovering, Descending, Dashing }
    public JumpState state = JumpState.Idle;

    [Header("Input Settings")]
    public InputActionAsset inputActions;
    private InputAction movementAction;

    [Header("Jump Settings")]
    public float maxJumpRange = 10f;
    public float maxHoldTime = 1f;

    [Header("Dash Settings")]
    public float maxDashRange = 10f;
    public float dashForceMultiplier = 1f;
    public float stopDashAfterDuration = .5f;

    [Header("Physics")]
    public float jumpForceMultiplier = 1f;
    public float accelerationRate = 20f;
    const float epsilon = 0.1f;
    public float clampMagnitudeMaxLength = 20f;

    [Header("Hover Settings")]
    public float hoverDelay = 0.1f;
    public float hoverDuration = 0.5f;
    public float minHoverHeight = 1f;
    public float minJumpHoverDistance = 1f;
    public float minDiagonalHoverHeight = 1f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;

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

    private readonly Vector2[] jumpDirections =
    {
        new Vector2(-0.92f, 0.38f), // WNW
        new Vector2(-0.71f, 0.71f), // NW
        new Vector2(-0.38f, 0.92f), // NNW
        new Vector2(0f, 1f),        // N
        new Vector2(0.38f, 0.92f),  // NNE
        new Vector2(0.71f, 0.71f),  // NE
        new Vector2(0.92f, 0.38f),  // ENE
    };

    private const float inputDeadzone = 0.1f;
    private const float sectorHalfAngle = 22.5f;

    void Awake()
    {
        var map = inputActions.FindActionMap("Player");
        movementAction = map.FindAction("Movement");
        movementAction.Enable();
    }

    void OnEnable()
    {
        movementAction.started += OnStickStarted;
        movementAction.performed += OnStickPerformed;
        movementAction.canceled += OnStickCanceled;
    }

    void OnDisable()
    {
        movementAction.started -= OnStickStarted;
        movementAction.performed -= OnStickPerformed;
        movementAction.canceled -= OnStickCanceled;
    }

    void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            Debug.Log($"Rigidbody settings: isKinematic={rb.isKinematic}, useGravity={rb.useGravity}, " +
                    $"mass={rb.mass}, drag={rb.linearDamping }, constraints={rb.constraints}");
        }
    }

    void Update()
    {
        switch (state)
        {
            case JumpState.Charging:
                holdTime += Time.deltaTime;
                if (holdTime >= maxHoldTime)
                {
                    holdTime = maxHoldTime;
                    BeginAction();
                }
                break;

            case JumpState.Jumping:
                break;

            case JumpState.Hovering:
                hoverTimer += Time.deltaTime;
                if (hoverTimer >= hoverStartDelay + hoverDuration)
                {
                    ExitHover();
                }
                break;

            case JumpState.Descending:
                // if (IsGrounded())
                // {
                //     TransitionTo(JumpState.Idle);
                // }
                break;
        }
    }

    void FixedUpdate()
    {
        IsGrounded();

        if (state == JumpState.Jumping || state == JumpState.Dashing)
        {
            distanceTravelled = Vector3.Distance(rb.position, startPos);

            // Initial push if stuck
            if (distanceTravelled < 0.01f)
            {
                rb.AddForce(targetVelocity.normalized * 5f, ForceMode.VelocityChange);
            }

            // For jumping, adjust velocity based on the multiplier
            Vector3 desiredVelocity = targetVelocity;

            if (state == JumpState.Dashing)
            {
                rb.linearVelocity  = desiredVelocity; // Apply the target velocity directly for dashing
            }
            else
            {
                // For jumping, apply velocity change with acceleration
                Vector3 velocityChange = desiredVelocity - rb.linearVelocity ;
                velocityChange = Vector3.ClampMagnitude(velocityChange, clampMagnitudeMaxLength);  // Limit the velocity change to avoid high-speed flickering
                rb.AddForce(velocityChange, ForceMode.Acceleration);
            }

            // Check for the completion threshold for dashing
            float completionThreshold = Mathf.Min(totalDistance * 0.9f, totalDistance - 0.5f);

            Debug.Log($"Total Distance: {totalDistance}, Distance Travelled: {distanceTravelled}, Threshold: {completionThreshold}");

            if (state == JumpState.Dashing && distanceTravelled >= completionThreshold)
            {
                Debug.Log("Dashing complete, transitioning to Idle");
                TransitionTo(JumpState.Idle);
            }
        }

        CheckEnterHover();

        if (IsGrounded() && (state == JumpState.Descending))
        {
            TransitionTo(JumpState.Idle);
        }
    }

    private void OnStickStarted(InputAction.CallbackContext ctx)
    {
        if (state != JumpState.Idle) return;
        Vector2 input = ctx.ReadValue<Vector2>();
        Debug.Log($"OnStickStarted: input={input}, magnitude={input.magnitude}, state={state}");

        if (input.magnitude > inputDeadzone)
        {
            lastDirection = input;
            holdTime = 0f;
            actionPerformed = false;
            TransitionTo(JumpState.Charging);
            rb.useGravity = false;
            Debug.Log($"Started charging with direction: {lastDirection}");
        }
    }

    private void OnStickPerformed(InputAction.CallbackContext ctx)
    {
        if (state != JumpState.Charging) return;
        Vector2 input = ctx.ReadValue<Vector2>();
        if (input.magnitude > inputDeadzone)
            lastDirection = input;
    }

    private void OnStickCanceled(InputAction.CallbackContext ctx)
    {
        if (state == JumpState.Charging && !actionPerformed)
        {
            BeginAction();
        }
    }

    private void BeginAction()
    {
        actionPerformed = true;
        holdRatio = holdTime / maxHoldTime;
        float angle = Mathf.Atan2(lastDirection.y, lastDirection.x) * Mathf.Rad2Deg;
        Debug.Log($"BeginAction: angle={angle}, jumpRatio={holdRatio}, lastDirection={lastDirection}");

        if (angle >= -30f && angle <= 30f)
        {
            Dash(Vector3.right * maxDashRange * holdRatio);
            return;
        }
        if (angle >= 150f || angle <= -150f)
        {
            Dash(Vector3.left * maxDashRange * holdRatio);
            return;
        }

        Vector2 norm = lastDirection.normalized;
        int best = 0; float minA = float.MaxValue;
        for (int i = 0; i < jumpDirections.Length; i++)
        {
            float dA = Vector2.Angle(norm, jumpDirections[i]);
            if (dA < minA) { minA = dA; best = i; }
        }
        Vector2 d = jumpDirections[best];
        startPos = rb.position;
        isDiagonalJump = Mathf.Abs(d.x) > 0.01f;
        Vector3 jumpVec = new Vector3(d.x * maxJumpRange * holdRatio, d.y * maxJumpRange * holdRatio, 0f);
        Jump(jumpVec);
    }

    private void CheckEnterHover()
    {
        if (state != JumpState.Jumping) return;

        float height = rb.position.y - startPos.y;
        float horiz = Mathf.Abs(rb.position.x - startPos.x);
        float expectedHeight = maxJumpRange * holdRatio * (isDiagonalJump ? 0.92f : 1f);
        float expectedHoriz = maxJumpRange * holdRatio * (isDiagonalJump ? 0.38f : 0f);

        if (height >= expectedHeight - epsilon &&
        (!isDiagonalJump || horiz >= expectedHoriz - epsilon))
        {
            hoverTimer = 0f;
            hoverStartDelay = hoverDelay;
            TransitionTo(JumpState.Hovering);
            rb.useGravity = false;
            rb.linearVelocity  = Vector3.zero;
        }
        else if (height < 0f)
        {
            TransitionTo(JumpState.Descending);
        }
    }

    private void ExitHover()
    {
        rb.useGravity = true;
        TransitionTo(JumpState.Descending);
    }
    private void Jump(Vector3 vec)
    {
        TransitionTo(JumpState.Jumping);
        rb.linearVelocity  = Vector3.zero;  // Reset the velocity at the start of the jump

        // Apply the jump multiplier to the velocity calculation
        targetVelocity = jumpForceMultiplier * jumpForceMultiplier * vec;
        totalDistance = vec.magnitude * jumpForceMultiplier * jumpForceMultiplier;  // Adjust the total distance accordingly

        Debug.Log("Jump: Start Position: " + rb.position + " | Target Velocity: " + targetVelocity + " | Total Distance: " + totalDistance);
    }

    private void Dash(Vector3 vec)
    {
        TransitionTo(JumpState.Dashing);
        rb.useGravity = false; // prevent gravity drag
        rb.linearDamping = 0f; // prevent velocity decay
        rb.linearVelocity = Vector3.zero;

        Vector3 dashDirection = vec.normalized;
        float dashSpeed = maxDashRange * holdRatio * dashForceMultiplier; // Apply the multiplier to the speed, not distance.

        // Set the target velocity based on the speed and direction
        targetVelocity = dashDirection * dashSpeed;
        startPos = rb.position;
        totalDistance = maxDashRange * holdRatio; // Total distance is now based only on maxDashRange and holdRatio.

        Debug.Log($"Dash: Start Position: {rb.position} | Target Velocity: {targetVelocity} | Total Distance: {totalDistance}");

        StartCoroutine(StopDashAfterDuration(stopDashAfterDuration)); // Stop the dash immediately after a short time
    }

    // Coroutine to stop the dash immediately
    private IEnumerator StopDashAfterDuration(float duration)
    {
        // Wait for a very short duration to apply dash and then stop
        yield return new WaitForSeconds(duration);
        
        // Stop any movement after the dash
        rb.linearVelocity = Vector3.zero;
        TransitionTo(JumpState.Idle); // Transition back to idle
    }


    private void TransitionTo(JumpState newState)
    {
        state = newState;

        if (newState == JumpState.Idle)
        {
            rb.useGravity = true;
            rb.linearDamping  = 0f; 
        }
    }

    private bool IsGrounded()
    {
        Vector3 origin = rb.position + Vector3.down * 0.5f;
        origin.y += 0.1f; // slightly above the bottom
        float sphereRadius = 0.2f; // tweak based on your player collider
        float rayLength = groundCheckDistance + 0.5f;

        bool grounded = Physics.SphereCast(origin, sphereRadius, Vector3.down, out RaycastHit hit, rayLength);

        if (grounded != lastGroundedState)
        {
            lastGroundedState = grounded;

            if (grounded)
            {
                Debug.Log($"✅ Grounded (sphere): {grounded} | Hit: {(hit.collider != null ? hit.collider.name : "null")}");
            }
            else
            {
                Debug.Log("❌ Not Grounded (sphere): No collider hit.");
            }
        }

        return grounded;
    }
}

