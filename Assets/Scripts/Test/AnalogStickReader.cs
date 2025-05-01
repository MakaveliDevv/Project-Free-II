using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class AnalogStickReader : MonoBehaviour
{
    public InputAction moveAction;
    public bool useRawInput = true;
    private Vector2 currentInput = Vector2.zero;

    [Header("Gizmo Settings")]
    public float gizmoScale = 2f;
    public Color gizmoColor = Color.green;

    [Header("Jump Settings")]
    public float jumpForce = 5f;
    public float horizontalForceMultiplier = 3f;
    public float maxJumpDistance = 5f;
    public float jumpSpeed = 10f;

    private Rigidbody rb;
    private bool isJumping = false;
    private Vector3 jumpTarget;

    void OnEnable()
    {
        moveAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
    }

    void Awake()
{
    rb = GetComponent<Rigidbody>();
    rb.isKinematic = true;
    startPosition = transform.position; // Store starting point
}


    void Update()
    {
        if (useRawInput && Gamepad.current != null)
        {
            currentInput = Gamepad.current.leftStick.ReadUnprocessedValue();
        }
        else
        {
            currentInput = moveAction.ReadValue<Vector2>();
        }

        Debug.Log("Analog Stick Input: " + currentInput);

        if (!isJumping && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            JumpInInputDirection();
        }

        if (isJumping)
        {
            // Move manually toward jumpTarget
            transform.position = Vector3.MoveTowards(transform.position, jumpTarget, jumpSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, jumpTarget) < 0.01f)
            {
                isJumping = false;
                rb.isKinematic = false; // Re-enable physics
            }
        }

        if (Vector3.Distance(transform.position, jumpTarget) < 0.01f)
{
    isJumping = false;
    landingPoints.Add(jumpTarget);

    if (returnToStartAfterJump)
    {
        isReturning = true;
        rb.isKinematic = true; // disable physics during return
    }
    else
    {
        rb.isKinematic = false;
    }
}
if (isReturning)
{
    transform.position = Vector3.MoveTowards(transform.position, startPosition, returnSpeed * Time.deltaTime);

    if (Vector3.Distance(transform.position, startPosition) < 0.01f)
    {
        isReturning = false;
        rb.isKinematic = false; // re-enable physics after return
    }
}

    }

    public void JumpInInputDirection()
    {
        if (currentInput.sqrMagnitude < 0.01f)
            return;

        // Get input angle
        float angle = Mathf.Atan2(currentInput.y, currentInput.x) * Mathf.Rad2Deg;

        // Rotate around local Z axis
        Quaternion rotation = Quaternion.AngleAxis(angle, transform.forward);
        Vector3 direction = rotation * transform.right; // local right rotated

        // Calculate target jump position relative to local Z rotation
        jumpTarget = transform.position + direction.normalized * maxJumpDistance + Vector3.up * jumpForce;

        rb.isKinematic = true;
        isJumping = true;
    }
[Header("Return Settings")]
public bool returnToStartAfterJump = false;
public float returnSpeed = 5f;

private Vector3 startPosition;
private bool isReturning = false;

private List<Vector3> landingPoints = new List<Vector3>();

    void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, gizmoScale);

        if (currentInput.sqrMagnitude > 0.001f)
        {
            // Convert input into an angle
            float angle = Mathf.Atan2(currentInput.y, currentInput.x) * Mathf.Rad2Deg;

            // Rotate around local Z axis
            Quaternion rotation = Quaternion.AngleAxis(angle, transform.forward);
            Vector3 rotatedDirection = rotation * transform.right;

            Gizmos.color = gizmoColor;
            Vector3 inputVector = rotatedDirection.normalized * gizmoScale;
            Gizmos.DrawLine(transform.position, transform.position + inputVector);
            Gizmos.DrawSphere(transform.position + inputVector, 0.05f);
        }

        // Show landing point
        if (isJumping)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(jumpTarget, 0.25f);
        }

        // Draw all past landing circles
        Gizmos.color = Color.green;
        foreach (var point in landingPoints)
        {
            Gizmos.DrawWireSphere(point, 0.25f);
        }

    }


}
