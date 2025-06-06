using UnityEngine;

public class PlayerFollowCamera : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform player; // Drag your player GameObject here

    [Header("Camera Offset Settings")]
    public Vector3 offset = new Vector3(0f, 5f, -10f); // Default offset

    [Header("Smooth Follow Settings")]
    public float smoothSpeed = 0.125f; // How smooth the camera follows

    private void LateUpdate()
    {
        if (player == null) return;

        // Desired position is player's position + offset
        Vector3 desiredPosition = player.position + offset;

        // Smoothly interpolate between current and desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Apply the smoothed position
        transform.position = smoothedPosition;

        // Optional: Look at the player
        transform.LookAt(player);
    }
}
