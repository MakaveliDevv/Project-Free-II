using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class BoxController : MonoBehaviour
{
    [Header("Landing Zone (smaller than top surface)")]
    public Vector3 landingZoneSize = new Vector3(0.8f, 0.8f, 0.8f);
    public Vector3 landingZoneOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Gravity Pull Zone (trigger)")]
    public Vector3 gravityZoneSize = new Vector3(1.2f, 1.2f, 1.2f);
    public Vector3 gravityZoneOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Pull / Return Timing")]
    public float pullTime = 0.2f;
    public float returnTime = 0.2f;
    [Tooltip("How far toward the box on Z before snapping to top")]
    public float forwardZOffset = 0.5f;

    BoxCollider _landingZone;
    BoxCollider _gravityZone;
    bool _playerInZone = false;
    bool _isInteracting = false;

    MovementSystem _playerMovement;
    Transform _playerT;
    Vector3 _origPlayerZ;

    GameObject lzGO;
    GameObject gzGO;

    private SphereCollider col;
    public float sColRadius = 1f;

    void Awake()
    {
        // Landing collider (non-trigger) on top
        lzGO = new GameObject("LandingZone");
        lzGO.transform.SetParent(transform, false);
        lzGO.transform.localPosition = landingZoneOffset;
        _landingZone = lzGO.AddComponent<BoxCollider>();
        _landingZone.size = new Vector3(landingZoneSize.x, landingZoneSize.y, landingZoneSize.z);
        _landingZone.isTrigger = false;
        _landingZone.gameObject.layer = LayerMask.NameToLayer("Ground");

        // Sphere collider
        col = GetComponent<SphereCollider>();
        col.radius = sColRadius;
        col.isTrigger = true;
        
        // Gravity pull trigger above
        gzGO = new GameObject("GravityZone");
        gzGO.transform.SetParent(transform, false);
        gzGO.transform.localPosition = gravityZoneOffset;
        _gravityZone = gzGO.AddComponent<BoxCollider>();
        _gravityZone.size = new Vector3(gravityZoneSize.x, gravityZoneSize.y, gravityZoneSize.z);
        _gravityZone.isTrigger = true;
    }

    void Start()
    {
        _playerMovement = FindFirstObjectByType <MovementSystem>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<MovementSystem>() != null)
        {
            Debug.Log("Player In zone");
            _playerInZone = true;
            _playerT = other.transform;
            _origPlayerZ = _playerT.position;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<MovementSystem>() != null)
            _playerInZone = false;
    }

    // void Update()
    // {
    //     // Check for left bumper press while in-air inside our gravity zone
    //     if (_playerInZone
    //         && !_isInteracting
    //         && _playerMovement.currentSurfaceState == AnalogStickReader.SurfaceState.Air
    //         && Gamepad.current != null
    //         && Gamepad.current.leftShoulder.wasPressedThisFrame)
    //     {
    //         StartCoroutine(BoxSequence());
    //     }
    // }

    // IEnumerator BoxSequence()
    // {
    //     _isInteracting = true;
    //     _playerMovement.enableZLock = false;

    //     // compute target positions
    //     Vector3 topCenter = transform.TransformPoint(landingZoneOffset);
    //     Vector3 pullTarget = topCenter + Vector3.forward * forwardZOffset;

    //     // 1) pull from current → forward
    //     float t = 0f;
    //     Vector3 start = _playerT.position;
    //     while (t < pullTime)
    //     {
    //         _playerT.position = Vector3.Lerp(start, pullTarget, t / pullTime);
    //         t += Time.deltaTime;
    //         yield return null;
    //     }
    //     _playerT.position = pullTarget;

    //     // 2) pull from forward → exact top
    //     t = 0f;
    //     while (t < pullTime)
    //     {
    //         _playerT.position = Vector3.Lerp(pullTarget, topCenter, t / pullTime);
    //         t += Time.deltaTime;
    //         yield return null;
    //     }
    //     _playerT.position = topCenter;

    //     yield return null; // allow movement system to register ground

    //     // 3) slide back out a bit on Z (to original Z + offset)
    //     Vector3 exitStart = _playerT.position;
    //     Vector3 exitTarget = new Vector3(
    //         exitStart.x,
    //         exitStart.y,
    //         _origPlayerZ.z + forwardZOffset
    //     );
    //     t = 0f;
    //     while (t < returnTime)
    //     {
    //         _playerT.position = Vector3.Lerp(exitStart, exitTarget, t / returnTime);
    //         t += Time.deltaTime;
    //         yield return null;
    //     }
    //     _playerT.position = exitTarget;

    //     // 4) trigger jump off in current stick direction
    //     Vector2 stick = Gamepad.current.leftStick.ReadValue();
    //     Vector3 dir = (stick.magnitude > _playerMovement.minStickMagnitude)
    //         ? new Vector3(stick.x, stick.y, 0f).normalized
    //         : Vector3.up;
    //     _playerMovement.LaunchOffBox(dir);

    //     _playerMovement.enableZLock = true;
    //     _isInteracting = false;

    //     yield break;
    // }

    // #if UNITY_EDITOR
    // void OnDrawGizmosSelected()
    // {
    //     // Landing Zone (green)
    //     Gizmos.color = Color.green;
    //     Vector3 lzCenter = transform.TransformPoint(landingZoneOffset);
    //     Gizmos.DrawWireCube(lzCenter, new Vector3(landingZoneSize.x, landingZoneSize.y, landingZoneSize.z));

    //     // Gravity Zone (blue)
    //     Gizmos.color = Color.blue;
    //     Vector3 gzCenter = transform.TransformPoint(gravityZoneOffset);
    //     Gizmos.DrawWireCube(gzCenter, new Vector3(gravityZoneSize.x, gravityZoneSize.y, gravityZoneSize.z));

    //     // Line from Player to Landing Zone (red)
    //     #if UNITY_EDITOR 
    //     // Draw only when playing and player has been detected
    //     if (Application.isPlaying && _playerT != null)
    //     {
    //         Gizmos.color = Color.red;
    //         Gizmos.DrawLine(_playerT.position, lzCenter);
    //     }
    //     #endif
    // }
    // #endif

}
