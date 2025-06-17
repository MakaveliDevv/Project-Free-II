using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public class MovementInteraction
    {
        public Interactable interactable;
        private readonly Player player;

        private GameObject firstInteractable = null;
        private Collider boxCol;
        private Coroutine pullCoroutine;

        private Vector3 snappedDir = Vector3.zero;
        private Vector3 top = Vector3.zero;

        public bool interacting = false;
        private bool isLaunching = false;

        public MovementInteraction(Player player)
        {
            this.player = player;
        }

        public void Update()
        {
            player.interacting = interacting;
            snappedDir = InputManager.GetSnappedDirection(
                InputManager.RightStickInput,
                player.playerSettings.snapDirectionsEnabled,
                player.playerSettings.directionCountSelection
            );

            if (isLaunching) return;

            if (interacting)
            {
                HandleWhileInteracting();
                return;
            }

            TryPullOntoFirstInteractable();
        }

        // ──────────────────────────────
        // Interaction Flow
        // ──────────────────────────────
        private void HandleWhileInteracting()
        {
            ClampZToGroundPlane();

            var best = SelectTargetRay(player.rb.position, player.interactionSettings.selectionRange);
            interactable.UpdateHighlight(best);

            if (InputManager.LeftTriggerPressed && !InputManager.TriggerLock && best != null)
            {
                InputManager.TriggerLock = true;
                StopPullIfActive();
                player.StartCoroutine(LaunchToBoxRoutine(best));
                interactable.ClearHighlight();
            }
            else if (!InputManager.LeftTriggerPressed || InputManager.LeftTriggerReleased)
            {
                InputManager.TriggerLock = false;
            }
        }

        private void TryPullOntoFirstInteractable()
        {
            if (firstInteractable != null && InputManager.NorthButtonPressed)
            {
                float distance = Vector3.Distance(player.rb.position, firstInteractable.transform.position);
                if (distance <= player.interactionSettings.interactionRadius)
                {
                    pullCoroutine = player.StartCoroutine(PullOntoBoxRoutine());
                    Debug.Log("Pulling..");
                }
            }
        }

        // ──────────────────────────────
        // Coroutine: Launch
        // ──────────────────────────────
        private IEnumerator LaunchToBoxRoutine(Interactable target)
        {
            Interactable current = interactable;
            InputManager.TriggerLock = false;

            isLaunching = true;
            interacting = false;
            player.playerSettings.movementState = MovementState.Launching;

            Vector3 start = player.rb.position;
            Vector3 end = GetBoxTopPosition(target);
            float duration = player.interactionSettings.launchDuration;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float pct = Mathf.SmoothStep(0f, 1f, t / duration);
                player.rb.MovePosition(Vector3.Lerp(start, end, pct));
                yield return null;
            }

            player.rb.MovePosition(end);
            yield return null;

            if (current != null)
            {
                current.isMarkedForDestruction = true;
                current.enabled = false;
            }

            interacting = true;
            isLaunching = false;
            player.playerSettings.movementState = MovementState.Interacting;
            player.playerSettings.currentSurfaceState = SurfaceState.Nothing;

            if (TryGetBoxCollider(target, out var destinationCol))
            {
                boxCol = destinationCol;
                boxCol.enabled = true;

                if (interactable != target)
                {
                    Debug.LogWarning("⚠️ Mismatch: current interactable isn't the launch target.");
                    yield break;
                }
            }

            if (current != null && current.isMarkedForDestruction)
            {
                player.StartCoroutine(DestroyAfterDelay(current.gameObject, 0.2f, current.name));
            }
        }

        // ──────────────────────────────
        // Coroutine: Pull
        // ──────────────────────────────
        private IEnumerator PullOntoBoxRoutine()
        {
            interacting = true;
            player.playerSettings.movementState = MovementState.Interacting;
            player.playerSettings.currentSurfaceState = SurfaceState.Nothing;

            yield return null;

            if (boxCol != null && boxCol.gameObject != null && !interactable.isMarkedForDestruction)
            {
                boxCol.enabled = true;
            }

            Vector3 start = player.rb.position;
            float t = 0f;
            float duration = player.interactionSettings.pullDuration;

            while (t < duration)
            {
                t += Time.deltaTime;
                float pct = Mathf.SmoothStep(0f, 1f, t / duration);
                var bounds = boxCol.bounds;
                float h = player.col.bounds.extents.y;
                top = new(bounds.center.x, bounds.max.y + h, bounds.center.z);
                player.rb.MovePosition(Vector3.Lerp(start, top, pct));
                yield return null;
            }

            pullCoroutine = null;
        }

        // ──────────────────────────────
        // Utilities
        // ──────────────────────────────
        public Interactable SelectTargetRay(Vector3 origin, float range)
        {
            if (snappedDir == Vector3.zero) return null;
            int mask = LayerMask.GetMask("SelectCol");

            RaycastHit[] hits = Physics.RaycastAll(origin, snappedDir, range, mask);
            if (hits == null || hits.Length == 0) return null;

            float bestDist = float.MaxValue;
            Interactable best = null;

            foreach (var h in hits)
            {
                var parent = h.collider.transform.parent;
                if (!parent.TryGetComponent<Interactable>(out var i)) continue;
                if (i == interactable) continue;

                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    best = i;
                }
            }

            return best;
        }

        private void ClampZToGroundPlane()
        {
            if (player.interactionSettings.groundBoundsCollider == null) return;

            var pos = player.rb.position;
            pos.z = Mathf.Clamp(
                pos.z,
                player.interactionSettings.groundBoundsCollider.bounds.min.z,
                player.interactionSettings.groundBoundsCollider.bounds.max.z
            );
            player.rb.MovePosition(pos);
        }

        private void StopPullIfActive()
        {
            if (pullCoroutine != null)
            {
                player.StopCoroutine(pullCoroutine);
                pullCoroutine = null;
            }
        }

        public void TrySetInteractable(Interactable candidate)
        {
            if (!interacting || player.playerSettings.movementState == MovementState.Launching)
            {
                interactable = candidate;
                boxCol = candidate.transform.GetChild(1).GetComponent<Collider>();
            }
        }

        private Vector3 GetBoxTopPosition(Interactable box)
        {
            var bounds = box.transform.GetChild(1).GetComponent<Collider>().bounds;
            float playerHeight = player.col.bounds.extents.y;
            return new Vector3(bounds.center.x, bounds.max.y + playerHeight, bounds.center.z);
        }

        private bool TryGetBoxCollider(Interactable box, out Collider col)
        {
            col = box.transform.GetChild(1).GetComponent<Collider>();
            return col != null;
        }

        private IEnumerator DestroyAfterDelay(GameObject obj, float delay, string originalName = "")
        {
            yield return new WaitForSeconds(delay);
            Debug.Log($"Destroying: {originalName}, current is: {interactable.gameObject.name}");
            Object.Destroy(obj);
            Debug.Log("interactable destroyed");
        }

        // ──────────────────────────────
        // Unity Event Hooks
        // ──────────────────────────────
        public void OnTriggerEnter(Collider collider)
        {
            if (!collider.CompareTag("Interactable")) return;

            if (player.playerSettings.movementState is MovementState.Hovering or MovementState.Jumping
                or MovementState.WallJump or MovementState.Descending)
            {
                firstInteractable = collider.gameObject;
            }

            var candidate = collider.GetComponent<Interactable>();
            TrySetInteractable(candidate);
        }

        public void OnTriggerExit(Collider collider)
        {

        }

        public void OnDrawGizmosRay(Vector3 origin, float range)
        {
#if UNITY_EDITOR
            if (snappedDir == Vector3.zero) return;

            Vector3 tip = origin + snappedDir * range;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, tip);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(tip, 0.1f);

            Handles.color = Color.yellow;
            Handles.DrawSolidDisc(tip, snappedDir, 0.1f);
#endif
        }
    }
}
