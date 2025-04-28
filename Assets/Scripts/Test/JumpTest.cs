using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class JumpTest : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float jumpRange = 5f;

    [Header("Allowed Jump Angles (degrees)")]
    public float[] allowedAngles = { 180f, 135f, 90f, 45f, 0f }; 

    private Vector2 inputDirection = Vector2.zero;
    private Vector2 lastSnappedDirection = Vector2.zero;
    private Vector2 storedDirection = Vector2.zero;
    private bool southButtonPressed;
    private Rigidbody rb;
    private Transform camTransform;
    public float deadzone = 0.2f;
    private bool onStickStarted;
    private bool onStickPerformed;
    private bool actionPerformed;

    public float maxHoldTime = 1f;
    public float holdTime = 0;
    private float holdRatio = 0; 

    public float horizontalThreshold = 30f; 
    private float angle;
    private bool isEastDirection;
    private bool isWestDirection;
    private bool actionFinished;
    public float maxJumpRange = 10f;

    private bool playerJump;
    private bool playerDash;
    private Vector2 targetVelocity = Vector2.zero;

    [Range(1, 10)]
    public float accelerationRate = 5f;
    public float dashSpeed = 20f;
    public float maxDashRange = 8f;
    
    // Added to control the action duration
    public float actionDuration = 0.8f;
    private float actionTimer;

    public InputActionAsset inputActions;
    private InputAction RightAnalogStickInput;
    private InputAction SouthButtonInput;

    void Awake()
    {
        var map = inputActions.FindActionMap("Player");
        RightAnalogStickInput = map.FindAction("Movement");
        SouthButtonInput = map.FindAction("Jump");

        RightAnalogStickInput.Enable();
        SouthButtonInput.Enable();

        rb = GetComponent<Rigidbody>();
        camTransform = Camera.main.transform;
    }

    void OnEnable()
    {
        RightAnalogStickInput.started += OnStickStarted;
        RightAnalogStickInput.performed += OnStickPerformed;
        RightAnalogStickInput.canceled += OnStickCanceled;

        SouthButtonInput.started += OnSouthButtonStarted;
        SouthButtonInput.performed += OnSouthButtonPerformed;
        SouthButtonInput.canceled += OnSouthButtonCanceled;
    }

    void OnDisable()
    {
        RightAnalogStickInput.started -= OnStickStarted;
        RightAnalogStickInput.performed -= OnStickPerformed;
        RightAnalogStickInput.canceled -= OnStickCanceled;

        SouthButtonInput.started -= OnSouthButtonStarted;
        SouthButtonInput.performed -= OnSouthButtonPerformed;
        SouthButtonInput.canceled -= OnSouthButtonCanceled;
    }

    void Update()
    {
        // Increment hold time while button is pressed and not yet performed
        if (southButtonPressed && !actionPerformed)
        {
            holdTime += Time.deltaTime;
            holdTime = Mathf.Min(holdTime, maxHoldTime);
        }
        
        // Handle action timer for ending jumps/dashes
        if (playerJump || playerDash)
        {
            actionTimer += Time.deltaTime;
            
            // End action after duration
            if (actionTimer >= actionDuration)
            {
                ResetActionState();
            }
        }
    }

    // private void FixedUpdate()
    // {
    //     if(southButtonPressed) 
    //     {
    //         if(playerJump) 
    //         {
    //             rb.AddForce(jumpForce * accelerationRate * (Time.fixedDeltaTime * targetVelocity), ForceMode.Acceleration);
    //         }
    //         else if (playerDash) 
    //         {
    //             rb.AddForce(dashSpeed * Time.fixedDeltaTime * targetVelocity, ForceMode.Impulse);
    //         }
    //     } 
    // }

    private void FixedUpdate()
    {
        if ((playerJump || playerDash) && !actionFinished)
        {
            if (playerJump) 
            {
                // Apply jump force once rather than continuously
                if (actionTimer < Time.fixedDeltaTime)
                {
                    Vector3 jumpVector = new Vector3(targetVelocity.x, targetVelocity.y, 0);
                    rb.linearVelocity  = jumpVector;
                    Debug.Log($"Applied jump velocity: {jumpVector}");
                }
            }
            else if (playerDash) 
            {
                // Apply dash force once rather than continuously
                if (actionTimer < Time.fixedDeltaTime)
                {
                    Vector3 dashVector = new Vector3(targetVelocity.x, 0, 0);
                    rb.linearVelocity  = dashVector;
                    Debug.Log($"Applied dash velocity: {dashVector}");
                }
            }
        }
    }


    public void OnSouthButtonStarted(InputAction.CallbackContext ctx)
    {
        if (ctx.started && lastSnappedDirection.y > 0.1f && !actionFinished) 
        {
            Debug.Log("South Button Started");
            southButtonPressed = true;
            holdTime = 0f; 
        }
    }

    public void OnSouthButtonPerformed(InputAction.CallbackContext ctx) 
    {
        if(ctx.performed && !actionFinished) 
        {
            Debug.Log("South Button Performed");
        }
    }

    public void OnSouthButtonCanceled(InputAction.CallbackContext ctx) 
    {
        if(ctx.canceled) 
        {
            southButtonPressed = false;

            if (!actionPerformed && holdTime > 0 && lastSnappedDirection != Vector2.zero)
            {
                PerformJumpOrDash();
            }
            Debug.Log("South Button Canceled");
        }
    }

    public void OnStickStarted(InputAction.CallbackContext ctx)
    {
        // Get the input direction
        inputDirection = ctx.ReadValue<Vector2>();
        onStickStarted = true;
    }

    public void OnStickPerformed(InputAction.CallbackContext ctx)
    {    
        onStickStarted = false;
        inputDirection = ctx.ReadValue<Vector2>(); 

        if(inputDirection.magnitude < deadzone) inputDirection = Vector2.zero;

        lastSnappedDirection = GetSnappedDirection(inputDirection).normalized; 

        if (lastSnappedDirection != Vector2.zero && !playerJump && !playerDash)
        {
            Debug.Log($"Snapped Direction: {lastSnappedDirection}");
        }
        
        onStickPerformed = true;
    }

    public void OnStickCanceled(InputAction.CallbackContext ctx)
    {
        onStickPerformed = false;
        storedDirection = lastSnappedDirection;
    }

    private void PerformJumpOrDash()
    {
        actionPerformed = true;
        holdRatio = holdTime / maxHoldTime;
        if (holdRatio < 0.1f) holdRatio = 0.1f; // Minimum ratio to prevent zero velocity

        angle = Mathf.Atan2(lastSnappedDirection.y, lastSnappedDirection.x) * Mathf.Rad2Deg;
        Debug.Log($"Angle: {angle}");
        
        // Check if pointing East (around 0°)
        isEastDirection = angle >= -horizontalThreshold && angle <= horizontalThreshold;
        
        // Check if pointing West (around ±180°)
        isWestDirection = angle >= 180 - horizontalThreshold || angle <= -180 + horizontalThreshold;
        
        // Determine if we should dash (east or west) or jump
        if (isEastDirection || isWestDirection)
        {
            Dash();
        }
        else
        {
            Jump(angle);
        }

        southButtonPressed = false;
        actionTimer = 0f; // Start the action timer
    }

    private void Jump(float angle) 
    {
        rb.useGravity = false;

        float horizontalDirection = Mathf.Cos(angle * Mathf.Deg2Rad);
        Vector2 horizontal = new Vector2(horizontalDirection, 0f);
        
        float estimatedDistance = maxJumpRange * holdRatio;
        float verticalVelocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * (estimatedDistance / 2f));

        targetVelocity = horizontal * estimatedDistance;
        targetVelocity.y = verticalVelocity;

        Debug.Log("Performing jump");
        playerJump = true;
        actionFinished = true;
    }

    private void Dash() 
    {
        rb.useGravity = false;

        Vector3 direction = isEastDirection ? Vector3.right : Vector3.left;
        float estimatedDistance = maxDashRange * holdRatio;

        targetVelocity = estimatedDistance * direction.normalized;
        targetVelocity.y = 0f;

        Debug.Log("Performing dash");
        playerDash = true;
        actionFinished = true;
    }

    private Vector3 GetSnappedDirection(Vector2 input)
    {
        if (input.magnitude < 0.2f)
            return Vector3.zero;

        // We’re working in X-Y plane now (left/right + up)
        Vector2 normalizedInput = input.normalized;

        float inputAngle = Mathf.Atan2(normalizedInput.y, normalizedInput.x) * Mathf.Rad2Deg;
        inputAngle = (inputAngle + 360f) % 360f;

        float closestAngle = 0f;
        float minDiff = float.MaxValue;

        foreach (float angle in allowedAngles)
        {
            float diff = Mathf.Abs(Mathf.DeltaAngle(inputAngle, angle));
            if (diff < minDiff)
            {
                minDiff = diff;
                closestAngle = angle;
            }
        }

        Vector2 snapped2D = new Vector2(
            Mathf.Cos(closestAngle * Mathf.Deg2Rad),
            Mathf.Sin(closestAngle * Mathf.Deg2Rad)
        );

        return new Vector3(snapped2D.x, snapped2D.y, 0f); // X-Y plane
    }

    private void ResetActionState()
    {
        // Reset all action states
        playerJump = false;
        playerDash = false;
        actionPerformed = false;
        actionFinished = false;
        rb.useGravity = true;
        actionTimer = 0f;
        holdTime = 0f;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 origin = transform.position;
        foreach (float angle in allowedAngles)
        {
            Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.right; // rotate around Z axis to stay in X/Y
            Gizmos.DrawLine(origin, origin + dir * jumpRange);
            DrawArrowHead(origin + dir * jumpRange, dir);
        }

        // Optional: show last target
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + (Vector3)lastSnappedDirection * jumpRange, 0.15f);

        if (lastSnappedDirection != Vector2.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position + (Vector3)lastSnappedDirection * jumpRange, 0.15f);
        }

        Gizmos.color = Color.green;
        Vector3 inputDir = GetSnappedDirection(inputDirection);
        if (inputDir != Vector3.zero)
        {
            Gizmos.DrawLine(transform.position, transform.position + inputDir * jumpRange);
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
    #endif

}

    // private void HandleAirMovement()
    // {
    //     // Only try to move in air if we have input and allowed by our conditions
    //     if (lastSnappedDirection.magnitude > deadzone && CanMoveInAir(true, false, true))
    //     {
    //         // Get movement direction from input relative to camera
    //         Vector3 movementDirection = new Vector3(lastSnappedDirection.x, 0, lastSnappedDirection.y);
            
    //         if (camTransform != null)
    //         {
    //             // Convert input to camera-relative direction
    //             movementDirection = camTransform.TransformDirection(movementDirection);
    //             movementDirection.y = 0; // Keep movement horizontal
    //             movementDirection.Normalize();
    //         }
            
    //         // Apply air movement force
    //         rb.AddForce(movementDirection * airMovementForce, ForceMode.Impulse);
            
    //         // Limit maximum air velocity
    //         if (rb.linearVelocity .magnitude > airMovementSpeed)
    //         {
    //             rb.linearVelocity  = rb.linearVelocity .normalized * airMovementSpeed;
    //         }
            
    //         Debug.Log($"Air movement applied: {movementDirection}, Speed: {rb.linearVelocity .magnitude}");
    //     }
        
    //     // Apply air drag when in air
    //     if (Time.time - lastContactTime > NO_CONTACT_THRESHOLD)
    //     {
    //         // Apply horizontal drag to limit air control
    //         Vector3 horizontalVelocity = new Vector3(rb.linearVelocity .x, 0, rb.linearVelocity .z);
    //         if (horizontalVelocity.magnitude > 0.1f)
    //         {
    //             Vector3 dragForce = airMovementDrag * horizontalVelocity.sqrMagnitude * -horizontalVelocity.normalized;
    //             rb.AddForce(dragForce, ForceMode.Impulse);
    //         }
    //     }
    // }

    // public bool CanMoveInAir(bool allowDuringActions = false, bool overrideFalling = false, bool consumeResource = false)
    // {
    //     // Basic in-air check
    //     bool isInAir = Time.time - lastContactTime > NO_CONTACT_THRESHOLD;
    //     if (!isInAir) return false;
        
    //     // Action in progress check
    //     if (actionInProgress && !allowDuringActions) return false;
        
    //     // Falling override
    //     if (isFalling && currentSurfaceState == SurfaceState.Ceiling && !overrideFalling) return false;
        
    //     // Resource consumption for air movement (future feature)
    //     if (consumeResource)
    //     {
    //         // Example implementation
    //         // if (airControlResource <= 0) return false;
    //         // airControlResource -= Time.deltaTime * airControlCost;
    //     }
        
    //     return true;
    // }
    
   // private Vector3 GetSnappedDirection(Vector2 input)
    // {
    //     if (input.magnitude < deadzone)
    //         return Vector3.zero;

    //     Vector2 normalizedInput = input.normalized;
    //     float inputAngle = Mathf.Atan2(normalizedInput.y, normalizedInput.x) * Mathf.Rad2Deg;
    //     inputAngle = (inputAngle + 360f) % 360f;

    //     // Snap based on sectors instead of closest angle
    //     float sectorSize = 360f / allowedAngles.Length;
    //     float halfSector = sectorSize / 2f;

    //     foreach (float allowedAngle in allowedAngles)
    //     {
    //         float lowerBound = (allowedAngle - halfSector + 360f) % 360f;
    //         float upperBound = (allowedAngle + halfSector) % 360f;

    //         bool inSector = lowerBound < upperBound
    //             ? inputAngle >= lowerBound && inputAngle < upperBound
    //             : inputAngle >= lowerBound || inputAngle < upperBound;

    //         if (inSector)
    //         {
    //             Vector2 snapped2D = new Vector2(
    //                 Mathf.Cos(allowedAngle * Mathf.Deg2Rad),
    //                 Mathf.Sin(allowedAngle * Mathf.Deg2Rad)
    //             );
    //             return new Vector3(snapped2D.x, snapped2D.y, 0f);
    //         }
    //     }

    //     return Vector3.zero;
    // }