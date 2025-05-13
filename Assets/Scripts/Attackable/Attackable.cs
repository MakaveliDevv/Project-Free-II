using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Attackable : MonoBehaviour
{
    [SerializeField]
    private float centerResetDelay = 1f;

    [SerializeField] private float sitckMagnitudeThresh = .9f;

    [SerializeField]
    private MovementSystem movementSystem;

    private static readonly string[] labels = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };

    private string  startLabel = null;
    private bool awaitingReset = false;
    private float resetTimer = 0f;
    private string previousEndLabel = null;

    void Update()
    {
        Vector2 raw = Gamepad.current?.rightStick.ReadUnprocessedValue() ?? Vector2.zero;
        bool tilted = raw.magnitude > sitckMagnitudeThresh;
        string snapped= tilted ? SnapTo8Label(raw) : "";

        if (awaitingReset)
        {
            resetTimer += Time.deltaTime;
            if (resetTimer >= centerResetDelay)
            {
                Debug.Log($"[Attackable] ↺ Auto-reset after {centerResetDelay}s; ready for next swipe");
                awaitingReset = false;
                resetTimer = 0f;
            }
            return;
        }

        if (startLabel == null)
        {
            if (!tilted) { return; } 

            if (previousEndLabel != null && snapped == previousEndLabel) { return; }

            startLabel = snapped;
            previousEndLabel = null;  
            Debug.Log($"[Attackable] → START at {startLabel}");
            return;
        }

        if (!tilted) { return; } 

        if (snapped != startLabel)
        {
            int aIdx = Array.IndexOf(labels, startLabel);
            string opposite = labels[(aIdx + 4) % 8];
            bool success = snapped == opposite;

            if (success) { Debug.Log($"[Attackable] ✅ SUCCESS: {startLabel} → {snapped}"); }
            else { Debug.Log($"[Attackable] ❌ FAILURE: {startLabel} → {snapped} (expected {opposite})"); }

            previousEndLabel = snapped;
            startLabel = null;
            awaitingReset = true;
            resetTimer = 0f;
        }
    }

    private string SnapTo8Label(Vector2 raw)
    {
        if (raw.sqrMagnitude < movementSystem.minStickMagnitude * movementSystem.minStickMagnitude) { return ""; }

        float angle = (Mathf.Atan2(raw.y, raw.x) * Mathf.Rad2Deg + 360f) % 360f;
        float snapAngle = Mathf.Round(angle / 45f) * 45f;
        int idx = Mathf.RoundToInt(snapAngle / 45f) % 8;
        return labels[idx];
    }
}



