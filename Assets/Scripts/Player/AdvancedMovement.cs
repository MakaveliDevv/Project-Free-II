using UnityEngine;

namespace Assets.Scripts.Player
{
    public class AdvancedMovement
    {
        private readonly Player player;
        private readonly MovementSystem movementSystem;
        public bool isAdvancedMovementActive = false;

        // public AdvancedMovement(Player player, MovementSettings settings, UnityEngine.InputSystem.InputActionAsset inputActionAsset) : base(player, settings, inputActionAsset) { }
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
        }

        private void BounceFromSurfaceUponCollision()
        {
            if (!isAdvancedMovementActive) return;

            string dirLabel = movementSystem.GetClosestDirectionLabel(movementSystem.snappedDir);

            // Check if that direction is allowed from current surface
            bool isAllowed = movementSystem.allowedMoveLabels.TryGetValue(movementSystem.settings.currentSurfaceState, out string[] allowedDirs) &&
                            System.Array.Exists(allowedDirs, d => d == dirLabel);

            Vector3 bounceDirection;
            if (isAllowed && movementSystem.snappedDir != Vector2.zero)
            {
                // Use the player’s intended direction if allowed
                bounceDirection = movementSystem.snappedDir;
            }
            else
            {
                // 6. Else: choose a random allowed direction
                if (allowedDirs != null && allowedDirs.Length > 0)
                {
                    string randomLabel = allowedDirs[Random.Range(0, allowedDirs.Length)];
                    float angleDeg = movementSystem.labelToAngle[randomLabel];
                    float angleRad = angleDeg * Mathf.Deg2Rad;
                    bounceDirection = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;
                }
                else
                {
                    // Fallback direction in case something goes wrong
                    bounceDirection = Vector2.up;
                }
            }

            // 7. Apply an impulse in the chosen bounce direction
            float bounceForce = movementSystem.settings.bounceSpeed;
            movementSystem.rb.linearVelocity = Vector3.zero;
            movementSystem.rb.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);

            // Reset relevant states to avoid conflicts
            movementSystem.ResetActionState();
            movementSystem.settings.movementState = MovementState.Jumping; 
            movementSystem.actionInProgress = true;
            movementSystem.hasAppliedForce = true;

            // Mark it as bounced to prevent repeated bouncing on the same surface
            movementSystem.hasBounced = true;
        }

        // private void BounceFromSurfaceUponCollision()
        // {
        //     if (!isAdvancedMovementActive) return;

        //     // 1. Fetch the player current position

        //     // 2. Fetch the surface on contact (i dont think this is needed since I already have a function to fetch the allowed direction based off the surface the player made contact with so step 3 would be good enough)

        //     // 3. Fetch the input dir right before contact or on contact

        //     // 4. Check if the input dir is allowed

        //     // 5. If so then launch the player into that direction

        //     // 6. Else fetch a random allowed dir and launch the player off into that direction

        //     // 7. Ultimately the player should bounce immediately off the surface
        // }

        public void OnCollisionEnter(Collision collision)
        {
            if (player.mode == Mode.AdvancedMovement) { BounceFromSurfaceUponCollision(); }
        }
    }
}

