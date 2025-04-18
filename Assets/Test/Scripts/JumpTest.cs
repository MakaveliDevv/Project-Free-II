using UnityEngine;

public class JumpTest : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpUpSpeed = 10f;
    public float maxHeight = 10f;
    public float fallSpeed = -10f;

    [Header("Hover Settings")]
    public float hoverWobbleAmplitude = 0.2f;
    public float hoverWobbleFrequency = 2f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;

    private Rigidbody rb;
    private float baseY;
    private float hoverTime = 0f;

    private enum JumpState { Idle, Ascending, Hovering, Descending }
    private JumpState state = JumpState.Idle;

    void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();
        rb.useGravity = true;
        baseY = rb.transform.position.y;
    }

    void Update()
    {
        HandleJump();
    }

    void FixedUpdate()
    {
        if (rb.linearVelocity .y < fallSpeed)
        {
            rb.linearVelocity  = new Vector3(rb.linearVelocity .x, fallSpeed, rb.linearVelocity .z);
        }
    }


    private void HandleJump()
    {
        bool isHoldingJump = Input.GetKey(KeyCode.W);
        float currentHeight = rb.transform.position.y - baseY;

        switch (state)
        {
            case JumpState.Idle:
                if (isHoldingJump && IsGrounded())
                {
                    rb.linearVelocity  = new Vector3(rb.linearVelocity .x, jumpUpSpeed, rb.linearVelocity .z);
                    state = JumpState.Ascending;
                }
                break;

            case JumpState.Ascending:
                if (!isHoldingJump || currentHeight >= maxHeight)
                {
                    rb.useGravity = false;
                    hoverTime = 0f;
                    state = JumpState.Hovering;
                }
                break;

            case JumpState.Hovering:
                hoverTime += Time.deltaTime;

                // Small vertical wobble motion
                float wobble = Mathf.Sin(hoverTime * hoverWobbleFrequency) * hoverWobbleAmplitude;
                rb.linearVelocity  = new Vector3(rb.linearVelocity .x, wobble, rb.linearVelocity .z);

                if (!isHoldingJump)
                {
                    rb.useGravity = true;
                    state = JumpState.Descending;
                }
                break;

            case JumpState.Descending:
                if (IsGrounded())
                {
                    state = JumpState.Idle;
                }
                break;
        }
    }

    private bool IsGrounded()
    {
        Collider col = rb.GetComponent<Collider>();
        Vector3 origin = col.bounds.center;
        origin.y = col.bounds.min.y + 0.01f;
        Vector3 direction = Vector3.down;
        float distance = groundCheckDistance + 0.1f;

        if (Physics.Raycast(origin, direction, distance, groundLayer)) 
        {
            Debug.DrawRay(origin, direction * distance, Color.green);
            return true;
        }
        else 
        {
            Debug.DrawRay(origin, direction * distance, Color.red);
            return false;
        }
    }
}
