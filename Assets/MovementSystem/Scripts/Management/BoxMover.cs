using UnityEngine;

public class BoxMover : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        transform.Translate(speed * Time.deltaTime * Vector3.back, Space.World);

        // destroy once behind camera/player
        if (transform.position.z < -20f)
            Destroy(gameObject);
    }
}