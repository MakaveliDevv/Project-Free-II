using Unity.VisualScripting;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public MovementSystem movementSystem;
    public AnimationController animationController;
    public Animator animator;

    //Particle systems
    public ParticleSystem JumpParticles;

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
                Debug.Log("Starting idle anim");
                IdleAnim();

                break;

            case MovementSystem.MovementState.Charging:
                ChargingAnim();

                break;

            case MovementSystem.MovementState.Jumping:
                if(movementSystem.isStraightJump) { JumpStraightAnim(); }
                else  
                {
                    if(movementSystem.isDiagonalJumpRight) { JumpDiagonalRightAnim(); }
                    else { JumpDiagonalLeftAnim(); }
                }
                break;

            case MovementSystem.MovementState.Dashing:
                switch (movementSystem.currentSurfaceState)
                {
                    case MovementSystem.SurfaceState.Ground:
                        GroundDashAnim();
                        break;

                    case MovementSystem.SurfaceState.Ceiling:
                        CeilingDashAnim();
                        break;

                    case MovementSystem.SurfaceState.RightWall:
                        RightWallDashAnim();
                        break;
                    
                    case MovementSystem.SurfaceState.LeftWall:
                        LeftWallDashAnim();
                        break;
                }

                break;

            case MovementSystem.MovementState.WallJump:
                if(movementSystem.currentSurfaceState != MovementSystem.SurfaceState.Ground ||
                    movementSystem.currentSurfaceState != MovementSystem.SurfaceState.Ceiling) 
                {
                    Debug.Log("Invoke wall jump anim");	
                    if(movementSystem.isWallJumpRight) 
                    {
                        InvokeLeftWallJumpAnim();
                    }
                    else 
                    {
                        InvokeRightWallJumpAnim();
                    }
                }

                break;

            case MovementSystem.MovementState.Hovering:
                HoverAnim();

                break;

            case MovementSystem.MovementState.Descending:
                DescendingAnim();

                break;

            case MovementSystem.MovementState.AirDashing:
                if(movementSystem.isVerticalAirDash) 
                {
                    if(movementSystem.isAirDashAscend) { AirDashVerticalAscend(); }
                    else { AirDashVerticalDescend(); }
                }
                else if(movementSystem.isHorizontalAirDash) 
                {
                    if(movementSystem.isRightAirDash) { AirDashHorizontalRight(); }
                    else { AirDashHorizontalLeft(); }
                }
                else 
                {
                    if(movementSystem.isRightDiagonalAirDash) 
                    {
                        if(movementSystem.isAirDashAscend) { AirDashDiagonalRightUp(); }
                        else { AirDashDiagonalRightDown(); }
                    }
                    else  
                    {
                        if(movementSystem.isAirDashAscend) { AirDashDiagonalLeftUp(); }
                        else { AirDashDiagonalLeftDown(); }
                    }
                }
              

                break;

            case MovementSystem.MovementState.Stucked:
                StuckedAnim();

                break;

            case MovementSystem.MovementState.WallDescending:
                if(movementSystem.currentSurfaceState == MovementSystem.SurfaceState.RightWall)
                {
                    RightWallDescendingAnim();
                }
                else if(movementSystem.currentSurfaceState == MovementSystem.SurfaceState.LeftWall)
                {
                    LeftWallDescendingAnim();
                }

                break;
        }
    }

    private void IdleAnim() 
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

    private void ChargingAnim() 
    {
        Debug.Log("Invoke charging anim");
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

    private void HoverAnim() 
    {
        Debug.Log("Invoke hover anim");
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
        Debug.Log("Invoke descend anim");
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

    private void RightWallDescendingAnim() 
    {
        Debug.Log("Invoke wall descend anim (right wall)");
    }

    private void LeftWallDescendingAnim() 
    {
        Debug.Log("Invoke wall descend anim (left wall)");
    }

    private void StuckedAnim() 
    {
        Debug.Log("Invoke stucked anim");
    }

    // Jump animations
    #region Jump Animations

        private void JumpStraightAnim() 
        {
            Debug.Log("Invoke jump straight anim");
            JumpParticles.Play();
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

        private void JumpDiagonalRightAnim()  
        {
            Debug.Log("Invoke jump diagonal anim (right)");
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

        private void JumpDiagonalLeftAnim()  
        {
            Debug.Log("Invoke jump diagonal anim (left)");
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

        private void InvokeLeftWallJumpAnim() // DONT TOUCH THIS METHOD
        {
            if(movementSystem.isWallJumpAscend) { RightWallJumpAscendAnim(); }
            else { RightWallJumpDescendAnim(); }
        }

        private void InvokeRightWallJumpAnim() // DONT TOUCH THIS METHOD
        {
            if(movementSystem.isWallJumpAscend) { LeftWallJumpAscendAnim(); }
            else { LeftWallJumpDescendAnim(); }
        }

        private void RightWallJumpAscendAnim() 
        {
            Debug.Log("Invoke right wall jump ascend anim");
        }

        private void RightWallJumpDescendAnim()
        {
            Debug.Log("Invoke right wall jump descend anim");
        }

        private void LeftWallJumpAscendAnim() 
        {
            Debug.Log("Invoke left wall jump ascend anim");
        }

        private void LeftWallJumpDescendAnim()
        {
            Debug.Log("Invoke left wall jump descend anim");
        }

    #endregion Jump Animations

    #region Dash Animations
        private void GroundDashAnim() 
        {
            Debug.Log("Invoke ground dash anim");
        }

        private void CeilingDashAnim() 
        {
            Debug.Log("Invoke ceiling dash anim");
        }

        private void RightWallDashAnim() 
        {
            Debug.Log("Invoke wall dash anim (right)");
        }

        private void LeftWallDashAnim() 
        {
            Debug.Log("Invoke wall dash anim (left)");
        }

    #endregion Dash Animations

    #region Air Dash Animations
        private void AirDashVerticalAscend() 
        {
            Debug.Log("Invoke air dash straight up anim");
        }

        private void AirDashVerticalDescend() 
        {
            Debug.Log("Invoke air dash straight down anim");
        }

        private void AirDashHorizontalRight() 
        {
            Debug.Log("Invoke air dash right anim (horizontal)");
        }

        private void AirDashHorizontalLeft() 
        {
            Debug.Log("Invoke air dash left anim (horizontal)");
        }

        private void AirDashDiagonalRightUp() 
        {
            Debug.Log("Invoke air dash up diagonally anim (right)");
        }

        private void AirDashDiagonalRightDown() 
        {
            Debug.Log("Invoke air dash down diagonally anim (right)");
        }

        private void AirDashDiagonalLeftUp() 
        {
            Debug.Log("Invoke air dash up diagonally anim (left)");
        }

        private void AirDashDiagonalLeftDown() 
        {
            Debug.Log("Invoke air dash down diagonally anim (left)");
        }

    #endregion Air Dash Animations
  
}
