using Unity.VisualScripting;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public MovementSystem movementSystem;
    public AnimationController animationController;
    public Animator animator;

    void Awake()
    {
        movementSystem = GetComponent<MovementSystem>();
        animationController = GetComponent<AnimationController>();
    }

    void Update()
    {
        switch (movementSystem.movementState)
        {
            case MovementSystem.MovementState.Idle:
                // Invoke the idle anim
                IdleAnim();

                break;

            case MovementSystem.MovementState.Charging:
                // Invoke the charging anim
                ChargingAnim();

                break;

            case MovementSystem.MovementState.Jumping:
                if(movementSystem.isDiagonalJump) // If jumping diagonal
                {
                  
                    if(movementSystem.rb.position.x > 0) 
                    {
                        // Invoke the diagonal jump anim
                        JumpDiagonalRightAnim();
                    }
                    else if(movementSystem.rb.position.x < 0) 
                    {
                        JumpDiagonalLeftAnim();
                    }
                }
                else // Straigh jump
                {
                    // Invoke the straigh vertical jump anim
                    JumpStraightAnim();
                }

                break;

            case MovementSystem.MovementState.WallJump:
                // Invoke wall jump
                if(movementSystem.currentSurfaceState != MovementSystem.SurfaceState.Ground ||
                    movementSystem.currentSurfaceState != MovementSystem.SurfaceState.Ceiling) 
                {
                    if(movementSystem.lastSurfaceObject.name.Contains("Left")) // If jumping from left wall
                    {
                        if(movementSystem.isDiagonalJump) 
                        {
                            if(movementSystem.rb.linearVelocity.magnitude > 0.01f) 
                            {
                                // Upward jumps
                                LeftWallJumpUpAnim();
                            }
                            else 
                            {
                                // Downward jump
                                LeftWallJumpDownAnim();
                            }
                        }
                    }
                    else if(movementSystem.lastSurfaceObject.name.Contains("Right"))
                    {
                        if(movementSystem.rb.linearVelocity.magnitude > 0.01f) 
                        {
                            // Upward jumps
                            RightWallJumpUpAnim();
                        }
                        else 
                        {
                            // Downward jump
                            RightWallJumpDownAnim();
                        }
                    }
                }

                break;

            case MovementSystem.MovementState.Hovering:
                // Invoke hovering
                HoverAnim();

                break;

            case MovementSystem.MovementState.Descending:
                // Invoke descending anim
                DescendingAnim();

                break;

            case MovementSystem.MovementState.Dashing:
                if(movementSystem.currentSurfaceState == MovementSystem.SurfaceState.Ground ||
                movementSystem.currentSurfaceState == MovementSystem.SurfaceState.Ceiling) 
                {
                    // Invoke ground dash
                    GroundDashAnim();
                }

                break;

            case MovementSystem.MovementState.AirDashing:
                if(movementSystem.rb.linearVelocity.magnitude < 0.01f) // Air dashing downward 
                {
                    // Invoke air dashing downward direction
                    // But check first if the air dash is diagonal 
                    if(movementSystem.isDiagonalAirDash) // If diagonal?
                    {
                        // Invoke diagonal air dash 
                        AirDashDownDiagonalAnim();
                    }
                    else 
                    {
                        // Invoke straight air dash
                        AirDashDownStraightAnim();
                    }
                }
                else // Air dashing upward
                {
                    // Invoke air dashing downward direction
                    if(movementSystem.isDiagonalAirDash) // If diagonal?
                    {
                        // Invoke diagonal air dash 
                        AirDashUpDiagonalAnim();
                    }
                    else 
                    {
                        // Invoke straight air dash
                        AirDashUpStraightAnim();
                    }
                }

                // Need to create an instance for air dashes left and right. I'll do that another time
                break;

            case MovementSystem.MovementState.WallDashing:
                // Invoke the wall dash anim
                    WallDashAnim();

                break;

            case MovementSystem.MovementState.Stucked:
                // Invoke stucked anim
                StuckedAnim();

                break;

            case MovementSystem.MovementState.WallDescending:
                // Invoke wall descending anim
                WallDescendingAnim();

                break;
        }
    }

    private void IdleAnim() // Idle
    {
        animator.SetBool("Idle", true);
        animator.SetBool("Charge", false);
        animator.SetBool("JumpUp", false);
        animator.SetBool("Hover", false);
        animator.SetBool("Descent", false);
        animator.SetBool("QuickDescent", false);
        animator.SetBool("Landing",false);
        animator.SetBool("DashR",false);
        animator.SetBool("DashL",false);
        animator.SetBool("Wallstick",false);


    }

    private void ChargingAnim() // Charging
    {
        animator.SetBool("Idle", false);
        animator.SetBool("Charge", true);
        animator.SetBool("JumpUp", false);
        animator.SetBool("Hover", false);
        animator.SetBool("Descent", false);
        animator.SetBool("QuickDescent", false);
        animator.SetBool("Landing",false);
        animator.SetBool("DashR",false);
        animator.SetBool("DashL",false);
        animator.SetBool("Wallstick",false);
    }

    private void JumpStraightAnim() // Jumping upward in a straight line
    {
        animator.SetBool("Idle", false);
        animator.SetBool("Charge", false);
        animator.SetBool("JumpUp", true);
        animator.SetBool("Hover", false);
        animator.SetBool("Descent", false);
        animator.SetBool("QuickDescent", false);
        animator.SetBool("Landing",false);
        animator.SetBool("DashR",false);
        animator.SetBool("DashL",false);
        animator.SetBool("Wallstick",false);
    }

    private void JumpDiagonalRightAnim()  // Jumping upward diagonal
    {

    }

    private void JumpDiagonalLeftAnim()  // Jumping upward diagonal
    {

    }

    private void LeftWallJumpUpAnim() // Jump from left wall diagonal upwards
    {

    }

    private void LeftWallJumpDownAnim() // Jump from left wall diagonal downwards
    {

    }

    private void RightWallJumpUpAnim() // Jump from right wall diagonal upwards
    {

    }

    private void RightWallJumpDownAnim() // Jump from right wall diagonal downwards
    {

    }

    private void AirDashDownStraightAnim() // Is basically jumping but then to the downward directions
    {

    }

    private void AirDashVerticalAnim() // Air dash left or right  
    {
        
    }

    private void AirDashUpStraightAnim() // Air dashing straight up
    {

    }

    private void AirDashUpDiagonalAnim() // Air dashing diagonal upwards
    {

    }

      private void AirDashDownDiagonalAnim() // Air dashing diagonal downwards
    {

    }

    private void GroundDashAnim() // Dash on the ground 
    {
           animator.SetBool("Idle", false);
        animator.SetBool("Charge", false;)
        animator.SetBool("JumpUp", false;)
        animator.SetBool("Hover", false;)
        animator.SetBool("Descent", false;)
        animator.SetBool("QuickDescent", false;)
        animator.SetBool("Landing",false;)
        animator.SetBool("DashR",true;)
        animator.SetBool("DashL",false;)
        animator.SetBool("Wallstick",false;)
    }

    private void WallDashAnim() // Dash on the wall
    {
        animator.SetBool("WallDash", true);
    }

    private void HoverAnim() // Hover anim
    {
       animator.SetBool("Idle", false);
        animator.SetBool("Charge", false);
        animator.SetBool("JumpUp", false);
        animator.SetBool("Hover", true);
        animator.SetBool("Descent", false);
        animator.SetBool("QuickDescent", false);
        animator.SetBool("Landing",false);
        animator.SetBool("DashR",false);
        animator.SetBool("DashL",false);
        animator.SetBool("Wallstick",false);
    }

    private void DescendingAnim() 
    {
       animator.SetBool("Idle", false);
        animator.SetBool("Charge", false);
        animator.SetBool("JumpUp", false);
        animator.SetBool("Hover", false);
        animator.SetBool("Descent", true);
        animator.SetBool("QuickDescent", false);
        animator.SetBool("Landing",false);
        animator.SetBool("DashR",false);
        animator.SetBool("DashL",false);
        animator.SetBool("Wallstick",false);
    }

    private void WallDescendingAnim() 
    {
        
    }

    private void StuckedAnim() 
    {

    }
}
