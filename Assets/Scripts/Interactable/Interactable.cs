using UnityEngine;
using System.Collections;
using Assets.Scripts.Player;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour
{
    Player player;
    Rigidbody playerRb;
    BoxMover boxMover;
    Collider col, pCol;
    // MovementInteraction moverInt;

    bool interacting = false;
    bool isLaunching = false;
    bool triggerLock = false;
    Coroutine pullCoroutine;

    BoxMover selectedBox;
    Color selectedOriginalColor;

    void Awake()
    {
        var go = GameObject.FindWithTag("Player");
        player = go.GetComponent<Player>();
        playerRb = go.GetComponent<Rigidbody>();
        boxMover = GetComponent<BoxMover>();
        col = transform.GetChild(0).GetComponent<Collider>();
        Debug.Log($"Col => {col.gameObject.name}");
        pCol = go.GetComponent<Collider>();
    }

    void Update()
    {
        if (isLaunching) return;

        if (interacting)
        {
            // clamp Z to ground plane
            if (player.interactionSettings.groundBoundsCollider != null)
            {
                var v = playerRb.position;
                v.z = Mathf.Clamp(v.z,
                    player.interactionSettings.groundBoundsCollider.bounds.min.z,
                    player.interactionSettings.groundBoundsCollider.bounds.max.z);
                playerRb.MovePosition(v);
            }

            // ray-based selection
            var best = player.moveInt.SelectTargetRay(
                playerRb.position,
                player.interactionSettings.selectionRange);

            UpdateHighlight(best);

            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.leftTrigger.isPressed && !triggerLock && selectedBox != null)
                {
                    triggerLock = true;
                    if (pullCoroutine != null)
                    {
                        StopCoroutine(pullCoroutine);
                        pullCoroutine = null;
                    }
                    StartCoroutine(LaunchToBoxRoutine(selectedBox));
                    ClearHighlight();
                }
                else if (!gp.leftTrigger.isPressed)
                {
                    triggerLock = false;
                }
            }
            return;
        }

        // pull onto this box
        var pad = Gamepad.current;
        if (pad != null
            && pad.buttonNorth.isPressed
            && Vector3.Distance(playerRb.position, transform.position) <= player.interactionSettings.interactionRadius)
        {
            pullCoroutine = StartCoroutine(PullOntoBoxRoutine());
        }
    }

    IEnumerator PullOntoBoxRoutine()
    {
        interacting = true;
        player.movementSettings.movementState = MovementState.Interacting;
        player.movementSettings.currentSurfaceState = SurfaceState.Nothing;

        Vector3 start = playerRb.position;
        float t = 0f;
        while (t < player.interactionSettings.pullDuration)
        {
            t += Time.deltaTime;
            float pct = Mathf.SmoothStep(0f, 1f, t / player.interactionSettings.pullDuration);
            var b = col.bounds;
            float h = pCol.bounds.extents.y;
            Vector3 top = new Vector3(b.center.x, b.max.y + h, b.center.z);
            playerRb.MovePosition(Vector3.Lerp(start, top, pct));
            yield return null;
        }
        if (boxMover != null) boxMover.enabled = false;
        pullCoroutine = null;
    }

    IEnumerator LaunchToBoxRoutine(BoxMover target)
    {
        isLaunching = true;
        interacting = false;
        triggerLock = false;
        player.movementSettings.movementState = MovementState.Launching;

        Vector3 start = playerRb.position;
        float t = 0f;
        while (t < player.interactionSettings.launchDuration)
        {
            t += Time.deltaTime;
            float pct = Mathf.SmoothStep(0f, 1f, t / player.interactionSettings.launchDuration);
            var b = target.transform.GetChild(0).GetComponent<Collider>().bounds;
            float h = pCol.bounds.extents.y;
            Vector3 top = new Vector3(b.center.x, b.max.y + h, b.center.z);
            playerRb.MovePosition(Vector3.Lerp(start, top, pct));
            yield return null;
        }

        var bf = target.transform.GetChild(0).GetComponent<Collider>().bounds;
        float hf = pCol.bounds.extents.y;
        playerRb.MovePosition(
            new Vector3(bf.center.x, bf.max.y + hf, bf.center.z));

        var mv = target.GetComponent<BoxMover>();
        if (mv != null) mv.enabled = false;

        interacting = true;
        player.movementSettings.movementState = MovementState.Interacting;
        player.movementSettings.currentSurfaceState = SurfaceState.Nothing;
        isLaunching = false;
    }

    void UpdateHighlight(BoxMover bm)
    {
        if (selectedBox == bm) return;
        ClearHighlight();
        if (bm == null) return;
        selectedBox = bm;
        if (bm.transform.GetChild(0).TryGetComponent<Renderer>(out var r))
        {
            selectedOriginalColor = r.material.color;
            r.material.color = Color.blue;
        }
    }

    void ClearHighlight()
    {
        if (selectedBox == null) return;
        if (selectedBox.transform.GetChild(0).TryGetComponent<Renderer>(out var r)) r.material.color = selectedOriginalColor;
        selectedBox = null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, player.interactionSettings.interactionRadius);
    }
}
