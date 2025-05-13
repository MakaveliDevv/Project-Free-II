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
                        InvokeRightWallJumpAnim();
                    }
                    else 
                    {
                        InvokeLeftWallJumpAnim();
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
        // -- DEFAULT JUMP
        private void JumpStraightAnim() // VERTICAL UP
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

        private void JumpDiagonalRightAnim()  // DIAGONAL RIGHT
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

        private void JumpDiagonalLeftAnim()  // DIAGONAL LEFT
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

        private void InvokeRightWallJumpAnim() // DONT TOUCH THIS METHOD
        {
            if(movementSystem.isWallJumpHorizontal) { RightWallJumpHorizontal(); }
            else if(movementSystem.isWallJumpAscend) { RightWallJumpAscendAnim(); }
            else { RightWallJumpDescendAnim(); }
        }

        private void InvokeLeftWallJumpAnim() // DONT TOUCH THIS METHOD
        {
            if(movementSystem.isWallJumpHorizontal) { LeftWallJumpHorizontal(); }
            else if(movementSystem.isWallJumpAscend) { LeftWallJumpAscendAnim(); }
            else { LeftWallJumpDescendAnim(); }
        }

        // -- WALL JUMP
        // Right wall
        private void RightWallJumpAscendAnim() // RIGHT WALL JUMP ASCEND
        {
            Debug.Log("Invoke right wall jump ascend anim");
        }

        private void RightWallJumpDescendAnim() // RIGHT WALL JUMP DESCEND
        {
            Debug.Log("Invoke right wall jump descend anim");
        }

        private void RightWallJumpHorizontal() // RIGHT WALL JUMP HORIZONTAL
        {
            Debug.Log("Invoke wall jump horizontal <--");
        }

        // Left wall
        private void LeftWallJumpAscendAnim() // LEFT WALL JUMP ASCEND
        {
            Debug.Log("Invoke left wall jump ascend anim");
        }

        private void LeftWallJumpDescendAnim() // LEFT WASLL JUMP DESCEND
        {
            Debug.Log("Invoke left wall jump descend anim");
        }

        private void LeftWallJumpHorizontal() // LEFT WALL JUMP HORIZONTAL
        {
            Debug.Log("Invoke left wall jump horizontal -->");
        }

    #endregion Jump Animations

    #region Dash Animations
        // -- GROUND DASH
        private void GroundDashAnim() // DONT TOUCH THIS METHOD
        {
            if(movementSystem.isRightGroundDash) { RightGroundDashAnim(); }
            else { LeftGroundDashAnim(); }
            Debug.Log("Invoke ground dash anim");
        }

        private void RightGroundDashAnim() // RIGHT ->> 
        {
            Debug.Log("Ground dash anim ->");
        }

        private void LeftGroundDashAnim() // LEFT <<-
        {
            Debug.Log("Ground dash anim <-");
        }

        // -- CEILING DASH
        private void CeilingDashAnim() // DONT TOUCH THIS METHOD
        {
            if(movementSystem.isRightGroundDash) { RightCeilingDashAnim(); }
            else { LeftCeilingDashAnim(); }
        }

        private void RightCeilingDashAnim() // RIGHT ->> 
        {
            Debug.Log("Ceiling dash anim ->");
        }

        private void LeftCeilingDashAnim() // LEFT <--
        {
            Debug.Log("Ceiling dash anim <-");
        }

        // -- WALL DASH
        // RIGHT WALL
        private void RightWallDashAnim() // DONT TOUCH THIS METHOD
        {
            if(movementSystem.isUpWallDash) { UpRightWallDashAnim(); }
            else { DownRightWallDashAnim(); }
            Debug.Log("Invoke wall dash anim (right)");
        }

        private void UpRightWallDashAnim() // UP 
        {
            Debug.Log("Invoke upward dash on the right wall");
        }

        private void DownRightWallDashAnim() // DOWN 
        {
            Debug.Log("Invoke downward dash on the right wall");
        }

        // LEFT WALL
        private void LeftWallDashAnim() // DONT TOUCH THIS METHOD
        {
            if(movementSystem.isUpWallDash) { UpLeftWallDashAnim(); }
            else { DownLeftWallDashAnim(); }
            Debug.Log("Invoke wall dash anim (left)");
        }

        private void UpLeftWallDashAnim() // UP
        {
            Debug.Log("Invoke upward dash on the left wall");
        }

        private void DownLeftWallDashAnim() // DOWN
        {
            Debug.Log("Invoke downward dash on the left wall");

        }

    #endregion Dash Animations

    #region Air Dash Animations
        private void AirDashVerticalAscend() // VERTICAL UP
        {
            Debug.Log("Invoke air dash straight up anim");
        }

        private void AirDashVerticalDescend() // VERTICAL DOWN
        {
            Debug.Log("Invoke air dash straight down anim");
        }

        private void AirDashHorizontalRight() // HORIZONTAL RIGHT
        {
            Debug.Log("Invoke air dash right anim (horizontal)");
        }

        private void AirDashHorizontalLeft() // HORIZONTAL LEFT
        {
            Debug.Log("Invoke air dash left anim (horizontal)");
        }

        private void AirDashDiagonalRightUp() // DIAGONAL RIGHT UP
        {
            Debug.Log("Invoke air dash up diagonally anim (right)");
        }

        private void AirDashDiagonalRightDown() // DIAGONAL RIGHT DOWN
        {
            Debug.Log("Invoke air dash down diagonally anim (right)");
        }

        private void AirDashDiagonalLeftUp() // DIAGONAL LEFT UP
        {
            Debug.Log("Invoke air dash up diagonally anim (left)");
        }

        private void AirDashDiagonalLeftDown() // DIAGONAL LEFT DOWN
        {
            Debug.Log("Invoke air dash down diagonally anim (left)");
        }

    #endregion Air Dash Animations
  
}
