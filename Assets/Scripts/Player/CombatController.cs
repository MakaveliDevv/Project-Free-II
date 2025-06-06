// using System;
// using System.Collections;
// using UnityEngine;

// public class CombatController : MonoBehaviour
// {
//     [SerializeField, Tooltip("How long after a swipe to wait before allowing the next one")]
//     private float centerResetDelay = 1f;
//     [SerializeField, Tooltip("How long staying at A without moving before cancelling swipe")]
//     private float startHoldDelay = 1f;
//     [SerializeField, Tooltip("Stick magnitude threshold")]
//     private float stickMagnitudeThresh = .9f;

//     private static readonly string[] labels = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };

//     // Swipe state
//     private string startLabel = null;
//     private string previousEndLabel = null;
//     private bool awaitingReset = false;
//     private float resetTimer = 0f;
//     private float centerHoldTimer = 0f;
//     private float startHoldTimer = 0f;
//     private bool tilted = false;

//     // Combat collisions
//     public bool inRangeForCombat = false;
//     private Attackable attackable = null;

//     public string LastDirectionKey { get; private set; } = "";
//     public string EndDirection => previousEndLabel;

//     public static event Action<string, bool> OnSwipePerformed;

//     public bool attacked = false;
//     public bool success = false;

//     void Update()
//     {
//         InputManager.UpdateInput();
//         ProcessRightStickSwipe();
//     }

//     private void ProcessRightStickSwipe()
//     {
//         var raw = InputManager.RightStickInput;
//         tilted = raw.magnitude > stickMagnitudeThresh;
//         string snapped = tilted ? SnapTo8Label(raw) : "";

//         if (awaitingReset)
//         {
//             resetTimer += Time.deltaTime;
//             if (resetTimer >= centerResetDelay)
//             {
//                 Debug.Log($"[Attackable] \u21BA Auto-reset after {centerResetDelay}s; ready for next swipe");
//                 awaitingReset = false;
//                 resetTimer = 0f;
//             }
//             return;
//         }

//         if (startLabel == null)
//         {
//             if (!tilted) return;
//             if (previousEndLabel != null && snapped == previousEndLabel) return;

//             startLabel = snapped;
//             centerHoldTimer = 0f;
//             startHoldTimer = 0f;
//             previousEndLabel = null;

//             Debug.Log($"[Attackable] → START at {startLabel}");
//             return;
//         }

//         if (!tilted)
//         {
//             centerHoldTimer += Time.deltaTime;
//             if (centerHoldTimer >= centerResetDelay)
//             {
//                 Debug.Log($"[Attackable] ✗ Cancel swipe: held in center for {centerHoldTimer:F2}s");
//                 Reset();
//             }
//             return;
//         }
//         centerHoldTimer = 0f;

//         if (snapped == startLabel)
//         {
//             startHoldTimer += Time.deltaTime;
//             if (startHoldTimer >= startHoldDelay)
//             {
//                 Debug.Log($"[Attackable] ✗ Cancel swipe: held at A={startLabel} for {startHoldTimer:F2}s");
//                 Reset();
//             }
//             return;
//         }

//         startHoldTimer = 0f;

//         int aIdx = Array.IndexOf(labels, startLabel);
//         int oppositeIndex = (aIdx + 4) % labels.Length;
//         string opposite = labels[oppositeIndex];
//         bool pass = snapped == opposite;
//         string key = $"{startLabel}-{snapped}";

//         if (inRangeForCombat)
//         {
//             Debug.Log("In range for combat, start processing...");
//             StartCoroutine(Processing(key, pass));
//         }

//         if (pass)
//             Debug.Log($"[Attackable] ✅ SUCCESS: {startLabel} → {snapped}");
//         else
//             Debug.Log($"[Attackable] ❌ FAILURE: {startLabel} → {snapped} (expected {opposite})");

//         previousEndLabel = snapped;
//         LastDirectionKey = key;

//         Reset();
//         OnSwipePerformed?.Invoke(key, pass);
//     }

//     private IEnumerator Processing(string key, bool pass)
//     {
//         if (attackable == null) yield break;

//         attacked = true;

//         if (attackable.directions.TryGetValue(key, out var performed)
//             && performed == attackable.attackDirection && pass)
//         {
//             Debug.Log($"[Attackable] ▶️ SUCCESS {key} → {performed}");
//             success = true;
//         }
//         else
//         {
//             Debug.Log($"[Attackable] ▶️ ❌ FAILURE {key} → mapped " +
//                       $"{(attackable.directions.TryGetValue(key, out performed) ? performed : (Attackable.AttackDirection)(-1))}, " +
//                       $"expected {attackable.attackDirection}");
//         }
//     }

//     private void Reset()
//     {
//         startLabel = null;
//         previousEndLabel = null;
//         awaitingReset = true;
//         resetTimer = 0f;
//         centerHoldTimer = 0f;
//         startHoldTimer = 0f;
//     }

//     private string SnapTo8Label(Vector2 raw)
//     {
//         if (raw.sqrMagnitude < stickMagnitudeThresh * stickMagnitudeThresh) return "";

//         float angle = (Mathf.Atan2(raw.y, raw.x) * Mathf.Rad2Deg + 360f) % 360f;
//         float snapAngle = Mathf.Round(angle / 45f) * 45f;
//         int idx = Mathf.RoundToInt(snapAngle / 45f) % 8;
//         return labels[idx];
//     }

//     void OnTriggerEnter(Collider collider)
//     {
//         if (collider.CompareTag("Attackable") && !inRangeForCombat)
//         {
//             inRangeForCombat = true;
//             if (collider.TryGetComponent<Attackable>(out var attackable))
//                 this.attackable = attackable;
//         }
//     }

//     void OnTriggerExit(Collider collider)
//     {
//         if (collider.CompareTag("Attackable"))
//         {
//             inRangeForCombat = false;
//             this.attackable = null;
//         }
//     }
// }

using System;
using System.Collections;
using Assets.Scripts.Player;
using UnityEngine;

public class CombatController
{
    private readonly MonoBehaviour mono;
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

    public CombatController(MonoBehaviour mono, CombatSettings settings)
    {
        this.mono = mono;
        this.settings = settings;
    }
    
    public void Update()
    {
        InputManager.UpdateInput();
        ProcessRightStickSwipe();
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

        if (inRangeForCombat)
        {
            Debug.Log("In range for combat, start processing...");
            mono.StartCoroutine(Processing(key, pass));
        }

        if (pass)
            Debug.Log($"[Attackable] ✅ SUCCESS: {startLabel} → {snapped}");
        else
            Debug.Log($"[Attackable] ❌ FAILURE: {startLabel} → {snapped} (expected {opposite})");

        previousEndLabel = snapped;
        LastDirectionKey = key;

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
                      $"{(attackable.directions.TryGetValue(key, out performed) ? performed : (Attackable.AttackDirection)(-1))}, " +
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
