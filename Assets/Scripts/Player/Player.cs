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
        public AnimationSettings animSettings;

        public MovementSystemController moveContrl;
        public CombatController combatContrl;
        public MovementInteraction moveInt;
        private AnimationController animContr;

        [HideInInspector] public Rigidbody rb;
        [HideInInspector] public Collider col;
        private Animator animator;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            animator = transform.GetChild(0).GetComponent<Animator>();

            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            InputSystem.settings.maxEventBytesPerUpdate = 0;

            playerSettings = new();
            InputManager.Initialize(inputActionAsset, playerSettings.useRawInput, movementSettings.minStickMagnitude);

            // Class instances
            moveContrl = new(this, movementSettings);
            combatContrl = new(this, combatSettings);
            moveInt = new(this);
            animContr = new(this, animSettings, animator);

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

            if (playerSettings.movementState == MovementState.Idle && playerSettings.currentSurfaceState == SurfaceState.Ground) moveInt.interacting = false;

            animContr.Update();

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
            moveInt.OnTriggerEnter(collider);
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
