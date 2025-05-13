using UnityEngine;

public class BoxInteractionZone : MonoBehaviour
{
    public Transform topOfBox; // empty GameObject on top of box
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<PlayerSnapping>(out var player)) 
            {
                Debug.Log("Player in range");
                player.SetNearbyBox(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<PlayerSnapping>(out var player))
                player.ClearNearbyBox(this);
        }
    }
}
