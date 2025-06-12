// using UnityEngine;
// using Assets.Scripts.Player;

// public class InteractableRhythmBox : MonoBehaviour
// {
//     [Tooltip("Target time (s) in song when landing should happen")]
//     public float beatTime;

//     [Header("Windows (s) around ideal size")]
//     public float perfectThreshold = 0.05f;
//     public float goodThreshold = 0.1f;
//     public float earlyLateThreshold = 0.2f;

//     private bool hasLanded = false;
//     private RewardSystem rewardSystem;
//     private ShrinkOverTime shrinker;

//     void Awake()
//     {
//         rewardSystem = FindFirstObjectByType<RewardSystem>();  
//     }

//     void Start()
//     {
//         shrinker = GetComponent<ShrinkOverTime>();
//         Debug.Log($"{shrinker.gameObject.name}");
//     }

//     void OnTriggerEnter(Collider other)
//     {
//         if (hasLanded || other.CompareTag("Player") == false) return;

//         hasLanded = true;

//         float sizeRatio = shrinker.GetCurrentSizeRatio();
//         float distanceFromIdeal = Mathf.Abs(sizeRatio - 0.5f); // 0.5 is the sweet spot

//         InteractTiming timing;
//         if (distanceFromIdeal <= perfectThreshold)
//             timing = InteractTiming.Perfect;
//         else if (distanceFromIdeal <= goodThreshold)
//             timing = InteractTiming.Good;
//         else if (sizeRatio > 0.5f && distanceFromIdeal <= earlyLateThreshold)
//             timing = InteractTiming.Early;
//         else if (sizeRatio < 0.5f && distanceFromIdeal <= earlyLateThreshold)
//             timing = InteractTiming.Late;
//         else
//             timing = InteractTiming.Miss;

//         rewardSystem.ApplyScore(timing);

//         Destroy(gameObject, 0.2f);
//     }
// }


using UnityEngine;
using Assets.Scripts.Player;

// public class InteractableRhythmBox : MonoBehaviour
// {
//     [Tooltip("Target time (s) in song when landing should happen")]
//     public float beatTime;

//     [Header("Windows (s) around ideal size")]
//     public float perfectThreshold = 0.05f;
//     public float goodThreshold = 0.1f;
//     public float earlyLateThreshold = 0.2f;

//     private bool hasLanded = false;
//     private RewardSystem rewardSystem;
//     private ShrinkOverTime shrinker;

//     void Awake()
//     {
//         rewardSystem = FindFirstObjectByType<RewardSystem>();
//         shrinker = GetComponent<ShrinkOverTime>();
//     }

//     void OnTriggerEnter(Collider other)
//     {
//         if (hasLanded || other.CompareTag("Player") == false) return;

//         hasLanded = true;

//         // Stop shrinking to visually show the player is standing on the box
//         shrinker.isShrinking = false;

//         float sizeRatio = shrinker.GetCurrentSizeRatio();
//         float distanceFromIdeal = Mathf.Abs(sizeRatio - 0.5f); // 0.5 is the sweet spot

//         InteractTiming timing;
//         if (distanceFromIdeal <= perfectThreshold)
//             timing = InteractTiming.Perfect;
//         else if (distanceFromIdeal <= goodThreshold)
//             timing = InteractTiming.Good;
//         else if (sizeRatio > 0.5f && distanceFromIdeal <= earlyLateThreshold)
//             timing = InteractTiming.Early;
//         else if (sizeRatio < 0.5f && distanceFromIdeal <= earlyLateThreshold)
//             timing = InteractTiming.Late;
//         else
//             timing = InteractTiming.Miss;

//         rewardSystem.ApplyScore(timing);

//         // Destroy will now be handled externally (e.g., on exit)
//     }
// }

public class InteractableRhythmBox : MonoBehaviour
{
    [Tooltip("Target time (s) in song when landing should happen")]
    public float beatTime;

    [Header("Windows (s) around ideal size")]
    public float perfectThreshold = 0.05f;
    public float goodThreshold = 0.1f;
    public float earlyLateThreshold = 0.2f;

    private bool hasLanded = false;
    private RewardSystem rewardSystem;
    private ShrinkOverTime shrinker;
    private bool isPlayerOnBox = false;

    void Awake()
    {
        rewardSystem = FindFirstObjectByType<RewardSystem>();
    }

    void Start()
    {
        shrinker = GetComponent<ShrinkOverTime>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasLanded || other.CompareTag("Player") == false) return;

        hasLanded = true;
        isPlayerOnBox = true;

        // Stop shrinking to visually show the player is standing on the box
        shrinker.isShrinking = false;

        float sizeRatio = shrinker.GetCurrentSizeRatio();
        float distanceFromIdeal = Mathf.Abs(sizeRatio - 0.5f); // 0.5 is the sweet spot

        InteractTiming timing;
        if (distanceFromIdeal <= perfectThreshold)
            timing = InteractTiming.Perfect;
        else if (distanceFromIdeal <= goodThreshold)
            timing = InteractTiming.Good;
        else if (sizeRatio > 0.5f && distanceFromIdeal <= earlyLateThreshold)
            timing = InteractTiming.Early;
        else if (sizeRatio < 0.5f && distanceFromIdeal <= earlyLateThreshold)
            timing = InteractTiming.Late;
        else
            timing = InteractTiming.Miss;

        rewardSystem.ApplyScore(timing);
    }

    void OnTriggerExit(Collider other)
    {
        if (!isPlayerOnBox || other.CompareTag("Player") == false) return;
        isPlayerOnBox = false;
        Destroy(gameObject, 0.2f); // optional fade can be triggered here too
    }
}
