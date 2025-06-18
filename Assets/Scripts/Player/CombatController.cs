using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public class CombatController
    {
        private readonly Player player;
        private readonly CombatSettings settings;
        private static readonly string[] labels = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };

        // Swipe state
        private string startLabel = null;
        private string previousEndLabel = null;
        private bool awaitingReset = false;
        private float resetTimer = 0f;
        private float centerHoldTimer = 0f;
        private float startHoldTimer = 0f;
        private bool tilted = false;

        // Combat collisions
        public bool inRangeForCombat = false;
        private Attackable attackable = null;

        public string LastDirectionKey { get; private set; } = "";
        public string EndDirection => previousEndLabel;

        public static event Action<string, bool> OnSwipePerformed;

        public bool attacked = false;
        public bool success = false;

        public bool isCombatModeActive = false;

        public CombatController(Player player, CombatSettings settings)
        {
            this.player = player;
            this.settings = settings;
        }

        public void Update()
        {
            if (InputManager.RightShoulderDoublePressed)
            {
                InputManager.RightShoulderPressed = false;
                player.mode = Mode.Normal;
                return;
            }

            if (InputManager.RightShoulderPressed && !isCombatModeActive)
            {
                player.mode = Mode.Combat;
            }

            if (player.mode == Mode.Combat) { ProcessRightStickSwipe(); }
        }

        private void ProcessRightStickSwipe()
        {
            var raw = InputManager.RightStickInput;
            tilted = raw.magnitude > settings.stickMagnitudeThresh;
            string snapped = tilted ? SnapTo8Label(raw) : "";

            if (awaitingReset)
            {
                resetTimer += Time.deltaTime;
                if (resetTimer >= settings.centerResetDelay)
                {
                    Debug.Log($"[Attackable] \u21BA Auto-reset after {settings.centerResetDelay}s; ready for next swipe");
                    awaitingReset = false;
                    resetTimer = 0f;
                }
                return;
            }

            if (startLabel == null)
            {
                if (!tilted) return;
                if (previousEndLabel != null && snapped == previousEndLabel) return;

                startLabel = snapped;
                centerHoldTimer = 0f;
                startHoldTimer = 0f;
                previousEndLabel = null;

                Debug.Log($"[Attackable] → START at {startLabel}");
                return;
            }

            if (!tilted)
            {
                centerHoldTimer += Time.deltaTime;
                if (centerHoldTimer >= settings.centerResetDelay)
                {
                    Debug.Log($"[Attackable] ✗ Cancel swipe: held in center for {centerHoldTimer:F2}s");
                    Reset();
                }
                return;
            }
            centerHoldTimer = 0f;

            if (snapped == startLabel)
            {
                startHoldTimer += Time.deltaTime;
                if (startHoldTimer >= settings.startHoldDelay)
                {
                    Debug.Log($"[Attackable] ✗ Cancel swipe: held at A={startLabel} for {startHoldTimer:F2}s");
                    Reset();
                }
                return;
            }

            startHoldTimer = 0f;

            int aIdx = Array.IndexOf(labels, startLabel);
            int oppositeIndex = (aIdx + 4) % labels.Length;
            string opposite = labels[oppositeIndex];
            bool pass = snapped == opposite;
            string key = $"{startLabel}-{snapped}";

            LastDirectionKey = key;

            if (inRangeForCombat)
            {
                Debug.Log("In range for combat, start processing...");
                player.StartCoroutine(Processing(key, pass));
            }

            if (pass)
                Debug.Log($"[Attackable] ✅ SUCCESS: {startLabel} → {snapped}");
            else
                Debug.Log($"[Attackable] ❌ FAILURE: {startLabel} → {snapped} (expected {opposite})");

            previousEndLabel = snapped;

            Reset();
            OnSwipePerformed?.Invoke(key, pass);
        }

        private IEnumerator Processing(string key, bool pass)
        {
            Debug.Log("Processing...");
            if (attackable == null) yield break;

            attacked = true;

            if (attackable.directions.TryGetValue(key, out var performed)
                && performed == attackable.attackDirection && pass)
            {
                Debug.Log($"[Attackable] ▶️ SUCCESS {key} → {performed}");
                success = true;
            }
            else
            {
                Debug.Log($"[Attackable] ▶️ ❌ FAILURE {key} → mapped " +
                        $"{(attackable.directions.TryGetValue(key, out performed) ? performed : (AttackDirection)(-1))}, " +
                        $"expected {attackable.attackDirection}");
            }
        }

        private void Reset()
        {
            startLabel = null;
            previousEndLabel = null;
            awaitingReset = true;
            resetTimer = 0f;
            centerHoldTimer = 0f;
            startHoldTimer = 0f;
        }

        private string SnapTo8Label(Vector2 raw)
        {
            if (raw.sqrMagnitude < settings.stickMagnitudeThresh * settings.stickMagnitudeThresh) return "";

            float angle = (Mathf.Atan2(raw.y, raw.x) * Mathf.Rad2Deg + 360f) % 360f;
            float snapAngle = Mathf.Round(angle / 45f) * 45f;
            int idx = Mathf.RoundToInt(snapAngle / 45f) % 8;
            return labels[idx];
        }

        public AttackDirection? GetAttemptedAttackDirection()
        {
            if (string.IsNullOrEmpty(LastDirectionKey) || attackable == null)
                return null;

            attackable.directions.TryGetValue(LastDirectionKey, out var dir);
            LastDirectionKey = null;

            return dir;
        }

        public void OnTriggerEnter(Collider collider)
        {
            if (collider.CompareTag("Attackable") && !inRangeForCombat)
            {
                inRangeForCombat = true;
                if (collider.TryGetComponent<Attackable>(out var attackable))
                    this.attackable = attackable;
            }
        }

        public void OnTriggerExit(Collider collider)
        {
            if (collider.CompareTag("Attackable"))
            {
                inRangeForCombat = false;
                this.attackable = null;
            }
        }
    }
}

