using System;
using System.Collections;
using UnityEngine;

[System.Serializable]
public class JumpDashSystemParam 
{
    [Header("Jump Settings")]
    public float maxJumpRange = 7f;
    public float maxHoldTime = 1f;

    [Header("Dash Settings")]
    public float maxDashRange = 10f;
    public float dashForceMultiplier = 1f;
    public float stopDashAfterDuration = .5f;

    [Header("Physics")]
    public float jumpForceMultiplier = 1f;
    public float epsilon = 0.1f;
    public float clampMagnitudeMaxLength = 20f;

    [Header("Hover Settings")]
    public float hoverDelay = 0.1f;
    public float hoverDuration = 0.5f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.1f;

    [Header("Gravity Settings")]
    public float gravityScale = 1f;
    public float fastFallMultiplier = 2f;

    [Header("Direction Settings")]
    public float horizontalLeftAngleThreshold = 150f;
    public float horizontalRightAngleThreshold = 30f;

    public float wallCheckDistance = .25f;
    public LayerMask wallLayer;
}

public static class DelayedCall
{
    public static void Invoke(MonoBehaviour caller, Action action, float delay)
    {
        caller.StartCoroutine(DelayCoroutine(action, delay));
    }

    private static IEnumerator DelayCoroutine(Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}
