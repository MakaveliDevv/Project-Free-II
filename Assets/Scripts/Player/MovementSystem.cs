using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

namespace Assets.Scripts.Player
{
    public class MovementSystem
    {
        // ─────────────────────────────────────────────────────────────────────────
        // PUBLIC VARIABLES
        // ─────────────────────────────────────────────────────────────────────────

        // ─ Movement Flags
        // ground jump
        [HideInInspector] public bool isStraightJump = false;
        [HideInInspector] public bool isDiagonalJumpRight = false;

        // Air dash
        [HideInInspector] public bool isRightGroundDash = false;
        [HideInInspector] public bool isUpWallDash = false;

        // To right
        [HideInInspector] public bool isVerticalAirDash = false;
        [HideInInspector] public bool isHorizontalAirDash = false;

        [HideInInspector] public bool isRightAirDash = false;

        [HideInInspector] public bool isRightDiagonalAirDash = false;
        [HideInInspector] public bool isAirDashAscend = false;


        // wall jump
        [HideInInspector] public bool isWallJumpRight = false;
        [HideInInspector] public bool isWallJumpAscend = false;
        [HideInInspector] public bool isWallJumpHorizontal = false;

        // ─ Movement Calculations
        [HideInInspector] public Vector2 snappedDir = Vector2.zero;
        [HideInInspector] public Rigidbody rb;

        // ─ Surface Memory
        [HideInInspector] public GameObject lastSurfaceObject;

        // ─────────────────────────────────────────────────────────────────────────
        // PRIVATE VARIABLES
        // ─────────────────────────────────────────────────────────────────────────

        // ─ Class References
        public readonly MovementSettings settings;
        private readonly Player player;

        // ─ Movement Flags
        private bool isJumping = false;
        private bool isWallJumping = false;
        private bool isDashing = false;
        private bool isAirDashing = false;
        private bool allowedToMove = false;
        private bool isInAir = false;
        private bool fastFalling = false;
        private bool isDropping = false;
        private bool hasBurstDropped = false;
        private bool prevStickDownDrop = false;
        // private bool stateChanged = false;
        [HideInInspector] public bool actionInProgress = false;
        private bool hasTriggeredHover = false;
        [HideInInspector] public bool hasAppliedForce = false;
        private bool hasReachedTarget = false;
        private bool isMoving = false;
        private bool isStuckFrozen = false;
        private bool isLandingBuffered = false;
        public bool hasBounced = false;
        private bool buttonPressedLongEnough = false;

        // ─ Timers & Counters
        // private float stateTimer = 0f;
        private float lastContactTime;
        private float surfaceContactTime;
        private float stuckTimer = 0f;
        private float hoverTimer = 0f;
        private float hoverWobbleTimer = 0f;
        private float buttonHoldTimer = 0f;

        // ─ Input Tracking & Actions
        private string currentAction = "";
        private string fetchedAction = "";
        private bool actionReady = true;

        // ─ Physics & Gravity State
        private float initialGravityStrength = 0;
        private float wallDescendingGravityStrength = 0f;
        private float initialBounciness = 0;

        private Vector3 ConvertToVector(GravityDirection dir)
        {
            return dir switch
            {
                GravityDirection.Down => Vector3.down,
                GravityDirection.Up => Vector3.up,
                GravityDirection.Left => Vector3.left,
                GravityDirection.Right => Vector3.right,
                _ => Vector3.down
            };
        }

        // ─ Movement Calculations
        private float targetDistance = 0f;
        private float forceMagnitude = 0f;
        private readonly float TravelEpsilon = .1f;

        public readonly Dictionary<SurfaceState, string[]> allowedMoveLabels = new()
        {
            { SurfaceState.Ground, new[] { "W", "WNW", "NW", "NNW", "N", "NNE", "NE", "ENE", "E" } },
            { SurfaceState.Ceiling, new[] { "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W" } },
            { SurfaceState.LeftWall, new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S" } },
            { SurfaceState.RightWall, new[] { "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW", "N" } },
            { SurfaceState.Air, new[] {
                "E", "ENE", "NE", "NNE", "N", "NNW", "NW", "WNW",
                "W", "WSW", "SW", "SSW", "S", "SSE", "SE", "ESE" }
            }
        };


        private Vector3 jumpStartPos = Vector3.zero;
        private Vector3 dashStartPos = Vector3.zero;
        private Vector3 airDashStartPos = Vector3.zero;

        // ─ References
        private Collider col;

        private Vector3 startPos;
        private Vector3 originalHoverPosition;
        private Vector3 predictedTargetPoint;

        // ─ Surface Memory
        private float lastSurfaceCheckTime;
        private const float surfaceMemoryDuration = 0.2f;

        // ─ Label Mapping
        public Dictionary<string, float> labelToAngle;

        // ─ Computed Properties
        private float HoldRatio => Mathf.Clamp01(buttonHoldTimer / settings.maxHoldTime);

        // ─ Constants
        private const float NO_CONTACT_THRESHOLD = 0.2f;

        // ─ Miscellaneous
        private SurfaceState lastWallSide;
        private Coroutine landingResetCoroutine;
        private Collider[] colliders = new Collider[10]; 
        private const int maxBufferSize = 1000;
        private Coroutine freezeCoroutine = null;
        private bool isJumpAllowed;

        public MovementSystem(Player player, MovementSettings settings, InputActionAsset inputActionAsset)
        {
            this.player = player;
            this.settings = settings;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // UNITY LIFECYCLE METHODS
        // ─────────────────────────────────────────────────────────────────────────
        #region UNITY LIFECYCLE
        public void Awake()
        {
            rb = player.GetComponent<Rigidbody>();
            col = player.GetComponent<Collider>();

            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            BuildLabelToAngleMap();
        }

        public void Start()
        {
            initialGravityStrength = settings.gravityStrength;
            initialBounciness = col.material.bounciness;
        }

        public void OnValidate()
        {
            wallDescendingGravityStrength = settings.gravityStrength * (settings.wallGravityPercent * 0.1f);
        }

        private IEnumerator FallDelay()
        {
            settings.movementState = MovementState.NOTHING;

            yield return new WaitForSeconds(settings.fallTimer);

            ResetActionState();
            ResetPhysicsSettings(false, true);

            // Debug.Log("Timer finished, starting descend...");

            yield break;
        }

        public void Update()
        {
            // InputManager.UpdateInput();
            Vector2 stick = InputManager.LeftStickInput;

            // Debug.Log($"Jump Pressed: {InputManager.SouthButtonPressed} | Released: {InputManager.SouthButtonReleased}");

            // if (InputManager.SouthButtonPressed)
            //     Debug.Log($"  [HoldTimer] = {buttonHoldTimer:F2} / {settings.minButtonPressTime}");

            FetchActionType();
            // Debug.Log($"  [Fetch] curSurface = {settings.currentSurfaceState} | isInAir = {isInAir} | dirLabel = {GetClosestDirectionLabel(snappedDir)} | isJumpAllowed = {isJumpAllowed}");


            bool stickMoving = InputManager.HasStickMovement();
            bool jumpHeld = InputManager.SouthButtonPressed;

            if (stickMoving)
            {
                snappedDir = GetSnappedDirection(stick).normalized;
            }
            else
            {
                snappedDir = Vector2.zero;
            }

            // Determine downward stick angle
            bool canDrop = settings.currentSurfaceState == SurfaceState.Air || settings.movementState == MovementState.Stucked;
            bool rawDown = false;

            if (stickMoving)
            {
                float rawAngle = Mathf.Atan2(stick.y, stick.x) * Mathf.Rad2Deg;
                rawAngle = (rawAngle + 360f) % 360f;
                float angleDiff = Mathf.DeltaAngle(rawAngle, 270f); // 270° is straight down
                rawDown = Mathf.Abs(angleDiff) <= settings.dropAngleTolerance;
            }

            isDropping = Gamepad.current != null &&
                        Gamepad.current.buttonEast.isPressed &&
                        rawDown &&
                        canDrop &&
                        settings.movementState != MovementState.Idle &&
                        settings.movementState != MovementState.Charging &&
                        !ActionInputDetected();

            if (jumpHeld && buttonHoldTimer >= settings.minButtonPressTime && !buttonPressedLongEnough)
            {
                buttonPressedLongEnough = true;
                // Debug.Log("✓ Button held long enough → charge initiated");

                if (settings.movementState != MovementState.Charging && stickMoving && allowedToMove)
                {
                    settings.movementState = MovementState.Charging;
                }
            }

            if (settings.movementState == MovementState.Jumping || settings.movementState == MovementState.WallJump || settings.movementState == MovementState.Dashing || settings.movementState == MovementState.AirDashing)
            {
                CheckArrivalAtTarget();
            }

            if (!settings.useAutoHover && (settings.movementState == MovementState.Jumping || settings.movementState == MovementState.WallJump || settings.movementState == MovementState.AirDashing))
            {
                if (hasReachedTarget)
                {
                    rb.linearDamping = 0;
                    player.StartCoroutine(FallDelay());
                    settings.movementState = MovementState.Descending;
                }
            }
            else if (settings.movementState == MovementState.Dashing)
            {
                float traveled = Vector3.Distance(rb.position, dashStartPos);

                if (hasReachedTarget || (traveled > TravelEpsilon && rb.linearVelocity.magnitude <= 0.01f))
                {
                    ResetActionState();
                    ResetPhysicsSettings(true, true);
                    settings.movementState = MovementState.Idle;
                }
            }

            if (isInAir) settings.currentSurfaceState = SurfaceState.Air;

            if (jumpHeld)
            {
                buttonHoldTimer += Time.deltaTime;
                if (actionReady && !actionInProgress && buttonHoldTimer >= settings.maxHoldTime && buttonPressedLongEnough)
                {
                    if (settings.currentSurfaceState == SurfaceState.LeftWall || settings.currentSurfaceState == SurfaceState.RightWall)
                    {
                        fetchedAction = "WallJump";
                        allowedToMove = true;
                    }
                    PerformMovementAction();
                    actionReady = false;
                }
            }

            if (InputManager.SouthButtonReleased)
            {
                buttonPressedLongEnough = false;

                if (snappedDir != Vector2.zero && settings.movementState == MovementState.Charging)
                {
                    PerformMovementAction();
                }
                else
                {
                    ResetActionState();
                }

                actionReady = true;
            }

            if (settings.movementState == MovementState.Idle || settings.movementState == MovementState.Dashing || settings.movementState == MovementState.WallDescending)
                col.material.bounciness = initialBounciness;
            else
                col.material.bounciness = 0;

            switch (settings.movementState)
            {
                case MovementState.Idle:
                    settings.currentSurfaceState = SurfaceState.Ground;
                    hasTriggeredHover = false;
                    break;
                case MovementState.Descending:
                    hasReachedTarget = false;
                    actionInProgress = false;
                    break;
                case MovementState.WallDescending:
                    actionInProgress = false;
                    settings.gravityStrength = wallDescendingGravityStrength;
                    break;
                case MovementState.Hovering:
                    isJumping = false;
                    break;
                case MovementState.Stucked:
                    actionInProgress = false;
                    break;
            }

            // InputManager.ResetFrameInputs();
        }

        public void LateUpdate()
        {
            if (settings.enableZLock)
            {
                Vector3 pos = rb.position;
                pos.z = 0;
                rb.position = pos;
            }
        }

        public void FixedUpdate()
        {
            // if (settings.movementState == MovementState.Stucked)
            // {
            //     FreezePlayer();
            //     return;
            // }

            if (settings.movementState != MovementState.Hovering) ApplyCustomGravity();
            GetLastCollidedSurface();

            // isInAir = !IsCollidingWithSurface();
            isInAir = !CheckSurfaces();

            if (settings.useHandleActionForces) { HandleActionForces(); }
            // Debug.Log($"isDropping = {isDropping}, prevStickDownDrop = {prevStickDownDrop}, hasBurstDropped = {hasBurstDropped}");
            if (isDropping && !prevStickDownDrop && !hasBurstDropped) { ApplyBurstDropForce(); }
            prevStickDownDrop = isDropping;

            SmoothMovement();

            isMoving = rb.linearVelocity.sqrMagnitude > settings.isMovingThreshold;
            // Debug.Log($"IsMoving = {isMoving}");

            if ((settings.movementState == MovementState.Jumping ||
                settings.movementState == MovementState.WallJump ||
                settings.movementState == MovementState.AirDashing) &&
                (!isDropping || !fastFalling) &&
                settings.currentSurfaceState == SurfaceState.Air) { TryStartHoverEffect(); }
            else if (settings.movementState == MovementState.Hovering && (!isDropping || !fastFalling)) { WobbleEffect(); }

            ForceIdleState();
        }

        public void OnCollisionEnter(Collision collision)
        {
            HandleSurfaceState(collision, out _);
            // if (player.mode != Mode.AdvancedMovement) { TryStickToWall(collision); }
            if (player.mode != Mode.AdvancedMovement) { StopMovementUponCollision(); }

            player.Invoke(nameof(ResetActionState), .1f);

            if (settings.movementState == MovementState.WallDescending &&
                settings.currentSurfaceState == SurfaceState.Ground && !hasBounced) { OnWallDescendingBounce(); }

            if (settings.currentSurfaceState == SurfaceState.Ground)
            {
                var contact = collision.contacts[0];
                SnapToGround(contact.point);
                surfaceContactTime = Time.time;
                if (landingResetCoroutine != null) { player.StopCoroutine(landingResetCoroutine); }
                landingResetCoroutine = player.StartCoroutine(DelayedLandingReset());
            }
        }

        public void OnCollisionExit(Collision collision)
        {
            if (landingResetCoroutine != null)
            {
                player.StopCoroutine(landingResetCoroutine);
                landingResetCoroutine = null;
            }

            lastContactTime = Time.time;
        }
        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        // ACTION DETECTION & HANDLING
        // ─────────────────────────────────────────────────────────────────────────
        #region ACTION DETECTION & HANDLING
        /// <summary>
        /// Determines if the stick movement and button hold satisfy the criteria for an action input.
        /// Used by Update to transition into the Charging state.
        /// </summary>
        private bool ActionInputDetected()
        {
            if (InputManager.HasStickMovement() && InputManager.SouthButtonPressed && buttonHoldTimer >= settings.minButtonPressTime) { return true; }

            buttonHoldTimer = 0;
            // InputManager.SouthButtonPressed = false;
            return false;
        }

        private void FetchActionType()
        {
            allowedToMove = false;
            fetchedAction = "";

            string dirLabel = GetClosestDirectionLabel(snappedDir);

            isJumpAllowed = settings.currentSurfaceState != SurfaceState.LeftWall
                && settings.currentSurfaceState != SurfaceState.RightWall
                && IsJumpDirectionAllowed(dirLabel);

            bool isWallJumpAllowed = (settings.movementState == MovementState.Stucked || settings.movementState == MovementState.WallDescending || settings.movementState == MovementState.Charging)
                && (settings.currentSurfaceState == SurfaceState.LeftWall
                || settings.currentSurfaceState == SurfaceState.RightWall)
                && IsJumpDirectionAllowed(dirLabel);

            bool isDashAllowed = IsDashDirectionAllowed(dirLabel);
            bool isAirDashAllowed = isInAir && settings.allowAirDash && IsAirDashDirectionAllowed(dirLabel);

            if (!isInAir)
            {
                if (isWallJumpAllowed)
                {
                    fetchedAction = "WallJump";
                    allowedToMove = true;
                }
                else if (isDashAllowed)
                {
                    fetchedAction = "Dash";
                    allowedToMove = true;
                }
                else if (isJumpAllowed)
                {
                    fetchedAction = "Jump";
                    allowedToMove = true;
                }
            }
            else if (isAirDashAllowed)
            {
                fetchedAction = "AirDash";
                allowedToMove = true;
            }
        }

        public void PerformMovementAction()
        {
            if (!allowedToMove || string.IsNullOrEmpty(fetchedAction)) { return; }

            float maxTravelDistance;
            float force;

            ResetPhysicsSettings(true, true);
            startPos = rb.position;

            if (fetchedAction == "Dash")
            {
                maxTravelDistance = settings.maxDashDistance;
                force = settings.dashForce;
            }
            else if (fetchedAction == "Jump")
            {
                maxTravelDistance = settings.maxJumpDistance;
                force = settings.jumpForce;
            }
            else if (fetchedAction == "AirDash")
            {
                maxTravelDistance = settings.maxAirDashDistance;
                force = settings.airDashForce;
            }
            else if (fetchedAction == "WallJump")
            {
                maxTravelDistance = settings.maxJumpDistance;
                force = settings.jumpForce;
            }
            else { Debug.LogWarning($"Unhandled action type: {fetchedAction}"); return; }

            if (landingResetCoroutine != null)
            {
                player.StopCoroutine(landingResetCoroutine);
                landingResetCoroutine = null;
            }

            SetupMovement(maxTravelDistance, force, fetchedAction);

            actionInProgress = true;
            hasTriggeredHover = false;
            hoverTimer = 0;
        }

        /// <summary>
        /// Called by BoxController right after pulling the player onto a box.
        /// </summary>
        public void LaunchOffBox(Vector3 direction)
        {
            // map 3D→2D stick dir
            snappedDir = new(direction.x, direction.y);
            fetchedAction = "Jump";
            allowedToMove = true;
            PerformMovementAction();
        }

        /// <summary>
        /// Configures targetDistance, forceMagnitude, predictedTargetPoint, and movementForceMode for a jump or dash.
        /// Called by PerformMovementAction and consumed by HandleActionForces and SmoothMovement.
        /// </summary>
        private void SetupMovement(float maxTravelDistance, float force, string action)
        {
            float hold = HoldRatio;
            targetDistance = maxTravelDistance * hold;
            forceMagnitude = force * Mathf.Pow(hold, settings.forceCurveExponent);
            predictedTargetPoint = player.transform.position + (Vector3)snappedDir * targetDistance;
            hasAppliedForce = false;
            currentAction = action;

            string dirLabel = GetClosestDirectionLabel(snappedDir);

            // Debug.Log($"Action = {action}" );

            if (action == "Dash")
            {
                settings.movementState = MovementState.Dashing;

                isJumping = false;
                isWallJumping = false;
                isAirDashing = false;
                isDashing = true;

                rb.useGravity = true;
                dashStartPos = rb.position;
                settings.movementForceMode = settings.dashForceMode;

                if (settings.currentSurfaceState == SurfaceState.Ground ||
                    settings.currentSurfaceState == SurfaceState.Ceiling)
                {
                    if (dirLabel == "E") { isRightGroundDash = true; }
                    else if (dirLabel == "W") { isRightGroundDash = false; }

                    // Debug.Log($"dirLabel = {dirLabel}");
                }
                else if (settings.currentSurfaceState == SurfaceState.LeftWall ||
                    settings.currentSurfaceState == SurfaceState.RightWall)
                {
                    if (dirLabel == "N") { isUpWallDash = true; }
                    else if (dirLabel == "S") { isUpWallDash = false; }
                    // Debug.Log($"dirLabel = {dirLabel}");
                }
            }
            else if (action == "Jump")
            {
                settings.movementState = MovementState.Jumping;

                // Movement flags
                isDashing = false;
                isWallJumping = false;
                isAirDashing = false;
                isJumping = true;

                rb.useGravity = false;
                jumpStartPos = rb.position;
                settings.movementForceMode = settings.jumpForceMode;

                if (dirLabel == "N") { isStraightJump = true; }
                else { isStraightJump = false; }

                bool isDiagonalJump = !isStraightJump && Mathf.Abs(snappedDir.x) > 0 && Mathf.Abs(snappedDir.y) > 0;
                if (isDiagonalJump && snappedDir.x > 0) { isDiagonalJumpRight = true; }
                else if (isDiagonalJump && snappedDir.x < 0) { isDiagonalJumpRight = false; }
            }
            else if (action == "WallJump")
            {
                settings.movementState = MovementState.WallJump;

                isJumping = false;
                isDashing = false;
                isAirDashing = false;
                isWallJumping = true;

                rb.useGravity = false;
                jumpStartPos = rb.position;
                settings.movementForceMode = settings.jumpForceMode;

                if (settings.currentSurfaceState == SurfaceState.RightWall)
                {
                    isWallJumpRight = true;
                    if (dirLabel == "W") { isWallJumpHorizontal = true; }
                    else { isWallJumpHorizontal = false; }
                }
                else if (settings.currentSurfaceState == SurfaceState.LeftWall)
                {
                    isWallJumpRight = false;
                    if (dirLabel == "E") { isWallJumpHorizontal = true; }
                    else { isWallJumpHorizontal = false; }
                }

                if (snappedDir.y > 0) { isWallJumpAscend = true; }
                else if (snappedDir.y < 0) { isWallJumpAscend = false; }
            }
            else if (action == "AirDash")
            {
                settings.movementState = MovementState.AirDashing;

                isJumping = false;
                isDashing = false;
                isWallJumping = false;
                isAirDashing = true;

                rb.useGravity = false;
                airDashStartPos = rb.position;
                settings.movementForceMode = settings.airDashForceMode;

                if (dirLabel == "N" || dirLabel == "S") { isVerticalAirDash = true; }
                else { isVerticalAirDash = false; }

                if (dirLabel == "E" || dirLabel == "W") { isHorizontalAirDash = true; }
                else { isHorizontalAirDash = false; }

                bool isDiagonalAirDash = !isVerticalAirDash && !isHorizontalAirDash && Mathf.Abs(snappedDir.x) > 0 && Mathf.Abs(snappedDir.y) > 0;

                if (isHorizontalAirDash)
                {
                    if (snappedDir.x > 0) { isRightAirDash = true; }
                    else if (snappedDir.x < 0) { isRightAirDash = false; }
                }
                else if (isDiagonalAirDash)
                {
                    if (snappedDir.x > 0) { isRightDiagonalAirDash = true; }
                    else if (snappedDir.x < 0) { isRightDiagonalAirDash = false; }
                }

                if (snappedDir.y > 0) { isAirDashAscend = true; }
                else if (snappedDir.y < 0) { isAirDashAscend = false; }
            }

            snappedDir = Vector3.Lerp(rb.linearVelocity.normalized, snappedDir.normalized, settings.lerpAmount);
        }

        /// <summary>
        /// Applies the initial impulse for a dash exactly once.
        /// Invoked in FixedUpdate when useHandleActionForces is true, immediately after SetupMovement for dashes.
        /// </summary>
        private void HandleActionForces()
        {
            if (settings.movementState == MovementState.Dashing &&
                !hasAppliedForce && snappedDir.sqrMagnitude > settings.minStickMagnitude)
            {
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(snappedDir.normalized * forceMagnitude, settings.movementForceMode);
                hasAppliedForce = true;
            }
        }

        // HELPER METHODS
        /// <summary>
        /// Marks hasReachedTarget true when the Rigidbody is within arrivalRadius of predictedTargetPoint.
        /// Used by hover-triggering logic and SmoothMovement.
        /// </summary>
        private void CheckArrivalAtTarget()
        {
            if (Vector3.Distance(rb.position, predictedTargetPoint) < settings.arrivalRadius && !isDropping)
                hasReachedTarget = true;
        }

        private void SnapToGround(Vector3 contactPoint)
        {
            float halfHeight = col.bounds.extents.y;
            Vector3 pos = rb.position;
            pos.y = contactPoint.y + halfHeight;
            rb.position = pos;
            rb.linearVelocity = Vector3.zero;
        }

        private IEnumerator DelayedLandingReset()
        {
            // Wait exactly landingBuffer seconds
            yield return new WaitForSeconds(settings.landingBuffer);

            // Only reset if we’re still grounded and not performing an action
            if (settings.currentSurfaceState == SurfaceState.Ground && !actionInProgress)
            {
                settings.movementState = MovementState.Idle;
                isLandingBuffered = true;
                hasBounced = false;
                hasBurstDropped = false;
                ResetPhysicsSettings(false, true);
                ResetActionState();
                // Debug.Log("DelayedLandingReset...");
            }

            landingResetCoroutine = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        // MOVEMENT SMOOTHING
        // ─────────────────────────────────────────────────────────────────────────
        #region MOVEMENT SMOOTHING
        /// <summary>
        /// Applies braking and forward force towards the target during Jump state, capping speed and adjusting damping.
        /// Driven by FixedUpdate and uses parameters set in SetupMovement.
        /// </summary>
        private void SmoothMovement()
        {
            if (settings.movementState != MovementState.Jumping && settings.movementState != MovementState.WallJump && settings.movementState != MovementState.AirDashing) return;

            Vector3 toTarget = predictedTargetPoint - rb.position;
            float remaining = toTarget.magnitude;

            if (remaining <= settings.arrivalRadius && !isDropping)
            {
                rb.position = predictedTargetPoint;
                rb.linearVelocity = Vector3.zero;
                return;
            }

            Vector3 dir = toTarget / remaining;
            float brakeZone = 1.0f;
            float velAlong = Vector3.Dot(rb.linearVelocity, dir);

            if (remaining < brakeZone && velAlong > 0f)
            {
                float brakeStrength = (1f - remaining / brakeZone) * forceMagnitude;
                rb.AddForce(-dir * brakeStrength, ForceMode.Acceleration);
            }

            float ratio = Mathf.Clamp01(remaining / targetDistance);
            float dynamicForce = forceMagnitude * ratio;
            rb.AddForce(dir * dynamicForce, settings.movementForceMode);

            if (rb.linearVelocity.magnitude > settings.maxJumpSpeed) { rb.linearVelocity = rb.linearVelocity.normalized * settings.maxJumpSpeed; }

            float closeRange = 1.0f;

            rb.linearDamping = (remaining < closeRange)
                ? Mathf.Lerp(0f, 5f, 1f - (remaining / closeRange))
                : settings.defaultDamping;

            float dampingRatio = Mathf.Clamp01(remaining / targetDistance);
            rb.linearDamping = Mathf.Lerp(settings.minHoverLinearDamping, settings.hoverLinearDamping, dampingRatio);
        }

        /// <summary>
        /// Coroutine that interpolates linearDamping and gravityStrength over ~0.25s for a smooth hover transition.
        /// Started by TryStartHoverEffect.
        /// </summary>
        private IEnumerator SmoothHoverTransition()
        {
            float transitionTime = 0.25f;
            float elapsed = 0f;
            float initialDamping = rb.linearDamping;
            float targetDamping = settings.hoverLinearDamping;
            float initialGravity = settings.gravityStrength;
            float targetGravity = 0f;

            while (elapsed < transitionTime)
            {
                float t = elapsed / transitionTime;
                rb.linearDamping = Mathf.Lerp(initialDamping, targetDamping, t);
                settings.gravityStrength = Mathf.Lerp(initialGravity, targetGravity, t);
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            rb.linearDamping = targetDamping;
            settings.gravityStrength = targetGravity;
        }
        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        // HOVER MECHANICS
        // ─────────────────────────────────────────────────────────────────────────
        #region HOVER MECHANICS
        /// <summary>
        /// Evaluates proximity and trajectory to decide when to enter Hover state.
        /// Zeroes gravity, sets damping, starts SmoothHoverTransition and WobbleEffect.
        /// Invoked each physics step in FixedUpdate after jump or air-dash.
        /// </summary>
        private bool TryStartHoverEffect()
        {
            if (!isInAir || settings.movementState == MovementState.Hovering ||
                hasTriggeredHover || isDropping || fastFalling) { return false; }

            if (IsWallAhead() || IsTargetNearWall())
                return false;

            Vector3 toTarget = predictedTargetPoint - rb.position;
            float forwardDot = Vector3.Dot(rb.linearVelocity.normalized, toTarget.normalized);
            float distanceToTarget = toTarget.magnitude;
            float hoverTriggerRadius = settings.hoverActivationRadius;
            float hoverForgivenessDistance = 2.5f;

            bool isCloseEnough = distanceToTarget <= hoverTriggerRadius;
            bool hasPassedTarget = forwardDot < 0f;
            bool isInForgivenessZone = hasPassedTarget && distanceToTarget <= hoverForgivenessDistance;

            if (!(isCloseEnough || isInForgivenessZone)) { return false; }

            if (Gamepad.current.buttonWest.IsPressed() && !settings.useAutoHover)
            {
                Hover();
                return true;
            }
            else if (settings.useAutoHover)
            {
                // Debug.Log("Auto hover.."); 
                Hover();
                return true;
            }

            return false;
        }

        private void Hover()
        {
            settings.movementState = MovementState.Hovering;
            settings.gravityStrength = 0;
            rb.linearDamping = settings.hoverLinearDamping;
            hoverTimer = settings.hoverDuration;
            hoverWobbleTimer = 0f;
            originalHoverPosition = rb.position;
            hasTriggeredHover = true;

            player.StartCoroutine(SmoothHoverTransition());
            WobbleEffect();
        }

        /// <summary>
        /// Applies a vertical sine-wave force during Hover to create a wobble effect.
        /// Called repeatedly during hover and invokes UpdateHoverTimer to manage hover duration.
        /// </summary>
        private void WobbleEffect()
        {
            if (settings.useHoverWobble && hoverTimer < (settings.hoverDuration - settings.hoverStartDelay) && hasReachedTarget)
            {
                rb.linearDamping = 0f;
                hoverWobbleTimer += Time.fixedDeltaTime;
                float wobbleFadeIn = Mathf.Clamp01(hoverWobbleTimer / settings.wobbleFadeInFactor);
                float wobbleOffset = Mathf.Sin(hoverWobbleTimer * settings.hoverWobbleSpeed) * settings.hoverWobbleHeight * wobbleFadeIn;
                Vector3 velChange = new(0f, wobbleOffset / Time.fixedDeltaTime, 0f);
                rb.AddForce(velChange, ForceMode.Acceleration);
            }

            UpdateHoverTimer();
        }

        /// <summary>
        /// Decrements hoverTimer each physics step and calls ExitHover when the timer elapses.
        /// Ensures hover state ends correctly.
        /// </summary>
        private void UpdateHoverTimer()
        {
            if (settings.movementState != MovementState.Hovering) return;
            hoverTimer -= Time.fixedDeltaTime;
            if (hoverTimer <= 0f) ExitHover();
        }

        /// <summary>
        /// Exits the hover state by transitioning to Descending, resetting timers, damping, gravity, and hover flags.
        /// Invoked by UpdateHoverTimer.
        /// </summary>    
        private void ExitHover()
        {
            settings.movementState = MovementState.Descending;
            hoverTimer = 0f;
            hoverWobbleTimer = 0f;
            rb.linearDamping = settings.defaultDamping;
            hasTriggeredHover = false;
            settings.gravityStrength = initialGravityStrength;
        }
        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        // GRAVITY & DROP FORCES
        // ─────────────────────────────────────────────────────────────────────────
        #region GRAVITY & DROP FORCES
        /// <summary>
        /// Applies gravity each physics step based on currentSurfaceState (via DetermineGravityDirection),
        /// and adjusts for jump/fall multipliers (lowJumpMultiplier, fallMultiplier), clamping by maxFallSpeed.
        /// </summary>
        private void ApplyCustomGravity()
        {
            Vector3 dir = DetermineGravityDirection().normalized;
            float verticalVelocity = Vector3.Dot(rb.linearVelocity, dir);
            float dropGravity = 0;
            if (fastFalling)
            {
                if (settings.currentSurfaceState == SurfaceState.Air) dropGravity = initialGravityStrength * settings.defaultDropMultiplier;
                else if (settings.currentSurfaceState == SurfaceState.LeftWall || settings.currentSurfaceState == SurfaceState.RightWall) dropGravity = initialGravityStrength * wallDescendingGravityStrength;

                rb.AddForce(dir * dropGravity, settings.fallForceMode);

                float dropCap = settings.maxFallSpeed * settings.defaultDropMultiplier;
                if (verticalVelocity > dropCap)
                    rb.linearVelocity -= dir * (verticalVelocity - dropCap);

                // Debug.Log($"[Fast-fall] speed={verticalVelocity:F2}  gravity={dropGravity:F2}");
                return;
            }

            float heightAboveStart = Vector3.Project(rb.position - startPos, -dir).magnitude;
            float dynamicGravity = settings.useDynamicGravityStrenght ? initialGravityStrength * (1f + heightAboveStart / settings.maxJumpDistance) : initialGravityStrength;

            if (verticalVelocity > 0.1f) { dynamicGravity *= settings.fallMultiplier; }
            rb.AddForce(dir * dynamicGravity, settings.fallForceMode);
            if (verticalVelocity > settings.maxFallSpeed) { rb.linearVelocity -= dir * (verticalVelocity - settings.maxFallSpeed); }

            // Debug.Log($"[Fall] speed={verticalVelocity:F2}  height={heightAboveStart:F2}  gravity={dynamicGravity:F2}");
        }


        /// <summary>
        /// Applies a burst downward force for fast-fall, sets fastFalling flag.
        /// Connected to drop input logic in Update and subsequent physics behavior.
        /// </summary>
        private void ApplyBurstDropForce()
        {
            if (landingResetCoroutine != null)
            {
                player.StopCoroutine(landingResetCoroutine);
                landingResetCoroutine = null;
            }

            float burstStrength;

            if (settings.movementState == MovementState.Stucked && isStuckFrozen)
            {
                if (freezeCoroutine != null) { player.StopCoroutine(freezeCoroutine); }
                ExitStuckState();
            }

            if (settings.movementState == MovementState.Hovering) { ExitHover(); player.StopAllCoroutines(); }

            if (settings.currentSurfaceState == SurfaceState.LeftWall ||
                settings.currentSurfaceState == SurfaceState.RightWall) { burstStrength = initialGravityStrength * settings.wallDropMultiplier; }
            else { burstStrength = initialGravityStrength * settings.defaultDropMultiplier; }

            rb.linearDamping = settings.defaultDamping;
            Vector3 gDir = DetermineGravityDirection();

            float velAlongDown = Vector3.Dot(rb.linearVelocity, gDir);
            rb.linearVelocity -= gDir * velAlongDown;
            rb.AddForce(gDir * burstStrength, ForceMode.VelocityChange);

            fastFalling = true;
            hasBurstDropped = true;
            predictedTargetPoint = rb.position;
            ResetActionState();

            // switch to the appropriate descending state
            if (settings.currentSurfaceState == SurfaceState.LeftWall ||
                settings.currentSurfaceState == SurfaceState.RightWall) { settings.movementState = MovementState.WallDescending; }
            else { settings.movementState = MovementState.Descending; }
        }

        /// <summary>
        /// Calculates the correct gravity direction vector from currentSurfaceState and contact time.
        /// Used by ApplyCustomGravity and ApplyBurstDropForce to orient gravity forces.
        /// </summary>
        private Vector3 DetermineGravityDirection()
        {
            Vector3 finalDir = settings.gravityDir.normalized;
            switch (settings.currentSurfaceState)
            {
                case SurfaceState.Ground:
                    finalDir = ConvertToVector(settings.gravityDirectionGround);
                    fastFalling = false;
                    break;
                case SurfaceState.Ceiling:
                    finalDir = ConvertToVector(settings.gravityDirectionCeiling);
                    break;
                case SurfaceState.LeftWall:
                    finalDir = ConvertToVector(settings.gravityDirectionLeftWall);
                    break;
                case SurfaceState.RightWall:
                    finalDir = ConvertToVector(settings.gravityDirectionRightWall);
                    break;
            }
            if (isInAir && Time.time - lastContactTime > NO_CONTACT_THRESHOLD)
                finalDir = settings.gravityDir.normalized;
            return finalDir;
        }
        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        // COLLISION & SURFACE DETECTION
        // ─────────────────────────────────────────────────────────────────────────
        #region COLLISION & SRUFACE DETECTION
        /// <summary>
        /// Processes collision contacts to identify the surface type (ground, ceiling, left/right wall),
        /// updates currentSurfaceState, lastSurfaceObject, and flags stateChanged.
        /// Called in OnCollisionEnter.
        /// </summary>
        private void HandleSurfaceState(Collision collision, out GameObject surfaceObject)
        {
            surfaceObject = null;
            int targetLayer = LayerMask.NameToLayer("Surface");
            if (collision.transform.gameObject.layer != targetLayer) return;

            settings.currentSurfaceState = SurfaceState.Air;
            SurfaceState detectedState = SurfaceState.Air;
            float bestDot = -1f;

            foreach (ContactPoint contact in collision.contacts)
            {
                Vector3 n = contact.normal;
                float dotUp = Vector3.Dot(n, Vector3.up);
                float dotDown = Vector3.Dot(n, Vector3.down);
                float dotRight = Vector3.Dot(n, Vector3.right);
                float dotLeft = Vector3.Dot(n, Vector3.left);

                if (dotUp > 0.7f && dotUp > bestDot)
                {
                    detectedState = SurfaceState.Ground;
                    surfaceObject = contact.otherCollider.gameObject;
                    bestDot = dotUp;
                }
                else if (dotDown > 0.7f && dotDown > bestDot)
                {
                    detectedState = SurfaceState.Ceiling;
                    surfaceObject = contact.otherCollider.gameObject;
                    bestDot = dotDown;
                }
                else if (dotLeft > settings.dotThreshold && dotLeft > bestDot)
                {
                    detectedState = SurfaceState.RightWall;
                    surfaceObject = contact.otherCollider.gameObject;
                    bestDot = dotLeft;
                }
                else if (dotRight > settings.dotThreshold && dotRight > bestDot)
                {
                    detectedState = SurfaceState.LeftWall;
                    surfaceObject = contact.otherCollider.gameObject;
                    bestDot = dotRight;
                }
            }

            if (surfaceObject != null)
            {
                lastSurfaceObject = surfaceObject;
                lastSurfaceCheckTime = Time.time;
            }

            if (detectedState != settings.currentSurfaceState) { settings.currentSurfaceState = detectedState; }
        }

        /// <summary>
        /// Checks for nearby colliders via Physics.OverlapSphereAlloc to determine if the Rigidbody is in contact with any surface.
        /// Drives isInAir logic and collision-based state transitions in StopMovementUponCollision.
        /// </summary>    
        private bool CheckSurfaces()
        {
            float radius = 0;
            if (player.TryGetComponent<Collider>(out var col))
            {
                radius = col.bounds.extents.y + 0.1f;
            }
            else { Debug.Log("Couldn't fetch the collider"); }
            // float radius = mono.GetComponent<Collider>().bounds.extents.y + 0.1f;
            Vector3 position = rb.position;

            int hitCount = Physics.OverlapSphereNonAlloc(
                position,
                radius,
                colliders,
                settings.surfaceLayer
            );

            // Try to resize once if buffer is full, but don't exceed maxBufferSize
            if (hitCount == colliders.Length && colliders.Length < maxBufferSize)
            {
                int newSize = Mathf.Min(colliders.Length * 2, maxBufferSize);
                colliders = new Collider[newSize];

                hitCount = Physics.OverlapSphereNonAlloc(
                    position,
                    radius,
                    colliders,
                    settings.surfaceLayer
                );
            }

            return hitCount > 0;
        }

        /// <summary>
        /// Returns the last collided surface object if within surfaceMemoryDuration; otherwise null.
        /// Provides brief memory of the last surface post-collision.
        /// </summary>
        private GameObject GetLastCollidedSurface()
        {
            if (Time.time - lastSurfaceCheckTime <= surfaceMemoryDuration) { return lastSurfaceObject; }
            return null;
        }

        private bool IsWallAhead()
        {
            // direction we’re moving toward
            Vector3 dir = (predictedTargetPoint - rb.position).normalized;
            if (dir == Vector3.zero) return false;
            // cast a short ray — if it hits a surface, skip hover
            return Physics.Raycast(
                rb.position,
                dir,
                settings.WallAheadCheckDistance,
                settings.surfaceLayer
            );
        }

        private bool IsTargetNearWall()
        {
            // sphere-check around the predicted landing point
            Collider[] hits = Physics.OverlapSphere(
                predictedTargetPoint,
                settings.TargetWallProximityRadius,
                settings.surfaceLayer
            );
            return hits.Length > 0;
        }

        /// <summary>
        /// Raycasts downward to detect ground proximity within groundProximityCheckDistance.
        /// Used in FixedUpdate for bounce logic and in OnGroundCollisionBounceFromWall.
        /// </summary>
        private bool IsNearGround()
        {
            // Debug.Log("IsNearGround activated");
            Vector3 origin = col.bounds.center;
            origin.y = col.bounds.min.y + 0.1f;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                settings.groundProximityCheckDistance + 0.25f, settings.surfaceLayer))
            {
#if UNITY_EDITOR
                Debug.DrawLine(origin, hit.point, Color.magenta);
                // Debug.Log($"surface->{hit.transform.gameObject.name}");
#endif
                return true;
            }

            return false;
        }
        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        // STATE MANAGEMENT
        // ─────────────────────────────────────────────────────────────────────────

        #region STATE MANAGEMENT
        /// <summary>
        /// Stops movement and transitions to Stucked state upon collision during movement,
        /// setting stuckTimer and isStuckFrozen. Unlocked later by FreezePlayer.
        /// </summary>
        // private void StopMovementUponCollision()
        // {
        //     if (isMoving && settings.movementState == MovementState.Dashing &&
        //         (settings.currentSurfaceState == SurfaceState.LeftWall ||
        //         settings.currentSurfaceState == SurfaceState.RightWall)) { settings.movementState = MovementState.Idle; return; }

        //     if (settings.movementState != MovementState.Jumping &&
        //         settings.movementState != MovementState.WallJump &&
        //         settings.movementState != MovementState.AirDashing)
        //         return;

        //     if (isMoving && settings.movementState == MovementState.Jumping || settings.movementState == MovementState.WallJump || settings.movementState == MovementState.AirDashing)
        //     {
        //         if (settings.movementState == MovementState.WallDescending) return;

        //         if ((settings.currentSurfaceState == SurfaceState.LeftWall || settings.currentSurfaceState == SurfaceState.RightWall)
        //             && settings.currentSurfaceState != SurfaceState.Ground)
        //         {
        //             settings.movementState = MovementState.Stucked;
        //             stuckTimer = settings.stuckDurationWall;
        //             isStuckFrozen = true;
        //         }
        //         else if (settings.currentSurfaceState == SurfaceState.Ceiling)
        //         {
        //             settings.movementState = MovementState.Stucked;
        //             stuckTimer = settings.stuckDurationCeiling;
        //             isStuckFrozen = true;
        //         }

        //         // FreezePlayer();
        //         if (freezeCoroutine != null)
        //             player.StopCoroutine(freezeCoroutine);

        //         freezeCoroutine = player.StartCoroutine(FreezePlayerCoroutine(settings.stuckDurationWall));
        //         isMoving = false;
        //     }
        // }

        private void StopMovementUponCollision()
        {
            if (settings.movementState != MovementState.Jumping &&
                settings.movementState != MovementState.WallJump &&
                settings.movementState != MovementState.AirDashing)
                return;

            if (isMoving)
            {
                if (settings.currentSurfaceState == SurfaceState.LeftWall || settings.currentSurfaceState == SurfaceState.RightWall)
                {
                    if (settings.movementState == MovementState.WallDescending) return;

                    settings.movementState = MovementState.Stucked;
                    stuckTimer = settings.stuckDurationWall;
                }
                else if (settings.currentSurfaceState == SurfaceState.Ceiling)
                {
                    settings.movementState = MovementState.Stucked;
                    stuckTimer = settings.stuckDurationCeiling;
                }

                if (freezeCoroutine != null)
                    player.StopCoroutine(freezeCoroutine);

                freezeCoroutine = player.StartCoroutine(FreezePlayerCoroutine(settings.stuckDurationWall));
                isMoving = false;
            }
        }

        private IEnumerator FreezePlayerCoroutine(float duration)
        {
            // Enter stuck state
            rb.isKinematic = true;
            // settings.gravityStrength = 0f;
            isStuckFrozen = true;

            yield return new WaitForSeconds(duration);

            // Exit stuck state
            ExitStuckState();
            freezeCoroutine = null;
        }

        /// <summary>
        /// Applies a one-time bounce impulse when wall descending and near ground.
        /// Triggered in FixedUpdate if conditions are met.
        /// </summary>
        private void OnWallDescendingBounce()
        {
            Vector3 bounceDir = (lastWallSide == SurfaceState.LeftWall) ? Vector3.right : Vector3.left;
            rb.AddForce(bounceDir * settings.bounceSpeed, ForceMode.Impulse);
            rb.linearVelocity = new Vector3(bounceDir.x * settings.bounceSpeed, 0f, 0f);
            hasBurstDropped = false;
            hasBounced = false;
            Debug.Log("Bounced from wall after descending");
        }


        /// <summary>
        /// Transitions to Idle when no action is in progress and velocity is near zero on the ground,
        /// and calls ResetPhysicsSettings to restore default physics parameters.
        /// </summary>
        private void ForceIdleState()
        {
            if (settings.movementState == MovementState.Charging ||
                settings.movementState == MovementState.Jumping ||
                settings.movementState == MovementState.WallJump ||
                settings.movementState == MovementState.Dashing ||
                settings.currentSurfaceState == SurfaceState.Ceiling ||
                actionInProgress)
                return;

            if (rb.linearVelocity.sqrMagnitude < 0.01f && settings.currentSurfaceState == SurfaceState.Ground)
            {
                settings.movementState = MovementState.Idle;
                ResetPhysicsSettings(false, true);
            }

            if (settings.movementState == MovementState.Idle && settings.currentSurfaceState == SurfaceState.Ground) { isLandingBuffered = false; }
        }

        /// <summary>
        /// Clears input-related flags and timers (southButtonPressed, buttonHoldTimer, stickHoldTimer, snappedDir).
        /// Used by OnSouthButtonCanceled to abort actions cleanly.
        /// </summary>
        public void ResetActionState()
        {
            // southButtonPressed = false;
            buttonHoldTimer = 0;
            actionInProgress = false;
            hasReachedTarget = false;
            isJumping = false;
            isDashing = false;
            isAirDashing = false;
            isWallJumping = false;
            snappedDir = Vector2.zero;
        }

        /// <summary>
        /// Restores gravityStrength, damping, velocity, and fastFalling. Optionally resets hasAppliedForce.
        /// Called by PerformMovementAction before new actions and by TrySetIdleState when entering Idle.
        /// </summary>
        private void ResetPhysicsSettings(bool resetAppliedForce, bool resetDamping)
        {
            settings.gravityStrength = initialGravityStrength;
            rb.isKinematic = false;
            rb.linearDamping = settings.defaultDamping;
            fastFalling = false;
            rb.linearVelocity = Vector3.zero;

            if (resetAppliedForce) hasAppliedForce = false;
            if (resetDamping) rb.linearDamping = settings.defaultDamping;
        }

        private void ExitStuckState()
        {
            Debug.Log("ExitStuckState stated...");
            stuckTimer = 0f;
            isStuckFrozen = false;
            ResetPhysicsSettings(true, true);
            settings.movementState = MovementState.WallDescending;

            // Remove the velocity component into the wall/ceiling
            // Vector3 restoredVel = Vector3.ProjectOnPlane(preStuckVelocity, preStuckNormal);
            // rb.linearVelocity = restoredVel;

        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        // DIRECTION & LABEL MAPPING
        // ─────────────────────────────────────────────────────────────────────────
        #region DIRECTION & LABEL MAPPING
        /// <summary>
        /// Converts raw 2D stick input into a world-space Vector3 direction,
        /// optionally snapping to discrete increments based on directionCount.
        /// Used by LeftAnalogStickInput.
        /// </summary>
        protected Vector3 GetSnappedDirection(Vector2 input)
        {
            if (input.sqrMagnitude < settings.minStickMagnitude) { return Vector3.zero; }

            float rawAngle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            if (settings.snapDirectionsEnabled)
            {
                float angleStep = 360f / settings.directionCount;
                rawAngle = Mathf.Round(rawAngle / angleStep) * angleStep;
            }

            return Quaternion.Euler(0f, 0f, rawAngle) * Vector3.right;
        }

        /// <summary>
        /// Finds the nearest direction label (e.g. "NNE") for a given Vector2 direction
        /// based on the labelToAngle map. Used by PerformMovementAction and permission checks.
        /// </summary>
        public string GetClosestDirectionLabel(Vector2 dir)
        {
            float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 360f) % 360f;
            float minDiff = float.MaxValue;
            string closestLabel = "N";

            foreach (var pair in labelToAngle)
            {
                float diff = Mathf.Abs(Mathf.DeltaAngle(angle, pair.Value));
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closestLabel = pair.Key;
                }
            }

            return closestLabel;
        }

        /// <summary>
        /// Determines if the specified direction label is permitted for a dash given the current surface state.
        /// On ground or ceiling only “W” (left) and “E” (right) are allowed; on walls only “N” (up) and “S” (down) are allowed.
        /// Used in PerformMovementAction to decide whether to execute a dash.
        /// </summary>
        private bool IsDashDirectionAllowed(string label)
        {
            if (settings.currentSurfaceState == SurfaceState.Ground || settings.currentSurfaceState == SurfaceState.Ceiling)
            { return label == "W" || label == "E"; }

            if (settings.currentSurfaceState == SurfaceState.LeftWall || settings.currentSurfaceState == SurfaceState.RightWall)
            { return label == "N" || label == "S"; }

            return false;
        }

        /// <summary>
        /// Determines if the specified direction label is permitted for a jump given the current surface state.
        /// Disallows pure horizontal labels (“W”/“E”) and labels parallel to a wall when on that wall, then checks the allowedMoveLabels map.
        /// Used in PerformMovementAction to decide whether to execute a jump.
        /// </summary>
        private bool IsJumpDirectionAllowed(string label)
        {
            if (label == "W" || label == "E")
            {
                if (settings.currentSurfaceState == SurfaceState.LeftWall ||
                    settings.currentSurfaceState == SurfaceState.RightWall)
                {
                    return true;
                }
                return false;
            }

            if ((settings.currentSurfaceState == SurfaceState.LeftWall || settings.currentSurfaceState == SurfaceState.RightWall) &&
                (label == "N" || label == "S")) { return false; }

            return allowedMoveLabels.TryGetValue(settings.currentSurfaceState, out var allowed) &&
                System.Array.Exists(allowed, l => l == label);
        }

        private bool IsAirDashDirectionAllowed(string label)
        {
            return allowedMoveLabels.TryGetValue(settings.currentSurfaceState, out var allowed) &&
                System.Array.Exists(allowed, l => l == label);
        }

        /// <summary>
        /// Generates a direction label string for a given index, supporting cardinal or full 16-way labels.
        /// Used by BuildLabelToAngleMap.
        /// </summary>
        private string GetDirectionLabel(int index)
        {
            if (!settings.useCardinalLabels) return (index + 1).ToString();
            string[] labels = new[]
            {
                "E","ENE","NE","NNE","N","NNW","NW","WNW",
                "W","WSW","SW","SSW","S","SSE","SE","ESE"
            };
            return labels[index % labels.Length];
        }

        /// <summary>
        /// Populates the labelToAngle dictionary by iterating through directionCount and calling GetDirectionLabel.
        /// Executed in Awake to initialize direction-label mappings.
        /// </summary>
        private void BuildLabelToAngleMap()
        {
            labelToAngle = new Dictionary<string, float>();
            float angleStep = 360f / settings.directionCount;
            for (int i = 0; i < settings.directionCount; i++)
            {
                string label = GetDirectionLabel(i);
                float angle = (i * angleStep + 360f) % 360f;
                labelToAngle[label] = angle;
            }
        }
        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        // DIRECTION & LABEL MAPPING
        // ─────────────────────────────────────────────────────────────────────────
        #region GIZMOS
        public void OnDrawGizmos()
        {
            if (settings.snapDirectionsEnabled)
            {
                Gizmos.color = settings.baseDirectionColor;
                float angleStep = 360f / settings.directionCount;

                for (int i = 0; i < settings.directionCount; i++)
                {
                    float angle = i * angleStep;
                    float angleRad = angle * Mathf.Deg2Rad;
                    Vector3 dir = new(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);
                    Vector3 endPt = player.transform.position + dir * settings.directionLineLength;

                    Gizmos.DrawLine(player.transform.position, endPt);

#if UNITY_EDITOR

                    if (settings.showDirectionLabels && !Application.isPlaying)
                    {
                        UnityEditor.Handles.color = Color.white;
                        UnityEditor.Handles.Label(endPt + Vector3.up * 0.1f, GetDirectionLabel(i));
                    }
#endif
                }
            }


            if (Application.isPlaying && predictedTargetPoint != Vector3.zero)
            {
                if (rb == null)
                {
                    rb = player.GetComponent<Rigidbody>();
                    if (rb == null) return;
                }

                float distanceToTarget = Vector3.Distance(rb.position, predictedTargetPoint);
                Gizmos.color = hasReachedTarget ? settings.jumpTargetColor : settings.landingPointColor;
                Gizmos.DrawSphere(predictedTargetPoint, 0.25f);
            }


            if (Application.isPlaying && labelToAngle != null && allowedMoveLabels.ContainsKey(settings.currentSurfaceState))
            {
                string[] labels = allowedMoveLabels[settings.currentSurfaceState];

                foreach (var label in labels)
                {
                    if (labelToAngle.TryGetValue(label, out float angle))
                    {
                        float angleRad = angle * Mathf.Deg2Rad;
                        Vector3 dir = new(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);

                        Gizmos.color = settings.allowedJumpColor;
                        Gizmos.DrawLine(rb.position, rb.position + dir.normalized * settings.directionLineLength);

#if UNITY_EDITOR
                        if (settings.showDirectionLabels)
                        {
                            UnityEditor.Handles.color = settings.allowedJumpColor;
                            UnityEditor.Handles.Label(rb.position + dir.normalized * (settings.directionLineLength + 0.1f), label);
                        }
#endif
                    }
                }
            }


            if (Application.isPlaying && labelToAngle != null)
            {
                foreach (var pair in labelToAngle)
                {
                    string label = pair.Key;
                    float angle = pair.Value;

                    if (!IsDashDirectionAllowed(label)) continue;

                    float angleRad = angle * Mathf.Deg2Rad;
                    Vector3 dir = new(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);

                    Gizmos.color = settings.dashDirectionColor;
                    Gizmos.DrawLine(rb.position, rb.position + dir.normalized * settings.directionLineLength);

#if UNITY_EDITOR
                    if (settings.showDirectionLabels)
                    {
                        UnityEditor.Handles.color = settings.dashDirectionColor;
                        UnityEditor.Handles.Label(rb.position + dir.normalized * (settings.directionLineLength + 0.1f), $"D:{label}");
                    }
#endif
                }
            }


            if (Application.isPlaying && InputManager.HasStickMovement())
            {
                Gizmos.color = settings.snappedInputColor;
                Vector3 start = rb.position;
                Vector3 end = start + (Vector3)snappedDir.normalized * settings.directionLineLength;


                Gizmos.DrawLine(start, end);


                float headAng = 20f, headLen = 0.25f;
                Quaternion look = Quaternion.LookRotation(Vector3.forward, end - start);
                Vector3 right = look * Quaternion.Euler(0, 0, headAng) * Vector3.up;
                Vector3 left = look * Quaternion.Euler(0, 0, -headAng) * Vector3.up;
                Gizmos.DrawLine(end, end - right * headLen);
                Gizmos.DrawLine(end, end - left * headLen);

#if UNITY_EDITOR
                if (settings.showDirectionLabels)
                {

                    var style = new GUIStyle();
                    style.normal.textColor = Color.red;


                    string lbl = GetClosestDirectionLabel(snappedDir);
                    UnityEditor.Handles.Label(end + Vector3.up * 0.1f, $"Input: {lbl}", style);
                }
#endif
            }
        }
        #endregion
    }

}
