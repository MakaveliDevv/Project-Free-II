using UnityEngine;
using System.Collections;
using Assets.Scripts.Player;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRadius = 2f;
    public float pullDuration = 0.2f;
    public Collider groundBoundsCollider;

    [Header("Selection Settings")]
    public float selectionRadius = 5f;
    public float minAimMagnitude = 0.2f;

    [Header("Launch Settings")]
    public float launchDuration = 0.3f;

    // refs
    private Player player;
    private Rigidbody playerRb;
    private Transform playerT;
    private BoxMover boxMover;
    private Collider col, pCol;

    // state
    private bool interacting = false;
    private bool isLaunching = false;
    private bool triggerLock = false;
    private float origZ, groundMinZ, groundMaxZ;
    private Coroutine pullCoroutine;

    // selection
    private BoxMover selectedBox;
    private Color selectedOriginalColor;

    void Awake()
    {
        var go = GameObject.FindWithTag("Player");
        player = go.GetComponent<Player>();
        playerRb = go.GetComponent<Rigidbody>();
        playerT = go.transform;
        boxMover = GetComponent<BoxMover>();
        col = GetComponent<Collider>();
        pCol = go.GetComponent<Collider>();

        if (groundBoundsCollider != null)
        {
            groundMinZ = groundBoundsCollider.bounds.min.z;
            groundMaxZ = groundBoundsCollider.bounds.max.z;
        }
    }

    void Update()
    {
        // block new input mid-flight
        if (isLaunching) return;

        // ── on this box: clamp Z, aim/select, leftTrigger to launch ──
        if (interacting)
        {
            // clamp to ground bounds on Z
            if (groundBoundsCollider != null)
            {
                var pos = playerRb.position;
                pos.z = Mathf.Clamp(pos.z, groundMinZ, groundMaxZ);
                playerRb.MovePosition(pos);
            }

            UpdateBoxSelection();

            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.leftTrigger.isPressed && !triggerLock && selectedBox != null)
                {
                    triggerLock = true;

                    // stop any ongoing pull
                    if (pullCoroutine != null)
                    {
                        StopCoroutine(pullCoroutine);
                        pullCoroutine = null;
                    }

                    var target = selectedBox;
                    StartCoroutine(LaunchToBoxRoutine(target));
                    ClearSelection();
                }
                else if (!gp.leftTrigger.isPressed)
                {
                    triggerLock = false;
                }
            }

            return;
        }

        // ── otherwise: north press + in‐range to pull onto this box ──
        var pad = Gamepad.current;
        if (pad != null
            && pad.buttonNorth.isPressed
            && Vector3.Distance(playerT.position, transform.position) <= interactionRadius)
        {
            pullCoroutine = StartCoroutine(PullOntoBoxRoutine());
        }
    }

    private IEnumerator PullOntoBoxRoutine()
    {
        interacting = true;
        origZ = playerRb.position.z;

        player.movementSettings.movementState = MovementState.Interacting;
        player.movementSettings.currentSurfaceState = SurfaceState.Nothing;

        Vector3 start = playerRb.position;
        float timer = 0f;

        while (timer < pullDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / pullDuration);

            var b = col.bounds;
            float h = pCol.bounds.extents.y;
            Vector3 top = new Vector3(b.center.x, b.max.y + h, b.center.z);

            playerRb.MovePosition(Vector3.Lerp(start, top, t));
            yield return null;
        }

        if (boxMover != null) boxMover.enabled = false;
        pullCoroutine = null;
    }

    private void UpdateBoxSelection()
    {
        var gp = Gamepad.current;
        if (gp == null) return;

        Vector2 aim = gp.rightStick.ReadUnprocessedValue();
        if (aim.sqrMagnitude < minAimMagnitude * minAimMagnitude)
        {
            ClearSelection();
            return;
        }

        Vector2 aimDir = aim.normalized;
        float bestDot = -1f;
        BoxMover best = null;
        Vector3 myPos = transform.position;

        foreach (var other in Object.FindObjectsByType<BoxMover>(FindObjectsSortMode.None))
        {
            if (other == boxMover) continue;
            Vector3 oPos = other.transform.position;

            if (oPos.z <= myPos.z) continue;
            if (groundBoundsCollider != null &&
               (oPos.z < groundMinZ || oPos.z > groundMaxZ)) continue;
            if ((oPos - myPos).sqrMagnitude > selectionRadius * selectionRadius) continue;

            Vector2 rel = new Vector2(oPos.x - myPos.x, oPos.z - myPos.z).normalized;
            float dot = Vector2.Dot(aimDir, rel);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = other;
            }
        }

        SetSelection(best);
    }

    private void SetSelection(BoxMover bm)
    {
        if (selectedBox == bm) return;
        ClearSelection();
        if (bm == null) return;
        selectedBox = bm;
        var rend = bm.GetComponent<Renderer>();
        if (rend != null)
        {
            selectedOriginalColor = rend.material.color;
            rend.material.color = Color.blue;
        }
    }

    private void ClearSelection()
    {
        if (selectedBox == null) return;
        var rend = selectedBox.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = selectedOriginalColor;
        selectedBox = null;
    }

    private IEnumerator LaunchToBoxRoutine(BoxMover target)
    {
        if (target == null)
        {
            isLaunching = false;
            yield break;
        }

        isLaunching = true;
        interacting = false;
        triggerLock = false;  // reset so you can trigger next time
        player.movementSettings.movementState = MovementState.Launching;

        Vector3 start = playerRb.position;
        float timer = 0f;

        while (timer < launchDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / launchDuration);

            var b = target.GetComponent<Collider>().bounds;
            float h = pCol.bounds.extents.y;
            Vector3 dynamicTop = new Vector3(b.center.x, b.max.y + h, b.center.z);

            playerRb.MovePosition(Vector3.Lerp(start, dynamicTop, t));
            yield return null;
        }

        // final snap to the moving top
        var bf = target.GetComponent<Collider>().bounds;
        float hf = pCol.bounds.extents.y;
        Vector3 end = new Vector3(bf.center.x, bf.max.y + hf, bf.center.z);
        playerRb.MovePosition(end);

        // stop the new box
        var mover = target.GetComponent<BoxMover>();
        if (mover != null) mover.enabled = false;

        // now landed: re-enter “on box” state so selection works again
        interacting = true;
        player.movementSettings.movementState = MovementState.Interacting;
        player.movementSettings.currentSurfaceState = SurfaceState.Nothing;
        isLaunching = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
        if (selectedBox != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(selectedBox.transform.position, 0.5f);
        }
    }
}