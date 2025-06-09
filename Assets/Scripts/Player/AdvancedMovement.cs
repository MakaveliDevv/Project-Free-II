using UnityEngine;

namespace Assets.Scripts.Player
{
    public class AdvancedMovement
    {
        private readonly Player player;
        private readonly MovementSystem movementSystem;
        public bool isAdvancedMovementActive = false;

        private bool bounceBufferActive = false;
        private float bounceBufferTimer = 0f;
        private Vector2 bufferedDirection = Vector2.zero;
        private bool hasBounced = false;
        private float postBounceTimer = 0f;

        public bool IsBlockingInput() => postBounceTimer > 0f;
        private Collider lastBouncedCollider = null;

        public AdvancedMovement(Player player, MovementSystem movementSystem)
        {
            this.player = player;
            this.movementSystem = movementSystem;
        }

        public void Update()
        {
            if (InputManager.LeftShoulderDoublePressed)
            {
                InputManager.LeftShoulderPressed = false;
                player.mode = Mode.Normal;
                return;
            }

            if (InputManager.LeftShoulderPressed && !isAdvancedMovementActive) { player.mode = Mode.AdvancedMovement; }

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

            if (hasBounced && movementSystem.settings.movementState != MovementState.Bouncing)
            {
                hasBounced = false;
            }

            if (postBounceTimer > 0f)
            {
                postBounceTimer -= Time.deltaTime;
            }
        }

        private void ApplyBufferedBounce()
        {
            string dirLabel = movementSystem.GetClosestDirectionLabel(bufferedDirection);

            bool isAllowed = movementSystem.allowedMoveLabels.TryGetValue(
                movementSystem.settings.currentSurfaceState,
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
            movementSystem.rb.AddForce(bounceDirection * player.advancedMovementSettings.bounceForce, ForceMode.Impulse);
            movementSystem.settings.movementState = MovementState.Bouncing;
            movementSystem.hasAppliedForce = true;

            postBounceTimer = player.advancedMovementSettings.postBounceCooldown;  // block actions for a short time
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (!isAdvancedMovementActive) return;

            SurfaceState currentSurface = movementSystem.settings.currentSurfaceState;

            if (currentSurface == SurfaceState.Air)
                return;

            Collider currentCollider = collision.collider;

            // Allow bounce if it’s a new surface, or a same surface but a valid action was performed
            if (currentCollider == lastBouncedCollider &&
                !movementSystem.HasPerformedActionSinceLastBounce())
                return;

            bounceBufferActive = true;
            bounceBufferTimer = player.advancedMovementSettings.bounceDelay;
            bufferedDirection = Vector2.zero;
            hasBounced = false;

            lastBouncedCollider = currentCollider;

            movementSystem.SetActionSinceLastBounce(false); 
        }
    }
}

