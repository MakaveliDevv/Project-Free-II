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

    private float originalRadius;
    private bool hasCheckedInteraction = false;
    public bool isMarkedForDestruction = false;

    private void Awake()
    {
        player = FindAnyObjectByType<Player>();
        sphereCol = GetComponent<SphereCollider>();
        childBoxCol = transform.GetChild(1).GetComponent<BoxCollider>();
        childBoxCol.enabled = false;

        originalRadius = player.interactionSettings.interactionRadius;
        sphereCol.radius = originalRadius;
    }

    private void Start()
    { 
        rewardSystem = FindFirstObjectByType<RewardSystem>();
        shrinker = GetComponent<ShrinkOverTime>();
        rhythmBox = new(rewardSystem, shrinker, perfectThreshold, goodThreshold, earlyLateThreshold);
    }

    private void Update()
    {
        if (player == null) return;

        bool playerIsBusy = player.moveContrl.advancedMovement.moveInt.interactable != null && player.moveContrl.advancedMovement.moveInt.interactable != this;

        if (playerIsBusy && !hasCheckedInteraction)
        {
            sphereCol.radius = 0.1f;
            hasCheckedInteraction = true;
        }
        else if (!playerIsBusy && hasCheckedInteraction)
        {
            sphereCol.radius = originalRadius;
            hasCheckedInteraction = false;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        rhythmBox?.OnTriggerEnter(collider);
    }

    private void OnTriggerStay(Collider collider)
    {
        rhythmBox?.OnTriggerStay(collider);
    }

    private void OnTriggerExit(Collider collider)
    {
        rhythmBox?.OnTriggerExit(collider);   
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
