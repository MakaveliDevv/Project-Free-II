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

// public class InteractableRhythmBox 
// {
//     private readonly RewardSystem rewardSystem;
//     private readonly ShrinkOverTime shrinker;

//     private readonly float perfectThreshold;
//     private readonly float goodThreshold;
//     private readonly float earlyLateThreshold;
//     private bool hasLanded = false;
//     private bool isPlayerOnBox = false;

//     public InteractableRhythmBox
//     (
//         RewardSystem rewardSystem,
//         ShrinkOverTime shrinker,
//         float perfectThreshold,
//         float goodThreshold,
//         float earlyLateThreshold
//     )
//     {
//         this.rewardSystem = rewardSystem;
//         this.shrinker = shrinker;
//         this.perfectThreshold = perfectThreshold;
//         this.goodThreshold = goodThreshold;
//         this.earlyLateThreshold = earlyLateThreshold;
//     }

//     public void OnTriggerEnter(Collider collider)
//     {
//         if (hasLanded || collider.CompareTag("Player") == false) return;
//         if (!collider.TryGetComponent<Player>(out var p)) return;

//         var owner = shrinker.GetComponent<Interactable>();
//         if (p.moveContrl.advancedMovement.moveInt.interactable != owner) return;

//         if (p.playerSettings.movementState != MovementState.Interacting) return;

//         hasLanded = true;
//         isPlayerOnBox = true;
//         shrinker.isShrinking = false;

//         float sizeRatio = shrinker.GetCurrentSizeRatio();
//         float distanceFromIdeal = Mathf.Abs(sizeRatio - 0.5f);

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
//     }

//     public void OnTriggerStay(Collider collider)
//     {
//         if (hasLanded || collider.CompareTag("Player") == false || !collider.TryGetComponent<Player>(out var p)) return;

//         var assignedBox = p.moveContrl.advancedMovement.moveInt.interactable;
//         var thisBox = shrinker.GetComponent<Interactable>();

//         // ✅ Only stop shrinking if the player is on THIS specific box and not just nearby
//         bool isSameBox = assignedBox == thisBox;
//         bool isInteracting = p.playerSettings.movementState == MovementState.Interacting;

//         if (isInteracting && isSameBox)
//         {
//             shrinker.isShrinking = false;
//         }
//         else
//         {
//             shrinker.isShrinking = true;
//         }
//     }

//     public void OnTriggerExit(Collider other)
//     {
//         if (!isPlayerOnBox || other.CompareTag("Player") == false) return;
//         isPlayerOnBox = false;
//         shrinker.isShrinking = true;
//     }

// }
