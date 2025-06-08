using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Player
{
    public enum MovementState
    {
        Idle,
        Charging,
        Jumping,
        WallJump,
        Hovering,
        Descending,
        Dashing,
        AirDashing,
        WallDashing,
        Stucked,
        WallDescending,
        Bouncing,
        Interacting,
        Nothing
    }

    public enum SurfaceState { Ground, LeftWall, RightWall, Ceiling, Air, Nothing }
    public enum GravityDirection { Down, Up, Left, Right }
    public enum Mode { Normal, AdvancedMovement, Combat }

    [RequireComponent(typeof(Rigidbody))]
    public class Player : MonoBehaviour
    {
        public InputActionAsset inputActionAsset;
        public Mode mode;

        // ─ Class References
        public MovementSettings movementSettings;
        public AdvancedMovementSettings advancedMovementSettings;
        public CombatSettings combatSettings;
        public MovementSystemController movementController;
        public CombatController combatController;

        void Awake()
        {
            InputSystem.settings.maxEventBytesPerUpdate = 0;
            InputManager.Initialize(inputActionAsset, movementSettings.useRawInput, movementSettings.minStickMagnitude);

            movementController = new(this, movementSettings, inputActionAsset);
            combatController = new(this, combatSettings);

            movementController?.Awake();
            movementSettings.currentSurfaceState = SurfaceState.Ground;

            mode = Mode.Normal;
        }

        void Start()
        {
            movementController?.Start();
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
            movementSettings.useAutoHover = InputManager.useAutoHover;

            movementController?.Update();
            combatController?.Update();

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
    }   
}
