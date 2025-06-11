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
        Launching,
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
        // settings
        public PlayerSettings playerSettings;
        public MovementSettings movementSettings;
        public AdvancedMovementSettings advancedMovementSettings;
        public CombatSettings combatSettings;
        public InteractionSettings interactionSettings;

        public MovementSystemController moveContrl;
        public CombatController combatContrl;
        public MovementInteraction moveInt;

        void Awake()
        {
            playerSettings = new();
            
            InputSystem.settings.maxEventBytesPerUpdate = 0;
            InputManager.Initialize(inputActionAsset, playerSettings.useRawInput, movementSettings.minStickMagnitude);

            moveContrl = new(this, movementSettings);
            combatContrl = new(this, combatSettings);
            moveInt = new(playerSettings);

            moveContrl?.Awake();
            playerSettings.currentSurfaceState = SurfaceState.Ground;

            mode = Mode.Normal;
        }

        void Start()
        {
            moveContrl?.Start();
        }

        void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            moveContrl?.OnValidate();
        }


        void Update()
        {
            moveInt.Update();

            InputManager.UpdateInput();
            movementSettings.useAutoHover = InputManager.UseAutoHover;

            moveContrl?.Update();
            combatContrl?.Update();

            switch (mode)
            {
                case Mode.Normal:
                    moveContrl.advancedMovement.isAdvancedMovementActive = false;
                    combatContrl.isCombatModeActive = false;
                    movementSettings.allowAirDash = false;

                    break;
                case Mode.AdvancedMovement:
                    combatContrl.isCombatModeActive = false;
                    moveContrl.advancedMovement.isAdvancedMovementActive = true;
                    movementSettings.allowAirDash = true;

                    break;
                case Mode.Combat:
                    moveContrl.advancedMovement.isAdvancedMovementActive = false;
                    movementSettings.allowAirDash = false;
                    combatContrl.isCombatModeActive = true;

                    break;

                default:
                    break;
            }

            InputManager.ResetFrameInputs();
        }

        void LateUpdate()
        {
            moveContrl?.LateUpdate();
        }

        void FixedUpdate()
        {
            moveContrl?.FixedUpdate();
        }

        void OnCollisionEnter(Collision collision)
        {
            moveContrl?.OnCollisionEnter(collision);
        }

        void OnCollisionExit(Collision collision)
        {
            moveContrl?.OnCollisionExit(collision);
        }

        void OnTriggerEnter(Collider collider)
        {
            combatContrl?.OnTriggerEnter(collider);
        }

        void OnTriggerExit(Collider collider)
        {
            combatContrl?.OnTriggerExit(collider);
        }


        void OnDrawGizmos()
        {
            moveContrl?.OnDrawGizmos();
            moveInt?.OnDrawGizmosRay(transform.position, interactionSettings.selectionRange);
        }
    }   
}
