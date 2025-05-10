using UnityEngine;
using System.Collections.Generic;

public class PhysicsSettingsManager : MonoBehaviour
{
    [Header("Solver Settings")]
    public int solverIterationCount = 12;
    public int solverVelocityIterationCount = 12;

    [Header("Apply To Selected Rigidbodies")]
    public List<Rigidbody> selectedRigidbodies = new();

    [Header("Rigidbody Config")]
    public CollisionDetectionMode collisionMode = CollisionDetectionMode.ContinuousDynamic;
    public RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate;

    [ContextMenu("Apply Settings To Selected")]
    public void ApplySettings()
    {
        Physics.defaultSolverIterations = solverIterationCount;
        Physics.defaultSolverVelocityIterations = solverVelocityIterationCount;

        foreach (var rb in selectedRigidbodies)
        {
            if (rb != null)
            {
                rb.collisionDetectionMode = collisionMode;
                rb.interpolation = interpolation;
            }
        }

        Debug.Log($"✅ Applied physics settings to {selectedRigidbodies.Count} selected rigidbodies.");
    }
}
