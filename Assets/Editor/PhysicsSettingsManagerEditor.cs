using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(PhysicsSettingsManager))]
public class PhysicsSettingsManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PhysicsSettingsManager manager = (PhysicsSettingsManager)target;

        if (GUILayout.Button("Fetch All Rigidbodies In Scene"))
        {
            manager.selectedRigidbodies.Clear();

            Rigidbody[] allRBs = FindObjectsOfType<Rigidbody>();
            foreach (var rb in allRBs)
            {
                manager.selectedRigidbodies.Add(rb);
            }

            Debug.Log($"🔍 Found {manager.selectedRigidbodies.Count} rigidbodies in the scene.");
        }

        if (GUILayout.Button("Clear Rigidbody List"))
        {
            manager.selectedRigidbodies.Clear();
        }

        if (GUILayout.Button("Apply Settings To Selected"))
        {
            manager.ApplySettings();
        }
    }
}
