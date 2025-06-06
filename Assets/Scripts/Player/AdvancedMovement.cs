namespace Assets.Scripts.Player
{
    public class AdvancedMovement : MovementSystem
    {
        public bool isAdvancedMovementActive  = false;
    
        public AdvancedMovement(Player player, MovementSettings settings, UnityEngine.InputSystem.InputActionAsset inputActionAsset) : base(player, settings, inputActionAsset) {}

        public new void Update()
        {
            if (InputManager.LeftShoulderDoublePressed)
            {
                InputManager.LeftShoulderPressed = false;
                player.mode = Mode.Normal;
                return;
            }

            if (InputManager.LeftShoulderPressed && !isAdvancedMovementActive) { player.mode = Mode.AdvancedMovement; }
        }
    }
}

