
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class VFXTrailBinder : MonoBehaviour
{
    public VisualEffect trailVFX;

    void Update()
    {
        if (trailVFX != null)
        {
            Vector3 size = transform.localScale;
            trailVFX.SetVector3("TrailSize", size);
            trailVFX.SetVector3("ObjectPosition", transform.position);
        }
    }
}
