using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public enum Mode { Default, AdvancedMovementMode, BattleMode }
    public enum JumpState { Idle, Charging, Jumping, Hovering, Descending, Dashing, Stucked, WallDescending }
    public Mode mode = Mode.Default;
    public JumpState state = JumpState.Idle;

    [Header("Input Settings")]
    public InputActionAsset inputActions;
    
    [Header("Jump & Dash System Parameters")]
    public JumpDashSystemParam jumpDashParams = new JumpDashSystemParam();
    
    private JumpDashSystem jumpDashSystem;
    private InputAction movementAction;
    private InputAction actionInput;

    void Awake()
    {
        var map = inputActions.FindActionMap("Player");
        movementAction = map.FindAction("Movement");
        actionInput = map.FindAction("Jump");
        
        // Create the jump dash system
        jumpDashSystem = new JumpDashSystem(this, movementAction);
        
        // Apply the parameters immediately
        ApplyJumpDashParameters();

        movementAction.Enable();
        actionInput.Enable();
    }

    void OnEnable()
    {
        movementAction.started += jumpDashSystem.OnStickStarted;
        movementAction.performed += jumpDashSystem.OnStickPerformed;
        movementAction.canceled += jumpDashSystem.OnStickCanceled;

        actionInput.started += jumpDashSystem.OnJump;
    }

    void OnDisable()
    {
        movementAction.started -= jumpDashSystem.OnStickStarted;
        movementAction.performed -= jumpDashSystem.OnStickPerformed;
        movementAction.canceled -= jumpDashSystem.OnStickCanceled;

        actionInput.started -= jumpDashSystem.OnJump;
    }

    void Start()
    {
        jumpDashSystem.Start();
    }

    void Update()
    {
        // THIS IS HERE FOR TEST PURPOSES
        // ApplyJumpDashParameters();
        
        jumpDashSystem.Update();
    }

    void FixedUpdate()
    {
        jumpDashSystem.FixedUpdate();
    }
    
    private void ApplyJumpDashParameters()
    {
        // Copy all parameter values to the JumpDashSystem
        jumpDashSystem.maxJumpRange = jumpDashParams.maxJumpRange;
        jumpDashSystem.maxHoldTime = jumpDashParams.maxHoldTime;
        jumpDashSystem.maxDashRange = jumpDashParams.maxDashRange;
        jumpDashSystem.dashForceMultiplier = jumpDashParams.dashForceMultiplier;
        jumpDashSystem.stopDashAfterDuration = jumpDashParams.stopDashAfterDuration;
        jumpDashSystem.jumpForceMultiplier = jumpDashParams.jumpForceMultiplier;
        jumpDashSystem.epsilon = jumpDashParams.epsilon;
        jumpDashSystem.clampMagnitudeMaxLength = jumpDashParams.clampMagnitudeMaxLength;
        jumpDashSystem.hoverDelay = jumpDashParams.hoverDelay;
        jumpDashSystem.hoverDuration = jumpDashParams.hoverDuration;
        jumpDashSystem.groundCheckDistance = jumpDashParams.groundCheckDistance;
        jumpDashSystem.gravityScale = jumpDashParams.gravityScale;
        jumpDashSystem.fastFallMultiplier = jumpDashParams.fastFallMultiplier;
        jumpDashSystem.horizontalLeftAngleThreshold = jumpDashParams.horizontalLeftAngleThreshold;
        jumpDashSystem.horizontalRightAngleThreshold = jumpDashParams.horizontalRightAngleThreshold;
        jumpDashSystem.wallCheckDistance = jumpDashParams.wallCheckDistance;
        jumpDashSystem.wallLayer = jumpDashParams.wallLayer;
    }
}
    // private string GetDirectionName(float angleDegrees)
    // {
    //     if (angleDegrees >= -22.5f && angleDegrees < 22.5f) return "E";
    //     if (angleDegrees >= 22.5f && angleDegrees < 67.5f) return "NE";
    //     if (angleDegrees >= 67.5f && angleDegrees < 112.5f) return "N";
    //     if (angleDegrees >= 112.5f && angleDegrees < 157.5f) return "NW";
    //     if (angleDegrees >= 157.5f || angleDegrees < -157.5f) return "W";
    //     if (angleDegrees >= -157.5f && angleDegrees < -112.5f) return "SW";
    //     if (angleDegrees >= -112.5f && angleDegrees < -67.5f) return "S";
    //     if (angleDegrees >= -67.5f && angleDegrees < -22.5f) return "SE";
    //     return "Unknown";
    // }