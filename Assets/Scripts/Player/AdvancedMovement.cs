using UnityEngine.InputSystem;

public class AdvancedMovement : MovementSystemm
{
    private InputAction leftShoulder, rightShoulder;
    private bool leftShoulderPerformed = false, rightShoulderPerformed = false;
    private bool isAdvancedMovementActive = false;

    void Awake()
    {
        SetupInputActions();
    }

    void Update()
    {
        if (leftShoulderPerformed && !isAdvancedMovementActive)
        {
            isAdvancedMovementActive = true;

            if (isAdvancedMovementActive)
            {
                allowAirDash = true;   
            }
        }
    }

    private void SetupInputActions()
    {
        var map = inputActions.FindActionMap("Player");
        leftShoulder = map.FindAction("AdvancedMovementMode");
        rightShoulder = map.FindAction("CombatMode");

        leftShoulder.Enable();
        rightShoulder.Enable();
    }

    void OnEnable()
    {
        RegisterInputCallbacks();
    }

    void OnDisable()
    {
        UnregisterInputCallbacks();
    }

    private void RegisterInputCallbacks()
    {
        leftShoulder.started += OnLeftShoulderStarted;
        leftShoulder.performed += OnLeftshoulderPerformed;
        leftShoulder.canceled += OnRightShoulderCanceled;

        rightShoulder.started += OnRightshoulderStarted;
        rightShoulder.performed += OnRightShoulderPerformed;
        rightShoulder.canceled += OnRightShoulderCanceled;

    }

    private void UnregisterInputCallbacks()
    {
        // Left shoulder (LB)
        leftShoulder.started -= OnLeftShoulderStarted;
        leftShoulder.performed -= OnLeftshoulderPerformed;
        leftShoulder.canceled -= OnLeftShoulderCanceled;

        // Right shoulder (RB)
        rightShoulder.started -= OnRightshoulderStarted;
        rightShoulder.performed -= OnRightShoulderPerformed;
        rightShoulder.canceled -= OnRightShoulderCanceled;
    }

    // Left shoulder (LB)
    private void OnLeftShoulderStarted(InputAction.CallbackContext ctx)
    {

    }

    private void OnLeftshoulderPerformed(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && !leftShoulderPerformed)
        {
            leftShoulderPerformed = true;
        }
    }

    private void OnLeftShoulderCanceled(InputAction.CallbackContext ctx)
    {
        leftShoulderPerformed = false;
    }

    // Right shoulder (RB)
    private void OnRightshoulderStarted(InputAction.CallbackContext ctx)
    {

    }

    private void OnRightShoulderPerformed(InputAction.CallbackContext ctx)
    {

    }

    private void OnRightShoulderCanceled(InputAction.CallbackContext ctx)
    {
        
    }
}