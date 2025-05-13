using System.Collections.Generic;
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
    public AttackDirection attackDirection;

    // map "startLabel-endLabel" → enum
    private readonly Dictionary<string, AttackDirection> directions = new()
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

    [Tooltip("Drag in your player’s MovementSystem here so we can reuse its snapping logic")]
    [SerializeField] private MovementSystem movementSystem;

    // attack‐tracking state
    private bool  isTrackingAttack = false;
    private string startLabel;
    private string endLabel;

    void Update()
    {
        // 1) read raw stick
        Vector2 stick = Gamepad.current?.rightStick.ReadUnprocessedValue() ?? Vector2.zero;
        bool  tilted = stick.magnitude > movementSystem.minStickMagnitude;

        // 2) if not yet tracking, wait for first tilt → that’s your start
        if (!isTrackingAttack)
        {
            if (tilted)
            {
                isTrackingAttack = true;
                startLabel = movementSystem.GetClosestDirectionLabel(stick);
                endLabel = startLabel; // initialize
            }
        }
        else
        {
            // 3) while still tilted, keep updating the “end” label
            if (tilted)
            {
                endLabel = movementSystem.GetClosestDirectionLabel(stick);
            }
            else
            {
                // 4) user let go → evaluate
                string key = $"{startLabel}-{endLabel}";
                if (directions.TryGetValue(key, out var performed))
                {
                    if (performed == attackDirection)
                        OnAttackSuccess();
                    else
                        OnAttackFailure();
                }
                else
                {
                    Debug.LogWarning($"Unmapped swipe: {key}");
                }

                // reset for next swipe
                isTrackingAttack = false;
                startLabel = endLabel = null;
            }
        }
    }

    private void OnAttackSuccess()
    {
        Debug.Log("Attack hit!");
        // ... your success logic here …
    }

    private void OnAttackFailure()
    {
        Debug.Log("Attack missed.");
        // ... your failure logic here …
    }
}
