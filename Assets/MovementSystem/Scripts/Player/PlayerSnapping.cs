using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSnapping : MonoBehaviour
{
    public InputActionAsset inputActions;
    private InputAction snapAction;
    private InputAction leftStick;

    public float snapSpeed = 5f;
    public float launchForce = 10f;

    private BoxInteractionZone nearbyBox;
    private Rigidbody rb;
    private bool isSnapping = false;

    private Vector2 stick;

    private BoxMover boxMover;
    private float originalSpeed = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        var map = inputActions.FindActionMap("Player");
        snapAction = map.FindAction("SnapAction");
        leftStick = map.FindAction("Movement");

        snapAction.Enable();
        leftStick.Enable();
    }

    void Update()
    {
        if (snapAction.triggered && nearbyBox != null && !isSnapping)
        {
            StartCoroutine(SnapToBox(nearbyBox));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("AttackableBox")) 
        {
            // if(other.TryGetComponent<BoxMover>(out var )) 
            // {

            // }
            boxMover = GetComponent<BoxMover>();
            originalSpeed = boxMover.speed;
        }
    }

    IEnumerator SnapToBox(BoxInteractionZone box)
    {
        isSnapping = true;
        rb.linearVelocity  = Vector3.zero;
        rb.useGravity = false;

        Vector3 target = box.topOfBox.position;
        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, snapSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f); // short delay before launch

        // Stop movement of the box
        boxMover.speed = 0f;

        stick = Gamepad.current.leftStick.ReadUnprocessedValue();

        Vector3 direction = new Vector3(stick.x, stick.y, 0).normalized;
        rb.useGravity = true;
        rb.linearVelocity  = direction * launchForce;

        isSnapping = false;

        yield return new WaitForSeconds(1f);
        boxMover.speed = originalSpeed;
    }

    public void SetNearbyBox(BoxInteractionZone box)
    {
        nearbyBox = box;
    }

    public void ClearNearbyBox(BoxInteractionZone box)
    {
        if (nearbyBox == box)
            nearbyBox = null;
    }
}
