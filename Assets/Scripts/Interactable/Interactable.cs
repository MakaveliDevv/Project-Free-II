using Assets.Scripts.Player;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Landing Score Thresholds")]
    public HitRange perfectRange = new() { max = 1f, min = 0.7f };
    public HitRange goodRange = new() { max = 0.7f, min = 0.4f };
    public HitRange lateRange = new() { max = 0.4f, min = 0.2f };

    [HideInInspector] public float beatTime;

    private Interactable selectedBox;
    private RewardSystem rewardSystem;
    private ShrinkOverTime shrinker;

    private Color selectedOriginalColor;
    private Player player;
    private SphereCollider sphereCol;
    private BoxCollider childBoxCol;

    private float originalRadius;
    private bool hasCheckedInteraction = false;
    public bool isMarkedForDestruction = false;
    private bool hasScored = false;

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

    private void OnTriggerStay(Collider collider)
    {
        if (hasScored) return;
        if (!collider.CompareTag("Player")) return;
        if (!collider.TryGetComponent<Player>(out var p)) return;

        // Only award points when player is actively interacting with THIS box
        if (p.playerSettings.movementState != MovementState.Interacting) return;
        if (p.moveContrl.advancedMovement.moveInt.interactable != this) return;

        hasScored = true;
        shrinker.isShrinking = false;

        float ratio = shrinker.GetCurrentSizeRatio();

        // float perfectThreshold = Mathf.Lerp(1f, shrinker.minScale, 1f / 3f);
        // float goodThreshold = Mathf.Lerp(1f, shrinker.minScale, 2f / 3f);
        // InteractTiming timing;
        // if (ratio >= goodThreshold)
        //     timing = InteractTiming.Perfect;
        // else if (ratio >= perfectThreshold)
        //     timing = InteractTiming.Good;
        // else
        //     timing = InteractTiming.Late;

        var timing = DetermineLandResult(ratio);
        rewardSystem.ApplyScore(timing);

        Debug.Log($"🎯 Landed: {timing} | +{GetPoints(timing)} | SizeRatio: {ratio:F2}");
    }

    private InteractTiming DetermineLandResult(float ratio)
    {
        if (perfectRange.InRange(ratio))
            return InteractTiming.Perfect;
        else if (goodRange.InRange(ratio))
            return InteractTiming.Good;
        else if (lateRange.InRange(ratio))
            return InteractTiming.Late;
        else
            return InteractTiming.Miss; 
    }


    private int GetPoints(InteractTiming timing)
    {
        return timing switch
        {
            InteractTiming.Perfect => 100,
            InteractTiming.Good => 70,
            InteractTiming.Late => 50,
            _ => 0
        };
    }

    private void OnTriggerExit(Collider collider)
    {

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
