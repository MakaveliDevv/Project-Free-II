using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public class MovementInteraction
    {
        readonly MovementSettings settings;
        readonly float sliceDeg;

        Vector3 snappedDir3D = Vector3.zero;
        public Vector3 SnappedDir3D => snappedDir3D;

        public MovementInteraction(MovementSettings settings)
        {
            this.settings = settings;

            sliceDeg = 360f / settings.directionCount2;
        }

        Vector3 GetSnappedDirection(Vector2 input)
        {
            float mag2 = settings.minStickMagnitude * settings.minStickMagnitude;
            if (input.sqrMagnitude < mag2) return Vector3.zero;

            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            float snapped = Mathf.Round(angle / sliceDeg) * sliceDeg;
            return (Quaternion.Euler(0f, 0f, snapped) * Vector3.right).normalized;
        }

        public void Update()
        {
            snappedDir3D = GetSnappedDirection(InputManager.RightStickInput);
        }

        public BoxMover SelectTargetRay(Vector3 origin, float range)
        {
            if (snappedDir3D == Vector3.zero) return null;
            int mask = LayerMask.GetMask("Interactable");

            // gather all hits along the ray
            RaycastHit[] hits = Physics.RaycastAll(origin, snappedDir3D, range, mask);
            if (hits == null || hits.Length == 0) return null;

            // find the closest
            float bestDist = float.MaxValue;
            BoxMover best = null;
            foreach (var h in hits)
            {
                if (h.distance < bestDist)
                {
                    if (!h.collider.TryGetComponent<BoxMover>(out var bm)) continue;

                    if (h.distance < bestDist)
                    {
                        bestDist = h.distance;
                        best = bm;
                    }
                }
            }
            return best;
        }

        public void OnDrawGizmosRay(Vector3 origin, float range)
        {
#if UNITY_EDITOR
            if (snappedDir3D == Vector3.zero) return;

            // compute the tip of the ray
            Vector3 tip = origin + snappedDir3D * range;

            // draw the ray
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, tip);

            // draw wire sphere at tip
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(tip, 0.1f);

            // draw a disc at the tip of the ray
            Handles.color = Color.yellow;
            Handles.DrawSolidDisc(tip, snappedDir3D, 0.1f);
#endif
        }
    }
}
