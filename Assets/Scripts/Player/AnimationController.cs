using UnityEngine;

namespace Assets.Scripts.Player
{
    public class AnimationController
    {
        private readonly Player player;
        private readonly AnimationSettings animSettings;
        private readonly Animator animator;

        private bool hasPlayedJumpParticles = false;
        private bool hasPlayedDashParticles = false;
        private bool hasPlayedStuckParticles = false;

        private MovementState _prevMovementState;
        private SurfaceState _prevSurfaceState;

        public AnimationController(Player player, AnimationSettings animSettings, Animator animator)
        {
            this.player = player;
            this.animSettings = animSettings;
            this.animator = animator;

            _prevMovementState = player.playerSettings.movementState;
            _prevSurfaceState = player.playerSettings.currentSurfaceState;

            Transform particlesParent = player.transform.Find("Particles");
            // Initialize particle prefabs if not assigned
            if (animSettings.jumpParticlePrefab == null) animSettings.jumpParticlePrefab = particlesParent.Find(animSettings.jumpParticlePrefabName).GetComponent<ParticleSystem>();
            if (animSettings.jumpEParticlePrefab == null) animSettings.jumpEParticlePrefab = particlesParent.Find(animSettings.jumpEParticlePrefabName).GetComponent<ParticleSystem>();
            if (animSettings.jumpWParticlePrefab == null) animSettings.jumpWParticlePrefab = particlesParent.Find(animSettings.jumpWParticlePrefabName).GetComponent<ParticleSystem>();
            if (animSettings.dashRParticlePrefab == null) animSettings.dashRParticlePrefab = particlesParent.Find(animSettings.dashRParticlePrefabName).GetComponent<ParticleSystem>();
            if (animSettings.dashLParticlePrefab == null) animSettings.dashLParticlePrefab = particlesParent.Find(animSettings.dashLParticlePrefabName).GetComponent<ParticleSystem>();
            if (animSettings.stuckLParticlePrefab == null) animSettings.stuckLParticlePrefab = particlesParent.Find(animSettings.stuckLParticlePrefabName).GetComponent<ParticleSystem>();
            if (animSettings.stuckRParticlePrefab == null) animSettings.stuckRParticlePrefab = particlesParent.Find(animSettings.stuckRParticlePrefabName).GetComponent<ParticleSystem>();
        }

        public void Update()
        {
            var settings = player.playerSettings;
            var currState = settings.movementState;
            var currSurface = settings.currentSurfaceState;

            if (currState != _prevMovementState || player.playerSettings.currentSurfaceState != _prevSurfaceState)
            {
                HandleStateEntry(currState, currSurface);
                _prevMovementState = currState;
                _prevSurfaceState = currSurface;
            }
        }

        private void HandleStateEntry(MovementState ms, SurfaceState ss)
        {
            // Landing detection: Descending -> Idle on Ground
            if (_prevMovementState == MovementState.Descending
                && ms == MovementState.Idle
                && ss == SurfaceState.Ground)
            {
                OnLandingAnim();
                return;
            }

            switch (ms)
            {
                case MovementState.Idle:
                    if (ss == SurfaceState.Ground)
                    {
                        IdleAnim();
                        Debug.Log("Played idle pose");
                    }
                    break;

                case MovementState.Charging:
                    ChargingAnim();
                    Debug.Log("Played charging pose");
                    break;

                case MovementState.Jumping:
                    if (player.moveContrl.movementSystem.isStraightJump) JumpStraightAnim();
                    else if (player.moveContrl.movementSystem.isDiagonalJumpRight) JumpDiagonalRightAnim();
                    else JumpDiagonalLeftAnim();
                    Debug.Log("Played jump pose");
                    break;

                case MovementState.Dashing:
                    switch (ss)
                    {
                        case SurfaceState.Ground: GroundDashAnim(); break;
                        case SurfaceState.Ceiling: CeilingDashAnim(); break;
                        case SurfaceState.RightWall: RightWallDashAnim(); break;
                        case SurfaceState.LeftWall: LeftWallDashAnim(); break;
                    }
                    Debug.Log("Played dash pose");
                    break;

                case MovementState.WallJump:
                    if (ss != SurfaceState.Ground && ss != SurfaceState.Ceiling)
                    {
                        if (player.moveContrl.movementSystem.isWallJumpRight) InvokeRightWallJumpAnim();
                        else InvokeLeftWallJumpAnim();
                        Debug.Log("Played wall-jump pose");
                    }
                    break;

                case MovementState.Hovering:
                    HoverAnim();
                    Debug.Log("Played hover pose");
                    break;

                case MovementState.Descending:
                    if (ss == SurfaceState.Ground)
                    {
                        // will be caught by landing logic above when transitioning to Idle
                    }
                    else if (ss == SurfaceState.Air)
                    {
                        DescendingAnim();
                        Debug.Log("Played descending pose");
                    }
                    else if (ss == SurfaceState.RightWall)
                    {
                        RightWallDescendingAnim();
                        Debug.Log("Played wall-descending pose (right)");
                    }
                    else if (ss == SurfaceState.LeftWall)
                    {
                        LeftWallDescendingAnim();
                        Debug.Log("Played wall-descending pose (left)");
                    }
                    break;

                case MovementState.AirDashing:
                    HandleAirDash(player.moveContrl.movementSystem);
                    Debug.Log("Played air-dash pose");
                    break;

                case MovementState.Stucked:
                    StuckedAnim();
                    Debug.Log("Played stucked pose");
                    break;
            }
        }

        private void HandleAirDash(MovementSystem msys)
        {
            if (msys.isVerticalAirDash)
            {
                if (msys.isAirDashAscend) AirDashVerticalAscend();
                else AirDashVerticalDescend();
            }
            else if (msys.isHorizontalAirDash)
            {
                if (msys.isRightAirDash) AirDashHorizontalRight();
                else AirDashHorizontalLeft();
            }
            else if (msys.isRightDiagonalAirDash)
            {
                if (msys.isAirDashAscend) AirDashDiagonalRightUp();
                else AirDashDiagonalRightDown();
            }
            else
            {
                if (msys.isAirDashAscend) AirDashDiagonalLeftUp();
                else AirDashDiagonalLeftDown();
            }
        }

        private void OnLandingAnim()
        {
            Debug.Log("Invoke landing anim");
            // Clear all and set landing bool

            if (InputManager.ActionInputDetected())
            {
                ChargingAnim();
                Debug.Log("Invoked charging anim on landing");
            }
            else
            {
                IdleAnim();
                animator.SetBool("Landing", true);
                Debug.Log("No input detected, transition to Idle anim");
            } 

        }

        // --- Core pose methods (reset & set one flag) ---
        private void IdleAnim()
        {
            hasPlayedDashParticles = false;
            SetAllFalse();
            animator.SetBool("Idle", true);
        }

        private void ChargingAnim()
        {
            SetAllFalse();
            animator.SetBool("Charge", true);
        }

        private void HoverAnim()
        {
            hasPlayedJumpParticles = false;
            hasPlayedStuckParticles = false;
            SetAllFalse();
            animator.SetBool("Hover", true);
        }

        private void DescendingAnim()
        {
            hasPlayedJumpParticles = false;
            SetAllFalse();
            animator.SetBool("Descent", true);
        }

        // Resets all relevant bools before setting one
        private void SetAllFalse()
        {
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
            animator.SetBool("LWJumpD", false);
        }

        // --- Jump Animations (particles + flags) ---
        private void JumpStraightAnim()
        {
            PlayJumpParticles(animSettings.jumpParticlePrefab);
            SetAllFalse();
            animator.SetBool("JumpUp", true);
        }

        private void JumpDiagonalRightAnim()
        {
            PlayJumpParticles(animSettings.jumpEParticlePrefab);
            SetAllFalse();
            animator.SetBool("JumpNE", true);
        }

        private void JumpDiagonalLeftAnim()
        {
            PlayJumpParticles(animSettings.jumpWParticlePrefab);
            SetAllFalse();
            animator.SetBool("JumpNW", true);
        }

        private void InvokeRightWallJumpAnim()
        {
            if (player.moveContrl.movementSystem.isWallJumpHorizontal)
                RightWallJumpHorizontal();
            else if (player.moveContrl.movementSystem.isWallJumpAscend) RightWallJumpAscendAnim();
            else RightWallJumpDescendAnim();
        }

        private void InvokeLeftWallJumpAnim()
        {
            if (player.moveContrl.movementSystem.isWallJumpHorizontal)
                LeftWallJumpHorizontal();
            else if (player.moveContrl.movementSystem.isWallJumpAscend) LeftWallJumpAscendAnim();
            else LeftWallJumpDescendAnim();
        }

        private void RightWallJumpAscendAnim()
        {
            PlayJumpParticles(animSettings.jumpEParticlePrefab);
            SetAllFalse();
            animator.SetBool("RWJumpA", true);
        }

        private void RightWallJumpDescendAnim()
        {
            PlayJumpParticles(animSettings.jumpEParticlePrefab);
            SetAllFalse();
            animator.SetBool("RWJumpH", true);
        }

        private void RightWallJumpHorizontal()
        {
            PlayJumpParticles(animSettings.jumpEParticlePrefab);
            SetAllFalse();
            animator.SetBool("RWJumpD", true);
        }

        private void LeftWallJumpAscendAnim()
        {
            PlayJumpParticles(animSettings.jumpWParticlePrefab);
            SetAllFalse();
            animator.SetBool("LWJumpA", true);
        }

        private void LeftWallJumpDescendAnim()
        {
            PlayJumpParticles(animSettings.jumpWParticlePrefab);
            SetAllFalse();
            animator.SetBool("LWJumpD", true);
        }

        private void LeftWallJumpHorizontal()
        {
            PlayJumpParticles(animSettings.jumpWParticlePrefab);
            SetAllFalse();
            animator.SetBool("LWJumpA", true);
        }

        // --- Dash Animations ---
        private void GroundDashAnim()
        {
            if (player.moveContrl.movementSystem.isRightGroundDash)
                RightGroundDashAnim();
            else LeftGroundDashAnim();
        }
        private void RightGroundDashAnim()
        {
            PlayDashParticles(animSettings.dashRParticlePrefab);
            SetAllFalse();
            animator.SetBool("DashR", true);
        }

        private void LeftGroundDashAnim()
        {
            PlayDashParticles(animSettings.dashLParticlePrefab);
            SetAllFalse();
            animator.SetBool("DashL", true);
        }

        private void CeilingDashAnim()
        {
            if (player.moveContrl.movementSystem.isRightGroundDash)
                RightCeilingDashAnim();
            else LeftCeilingDashAnim();
        }

        private void RightCeilingDashAnim()
        {
            SetAllFalse();
            animator.SetBool("DashR", true);
        }

        private void LeftCeilingDashAnim()
        {
            SetAllFalse();
            animator.SetBool("DashL", true);
        }

        private void RightWallDashAnim()
        {
            if (player.moveContrl.movementSystem.isUpWallDash)
                UpRightWallDashAnim();
            else DownRightWallDashAnim();
        }

        private void UpRightWallDashAnim()
        {
            Debug.Log("Invoke upward dash on the right wall");
            SetAllFalse();
        }

        private void DownRightWallDashAnim()
        {
            Debug.Log("Invoke downward dash on the right wall");
            SetAllFalse();
        }

        private void LeftWallDashAnim()
        {
            if (player.moveContrl.movementSystem.isUpWallDash)
                UpLeftWallDashAnim();
            else DownLeftWallDashAnim();
        }

        private void UpLeftWallDashAnim()
        {
            Debug.Log("Invoke upward dash on the left wall");
            SetAllFalse();
        }

        private void DownLeftWallDashAnim()
        {
            Debug.Log("Invoke downward dash on the left wall");
            SetAllFalse();
        }

        // --- Air Dash Animations ---
        private void AirDashVerticalAscend() 
        { 
            Debug.Log("Invoke air dash straight up anim"); SetAllFalse(); }
        
        private void AirDashVerticalDescend() 
        { 
            Debug.Log("Invoke air dash straight down anim"); SetAllFalse(); 
        }
        private void AirDashHorizontalRight() 
        { 
            Debug.Log("Invoke air dash right anim (horizontal)"); SetAllFalse(); 
        }
        private void AirDashHorizontalLeft() 
        { 
            Debug.Log("Invoke air dash left anim (horizontal)"); SetAllFalse(); 
        }
        private void AirDashDiagonalRightUp() 
        { 
            Debug.Log("Invoke air dash up diagonally anim (right)"); SetAllFalse(); 
        }
        private void AirDashDiagonalRightDown() 
        { 
            Debug.Log("Invoke air dash down diagonally anim (right)"); SetAllFalse(); }
        
        private void AirDashDiagonalLeftUp() 
        { 
            Debug.Log("Invoke air dash up diagonally anim (left)"); SetAllFalse(); 
        }
        private void AirDashDiagonalLeftDown() 
        { 
            Debug.Log("Invoke air dash down diagonally anim (left)"); SetAllFalse(); 
        }

        // --- Stuck Animation ---
        private void StuckedAnim()
        {
            hasPlayedJumpParticles = false;
            if (!hasPlayedStuckParticles)
            {
                PlayStuckParticles(animSettings.stuckRParticlePrefab);
                hasPlayedStuckParticles = true;
            }
            SetAllFalse();
            animator.SetBool("StuckedR", true);
        }

        // --- Utility for particles ---
        private void PlayJumpParticles(ParticleSystem prefab)
        {
            if (hasPlayedJumpParticles) return;
            var p = Object.Instantiate(prefab, player.transform.position, Quaternion.identity) as ParticleSystem;
            p.Play();
            Object.Destroy(p.gameObject, p.main.duration + p.main.startLifetime.constantMax);
            hasPlayedJumpParticles = true;
        }
        private void PlayDashParticles(ParticleSystem prefab)
        {
            if (hasPlayedDashParticles) return;
            var p = Object.Instantiate(prefab, player.transform.position, Quaternion.identity) as ParticleSystem;
            p.Play();
            Object.Destroy(p.gameObject, p.main.duration + p.main.startLifetime.constantMax);
            hasPlayedDashParticles = true;
        }
        private void PlayStuckParticles(ParticleSystem prefab)
        {
            var p = Object.Instantiate(prefab, player.transform.position, Quaternion.identity) as ParticleSystem;
            p.Play();
            Object.Destroy(p.gameObject, p.main.duration + p.main.startLifetime.constantMax);
        }

        private void RightWallDescendingAnim()
        {
            Debug.Log("Invoke wall descend anim (right wall)");
        }

        private void LeftWallDescendingAnim()
        {
            Debug.Log("Invoke wall descend anim (left wall)");
        }
    }
}