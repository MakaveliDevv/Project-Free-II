using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Attackable : MonoBehaviour
{
    public enum AttackDirection
    {
        TopToBottom,           // ↓
        BottomToTop,           // ↑
        LeftToRight,           // →
        RightToLeft,           // ←
        BottomLeftToTopRight,  // ↗
        TopRightToBottomLeft,  // ↙
        BottomRightToTopLeft,  // ↖
        TopLeftToBottomRight   // ↘
    }

    [Tooltip("Which swipe direction counts as a hit")]
    public AttackDirection attackDirection;

    [SerializeField, Tooltip("Provides dead-zone threshold")]
    private MovementSystem movementSystem;

    // Eight labels, clockwise from East
    private static readonly string[] labels = { "E","NE","N","NW","W","SW","S","SE" };

    // Map "A-B" → AttackDirection
    private static readonly System.Collections.Generic.Dictionary<string,AttackDirection> directions = new()
    {
        { "N-S", AttackDirection.TopToBottom },
        { "S-N", AttackDirection.BottomToTop },
        { "W-E", AttackDirection.LeftToRight },
        { "E-W", AttackDirection.RightToLeft },
        { "SW-NE", AttackDirection.BottomLeftToTopRight },
        { "NE-SW", AttackDirection.TopRightToBottomLeft },
        { "SE-NW", AttackDirection.BottomRightToTopLeft },
        { "NW-SE", AttackDirection.TopLeftToBottomRight },
    };

    // how many neighbor steps to forgive on either side of B
    [Tooltip("How many adjacent directions to accept as a valid end")]
    [Range(0,3)]
    public int forgivenessSteps = 1;

    private enum State { Idle, Tracking, AwaitingReset }
    private State currentState = State.Idle;

    private string startLabel, targetLabel;

    void Update()
    {
        Vector2 raw   = Gamepad.current?.rightStick.ReadUnprocessedValue() ?? Vector2.zero;
        bool tilted   = raw.magnitude > movementSystem.minStickMagnitude;
        Vector2 snap8 = SnapTo8(raw);

        switch (currentState)
        {
            case State.Idle:
                if (tilted)
                {
                    // record A and compute exact opposite B
                    startLabel  = LabelFromVector(snap8);
                    int idx     = Array.IndexOf(labels, startLabel);
                    targetLabel = labels[(idx + 4) % 8];
                    Debug.Log($"[Attackable] → START. A={startLabel}, exact B={targetLabel}");
                    currentState = State.Tracking;
                }
                break;

            case State.Tracking:
                if (tilted)
                {
                    string current = LabelFromVector(snap8);
                    int startIdx   = Array.IndexOf(labels, startLabel);
                    int targetIdx  = Array.IndexOf(labels, targetLabel);
                    int currIdx    = Array.IndexOf(labels, current);

                    // build forgiven set around targetIdx
                    bool inForgiveZone = false;
                    for (int off = -forgivenessSteps; off <= forgivenessSteps; off++)
                    {
                        int test = (targetIdx + off + 8) % 8;
                        if (currIdx == test)
                        {
                            inForgiveZone = true;
                            break;
                        }
                    }

                    if (inForgiveZone)
                    {
                        Debug.Log($"[Attackable] ✓ Hit forgiveness zone: {current} ≈ {targetLabel}");
                        EvaluateSwipe(pass: true);
                    }
                    else if (currIdx != startIdx)
                    {
                        Debug.Log($"[Attackable] ✗ Moved to invalid C={current}");
                        EvaluateSwipe(pass: false);
                    }
                    // else still at A → keep waiting
                }
                else
                {
                    // released to center before reaching any B-zone → cancel
                    Debug.Log("[Attackable] ✗ Released to center mid-swipe; cancel");
                    EvaluateSwipe(pass: false);
                }
                break;

            case State.AwaitingReset:
                if (!tilted)
                {
                    Debug.Log("[Attackable] ↺ Reset complete; ready for next swipe");
                    currentState = State.Idle;
                }
                break;
        }
    }

    private void EvaluateSwipe(bool pass)
    {
        string key = $"{startLabel}-{targetLabel}";

        if (pass)
        {
            if (directions.TryGetValue(key, out var performed) && performed == attackDirection)
                Debug.Log($"[Attackable] ▶️ SUCCESS {key} → {performed}");
            else
                Debug.Log($"[Attackable] ❌ FAILURE {key} → mapped {(directions.TryGetValue(key, out performed)? performed : (AttackDirection)(-1))}, expected {attackDirection}");
        }
        else
        {
            Debug.Log($"[Attackable] ❌ FAILURE {key} (did not reach correct B-zone)");
        }

        currentState = State.AwaitingReset;
    }

    private Vector2 SnapTo8(Vector2 v)
    {
        if (v.sqrMagnitude < movementSystem.minStickMagnitude * movementSystem.minStickMagnitude)
            return Vector2.zero;
        float angle = (Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + 360f) % 360f;
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        float rad = snappedAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private string LabelFromVector(Vector2 v)
    {
        if (v == Vector2.zero) return "";
        float angle = (Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + 360f) % 360f;
        int idx = Mathf.RoundToInt(angle / 45f) % 8;
        return labels[idx];
    }
}
