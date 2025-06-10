// using UnityEngine;
// using System.Collections;
// using Assets.Scripts.Player;
// using UnityEngine.InputSystem;

// public class InteractableTest : MonoBehaviour
// {
//     [Header("Interaction Settings")]
//     public float interactionRadius = 2f;
//     public float pullDuration = 0.2f;
//     public Collider groundBoundsCollider;

//     [Header("Selection Settings")]
//     public float selectionRange = 5f;
//     public float selectionConeRadius = 0.5f;

//     [Header("Launch Settings")]
//     public float launchDuration = 0.3f;

//     Player player;
//     Rigidbody playerRb;
//     BoxMover boxMover;
//     Collider col, pCol;

//     bool interacting = false;
//     bool isLaunching = false;
//     bool triggerLock = false;
//     Coroutine pullCoroutine;

//     BoxMover selectedBox;
//     Color selectedOriginalColor;

//     void Awake()
//     {
//         var go = GameObject.FindWithTag("Player");
//         player = go.GetComponent<Player>();
//         playerRb = go.GetComponent<Rigidbody>();
//         boxMover = GetComponent<BoxMover>();
//         col = GetComponent<Collider>();
//         pCol = go.GetComponent<Collider>();
//     }

//     void Update()
//     {
//         if (isLaunching) return;

//         if (interacting)
//         {
//             // clamp Z to ground
//             if (groundBoundsCollider != null)
//             {
//                 var v = playerRb.position;
//                 v.z = Mathf.Clamp(v.z,
//                     groundBoundsCollider.bounds.min.z,
//                     groundBoundsCollider.bounds.max.z);
//                 playerRb.MovePosition(v);
//             }

//             var best = moverInt.SelectTargetCone(
//                 playerRb.position,
//                 selectionRange,
//                 selectionConeRadius,
//                 groundBoundsCollider);

//             UpdateHighlight(best);

//             var gp = Gamepad.current;
//             if (gp != null)
//             {
//                 if (gp.leftTrigger.isPressed && !triggerLock && selectedBox != null)
//                 {
//                     triggerLock = true;
//                     if (pullCoroutine != null)
//                     {
//                         StopCoroutine(pullCoroutine);
//                         pullCoroutine = null;
//                     }
//                     StartCoroutine(LaunchToBoxRoutine(selectedBox));
//                     ClearHighlight();
//                 }
//                 else if (!gp.leftTrigger.isPressed)
//                 {
//                     triggerLock = false;
//                 }
//             }
//             return;
//         }

//         var pad = Gamepad.current;
//         if (pad != null
//             && pad.buttonNorth.isPressed
//             && Vector3.Distance(playerRb.position, transform.position) <= interactionRadius)
//         {
//             pullCoroutine = StartCoroutine(PullOntoBoxRoutine());
//         }
//     }

//     IEnumerator PullOntoBoxRoutine()
//     {
//         interacting = true;
//         player.movementSettings.movementState = MovementState.Interacting;
//         player.movementSettings.currentSurfaceState = SurfaceState.Nothing;

//         Vector3 start = playerRb.position;
//         float t = 0f;
//         while (t < pullDuration)
//         {
//             t += Time.deltaTime;
//             float pct = Mathf.SmoothStep(0f, 1f, t / pullDuration);
//             var b = col.bounds;
//             float h = pCol.bounds.extents.y;
//             Vector3 top = new Vector3(b.center.x, b.max.y + h, b.center.z);
//             playerRb.MovePosition(Vector3.Lerp(start, top, pct));
//             yield return null;
//         }
//         if (boxMover != null) boxMover.enabled = false;
//         pullCoroutine = null;
//     }

//     IEnumerator LaunchToBoxRoutine(BoxMover target)
//     {
//         isLaunching = true;
//         interacting = false;
//         triggerLock = false;
//         player.movementSettings.movementState = MovementState.Launching;

//         Vector3 start = playerRb.position;
//         float t = 0f;
//         while (t < launchDuration)
//         {
//             t += Time.deltaTime;
//             float pct = Mathf.SmoothStep(0f, 1f, t / launchDuration);
//             var b = target.GetComponent<Collider>().bounds;
//             float h = pCol.bounds.extents.y;
//             Vector3 top = new Vector3(b.center.x, b.max.y + h, b.center.z);
//             playerRb.MovePosition(Vector3.Lerp(start, top, pct));
//             yield return null;
//         }

//         var bf = target.GetComponent<Collider>().bounds;
//         float hf = pCol.bounds.extents.y;
//         playerRb.MovePosition(
//             new Vector3(bf.center.x, bf.max.y + hf, bf.center.z));

//         var mv = target.GetComponent<BoxMover>();
//         if (mv != null) mv.enabled = false;

//         interacting = true;
//         player.movementSettings.movementState = MovementState.Interacting;
//         player.movementSettings.currentSurfaceState = SurfaceState.Nothing;
//         isLaunching = false;
//     }

//     void UpdateHighlight(BoxMover bm)
//     {
//         if (selectedBox == bm) return;
//         ClearHighlight();
//         if (bm == null) return;
//         selectedBox = bm;
//         var r = bm.GetComponent<Renderer>();
//         if (r != null)
//         {
//             selectedOriginalColor = r.material.color;
//             r.material.color = Color.blue;
//         }
//     }

//     void ClearHighlight()
//     {
//         if (selectedBox == null) return;
//         var r = selectedBox.GetComponent<Renderer>();
//         if (r != null) r.material.color = selectedOriginalColor;
//         selectedBox = null;
//     }

//     void OnDrawGizmos()
//     {
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireSphere(transform.position, interactionRadius);
//     }
// }

// using UnityEngine;
// using System.Collections;
// using Assets.Scripts.Player;
// using UnityEngine.InputSystem;

// public class InteractableTest : MonoBehaviour
// {
//     // refs
//     private Player player;
//     private Rigidbody playerRb;
//     private BoxMover boxMover;
//     private Collider col, pCol;

//     // state
//     private bool interacting = false;
//     private bool isLaunching = false;
//     private bool triggerLock = false;
//     private Coroutine pullCoroutine;

//     // highlight
//     private BoxMover selectedBox;
//     private Color selectedOriginalColor;

//     void Awake()
//     {
//         var go = GameObject.FindWithTag("Player");
//         player = go.GetComponent<Player>();
//         playerRb = go.GetComponent<Rigidbody>();
//         boxMover = GetComponent<BoxMover>();
//         col = GetComponent<Collider>();
//         pCol = go.GetComponent<Collider>();
//     }

//     void Update()
//     {
//         if (isLaunching) return;

//         // when on this box: auto-select & launch 
//         if (interacting)
//         {
//             // clamp Z to ground plane
//             if (player.intSettings.groundBoundsCollider != null)
//             {
//                 var v = playerRb.position;
//                 v.z = Mathf.Clamp(v.z,
//                     player.intSettings.groundBoundsCollider.bounds.min.z,
//                     player.intSettings.groundBoundsCollider.bounds.max.z);
//                 playerRb.MovePosition(v);
//             }

//             // perform overlap-box in front of player
//             Vector3 origin = playerRb.position;
//             Quaternion orient = player.transform.rotation;
//             Vector3 halfExt = player.intSettings.selectionBoxSize * 0.5f;
//             Vector3 center = origin + player.transform.forward * halfExt.z;

//             Collider[] hits = Physics.OverlapBox(center, halfExt, orient);
//             float stickX = Gamepad.current?.rightStick.x.ReadValue() ?? 0f;

//             // if stick X in deadzone, clear selection
//             if (Mathf.Abs(stickX) < .9f)
//             {
//                 ClearHighlight();
//             }
//             else
//             {
//                 int side = stickX > 0f ? 1 : -1;
//                 BoxMover best = null;
//                 float bestZ = float.MaxValue;

//                 foreach (var c in hits)
//                 {
//                     var bm = c.GetComponent<BoxMover>();
//                     if (bm == null) continue;

//                     // compute local pos relative to player
//                     Vector3 local = player.transform.InverseTransformPoint(bm.transform.position);
//                     // must be ahead
//                     if (local.z <= 0f) continue;
//                     // must be on chosen side
//                     if (Mathf.Sign(local.x) != side) continue;
//                     // within ground Z bounds?
//                     if (player.intSettings.groundBoundsCollider != null)
//                     {
//                         float gzMin = player.intSettings.groundBoundsCollider.bounds.min.z - origin.z;
//                         float gzMax = player.intSettings.groundBoundsCollider.bounds.max.z - origin.z;
//                         if (local.z < gzMin || local.z > gzMax) continue;
//                     }

//                     // pick nearest in Z
//                     if (local.z < bestZ)
//                     {
//                         bestZ = local.z;
//                         best = bm;
//                     }
//                 }

//                 UpdateHighlight(best);
//             }

//             var gp = Gamepad.current;
//             if (gp != null)
//             {
//                 if (gp.leftTrigger.isPressed && !triggerLock && selectedBox != null)
//                 {
//                     triggerLock = true;
//                     if (pullCoroutine != null)
//                     {
//                         StopCoroutine(pullCoroutine);
//                         pullCoroutine = null;
//                     }
//                     StartCoroutine(LaunchToBoxRoutine(selectedBox));
//                     ClearHighlight();
//                 }
//                 else if (!gp.leftTrigger.isPressed)
//                 {
//                     triggerLock = false;
//                 }
//             }

//             return;
//         }

//         var pad = Gamepad.current;
//         if (pad != null
//             && pad.buttonNorth.isPressed
//             && Vector3.Distance(playerRb.position, transform.position) <= player.intSettings.interactionRadius)
//         {
//             pullCoroutine = StartCoroutine(PullOntoBoxRoutine());
//         }
//     }

//     private IEnumerator PullOntoBoxRoutine()
//     {
//         interacting = true;
//         player.movementSettings.movementState = MovementState.Interacting;
//         player.movementSettings.currentSurfaceState = SurfaceState.Nothing;

//         Vector3 start = playerRb.position;
//         float t = 0f;
//         while (t < player.intSettings.pullDuration)
//         {
//             t += Time.deltaTime;
//             float pct = Mathf.SmoothStep(0f, 1f, t / player.intSettings.pullDuration);
//             var b = col.bounds;
//             float h = pCol.bounds.extents.y;
//             Vector3 top = new Vector3(b.center.x, b.max.y + h, b.center.z);
//             playerRb.MovePosition(Vector3.Lerp(start, top, pct));
//             yield return null;
//         }
//         if (boxMover != null) boxMover.enabled = false;
//         pullCoroutine = null;
//     }

//     private IEnumerator LaunchToBoxRoutine(BoxMover target)
//     {
//         isLaunching = true;
//         interacting = false;
//         triggerLock = false;
//         player.movementSettings.movementState = MovementState.Launching;

//         Vector3 start = playerRb.position;
//         float t = 0f;
//         while (t < player.intSettings.launchDuration)
//         {
//             t += Time.deltaTime;
//             float pct = Mathf.SmoothStep(0f, 1f, t / player.intSettings.launchDuration);
//             var b = target.GetComponent<Collider>().bounds;
//             float h = pCol.bounds.extents.y;
//             Vector3 top = new Vector3(b.center.x, b.max.y + h, b.center.z);
//             playerRb.MovePosition(Vector3.Lerp(start, top, pct));
//             yield return null;
//         }

//         var bf = target.GetComponent<Collider>().bounds;
//         float hf = pCol.bounds.extents.y;
//         playerRb.MovePosition(
//             new Vector3(bf.center.x, bf.max.y + hf, bf.center.z));

//         var mv = target.GetComponent<BoxMover>();
//         if (mv != null) mv.enabled = false;

//         interacting = true;
//         player.movementSettings.movementState = MovementState.Interacting;
//         player.movementSettings.currentSurfaceState = SurfaceState.Nothing;
//         isLaunching = false;
//     }

//     private void UpdateHighlight(BoxMover bm)
//     {
//         if (selectedBox == bm) return;
//         ClearHighlight();
//         if (bm == null) return;
//         selectedBox = bm;
//         var r = bm.GetComponent<Renderer>();
//         if (r != null)
//         {
//             selectedOriginalColor = r.material.color;
//             r.material.color = Color.blue;
//         }
//     }

//     private void ClearHighlight()
//     {
//         if (selectedBox == null) return;
//         var r = selectedBox.GetComponent<Renderer>();
//         if (r != null) r.material.color = selectedOriginalColor;
//         selectedBox = null;
//     }

//     void OnDrawGizmos()
//     {
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireSphere(transform.position, player.intSettings.interactionRadius);
//     }
// }