using UnityEngine;

namespace Assets.Scripts.Player
{
    public class AdvancedMovement
    {
        private readonly Player player;
        private readonly MovementSystem movementSystem;
        public MovementInteraction moveInt;
        private readonly AdvancedMovementSettings advMoveSettings;
        public bool isAdvancedMovementActive = false;

        private bool bounceBufferActive = false;
        private float bounceBufferTimer = 0f;
        private Vector2 bufferedDirection = Vector2.zero;
        private bool hasBounced = false;
        private float postBounceTimer = 0f;

        public bool IsBlockingInput() => postBounceTimer > 0f;
        private Collider lastBouncedCollider = null;
        private readonly bool useBounce;

        public AdvancedMovement
        (
            Player player,
            MovementSystem movementSystem,
            AdvancedMovementSettings advMoveSettings,
            bool useBounce
        )
        {
            this.player = player;
            this.movementSystem = movementSystem;
            this.advMoveSettings = advMoveSettings;
            this.useBounce = useBounce;

            moveInt = new(player);
        }

        public void Update()
        {
            // if (InputManager.LeftShoulderDoublePressed)
            // {
            //     InputManager.LeftShoulderPressed = false;
            //     player.mode = Mode.Normal;
            //     return;
            // }

            // if (InputManager.LeftShoulderPressed && !isAdvancedMovementActive) { player.mode = Mode.AdvancedMovement; }


if (InputManager.LeftShoulderPressed) 
            {
                player.mode = Mode.AdvancedMovement;
            }

            if (InputManager.LeftShoulderReleased)
            {
                player.mode = Mode.Normal;
            }
            
            if (player.mode == Mode.AdvancedMovement)
            {
                if (useBounce)
                {
                    if (bounceBufferActive)
                    {
                        bounceBufferTimer -= Time.deltaTime;

                        if (InputManager.LeftStickInput.magnitude > 0.1f)
                            bufferedDirection = InputManager.LeftStickInput.normalized;

                        if (bounceBufferTimer <= 0f && !hasBounced)
                        {
                            ApplyBufferedBounce();
                            hasBounced = true;
                            bounceBufferActive = false;
                        }
                    }

                    if (hasBounced && player.playerSettings.movementState != MovementState.Bouncing)
                    {
                        hasBounced = false;
                    }

                    if (postBounceTimer > 0f)
                    {
                        postBounceTimer -= Time.deltaTime;
                    }
                }

                moveInt.Update();
            }
            
        }

        private void ApplyBufferedBounce()
        {
            string dirLabel = movementSystem.GetClosestDirectionLabel(bufferedDirection);

            bool isAllowed = movementSystem.allowedMoveLabels.TryGetValue(
                player.playerSettings.currentSurfaceState,
                out string[] allowedDirs
            ) && System.Array.Exists(allowedDirs, d => d == dirLabel);

            Vector3 bounceDirection;
            if (isAllowed && bufferedDirection != Vector2.zero)
            {
                bounceDirection = bufferedDirection;
            }
            else if (allowedDirs != null && allowedDirs.Length > 0)
            {
                string randomLabel = allowedDirs[Random.Range(0, allowedDirs.Length)];
                float angleDeg = movementSystem.labelToAngle[randomLabel];
                float angleRad = angleDeg * Mathf.Deg2Rad;
                bounceDirection = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;
            }
            else
            {
                bounceDirection = Vector2.up;
            }

            movementSystem.rb.linearVelocity = Vector3.zero;
            movementSystem.rb.AddForce(bounceDirection * advMoveSettings.bounceForce, ForceMode.Impulse);
            player.playerSettings.movementState = MovementState.Bouncing;
            movementSystem.hasAppliedForce = true;

            postBounceTimer = advMoveSettings.postBounceCooldown;  // block actions for a short time
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (!isAdvancedMovementActive) return;

            SurfaceState currentSurface = player.playerSettings.currentSurfaceState;

            if (currentSurface == SurfaceState.Air)
                return;

            Collider currentCollider = collision.collider;

            // Allow bounce if it’s a new surface, or a same surface but a valid action was performed
            if (currentCollider == lastBouncedCollider &&
                !movementSystem.HasPerformedActionSinceLastBounce())
                return;

            bounceBufferActive = true;
            bounceBufferTimer = advMoveSettings.bounceDelay;
            bufferedDirection = Vector2.zero;
            hasBounced = false;

            lastBouncedCollider = currentCollider;

            movementSystem.SetActionSinceLastBounce(false);

        }
        public void OnTriggerEnter(Collider collider)
        {
            moveInt.OnTriggerEnter(collider);
        }

        public void OnTriggerExit(Collider collider)
        { 
            moveInt.OnTriggerExit(collider);
        }

        public void OnDrawGizmos(Vector3 origin, float range)
        {
            moveInt.OnDrawGizmosRay(origin, range);
        }
    }
}

