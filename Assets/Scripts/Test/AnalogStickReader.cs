using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class AnalogStickReader : MonoBehaviour
{
    public InputAction moveAction;
    public bool useRawInput = true;
    private Vector2 currentInput = Vector2.zero;

    [Header("Gizmo Settings")]
    public float gizmoScale = 2f;
    public Color gizmoColor = Color.green;
    public float directionLineLength = 1.5f;

    [Header("Jump Settings")]
    public float jumpForce = 5f;
    public float horizontalForceMultiplier = 3f;
    public float maxJumpDistance = 5f;
    public float jumpSpeed = 10f;

    [Header("Return Settings")]
    public bool returnToStartAfterJump = false;
    public float returnSpeed = 5f;

    [Header("Snapped Input Settings")]
    public bool snapDirectionsEnabled = false;
    public int directionCount = 16;

    [Header("Label Settings")]
    public bool showDirectionLabels = true;
    public bool useCardinalLabels = true;

    private Rigidbody rb;
    private Vector3 startPosition;
    private Vector3 jumpTarget;
    private bool isJumping = false;
    private bool isReturning = false;
    private List<Vector3> landingPoints = new List<Vector3>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        startPosition = transform.position;
    }

    void OnEnable()
    {
        moveAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
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

        // Handle jump input
        if (!isJumping && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            JumpInInputDirection();
        }

        // Handle jump movement
        if (isJumping)
        {
            transform.position = Vector3.MoveTowards(transform.position, jumpTarget, jumpSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, jumpTarget) < 0.01f)
            {
                isJumping = false;
                landingPoints.Add(jumpTarget);

                if (returnToStartAfterJump)
                {
                    isReturning = true;
                    rb.isKinematic = true;
                }
                else
                {
                    rb.isKinematic = false;
                }
            }
        }

        // Handle return to start
        if (isReturning)
        {
            transform.position = Vector3.MoveTowards(transform.position, startPosition, returnSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, startPosition) < 0.01f)
            {
                isReturning = false;
                rb.isKinematic = false;
            }
        }
    }

    public void JumpInInputDirection()
    {
        Vector3 snappedDir = GetSnappedDirection();
        if (snappedDir.sqrMagnitude < 0.01f)
            return;

        jumpTarget = transform.position + snappedDir.normalized * maxJumpDistance + Vector3.up * jumpForce;

        rb.isKinematic = true;
        isJumping = true;
    }

    private Vector3 GetSnappedDirection()
    {
        if (currentInput.sqrMagnitude < 0.01f)
            return Vector3.zero;

        float rawAngle = Mathf.Atan2(currentInput.y, currentInput.x) * Mathf.Rad2Deg;

        if (snapDirectionsEnabled)
        {
            float angleStep = 360f / directionCount;
            rawAngle = Mathf.Round(rawAngle / angleStep) * angleStep;
        }

        Quaternion rotation = Quaternion.AngleAxis(rawAngle, transform.forward);
        return rotation * transform.right;
    }

    private string GetDirectionLabel(int index)
    {
        if (!useCardinalLabels)
            return (index + 1).ToString();

        // Standard 16-point compass
        string[] labels = new string[]
        {
            "E", "ENE", "NE", "NNE",
            "N", "NNW", "NW", "WNW",
            "W", "WSW", "SW", "SSW",
            "S", "SSE", "SE", "ESE"
        };

        return labels[index % labels.Length];
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, gizmoScale);

        // 🔵 Draw all direction segments (in editor and play mode)
        if (snapDirectionsEnabled)
        {
            Gizmos.color = Color.blue;
            float angleStep = 360f / directionCount;

            for (int i = 0; i < directionCount; i++)
            {
                float angle = i * angleStep;
                Quaternion rotation = Quaternion.AngleAxis(angle, transform.forward);
                Vector3 dir = rotation * transform.right;

                Vector3 endPoint = transform.position + dir * directionLineLength;
                Gizmos.DrawLine(transform.position, endPoint);

                // 🏷️ Draw label
                if (showDirectionLabels)
                {
        #if UNITY_EDITOR
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.Label(endPoint + Vector3.up * 0.1f, GetDirectionLabel(i));
        #endif
                }
            }
        }

        // 🟢 Show current snapped input (only in Play mode)
        if (Application.isPlaying && currentInput.sqrMagnitude > 0.01f)
        {
            Vector3 snappedDir = GetSnappedDirection();
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(transform.position, transform.position + snappedDir.normalized * directionLineLength);
        }

        // 🔴 Draw jump target
        if (isJumping)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(jumpTarget, 0.25f);
        }

        // ✅ Draw all previous landings
        Gizmos.color = Color.green;
        foreach (var point in landingPoints)
        {
            Gizmos.DrawWireSphere(point, 0.25f);
        }
    }
}
