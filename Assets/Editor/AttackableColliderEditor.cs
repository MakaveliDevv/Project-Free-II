using UnityEngine;

public class AttackableColliderEditor : MonoBehaviour
{
    private BoxCollider col;
    public Vector3 colSize = Vector3.one;
    public Vector3 colOffset = Vector3.zero;

    void Awake()
    {
        col = GetComponent<BoxCollider>();
    }

    void OnValidate()
    {
        if (col == null)
            col = GetComponent<BoxCollider>();

        if (col != null)
        {
            col.isTrigger = false;
            col.size = colSize;
            col.center = colOffset;
        }
    }
}
