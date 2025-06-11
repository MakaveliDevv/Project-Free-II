using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public class AnimationController2 : MonoBehaviour
    {
        public Animator animator;
        public AnimationController2 animationController;
        private Player player;

        //Particle systems
        [SerializeField] private ParticleSystem jumpParticlePrefab;
        [SerializeField] private ParticleSystem jumpEParticlePrefab;
        [SerializeField] private ParticleSystem jumpWParticlePrefab;
        [SerializeField] private ParticleSystem dashRParticlePrefab;
        [SerializeField] private ParticleSystem dashLParticlePrefab;
        [SerializeField] private ParticleSystem stuckLParticlePrefab;
        [SerializeField] private ParticleSystem stuckRParticlePrefab;


        private bool hasPlayedJumpParticles = false;
        private bool hasPlayedDashParticles = false;
        private bool hasPlayedStuckParticles = false;


        void Awake()
        {
            player = GetComponent<Player>();
            animationController = GetComponent<AnimationController2>();
        }

        void Update()
        {
            switch (player.playerSettings.movementState)
            {
                case MovementState.Idle:
                    Debug.Log("Starting idle anim");
                    IdleAnim();

                    break;

                case MovementState.Charging:
                    ChargingAnim();

                    break;

                case MovementState.Jumping:
                    if (player.moveContrl.movementSystem.isStraightJump) { JumpStraightAnim(); }
                    else
                    {
                        if (player.moveContrl.movementSystem.isDiagonalJumpRight) { JumpDiagonalRightAnim(); }
                        else { JumpDiagonalLeftAnim(); }
                    }
                    break;

                case MovementState.Dashing:
                    switch (player.playerSettings.currentSurfaceState)
                    {
                        case SurfaceState.Ground:
                            GroundDashAnim();
                            break;

                        case SurfaceState.Ceiling:
                            CeilingDashAnim();
                            break;

                        case SurfaceState.RightWall:
                            RightWallDashAnim();
                            break;

                        case SurfaceState.LeftWall:
                            LeftWallDashAnim();
                            break;
                    }

                    break;

                case MovementState.WallJump:
                    if (player.playerSettings.currentSurfaceState != SurfaceState.Ground ||
                        player.playerSettings.currentSurfaceState != SurfaceState.Ceiling)
                    {
                        Debug.Log("Invoke wall jump anim");
                        if (player.moveContrl.movementSystem.isWallJumpRight)
                        {
                            InvokeRightWallJumpAnim();
                        }
                        else
                        {
                            InvokeLeftWallJumpAnim();
                        }
                    }

                    break;

                case MovementState.Hovering:
                    HoverAnim();

                    break;

                case MovementState.Descending:
                    DescendingAnim();

                    break;

                case MovementState.AirDashing:
                    if (player.moveContrl.movementSystem.isVerticalAirDash)
                    {
                        if (player.moveContrl.movementSystem.isAirDashAscend) { AirDashVerticalAscend(); }
                        else { AirDashVerticalDescend(); }
                    }
                    else if (player.moveContrl.movementSystem.isHorizontalAirDash)
                    {
                        if (player.moveContrl.movementSystem.isRightAirDash) { AirDashHorizontalRight(); }
                        else { AirDashHorizontalLeft(); }
                    }
                    else
                    {
                        if (player.moveContrl.movementSystem.isRightDiagonalAirDash)
                        {
                            if (player.moveContrl.movementSystem.isAirDashAscend) { AirDashDiagonalRightUp(); }
                            else { AirDashDiagonalRightDown(); }
                        }
                        else
                        {
                            if (player.moveContrl.movementSystem.isAirDashAscend) { AirDashDiagonalLeftUp(); }
                            else { AirDashDiagonalLeftDown(); }
                        }
                    }


                    break;

                case MovementState.Stucked:
                    StuckedAnim();

                    break;

                case MovementState.WallDescending:
                    if (player.playerSettings.currentSurfaceState == SurfaceState.RightWall)
                    {
                        RightWallDescendingAnim();
                    }
                    else if (player.playerSettings.currentSurfaceState == SurfaceState.LeftWall)
                    {
                        LeftWallDescendingAnim();
                    }

                    break;
            }
        }

        private void IdleAnim()
        {
            hasPlayedDashParticles = false;

            animator.SetBool("Idle", true);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpNE", false);
            animator.SetBool("JumpNW", false);
            animator.SetBool("JumpE", false);
            animator.SetBool("JumpW", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", false);
            animator.SetBool("RWJumpH", false);
            animator.SetBool("RWJumpD", false);
            animator.SetBool("LWJumpA", false);
            animator.SetBool("LWJumpH", false);
            animator.SetBool("LWJumpD", false);
        }

        private void ChargingAnim()
        {
            Debug.Log("Invoke charging anim");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", true);
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpNE", false);
            animator.SetBool("JumpNW", false);
            animator.SetBool("JumpE", false);
            animator.SetBool("JumpW", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", false);
            animator.SetBool("RWJumpH", false);
            animator.SetBool("RWJumpD", false);
            animator.SetBool("LWJumpA", false);
            animator.SetBool("LWJumpH", false);
            animator.SetBool("LWJumpD", false);
        }

        private void HoverAnim()
        {

            hasPlayedJumpParticles = false; // Mark as played
            hasPlayedStuckParticles = false;

            Debug.Log("Invoke hover anim");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpNE", false);
            animator.SetBool("JumpNW", false);
            animator.SetBool("JumpE", false);
            animator.SetBool("JumpW", false);
            animator.SetBool("Hover", true);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", false);
            animator.SetBool("RWJumpH", false);
            animator.SetBool("RWJumpD", false);
            animator.SetBool("LWJumpA", false);
            animator.SetBool("LWJumpH", false);
            animator.SetBool("LWJumpD", false);
        }

        private void DescendingAnim()
        {


            hasPlayedJumpParticles = false; // Mark as played


            Debug.Log("Invoke descend anim");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpNE", false);
            animator.SetBool("JumpNW", false);
            animator.SetBool("JumpE", false);
            animator.SetBool("JumpW", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", true);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", false);
            animator.SetBool("RWJumpH", false);
            animator.SetBool("RWJumpD", false);
            animator.SetBool("LWJumpA", false);
            animator.SetBool("LWJumpH", false);
            animator.SetBool("LWJumpD", false);
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
            hasPlayedJumpParticles = false;

            if (!hasPlayedStuckParticles)
            {
                Debug.Log("Invoke jump straight anim");

                // Spawn and play a temporary particle system at current position
                ParticleSystem newParticles = Instantiate(stuckRParticlePrefab, transform.position, Quaternion.identity);
                newParticles.Play();

                float totalDuration = newParticles.main.duration + newParticles.main.startLifetime.constantMax;
                Destroy(newParticles.gameObject, totalDuration);

                hasPlayedStuckParticles = true; // Mark as played
            }

            Debug.Log("Invoke stucked anim");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpNE", false);
            animator.SetBool("JumpNW", false);
            animator.SetBool("JumpE", false);
            animator.SetBool("JumpW", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", true);

        }

        // Jump animations
        #region Jump Animations
        // -- DEFAULT JUMP
        private void JumpStraightAnim() // VERTICAL UP
        {
            Debug.Log("Invoke jump straight anim");

            if (!hasPlayedJumpParticles)
            {
                Debug.Log("Invoke jump straight anim");

                // Spawn and play a temporary particle system at current position
                ParticleSystem newParticles = Instantiate(jumpParticlePrefab, transform.position, Quaternion.identity);
                newParticles.Play();

                float totalDuration = newParticles.main.duration + newParticles.main.startLifetime.constantMax;
                Destroy(newParticles.gameObject, totalDuration);

                hasPlayedJumpParticles = true; // Mark as played
            }

            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", true);
            animator.SetBool("JumpNE", false);
            animator.SetBool("JumpNW", false);
            animator.SetBool("JumpE", false);
            animator.SetBool("JumpW", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", false);
            animator.SetBool("RWJumpH", false);
            animator.SetBool("RWJumpD", false);
            animator.SetBool("LWJumpA", false);
            animator.SetBool("LWJumpH", false);
            animator.SetBool("LWJumpD", false);
        }

        private void JumpDiagonalRightAnim()  // DIAGONAL RIGHT
        {

            Debug.Log("Invoke jump straight anim");

            if (!hasPlayedJumpParticles)
            {
                Debug.Log("Invoke jump straight anim");

                // Spawn and play a temporary particle system at current position
                ParticleSystem newParticles = Instantiate(jumpEParticlePrefab, transform.position, Quaternion.identity);
                newParticles.Play();

                float totalDuration = newParticles.main.duration + newParticles.main.startLifetime.constantMax;
                Destroy(newParticles.gameObject, totalDuration);

                hasPlayedJumpParticles = true; // Mark as played
            }


            Debug.Log("Invoke jump diagonal anim (right)");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpNE", true);
            animator.SetBool("JumpNW", false);
            animator.SetBool("JumpE", false);
            animator.SetBool("JumpW", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", false);
            animator.SetBool("RWJumpH", false);
            animator.SetBool("RWJumpD", false);
            animator.SetBool("LWJumpA", false);
            animator.SetBool("LWJumpH", false);
            animator.SetBool("LWJumpD", false);
        }

        private void JumpDiagonalLeftAnim()  // DIAGONAL LEFT
        {

            if (!hasPlayedJumpParticles)
            {
                Debug.Log("Invoke jump straight anim");

                // Spawn and play a temporary particle system at current position
                ParticleSystem newParticles = Instantiate(jumpWParticlePrefab, transform.position, Quaternion.identity);
                newParticles.Play();

                float totalDuration = newParticles.main.duration + newParticles.main.startLifetime.constantMax;
                Destroy(newParticles.gameObject, totalDuration);

                hasPlayedJumpParticles = true; // Mark as played
            }

            Debug.Log("Invoke jump diagonal anim (left)");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpNE", false);
            animator.SetBool("JumpNW", true);
            animator.SetBool("JumpE", false);
            animator.SetBool("JumpW", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", false);
            animator.SetBool("RWJumpH", false);
            animator.SetBool("RWJumpD", false);
            animator.SetBool("LWJumpA", false);
            animator.SetBool("LWJumpH", false);
            animator.SetBool("LWJumpD", false);
        }

        private void InvokeRightWallJumpAnim() // DONT TOUCH THIS METHOD
        {
            if (player.moveContrl.movementSystem.isWallJumpHorizontal) { RightWallJumpHorizontal(); }
            else if (player.moveContrl.movementSystem.isWallJumpAscend) { RightWallJumpAscendAnim(); }
            else { RightWallJumpDescendAnim(); }
        }

        private void InvokeLeftWallJumpAnim() // DONT TOUCH THIS METHOD
        {
            if (player.moveContrl.movementSystem.isWallJumpHorizontal) { LeftWallJumpHorizontal(); }
            else if (player.moveContrl.movementSystem.isWallJumpAscend) { LeftWallJumpAscendAnim(); }
            else { LeftWallJumpDescendAnim(); }
        }

        // -- WALL JUMP
        // Right wall
        private void RightWallJumpAscendAnim() // RIGHT WALL JUMP ASCEND
        {
            if (!hasPlayedJumpParticles)
            {
                Debug.Log("Invoke jump straight anim");

                // Spawn and play a temporary particle system at current position
                ParticleSystem newParticles = Instantiate(jumpEParticlePrefab, transform.position, Quaternion.identity);
                newParticles.Play();

                float totalDuration = newParticles.main.duration + newParticles.main.startLifetime.constantMax;
                Destroy(newParticles.gameObject, totalDuration);

                hasPlayedJumpParticles = true; // Mark as played
            }


            Debug.Log("Invoke right wall jump ascend anim");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", true);
            animator.SetBool("RWJumpH", false);
            animator.SetBool("RWJumpD", false);
            animator.SetBool("LWJumpA", false);
            animator.SetBool("LWJumpH", false);
            animator.SetBool("LWJumpD", false);
        }

        private void RightWallJumpDescendAnim() // RIGHT WALL JUMP DESCEND
        {

            if (!hasPlayedJumpParticles)
            {
                Debug.Log("Invoke jump straight anim");

                // Spawn and play a temporary particle system at current position
                ParticleSystem newParticles = Instantiate(jumpEParticlePrefab, transform.position, Quaternion.identity);
                newParticles.Play();

                float totalDuration = newParticles.main.duration + newParticles.main.startLifetime.constantMax;
                Destroy(newParticles.gameObject, totalDuration);

                hasPlayedJumpParticles = true; // Mark as played
            }

            Debug.Log("Invoke right wall jump descend anim");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", false);
            animator.SetBool("RWJumpH", true);
            animator.SetBool("RWJumpD", false);
            animator.SetBool("LWJumpA", false);
            animator.SetBool("LWJumpH", false);
            animator.SetBool("LWJumpD", false);


        }

        private void RightWallJumpHorizontal() // RIGHT WALL JUMP HORIZONTAL
        {

            if (!hasPlayedJumpParticles)
            {
                Debug.Log("Invoke jump straight anim");

                // Spawn and play a temporary particle system at current position
                ParticleSystem newParticles = Instantiate(jumpEParticlePrefab, transform.position, Quaternion.identity);
                newParticles.Play();

                float totalDuration = newParticles.main.duration + newParticles.main.startLifetime.constantMax;
                Destroy(newParticles.gameObject, totalDuration);

                hasPlayedJumpParticles = true; // Mark as played
            }

            Debug.Log("Invoke wall jump horizontal <--");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", false);
            animator.SetBool("RWJumpH", false);
            animator.SetBool("RWJumpD", true);

        }

        // Left wall
        private void LeftWallJumpAscendAnim() // LEFT WALL JUMP ASCEND
        {
            if (!hasPlayedJumpParticles)
            {
                Debug.Log("Invoke jump straight anim");

                // Spawn and play a temporary particle system at current position
                ParticleSystem newParticles = Instantiate(jumpWParticlePrefab, transform.position, Quaternion.identity);
                newParticles.Play();

                float totalDuration = newParticles.main.duration + newParticles.main.startLifetime.constantMax;
                Destroy(newParticles.gameObject, totalDuration);

                hasPlayedJumpParticles = true; // Mark as played
            }

            Debug.Log("Invoke left wall jump ascend anim");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpNE", false);
            animator.SetBool("JumpNW", false);
            animator.SetBool("JumpE", false);
            animator.SetBool("JumpW", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", false);
            animator.SetBool("RWJumpH", false);
            animator.SetBool("RWJumpD", false);
            animator.SetBool("LWJumpA", true);
            animator.SetBool("LWJumpH", false);
            animator.SetBool("LWJumpD", false);
        }

        private void LeftWallJumpDescendAnim() // LEFT WASLL JUMP DESCEND
        {
            Debug.Log("Invoke left wall jump descend anim");
            if (!hasPlayedJumpParticles)
            {
                Debug.Log("Invoke jump straight anim");

                // Spawn and play a temporary particle system at current position
                ParticleSystem newParticles = Instantiate(jumpWParticlePrefab, transform.position, Quaternion.identity);
                newParticles.Play();

                float totalDuration = newParticles.main.duration + newParticles.main.startLifetime.constantMax;
                Destroy(newParticles.gameObject, totalDuration);

                hasPlayedJumpParticles = true; // Mark as played
            }


            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpNE", false);
            animator.SetBool("JumpNW", false);
            animator.SetBool("JumpE", false);
            animator.SetBool("JumpW", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", false);
            animator.SetBool("RWJumpH", false);
            animator.SetBool("RWJumpD", false);
            animator.SetBool("LWJumpA", false);
            animator.SetBool("LWJumpH", false);
            animator.SetBool("LWJumpD", true);
        }

        private void LeftWallJumpHorizontal() // LEFT WALL JUMP HORIZONTAL
        {
            if (!hasPlayedJumpParticles)
            {
                Debug.Log("Invoke jump straight anim");

                // Spawn and play a temporary particle system at current position
                ParticleSystem newParticles = Instantiate(jumpWParticlePrefab, transform.position, Quaternion.identity);
                newParticles.Play();

                float totalDuration = newParticles.main.duration + newParticles.main.startLifetime.constantMax;
                Destroy(newParticles.gameObject, totalDuration);

                hasPlayedJumpParticles = true; // Mark as played
            }

            Debug.Log("Invoke left wall jump horizontal -->");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpNE", false);
            animator.SetBool("JumpNW", false);
            animator.SetBool("JumpE", false);
            animator.SetBool("JumpW", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", false);
            animator.SetBool("StuckedR", false);
            animator.SetBool("RWJumpA", false);
            animator.SetBool("RWJumpH", false);
            animator.SetBool("RWJumpD", false);
            animator.SetBool("LWJumpA", true);
            animator.SetBool("LWJumpH", false);
            animator.SetBool("LWJumpD", false);
        }

        #endregion Jump Animations

        #region Dash Animations
        // -- GROUND DASH
        private void GroundDashAnim() // DONT TOUCH THIS METHOD
        {
            if (player.moveContrl.movementSystem.isRightGroundDash) { RightGroundDashAnim(); }
            else { LeftGroundDashAnim(); }
            Debug.Log("Invoke ground dash anim");
        }

        private void RightGroundDashAnim() // RIGHT ->> 
        {
            if (!hasPlayedDashParticles)
            {
                Debug.Log("Invoke dash particles");

                // Spawn and play a temporary particle system at current position
                ParticleSystem newParticles = Instantiate(dashRParticlePrefab, transform.position, Quaternion.identity);
                newParticles.Play();

                float totalDuration = newParticles.main.duration + newParticles.main.startLifetime.constantMax;
                Destroy(newParticles.gameObject, totalDuration);

                hasPlayedDashParticles = true; // Mark as played
            }
            Debug.Log("Ground dash anim ->");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", true);
            animator.SetBool("DashL", false);
            animator.SetBool("Wallstick", false);
        }

        private void LeftGroundDashAnim() // LEFT <<-
        {

            if (!hasPlayedDashParticles)
            {
                Debug.Log("Invoke dash particles");

                // Spawn and play a temporary particle system at current position
                ParticleSystem newParticles = Instantiate(dashLParticlePrefab, transform.position, Quaternion.identity);
                newParticles.Play();

                float totalDuration = newParticles.main.duration + newParticles.main.startLifetime.constantMax;
                Destroy(newParticles.gameObject, totalDuration);

                hasPlayedDashParticles = true; // Mark as played
            }
            Debug.Log("Ground dash anim <-");
            animator.SetBool("Idle", false);
            animator.SetBool("Charge", false);
            animator.SetBool("JumpUp", false);
            animator.SetBool("Hover", false);
            animator.SetBool("Descent", false);
            animator.SetBool("QuickDescent", false);
            animator.SetBool("Landing", false);
            animator.SetBool("DashR", false);
            animator.SetBool("DashL", true);
            animator.SetBool("Wallstick", false);
        }

        // -- CEILING DASH
        private void CeilingDashAnim() // DONT TOUCH THIS METHOD
        {
            if (player.moveContrl.movementSystem.isRightGroundDash) { RightCeilingDashAnim(); }
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
            if (player.moveContrl.movementSystem.isUpWallDash) { UpRightWallDashAnim(); }
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
            if (player.moveContrl.movementSystem.isUpWallDash) { UpLeftWallDashAnim(); }
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
}
