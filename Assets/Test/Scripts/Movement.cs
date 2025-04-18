using System.Collections;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveDistance = 3f;
    public float jumpDuration = 0.3f;
    public float hangTime = 1f;
    public float fallSpeed = 5f;

    private bool isJumping = false;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponentInChildren<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) && !isJumping)
        {
            Debug.Log("W button pressed");
            Vector3 jumpDirection = Vector3.up;

            if (Input.GetKey(KeyCode.A))
            {
                Debug.Log("A button pressed");
                jumpDirection += Vector3.left;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                Debug.Log("D button pressed");
                jumpDirection += Vector3.right;
            }

            StartCoroutine(JumpSequence(jumpDirection.normalized));
        }
    }
    
    private IEnumerator JumpSequence(Vector3 direction)
    {
        isJumping = true;

        Vector3 startPos = rb.transform.position;
        Vector3 peakPos = startPos + direction * moveDistance;
        float timer = 0f;

        // Phase 1: Jump up
        while (timer < jumpDuration)
        {
            timer += Time.deltaTime;
            float t = timer / jumpDuration;
            rb.transform.position = Vector3.Lerp(startPos, peakPos, t);
            yield return null;
        }

        rb.transform.position = peakPos;

        // Phase 2: Air hang with subtle motion (NOT moving up)
        float hangTimer = 0f;
        rb.useGravity = false;

        float floatAmplitude = 0.05f;     // how much to float up/down
        float floatFrequency = 4f;        // speed of the float motion
        Vector3 baseHangPos = rb.transform.position;

        while (hangTimer < hangTime)
        {
            hangTimer += Time.deltaTime;

            // Apply gentle vertical sinusoidal motion
            float floatOffset = Mathf.Sin(hangTimer * floatFrequency) * floatAmplitude;
            Vector3 newPos = baseHangPos + Vector3.up * floatOffset;
            rb.transform.position = newPos;

            yield return null;
        }

        // Phase 3: Let gravity take over again
        isJumping = false;
        rb.useGravity = true;
    }


}


// using System.Collections;
// using UnityEngine;

// public class Movement : MonoBehaviour
// {
//     public float moveDistance = 3f;
//     public float jumpDuration = 0.3f;
//     public float hangTime = 1f;
//     public float fallSpeed = 5f;

//     private bool isJumping = false;

//     private Rigidbody rb;

//     private void Awake()
//     {
//         rb = GetComponentInChildren<Rigidbody>();
//     }

//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.W) && !isJumping)
//         {
//             Debug.Log("W button pressed");
//             Vector3 jumpDirection = Vector3.up;

//             if (Input.GetKey(KeyCode.A))
//             {
//                 Debug.Log("A button pressed");
//                 jumpDirection += Vector3.left;
//             }
//             else if (Input.GetKey(KeyCode.D))
//             {
//                 Debug.Log("D button pressed");
//                 jumpDirection += Vector3.right;
//             }

//             StartCoroutine(JumpSequence(jumpDirection.normalized));
//         }
//     }

//     private IEnumerator JumpSequence(Vector3 direction)
//     {
//         isJumping = true;

//         // Phase 1: Move Upward
//         Vector3 startPos = rb.transform.position;
//         Vector3 peakPos = startPos + direction * moveDistance;
//         float timer = 0f;

//         while (timer < jumpDuration)
//         {
//             timer += Time.deltaTime;
//             float t = timer / jumpDuration;
//             rb.transform.position = Vector3.Lerp(startPos, peakPos, t);
//             yield return null;
//         }

//         rb.transform.position = peakPos;

//         // Phase 2: Hang in the air
//         yield return new WaitForSeconds(hangTime);
//         rb.useGravity = true;

//         // Phase 3: Fall Down
//         while (true)
//         {
//             rb.transform.position += fallSpeed * Time.deltaTime * Vector3.down;

//             // Optional: Stop falling if we hit ground level (you can change this to a ground check)
//             if (rb.transform.position.y <= startPos.y)
//             {
//                 Vector3 landedPos = rb.transform.position;
//                 landedPos.y = startPos.y;
//                 rb.transform.position = landedPos;
//                 break;
//             }

//             yield return null;
//         }

//         isJumping = false;
//         rb.useGravity = false;
//     }
// }