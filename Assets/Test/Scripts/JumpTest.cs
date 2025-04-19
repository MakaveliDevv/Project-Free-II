using UnityEngine;
using UnityEngine.InputSystem;

public class JumpTest : MonoBehaviour
{
    public InputActionAsset inputActions; // assign in inspector
    private InputAction movementAction;
    
    [Header("Jump Settings")]
    public float jumpUpSpeed = 10f;
    public float maxHeight = 10f;
    public float fallSpeed = -10f;
    public float sideMoveSpeed = 5f;

    [Header("Hover Settings")]
    public float hoverDuration = 1f;
    public float hoverWobbleAmplitude = 0.2f;
    public float hoverWobbleFrequency = 2f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;

    private Rigidbody rb;
    private float baseY;
    private float hoverTimer = 0f;

    public enum JumpState { Idle, Ascending, Hovering, Descending }
    public JumpState state = JumpState.Idle;

    void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();
        rb.useGravity = true;
        baseY = rb.transform.position.y;

        // Get the action map
        var playerMap = inputActions.FindActionMap("Player");

        // Enable the map (optional if already enabled elsewhere)
        playerMap.Enable();

        // Get the action
        movementAction = playerMap.FindAction("Movement");

        // Enable the action
        movementAction.Enable();
    }

    void Update()
    {
        HandleJump();
    }

    void FixedUpdate()
    {
        if (rb.linearVelocity.y < fallSpeed)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, fallSpeed, rb.linearVelocity.z);
        }
    }

    private void HandleJump()
    {
        Vector2 moveInput = movementAction.ReadValue<Vector2>();
        moveInput = moveInput.sqrMagnitude < 0.01f ? Vector2.zero : moveInput.normalized;

        bool isHoldingJump = moveInput.y > 0.1f;
        bool isPressingDrop = moveInput.y < -0.1f;
        bool isMovingLeft = moveInput.x < -0.1f;
        bool isMovingRight = moveInput.x > 0.1f;

        // bool isHoldingJump = Input.GetKey(KeyCode.W);
        // bool isPressingDrop = Input.GetKey(KeyCode.S);
        float currentHeight = rb.transform.position.y - baseY;

        if (isPressingDrop && state != JumpState.Idle)
        {
            ForceDrop();
            return;
        }

        Vector3 horizontalInput = Vector3.zero;
        if (isMovingLeft) horizontalInput += Vector3.left;
        if (isMovingRight) horizontalInput += Vector3.right;
        float xVelocity = horizontalInput.normalized.x * sideMoveSpeed;

        switch (state)
        {
            case JumpState.Idle:
                if (isHoldingJump && IsGrounded())
                {
                    Vector3 jumpVelocity = new Vector3(xVelocity, jumpUpSpeed, 0f);
                    rb.linearVelocity = jumpVelocity;
                    state = JumpState.Ascending;
                }
                break;

            case JumpState.Ascending:
                rb.linearVelocity = new Vector3(xVelocity, rb.linearVelocity.y, rb.linearVelocity.z);

                if (!isHoldingJump || currentHeight >= maxHeight)
                {
                    rb.useGravity = false;
                    hoverTimer = 0f;
                    state = JumpState.Hovering;
                }
                break;

            case JumpState.Hovering:
                hoverTimer += Time.deltaTime;

                // Calculate target height (hover anchor point)
                float hoverAnchorY = baseY + currentHeight; // lock to where hover started

                // Calculate the difference between current Y and the anchor
                float heightDelta = hoverAnchorY - rb.transform.position.y;

                // Apply a smooth pull towards the anchor height (simulate "magnetic" hover pull)
                float pullForce = heightDelta * 10f; // the '10f' is like spring stiffness — tweak as needed

                // Add wobble as an offset
                float wobble = Mathf.Sin(hoverTimer * hoverWobbleFrequency) * hoverWobbleAmplitude;

                // Total vertical velocity = hover pull + wobble
                float hoverVelocityY = pullForce + wobble;

                rb.linearVelocity = new Vector3(0f, hoverVelocityY, rb.linearVelocity.z); // xVelocity = 0 during hover

                if (hoverTimer >= hoverDuration)
                {
                    rb.useGravity = true;
                    state = JumpState.Descending;
                }
                break;
            
            // case JumpState.Hovering:
            //     hoverTimer += Time.deltaTime;

            //     float wobble = Mathf.Sin(hoverTimer * hoverWobbleFrequency) * hoverWobbleAmplitude;
            //     rb.linearVelocity = new Vector3(xVelocity, wobble, rb.linearVelocity.z);

            //     if (hoverTimer >= hoverDuration)
            //     {
            //         rb.useGravity = true;
            //         state = JumpState.Descending;
            //     }
            //     break;

            case JumpState.Descending:
                rb.linearVelocity = new Vector3(xVelocity, rb.linearVelocity.y, rb.linearVelocity.z);

                if (IsGrounded())
                {
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);
                    state = JumpState.Idle;
                }
                break;
        }
    }

    private void ForceDrop()
    {
        rb.useGravity = true;
        rb.linearVelocity = new Vector3(0f, fallSpeed, rb.linearVelocity.z);
        state = JumpState.Descending;
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

