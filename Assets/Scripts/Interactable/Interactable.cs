// using UnityEngine;
// using System.Collections;
// using Assets.Scripts.Player;
// using UnityEngine.InputSystem;

// public class Interactable : MonoBehaviour
// {
//     [Header("Interaction Settings")]
//     [Tooltip("Radius within which the player can trigger the pull.")]
//     public float interactionRadius = 2f;
//     [Tooltip("Duration to pull player onto box.")]
//     public float pullDuration = 0.2f;
//     [Tooltip("Random min delay before jump-off.")]
//     public float jumpDelayMin = 0.1f;
//     [Tooltip("Random max delay before jump-off.")]
//     public float jumpDelayMax = 0.3f;
//     [Tooltip("Assign your ground collider to clamp Z during interaction.")]
//     public Collider groundBoundsCollider;

//     // — references —
//     private Player player;
//     private Transform _playerT;
//     private Rigidbody _playerRb;
//     private Collider _boxCol;

//     // — internal state —
//     private bool _interacting = false;
//     private float _origZ;
//     private float _groundMinZ, _groundMaxZ;

//     void Awake()
//     {
//         // grab player refs
//         var playerGO = GameObject.FindWithTag("Player");
//         if (playerGO == null) Debug.LogError("Player tag missing!");
//         _playerT = playerGO.transform;
//         _playerRb = playerGO.GetComponent<Rigidbody>();
//         player = playerGO.GetComponent<Player>();
//         _boxCol = GetComponent<Collider>();

//         // cache ground Z-bounds
//         if (groundBoundsCollider != null)
//         {
//             _groundMinZ = groundBoundsCollider.bounds.min.z;
//             _groundMaxZ = groundBoundsCollider.bounds.max.z;
//         }
//     }

//     void Update()
//     {
//         // while interacting, only clamp Z
//         if (_interacting && groundBoundsCollider != null)
//         {
//             Vector3 pos = _playerRb.position;
//             pos.z = Mathf.Clamp(pos.z, _groundMinZ, _groundMaxZ);
//             _playerRb.MovePosition(pos);
//             return;
//         }

//         // detect button + range
//         if (!_interacting &&
//             Vector3.Distance(_playerT.position, transform.position) <= interactionRadius &&
//             Gamepad.current.buttonNorth.isPressed)
//         {
//             StartInteraction();
//         }
//     }

//     private void StartInteraction()
//     {
//         _interacting = true;
//         _origZ = _playerRb.position.z;

//         // switch into interacting mode
//         player.movementSettings.movementState = MovementState.Interacting;
//         player.movementSettings.currentSurfaceState = SurfaceState.Nothing;

//         StartCoroutine(InteractionRoutine());
//     }

//     private IEnumerator InteractionRoutine()
//     {
//         // compute top-of-box world pos
//         Bounds b = _boxCol.bounds;
//         float halfH = _playerRb.GetComponent<Collider>().bounds.extents.y;
//         Vector3 top = new(
//             b.center.x,
//             b.max.y + halfH,
//             b.center.z
//         );

//         // 1) pull up onto box
//         Vector3 start = _playerRb.position;
//         float timer = 0f;
//         while (timer < pullDuration)
//         {
//             timer += Time.deltaTime;
//             float t = Mathf.SmoothStep(0f, 1f, timer / pullDuration);
//             Vector3 target = Vector3.Lerp(start, top, t);
//             _playerRb.MovePosition(target);
//             yield return null;
//         }

//         // 2) small random pause
//         yield return new WaitForSeconds(Random.Range(jumpDelayMin, jumpDelayMax));

//         // 3) begin restoring Z in parallel
//         StartCoroutine(RestoreZRoutine(fromZ: top.z));

//         // 4) launch off with snapped dir (Z jump itself comes from MovementSystem)
//         Vector2 snap = player.movementController.movementSystem.snappedDir;
//         player.movementController.movementSystem.LaunchOffBox(new(snap.x, snap.y, 0f));

//         // 5) set MovementState to Nothing (don’t restore old state)
//         // player.movementSettings.movementState = MovementState.Nothing;

//         _interacting = false;
//     }

//     private IEnumerator RestoreZRoutine(float fromZ)
//     {
//         float timer = 0f;
//         while (timer < pullDuration)
//         {
//             timer += Time.deltaTime;
//             float t = Mathf.SmoothStep(0f, 1f, timer / pullDuration);
//             float z = Mathf.Lerp(fromZ, _origZ, t);
//             Vector3 pos = _playerRb.position;
//             pos.z = z;
//             _playerRb.MovePosition(pos);
//             yield return null;
//         }
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

// public class Interactable : MonoBehaviour
// {
//     [Header("Interaction Settings")]
//     public float interactionRadius = 2f;
//     public float pullDuration = 0.2f;
//     public Collider groundBoundsCollider;

//     // references
//     private Player player;
//     private Rigidbody playerRb;
//     private BoxMover boxMover;

//     // state
//     private bool interacting = false;
//     private float origZ;
//     private float groundMinZ, groundMaxZ;

//     void Awake()
//     {
//         // grab player & box refs
//         var playerGO = GameObject.FindWithTag("Player");
//         player = playerGO.GetComponent<Player>();
//         playerRb = playerGO.GetComponent<Rigidbody>();
//         boxMover = GetComponent<BoxMover>();

//         if (groundBoundsCollider != null)
//         {
//             groundMinZ = groundBoundsCollider.bounds.min.z;
//             groundMaxZ = groundBoundsCollider.bounds.max.z;
//         }
//     }

//     void Update()
//     {
//         // while on the box, clamp Z
//         if (interacting && groundBoundsCollider != null)
//         {
//             var pos = playerRb.position;
//             pos.z = Mathf.Clamp(pos.z, groundMinZ, groundMaxZ);
//             playerRb.MovePosition(pos);
//             Debug.Log("Shifting player Z position");
//             return;
//         }

//         // initial pull trigger: North button + in radius
//         if (!interacting
//             && Vector3.Distance(playerRb.position, transform.position) <= interactionRadius
//             && Gamepad.current.buttonNorth.wasPressedThisFrame)
//         {
//             StartInteraction();
//         }
//     }

//     private void StartInteraction()
//     {
//         Debug.Log("Interaction started");
//         interacting = true;
//         origZ = playerRb.position.z;

//         // enter interacting state
//         player.movementSettings.movementState = MovementState.Interacting;
//         player.movementSettings.currentSurfaceState = SurfaceState.Nothing;

//         StartCoroutine(InteractionRoutine());
//     }

//     private IEnumerator InteractionRoutine()
//     {
//         Debug.Log("Performing interaction process..");
//         // 1) pull the player up onto the top of this box
//         var b = GetComponent<Collider>().bounds;
//         float h = playerRb.GetComponent<Collider>().bounds.extents.y;
//         Vector3 top = new Vector3(b.center.x, b.max.y + h, playerRb.position.z);

//         Vector3 start = playerRb.position;
//         float t = 0f;
//         while (t < pullDuration)
//         {
//             t += Time.deltaTime;
//             float pct = Mathf.SmoothStep(0f, 1f, t / pullDuration);
//             playerRb.MovePosition(Vector3.Lerp(start, top, pct));
//             yield return null;
//         }

//         // 2) stop the box immediately
//         if (boxMover != null) boxMover.enabled = false;             

//         // 3) now wait for West button to launch
//         yield return new WaitUntil(() => Gamepad.current.buttonWest.wasPressedThisFrame);

//         // 4) perform your snapped‐dir jump via LaunchOffBox
//         Vector2 snap = player.movementController.movementSystem.snappedDir;
//         player.movementController.movementSystem.LaunchOffBox(new(snap.x, snap.y, 0f));     

//         // end interaction
//         interacting = false;
//     }

//     void OnDrawGizmos()
//     {
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireSphere(transform.position, interactionRadius);
//     }
// }

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

    // references
    private Player player;
    private Transform playerT;
    private Rigidbody playerRb;
    private BoxMover boxMover;

    // state
    private bool interacting = false;
    private float origZ;
    private float groundMinZ, groundMaxZ;

    void Awake()
    {
        var go = GameObject.FindWithTag("Player");
        if (go == null) Debug.LogError("Player tag missing!");
        player = go.GetComponent<Player>();
        playerT = go.transform;
        playerRb = go.GetComponent<Rigidbody>();
        boxMover = GetComponent<BoxMover>();

        if (groundBoundsCollider != null)
        {
            groundMinZ = groundBoundsCollider.bounds.min.z;
            groundMaxZ = groundBoundsCollider.bounds.max.z;
        }
    }

    void Update()
    {
        // 1) If we’re in the “landed on box” state, clamp Z and listen for West
        if (interacting)
        {
            // clamp Z within ground bounds
            if (groundBoundsCollider != null)
            {
                var pos = playerRb.position;
                pos.z = Mathf.Clamp(pos.z, groundMinZ, groundMaxZ);
                playerRb.MovePosition(pos);
            }

            // press West to jump off
            var gp = Gamepad.current;
            if (gp != null && gp.buttonWest.isPressed)
            {
                // restore Z and launch off
                StartCoroutine(RestoreZRoutine());
                Vector2 snap = player.movementController.movementSystem.snappedDir;
                player.movementController.movementSystem.LaunchOffBox(new Vector3(snap.x, snap.y, 0f));  // :contentReference[oaicite:2]{index=2}

                interacting = false;
            }
            return;
        }

        // 2) Otherwise, look for North + in‐range to start the pull
        var pad = Gamepad.current;
        if (pad != null
            && pad.buttonNorth.isPressed
            && Vector3.Distance(playerT.position, transform.position) <= interactionRadius)
        {
            StartCoroutine(PullOntoBoxRoutine());
        }
    }

    private IEnumerator PullOntoBoxRoutine()
    {
        interacting = true;
        origZ = playerRb.position.z;

        // enter Interacting/Nothing states
        player.movementSettings.movementState = MovementState.Interacting;
        player.movementSettings.currentSurfaceState = SurfaceState.Nothing;

        // compute top‐of‐box position
        var b = GetComponent<Collider>().bounds;
        float h = playerRb.GetComponent<Collider>().bounds.extents.y;
        Vector3 top = new Vector3(b.center.x, b.max.y + h, b.center.z);

        // pull up
        float t = 0f;
        Vector3 start = playerRb.position;
        while (t < pullDuration)
        {
            t += Time.deltaTime;
            float pct = Mathf.SmoothStep(0f, 1f, t / pullDuration);
            playerRb.MovePosition(Vector3.Lerp(start, top, pct));
            yield return null;
        }

        // immediately stop the box
        if (boxMover != null) boxMover.enabled = false;
    }

    private IEnumerator RestoreZRoutine()
    {
        // smoothly bring Z back to origZ
        float t = 0f;
        float fromZ = playerRb.position.z;
        while (t < pullDuration)
        {
            t += Time.deltaTime;
            float pct = Mathf.SmoothStep(0f, 1f, t / pullDuration);
            var pos = playerRb.position;
            pos.z = Mathf.Lerp(fromZ, origZ, pct);
            playerRb.MovePosition(pos);
            yield return null;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}



