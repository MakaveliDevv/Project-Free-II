using UnityEngine;

namespace Assets.Scripts.Player
{
    [System.Serializable]
    public class MovementSettings
    {
        // ─ Movement State
        [Header("Movement State")]
        [Tooltip("Current movement state of the character")]
        public MovementState movementState = MovementState.Idle;

        [Tooltip("Surface the character is currently on")]
        public SurfaceState currentSurfaceState = SurfaceState.Ground;

        [Tooltip("If true, bypasses Unity’s smoothing to use raw input values from inputActions")]
        public bool useRawInput = true;

        // ─ Direction Snapping
        [Header("Direction Snapping")]
        [Tooltip("Enable snapping of stick input to discrete directions; if false, full analog input is used")]
        public bool snapDirectionsEnabled = false;

        [Tooltip("Number of directions to snap to when snapDirectionsEnabled is true; used in angle quantization")]
        public int directionCount = 16;

        // ─ Stuck Mechanics
        [Header("Stuck Mechanics")]
        [Tooltip("Time in seconds the character remains stuck when hitting a wall")]
        public float stuckDurationWall = 1.0f;

        [Tooltip("Time in seconds the character remains stuck when hitting the ceiling")]
        public float stuckDurationCeiling = 1.5f;

        [Tooltip("Cooldown time after being stuck before stuck state can trigger again; ensures brief recovery")]
        public float stuckCooldownDuration = 1f;

        // ─ Display Settings
        [Header("Display Settings")]
        [Tooltip("Toggle the rendering of direction labels in gizmos; if false, useCardinalLabels is ignored")]
        public bool showDirectionLabels = true;

        [Tooltip("When true, uses only N/E/S/W labels instead of full 16 directions; requires showDirectionLabels")]
        public bool useCardinalLabels = true;

        // ─ Jump & Dash Parameters
        [Header("Jump & Dash Parameters")]
        [Tooltip("Upward force applied when initiating a jump")]
        public float jumpForce = 5f;

        [Tooltip("Maximum horizontal distance allowed during a jump; used to validate jump targets")]
        public float maxJumpDistance = 5f;

        [Tooltip("Speed threshold below which landing is considered safe; used with jumpForceMode")]
        public float maxJumpSpeed = 10f;

        [Tooltip("A safeguard when moving too fast")]
        public float bounceSpeed = 5f;

        [Tooltip("Force applied when starting a dash; used with dashForceMode")]
        public float dashForce = 5f;

        [Tooltip("Maximum distance allowed for a dash; used to calculate dash endpoints")]
        public float maxDashDistance = 5f;

        [Tooltip("Force applied when starting an air dash; used with airDashForceMode")]
        public float airDashForce = 5f;

        [Tooltip("Maximum distance allowed for a air dash; used to calculate air dash endpoints")]
        public float maxAirDashDistance = 5f;

        [Tooltip("Default linear damping applied to the Rigidbody when no jump or dash is active")]
        public float defaultDamping = 0f;

        // ─ Force Modes
        [Header("Force Modes")]
        [Tooltip("ForceMode used for standard movement forces")]
        public ForceMode movementForceMode;

        [Tooltip("ForceMode applied to jumpForce; defaults to VelocityChange for instant velocity change")]
        public ForceMode jumpForceMode = ForceMode.VelocityChange;

        [Tooltip("ForceMode applied to dashForce; defaults to Impulse for a quick burst")]
        public ForceMode dashForceMode = ForceMode.Impulse;

        [Tooltip("ForceMode applied to airDashForce; defaults to Impulse for a quick burst")]
        public ForceMode airDashForceMode = ForceMode.Impulse;

        // ─ Gravity Settings
        [Header("Gravity Settings")]
        [Tooltip("Magnitude of gravitational pull applied each physics frame")]
        public float gravityStrength = 9.81f;
        public bool useDynamicGravityStrenght = false;

        [Tooltip("Percentage of gravityStrength applied when on walls; reduces slip along walls")]
        public int wallGravityPercent = 10;

        [Tooltip("Vector direction of gravity; set automatically based on gravityDirection* enums")]
        public Vector3 gravityDir = Vector3.down;

        [Tooltip("GravityDirection enum used when on ground; updates gravityDir accordingly")]
        public GravityDirection gravityDirectionGround = GravityDirection.Down;

        [Tooltip("GravityDirection enum used when on ceiling; updates gravityDir accordingly")]
        public GravityDirection gravityDirectionCeiling = GravityDirection.Down;

        [Tooltip("GravityDirection enum used when on left wall; updates gravityDir accordingly")]
        public GravityDirection gravityDirectionLeftWall = GravityDirection.Right;

        [Tooltip("GravityDirection enum used when on right wall; updates gravityDir accordingly")]
        public GravityDirection gravityDirectionRightWall = GravityDirection.Left;

        // ─ Jump / Fall Multipliers
        [Header("Jump / Fall Multipliers")]
        [Tooltip("Gravity multiplier when performing a low jump (button released early); increases gravity to shorten jump")]
        public float lowJumpMultiplier = 4.0f;

        [Tooltip("Gravity multiplier when falling normally; makes descent faster or slower")]
        public float fallMultiplier = 1f;

        [Tooltip("Gravity multiplier for a fast-drop action when in the air;")]
        public float defaultDropMultiplier = 2f;

        [Tooltip("Gravity multiplier for a fast-drop action when stucked on the wall;")]
        public float wallDropMultiplier = 2f;

        [Tooltip("Maximum allowed downward speed when falling; caps fall velocity")]
        [Range(1f, 100f)]
        public float maxFallSpeed = 40f;

        [Header("Drop Settings")]
        [Tooltip("Half‐angle (in degrees) around straight down in which a stick flick counts as a drop")]
        [Range(0f, 45f)]
        public float dropAngleTolerance = 15f;

        [Tooltip("ForceMode applied when dropMultiplier is active; defaults to Impulse")]
        public ForceMode dropForceMode = ForceMode.Impulse;

        [Tooltip("ForceMode applied when fallMultiplier is active; defaults to Acceleration")]
        public ForceMode fallForceMode = ForceMode.Acceleration;

        // ─ Hover Settings
        [Header("Hover Settings")]
        [Tooltip("Enable vertical wobble effect during hover")]
        public bool useHoverWobble = true;

        [Tooltip("Speed of vertical wobble when hovering; only used if useHoverWobble is true")]
        public float hoverWobbleSpeed = 2f;

        [Tooltip("Height amplitude of vertical wobble during hover; only used if useHoverWobble is true")]
        public float hoverWobbleHeight = 0.2f;

        [Tooltip("Delay before allowing hover")]
        public float hoverStartDelay = 0.1f;

        [Tooltip("Fade-in factor for wobble effect at hover start; between 0 (no fade) and 1 (full fade)")]
        public float wobbleFadeInFactor = 0.25f;

        [Tooltip("Radius around calculated flight target in which hover activates; ensures hover only near peak")]
        public float hoverActivationRadius = 1.5f;

        [Tooltip("Total time allowed for hover state before forcing descent")]
        public float hoverDuration = 2f;

        [Tooltip("Minimum height above ground required to initiate hover; prevents hover too close to surface")]
        public float minHoverHeight = 2.0f;

        [Tooltip("Minimum linear drag applied during hover; smooths out motion")]
        public float minHoverLinearDamping = 0f;

        [Tooltip("Linear drag applied while hovering to dampen movement")]
        public float hoverLinearDamping = 5f;

        // ─ Surface Detection
        [Header("Surface Detection")]
        [Tooltip("Distance to check below character for ground proximity; used in raycasts")]
        public float groundProximityCheckDistance = 1.0f;

        [Tooltip("Minimum time the character must be airborne before allowing Idle state; avoids flicker")]
        public float minAirborneTimeBeforeIdle = 0.15f;

        [Tooltip("Raycast distance threshold to detect any surface; used for landing and sticking")]
        public float distanceToSurfaceThreshold = 0.05f;

        [Tooltip("Runtime-adjustable check distance, initialized from distanceToSurfaceThreshold")]
        public float checkDistance;
        [Tooltip("LayerMask defining which layers count as surfaces for detection raycasts")]
        public LayerMask surfaceLayer;

        // ─ Input Hold Timing
        [Header("Input Hold Timing")]
        [Tooltip("Minimum duration the stick must be held before registering a direction")]
        public float minStickHoldTime = 0.1f;

        [Tooltip("Minimum stick tilt magnitude to consider as valid input")]
        public float minStickMagnitude = 0.2f;

        [Tooltip("Minimum duration a button must be held to register a press")]
        public float minButtonPressTime = 0.1f;

        [Tooltip("Maximum duration to hold a button for charge actions; used in HoldRatio")]
        public float maxHoldTime = 0.5f;

        // ─ Gizmo Visualization
        [Header("Gizmo Visualization")]
        [Tooltip("Overall scale factor for all debug gizmos")]
        public float gizmoScale = 2f;

        [Tooltip("Length of directional lines drawn for stick input in gizmos")]
        public float directionLineLength = 1.5f;

        [Tooltip("Color of the base direction line when no action is active")]
        public Color baseDirectionColor = Color.blue;

        [Tooltip("Color indicating valid jump directions in gizmos")]
        public Color allowedJumpColor = Color.green;

        [Tooltip("Color used for dash direction lines in gizmos")]
        public Color dashDirectionColor = Color.cyan;

        [Tooltip("Color used to show snapped input direction when snapping is enabled")]
        public Color snappedInputColor = Color.yellow;

        [Tooltip("Color marking the jump target point in gizmos")]
        public Color jumpTargetColor = Color.red;

        [Tooltip("Color marking the landing point in gizmos")]
        public Color landingPointColor = Color.green;

        [Tooltip("Color of the ground check ray in gizmos")]
        public Color groundCheckDistanceColor;

        // ─ Miscellaneous
        [Header("Miscellaneous")]
        [Tooltip("Radius around a target position to consider arrival complete")]
        public float arrivalRadius = 0.05f;

        // [Tooltip("Buffer time between state transitions to prevent rapid toggling")]
        // public float stateBuffer = 0.25f;

        [Tooltip("Interpolation factor (0–1) used when smoothing movement toward a target")]
        public float lerpAmount = 0.85f;

        [Tooltip("When false, LateUpdate will not force Z=0 (allows small Z offsets)")]
        public bool enableZLock = true;

        [Tooltip("If true, uses InputSystem action callbacks to apply forces instead of manual polling")]
        public bool useHandleActionForces = true;

        [Tooltip("Velocity magnitude threshold above which the object is considered 'moving'")]
        public float isMovingThreshold = 0.2f;

        [Tooltip("Exponent applied to input magnitude for custom response curves; shapes force application")]
        public float forceCurveExponent = 1.0f;

        [Tooltip("If true, player can perform an air dash")]
        public bool allowAirDash = false;
        public float landingBuffer = 0.1f;

        [Header("Test")]
        public float fallTimer = .25f;
        public bool useAutoHover = false;
    }

    [System.Serializable]
    public class CombatSettings
    {
        [Tooltip("How long after a swipe to wait before allowing the next one")]
        public float centerResetDelay = 1f;
        [Tooltip("How long staying at A without moving before cancelling swipe")]
        public float startHoldDelay = 1f;
        [Tooltip("Stick magnitude threshold")]
        public float stickMagnitudeThresh = .9f;
    }
}

