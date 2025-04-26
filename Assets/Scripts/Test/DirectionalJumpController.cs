using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DirectionalJumpController : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float jumpRange = 5f;
    public Transform jumpDirectionArcRoot; // optional, for visualization/debugging

    [Header("Allowed Jump Angles (degrees)")]
    public float[] allowedAngles = { 180f, 157.5f, 135f, 112.5f, 90f, 67,5f, 45f, 22,5f, 0f }; 

    private Vector2 inputDirection;
    private bool jumpPressed;
    private Rigidbody rb;
    private Transform camTransform;
    public float deadzone;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        camTransform = Camera.main.transform;
    }

    void Update()
    {
        JoystickDeadZone.InputWithScaledRadialDeadZone(inputDirection.x, inputDirection.y);
        return;
        if(inputDirection.magnitude < deadzone) 
        {
            inputDirection = Vector3.zero;
        }
        else 
        {
            // Debug.Log($"Input: {inputDirection}");
            return;
        }

        if (jumpPressed)
        {
            Vector3 jumpDir = GetSnappedDirection(inputDirection);
            lastSnappedDirection = jumpDir; 

            if (jumpDir != Vector3.zero)
            {
                rb.linearVelocity  = Vector3.zero; // Reset current velocity
                rb.AddForce(jumpDir * jumpForce, ForceMode.VelocityChange);

                jumpPressed = false; // Prevent repeated jumps
            }
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        inputDirection = context.ReadValue<Vector2>();
        Debug.Log($"RAW INPUT: {inputDirection} || Magnitude: {inputDirection.magnitude} || Normalized: {inputDirection.normalized}");
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && inputDirection.y > 0.1f) // prevent downward input
        {
            jumpPressed = true;
        }
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

        // Debug.Log($"Input angle: {inputAngle}, Snapped angle: {closestAngle}");
        return new Vector3(snapped2D.x, snapped2D.y, 0f); // X-Y plane
    }

    private Vector3 lastSnappedDirection = Vector3.zero;
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
        Gizmos.DrawSphere(transform.position + lastSnappedDirection * jumpRange, 0.15f);

        if (lastSnappedDirection != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position + lastSnappedDirection * jumpRange, 0.15f);
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

public static class JoystickDeadZone
{
    public const float defaultDeadzone = 0.001f;
    
    /// <summary>
    /// Returns joystick input with proper radial deadzone.
    /// </summary>
    /// <param name="horizontalInput"></param>
    /// <param name="verticalInput"></param>
    /// <param name="deadzone"></param>
    /// <returns></returns>
    public static Vector2 InputWithRadialDeadZone(float horizontalInput, float verticalInput, float deadzone=defaultDeadzone)
    {
        Vector2 joystickInput = new Vector2(horizontalInput, verticalInput);
        
        // if(joystickInput.magnitude < deadzone) joystickInput = Vector2.zero;
        // Debug.Log($"joystick input: {joystickInput} || joystick magnitude {joystickInput.magnitude}");

        return joystickInput;
    }
    
    /// <summary>
    /// Returns joystick input with scaled radial deadzone, that feels like accceleration.
    /// Good when high precision is needed
    /// </summary>
    /// <param name="horizontalInput"></param>
    /// <param name="verticalInput"></param>
    /// <param name="deadzone"></param>
    /// <returns></returns>
    public static Vector2 InputWithScaledRadialDeadZone(float horizontalInput, float verticalInput,
        float deadzone = defaultDeadzone)
    {
        Vector2 joystickInput = new Vector2(horizontalInput, verticalInput);
        if(joystickInput.magnitude < deadzone)
            joystickInput = Vector2.zero;
        else
            joystickInput = joystickInput.normalized * ((joystickInput.magnitude - deadzone) / (1 - deadzone));

        // Debug.Log($"joystick input: {joystickInput} || joystick input normalized: {joystickInput.normalized} || joystick magnitude {joystickInput.magnitude}");
        return joystickInput;
    }
}
