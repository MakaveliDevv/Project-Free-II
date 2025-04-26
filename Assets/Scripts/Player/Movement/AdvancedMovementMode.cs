using UnityEngine;
using UnityEngine.InputSystem;

public class AdvancedMovementMode : JumpDashSystem
{
    private PlayerController playerController;

    public AdvancedMovementMode(MonoBehaviour mono, InputAction movementAction) : base(mono, movementAction)
    {
        playerController = mono.GetComponent<PlayerController>();
    }

    public void CustomUpdate() 
    {
        if(playerController.mode == PlayerController.Mode.AdvancedMovementMode)  
        {
            AdvancedMovement();
        }
    }

    public void AdvancedMovement() 
    {
        // Allow longer range for dashes
        
        // Allow jump from wall instead descending

        if(playerController.state == PlayerController.JumpState.Descending) 
        {
            // Allow dash but default range
        }
    }
}
