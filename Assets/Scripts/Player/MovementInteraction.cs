using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public class MovementInteraction
    {
        private readonly Player player;
        private Interactable interactable;
        // private BoxMover bm; // Remove later
        private Collider interactableCol;
        private Vector3 snappedDir = Vector3.zero;

        public bool interacting = false;
        private bool isLaunching = false;

        private Coroutine pullCoroutine;
        private GameObject firstInteractable = null;

        public MovementInteraction(Player player)
        {
            this.player = player;
        }

        public void Update()
        {
            Debug.Log("Activated");
            snappedDir = InputManager.GetSnappedDirection(
                InputManager.RightStickInput,
                player.playerSettings.snapDirectionsEnabled,
                player.playerSettings.directionCountSelection
            );

            if (isLaunching) return;

            if (interacting)
            {
                // clamp Z to ground plane
                if (player.interactionSettings.groundBoundsCollider != null)
                {
                    var v = player.rb.position;
                    v.z = Mathf.Clamp(v.z,
                        player.interactionSettings.groundBoundsCollider.bounds.min.z,
                        player.interactionSettings.groundBoundsCollider.bounds.max.z);
                    player.rb.MovePosition(v);
                }

                // ray-based selection
                var best = SelectTargetRay(
                    player.rb.position,
                    player.interactionSettings.selectionRange);

                interactable.UpdateHighlight(best);

                if (InputManager.LeftTriggerPressed && !InputManager.TriggerLock && best != null)
                {
                    InputManager.TriggerLock = true;
                    if (pullCoroutine != null)
                    {
                        player.StopCoroutine(pullCoroutine);
                        pullCoroutine = null;
                    }
                    player.StartCoroutine(LaunchToBoxRoutine(best));
                    interactable.ClearHighlight();
                }
                else if (!InputManager.LeftTriggerPressed || InputManager.LeftTriggerReleased)
                {
                    InputManager.TriggerLock = false;
                }

                return;
            }

            if (firstInteractable != null)
            {
                // pull onto this box
                if (InputManager.NorthButtonPressed
                && Vector3.Distance(player.rb.position, firstInteractable.transform.position) <= player.interactionSettings.interactionRadius)
                    pullCoroutine = player.StartCoroutine(PullOntoBoxRoutine());
            }
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

        IEnumerator PullOntoBoxRoutine()
        {
            interacting = true;
            player.playerSettings.movementState = MovementState.Interacting;
            player.playerSettings.currentSurfaceState = SurfaceState.Nothing;

            Vector3 start = player.rb.position;
            float t = 0f;
            while (t < player.interactionSettings.pullDuration)
            {
                t += Time.deltaTime;
                float pct = Mathf.SmoothStep(0f, 1f, t / player.interactionSettings.pullDuration);
                var b = interactableCol.bounds;
                float h = player.col.bounds.extents.y;
                Vector3 top = new(b.center.x, b.max.y + h, b.center.z);
                player.rb.MovePosition(Vector3.Lerp(start, top, pct));
                yield return null;
            }
            // if (bm != null) bm.enabled = false;
            pullCoroutine = null;
        }

        IEnumerator LaunchToBoxRoutine(BoxMover target)
        {
            isLaunching = true;
            interacting = false;
            InputManager.TriggerLock = false;
            player.playerSettings.movementState = MovementState.Launching;

            Vector3 start = player.rb.position;
            float t = 0f;
            while (t < player.interactionSettings.launchDuration)
            {
                t += Time.deltaTime;
                float pct = Mathf.SmoothStep(0f, 1f, t / player.interactionSettings.launchDuration);
                var b = target.transform.GetChild(0).GetComponent<Collider>().bounds;
                float h = player.col.bounds.extents.y;
                Vector3 top = new(b.center.x, b.max.y + h, b.center.z);
                player.rb.MovePosition(Vector3.Lerp(start, top, pct));
                yield return null;
            }

            var bf = target.transform.GetChild(0).GetComponent<Collider>().bounds;
            float hf = player.col.bounds.extents.y;
            player.rb.MovePosition(
                new Vector3(bf.center.x, bf.max.y + hf, bf.center.z));

            if (target.TryGetComponent<BoxMover>(out var mv)) mv.enabled = false;

            interacting = true;
            player.playerSettings.movementState = MovementState.Interacting;
            player.playerSettings.currentSurfaceState = SurfaceState.Nothing;
            isLaunching = false;
        }

        public void OnTriggerEnter(Collider collider)
        {
            if ((player.playerSettings.movementState == MovementState.Hovering
            || player.playerSettings.movementState == MovementState.Jumping
            || player.playerSettings.movementState == MovementState.WallJump
            || player.playerSettings.movementState == MovementState.Descending)
            && collider.CompareTag("Interactable"))
            {
                firstInteractable = collider.gameObject;
                Debug.Log($"First Interactable => {firstInteractable} ");
            }

            if (collider.CompareTag("Interactable"))
            {
                Collider col = collider.transform.GetChild(0).GetComponent<Collider>();
                interactableCol = col;

                // BoxMover bm = collider.GetComponent<BoxMover>();
                // this.bm = bm;

                Interactable interactable = collider.GetComponent<Interactable>();
                this.interactable = interactable;
            }
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
