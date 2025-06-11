using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public class MovementInteraction
    {
        readonly PlayerSettings playerSettings;

        Vector3 snappedDir = Vector3.zero;
        public Vector3 SnappedDir3D => snappedDir;

        public MovementInteraction(PlayerSettings playerSettings)
        {
            this.playerSettings = playerSettings;
        }

        public void Update()
        {
            snappedDir = InputManager.GetSnappedDirection(InputManager.RightStickInput, playerSettings.snapDirectionsEnabled, playerSettings.directionCountSelection);
        }

        public BoxMover SelectTargetRay(Vector3 origin, float range)
        {
            if (snappedDir == Vector3.zero) return null;
            int mask = LayerMask.GetMask("Interactable");

            // gather all hits along the ray
            RaycastHit[] hits = Physics.RaycastAll(origin, snappedDir, range, mask);
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
            if (snappedDir == Vector3.zero) return;

            // compute the tip of the ray
            Vector3 tip = origin + snappedDir * range;

            // draw the ray
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, tip);

            // draw wire sphere at tip
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(tip, 0.1f);

            // draw a disc at the tip of the ray
            Handles.color = Color.yellow;
            Handles.DrawSolidDisc(tip, snappedDir, 0.1f);
#endif
        }
    }
}
