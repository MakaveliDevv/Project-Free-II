using Assets.Scripts.Player;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Tooltip("Target time (s) in song when landing should happen")]
    public float beatTime;

    [Header("Windows (s) around ideal size")]
    public float perfectThreshold = 0.05f;
    public float goodThreshold = 0.1f;
    public float earlyLateThreshold = 0.2f;

    private Interactable selectedBox;
    private InteractableRhythmBox rhythmBox;
    private RewardSystem rewardSystem;
    private ShrinkOverTime shrinker;

    private Color selectedOriginalColor;
    private Player player;
    private SphereCollider sphereCol;
    private BoxCollider childBoxCol;


    private void Awake()
    {
        player = FindAnyObjectByType<Player>();
        sphereCol = GetComponent<SphereCollider>();
        childBoxCol = transform.GetChild(1).GetComponent<BoxCollider>();
        childBoxCol.enabled = false;
        sphereCol.radius = player.interactionSettings.interactionRadius;


    }

    private void Start()
    { 
        rewardSystem = FindFirstObjectByType<RewardSystem>();
        shrinker = GetComponent<ShrinkOverTime>();

        bool s = shrinker;
        bool r = rewardSystem;
        Debug.Log($"shrinker -> {s}, rewardsystem -> {r}");
        rhythmBox = new(rewardSystem, shrinker, perfectThreshold, goodThreshold, earlyLateThreshold);
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (!collider.TryGetComponent<Player>(out var p)) return;

        // If the player is already inside another Interactable, skip
        if (p.currentInteractable != null && p.currentInteractable != this)
            return;

        // Mark this interactable as the one the player is in
        p.currentInteractable = this;
        rhythmBox?.OnTriggerEnter(collider);
    }

    private void OnTriggerExit(Collider collider)
    { 
        if (collider.TryGetComponent<Player>(out var p))
        {
            // Only clear if *this* is the current interactable
            if (p.currentInteractable == this)
                p.currentInteractable = null;
        }

        rhythmBox?.OnTriggerExit(collider);
    }

    private void OnTriggerStay(Collider collider)
    { 
        rhythmBox?.OnTriggerStay(collider);
    }

    public void UpdateHighlight(Interactable interactable)
    {
        if (selectedBox == interactable) return;
        ClearHighlight();
        if (interactable == null) return;
        selectedBox = interactable;
        if (interactable.transform.GetChild(1).TryGetComponent<Renderer>(out var r))
        {
            selectedOriginalColor = r.material.color;
            r.material.color = Color.blue;
        }
        else { Debug.Log("Couldn't feth the Renderer"); }
    }

    public void ClearHighlight()
    {
        if (selectedBox == null) return;
        if (selectedBox.transform.GetChild(1).TryGetComponent<Renderer>(out var r)) r.material.color = selectedOriginalColor;
        selectedBox = null;
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, player.interactionSettings.interactionRadius);
    }
}
