using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.MovementSystem.Scripts.Player
{
    public enum MovementState { Idle, Charging, Jumping, WallJump, Hovering, Descending, Dashing, AirDashing, WallDashing, Stucked, WallDescending, NOTHING }
    public enum SurfaceState { Ground, LeftWall, RightWall, Ceiling, Air }
    public enum GravityDirection { Down, Up, Left, Right }

    public class Player : MonoBehaviour
    {
        private MovementSystem movementSystem;
        public MovementSettings movementSettings;
        private bool inRangeForInteractable = false;
        private bool inRangeForAttackable = false;

        private List<GameObject> interactable = new();
        private List<GameObject> attackable = new();

        private GameObject _interactable;
        private GameObject _attackable;

        public InputActionAsset inputActionAsset;
        private InputAction dPadUp;
        private InputAction dPadRight;

        // ---------------------------------------

        public enum Mode { Normal, AdvancedMovement, Attack }
        public Mode mode;

        void Awake()
        {
            SetupInputActions();
            movementSystem = new MovementSystem(this, movementSettings, inputActionAsset);
            movementSystem.Awake();
            movementSettings.currentSurfaceState = SurfaceState.Ground;

            mode = Mode.Normal;
        }

        void Start()
        {
            movementSystem.Start();
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

        // void OnValidate()
        // {
        //     if (Application.isPlaying)
        //         movementSystem.OnValidate();
        // }

        void Update()
        {
            movementSystem.Update();

            if (inRangeForInteractable && interactable.Count == 1)
            {
                // If player pressed the button for the gravitational pull

                // Then activate the gravitational pull method
            }
        }

        void LateUpdate()
        {
            movementSystem.LateUpdate();
        }

        void FixedUpdate()
        {
            movementSystem.FixedUpdate();
        }

        void OnCollisionEnter(Collision collision)
        {
            movementSystem.OnCollisionEnter(collision);
        }

        void OnCollisionExit(Collision collision)
        {
            movementSystem.OnCollisionExit(collision);
        }

        void OnDrawGizmos()
        {
            if(Application.isPlaying)
                movementSystem.OnDrawGizmos();
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
