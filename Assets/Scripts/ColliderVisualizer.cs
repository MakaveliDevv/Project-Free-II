using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ColliderVisualizer : MonoBehaviour
{
    public Color gizmoColor = new Color(0, 1, 0, 0.5f); // Default to semi-transparent green
    private Collider col;

    void Awake()
    {
        col = GetComponent<Collider>();
    }

    void OnDrawGizmos()
    {
        if (col == null)
            col = GetComponent<Collider>();

        Gizmos.color = gizmoColor;

        if (col is BoxCollider box)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.bounds.center, sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z));
        }
        else if (col is CapsuleCollider capsule)
        {
            DrawFilledCapsule(capsule);
        }
        else if (col is MeshCollider mesh)
        {
            Gizmos.DrawMesh(mesh.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
        }
    }

    void DrawFilledCapsule(CapsuleCollider capsule)
    {
        Vector3 up = transform.up * (capsule.height / 2 - capsule.radius);
        Vector3 center = capsule.bounds.center;

        Gizmos.DrawSphere(center + up, capsule.radius);
        Gizmos.DrawSphere(center - up, capsule.radius);
    }
}
