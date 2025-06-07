using UnityEngine;

namespace Assets.Scripts.Player
{
    public class MovementSystemController
    {
        public readonly MovementSystem movementSystem;
        public AdvancedMovement advancedMovement;

        public MovementSystemController(Player player, MovementSettings settings, UnityEngine.InputSystem.InputActionAsset inputActionAsset)
        {
            movementSystem = new(player, settings, inputActionAsset);
            advancedMovement = new(player, movementSystem);
        }

        public void Awake()
        {
            movementSystem?.Awake();
        }

        public void Start()
        {
            movementSystem?.Start();
        }

        public void OnValidate()
        {
            movementSystem?.OnValidate();
        }

        public void Update()
        {
            movementSystem?.Update();
            advancedMovement?.Update();
        }

        public void LateUpdate()
        {
            movementSystem?.LateUpdate();
        }

        public void FixedUpdate()
        {
            movementSystem?.FixedUpdate();
        }

        public void OnCollisionEnter(Collision collision)
        {
            movementSystem?.OnCollisionEnter(collision);
            advancedMovement?.OnCollisionEnter(collision);
        }

        public void OnCollisionExit(Collision collision)
        {
            movementSystem?.OnCollisionExit(collision);
            // advancedMovement?.OnCollisionExit(collision);
        }

        public void OnDrawGizmos()
        {
            if (Application.isPlaying)
                movementSystem?.OnDrawGizmos();
        }
    }
}