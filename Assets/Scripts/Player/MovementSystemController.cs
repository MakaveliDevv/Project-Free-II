using UnityEngine;

namespace Assets.Scripts.Player
{
    public class MovementSystemController
    {
        public readonly MovementSystem movementSystem;
        public AdvancedMovement advancedMovement;

        public MovementSystemController(Player player, MovementSettings settings, bool useBounce)
        {
            movementSystem = new(player, player.playerSettings, settings);
            advancedMovement = new(player, movementSystem, player.advancedMovementSettings, useBounce);
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
            movementSystem?.OnCollisionExit();
        }

        public void OnTriggerEnter(Collider collider)
        { 
            advancedMovement?.OnTriggerEnter(collider);
        }

        public void OnDrawGizmos(Vector3 origin, float range)
        {
            if (Application.isPlaying)
            {
                movementSystem?.OnDrawGizmos();
                advancedMovement?.OnDrawGizmos(origin, range);
            }
        }
    }
}