using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Player
{
    public enum MovementState { Idle, Charging, Jumping, WallJump, Hovering, Descending, Dashing, AirDashing, WallDashing, Stucked, WallDescending, NOTHING }
    public enum SurfaceState { Ground, LeftWall, RightWall, Ceiling, Air }
    public enum GravityDirection { Down, Up, Left, Right }
    public enum Mode { Normal, AdvancedMovement, Combat }

    [RequireComponent(typeof(Rigidbody))]
    public class Player : MonoBehaviour
    {
        public InputActionAsset inputActionAsset;

        // ─ Class References
        public MovementSettings movementSettings;
        public CombatSettings combatSettings;
        private MovementSystemController movementController;
        [HideInInspector] public CombatController combatController;

        public Mode mode;

        private bool inRangeForInteractable = false;
        private bool inRangeForAttackable = false;

        private List<GameObject> interactable = new();
        private List<GameObject> attackable = new();

        private GameObject _interactable;
        private GameObject _attackable;

        private InputAction dPadUp;
        private InputAction dPadRight;

        // ---------------------------------------

        void Awake()
        {
            InputSystem.settings.maxEventBytesPerUpdate = 0;
            InputManager.Initialize(inputActionAsset, movementSettings.useRawInput, movementSettings.minStickMagnitude);

            movementController = new(this, movementSettings, inputActionAsset);
            combatController = new(this, combatSettings);

            movementController?.Awake();
            movementSettings.currentSurfaceState = SurfaceState.Ground;

            mode = Mode.Normal;

            SetupInputActions();
        }

        void Start()
        {
            movementController?.Start();
        }

        private void SetupInputActions()
        {
            var map = inputActionAsset.FindActionMap("ToggleMechanics");
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
            if (ctx.started)
            {
                Debug.Log("Toggle Auto Hover");
                movementSettings.useAutoHover = !movementSettings.useAutoHover;
            }
        }

        private void ToggleAirDash(InputAction.CallbackContext ctx)
        {
            if (ctx.started)
            {
                Debug.Log("Toggle Air Dash");
                movementSettings.allowAirDash = !movementSettings.allowAirDash;
            }
        }

        void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            movementController?.OnValidate();
        }


        void Update()
        {
            InputManager.UpdateInput();

            movementController?.Update();
            combatController?.Update();

            if (inRangeForInteractable && interactable.Count == 1)
            {
                // If player pressed the button for the gravitational pull

                // Then activate the gravitational pull method
            }

            switch (mode)
            {
                case Mode.Normal:
                    movementController.advancedMovement.isAdvancedMovementActive = false;
                    combatController.isCombatModeActive = false;
                    movementSettings.allowAirDash = false;

                    break;
                case Mode.AdvancedMovement:
                    combatController.isCombatModeActive = false;
                    movementController.advancedMovement.isAdvancedMovementActive = true;
                    movementSettings.allowAirDash = true;

                    break;
                case Mode.Combat:
                    movementController.advancedMovement.isAdvancedMovementActive = false;
                    movementSettings.allowAirDash = false;
                    combatController.isCombatModeActive = true;

                    break;

                default:
                    break;
            }

            InputManager.ResetFrameInputs();
        }

        void LateUpdate()
        {
            movementController?.LateUpdate();
        }

        void FixedUpdate()
        {
            movementController?.FixedUpdate();
        }

        void OnCollisionEnter(Collision collision)
        {
            movementController?.OnCollisionEnter(collision);
        }

        void OnCollisionExit(Collision collision)
        {
            movementController?.OnCollisionExit(collision);
        }

        void OnTriggerEnter(Collider collider)
        {
            combatController?.OnTriggerEnter(collider);
        }

        void OnTriggerExit(Collider collider)
        {
            combatController?.OnTriggerExit(collider);
        }

        void OnDrawGizmos()
        {
            if (Application.isPlaying)
                movementController?.OnDrawGizmos();
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
}
