using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour 
{
    private MovementSystem movementSystem;
    private bool inRangeForInteractable = false;
    private bool inRangeForAttackable = false;

    private List<GameObject> interactable = new();
    private List<GameObject> attackable = new();

    private GameObject _interactable;
    private GameObject _attackable;

    public InputActionAsset inputActions;
    private InputAction dPadUp;
    private InputAction dPadRight;

    // ---------------------------------------

    public enum Mode { Normal, AdvancedMovement, Attack }
    public Mode mode;

    private void Awake()
    {
        movementSystem = GetComponent<MovementSystem>();
        SetupInputActions();
        
        mode = Mode.Normal;
    }

    private void SetupInputActions()
    {
        var map = inputActions.FindActionMap("ToggleMechanics");
        dPadUp = map.FindAction("ToggleAutoHover");
        dPadRight = map.FindAction("ToggleAirDash");

        dPadUp.Enable();
        dPadRight.Enable();
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
        dPadUp.started += ToggleAutoHover;
        dPadRight.started += ToggleAirDash;
    }

    private void UnregisterInputCallbacks()
    {
        dPadUp.started -= ToggleAutoHover;
        dPadRight.started -= ToggleAirDash;
    }

    private void ToggleAutoHover(InputAction.CallbackContext ctx) 
    {
        Debug.Log("swag");
        if(ctx.started) 
        {
            Debug.Log("Toggle Auto Hover");
            movementSystem.useAutoHover = !movementSystem.useAutoHover;
        }
    }

    private void ToggleAirDash(InputAction.CallbackContext ctx) 
    {
        if(ctx.started) 
        {
            Debug.Log("Toggle Air Dash");
            movementSystem.allowAirDash = !movementSystem.allowAirDash;
        }
    }
    
    private void Update()
    {
        if(inRangeForInteractable && interactable.Count == 1) 
        {
            // If player pressed the button for the gravitational pull

            // Then activate the gravitational pull method
        } 
    }

    // private void OnTriggerEnter(Collider collider) 
    // {
    //     if(collider.CompareTag("Interactable")) 
    //     {
    //         inRangeForInteractable = true;
    //         if(interactable.Count == 0) 
    //         {
    //             interactable.Add(collider.gameObject);
    //             _interactable = interactable[0];
    //         }
    //     }

    //     if(collider.CompareTag("Attackable")) 
    //     {
    //         inRangeForAttackable = true;
    //         if(interactable.Count == 0) 
    //         {
    //             attackable.Add(collider.gameObject);
    //             _attackable = attackable[0];
    //         }
    //     }
    // }

    // private void OnTriggerExit(Collider collider) 
    // {
    //     if(collider.CompareTag("Interactable")) 
    //     {
    //         inRangeForInteractable = false;

    //         if(interactable.Count > 0) 
    //         {
    //             interactable.Clear();
    //             _interactable = null;
    //         }
    //     }

    //     if(collider.CompareTag("Attackable")) 
    //     {
    //         inRangeForAttackable = false;
    //         if(interactable.Count == 0) 
    //         {
    //             attackable.Add(collider.gameObject);
    //             _attackable = null;
    //         }
    //     }
    // }
}