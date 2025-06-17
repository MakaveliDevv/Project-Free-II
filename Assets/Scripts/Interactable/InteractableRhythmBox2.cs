// using UnityEngine;
// using Assets.Scripts.Player;

// public class InteractableRhythmBox2 : MonoBehaviour
// {
//     [Tooltip("Target time (s) in song when landing should happen")]
//     public float beatTime;

//     [Header("Windows (s) around beatTime")]
//     public float perfectWindow = 0.1f;
//     public float goodWindow    = 0.25f;
//     public float earlyWindow   = 0.5f;

//     private bool hasLanded = false;
//     private RewardSystem rewardSystem;
//     private double songStartDSP;

//     void Awake()
//     {
//         rewardSystem = FindFirstObjectByType<RewardSystem>();
//         songStartDSP = RhythmSystem.SongStartDSP; 
//     }

//     void OnCollisionEnter(Collision collision)
//     {
//         if (hasLanded) return;

//         if (!collision.gameObject.CompareTag("Player")) 
//             return;

//         hasLanded = true;

//         float songTime = (float)(AudioSettings.dspTime - songStartDSP);
//         float delta    = songTime - beatTime;
//         float absDelta = Mathf.Abs(delta);

//         InteractTiming timing;
//         if (absDelta <= perfectWindow)
//             timing = InteractTiming.Perfect;
//         else if (absDelta <= goodWindow)
//             timing = InteractTiming.Good;
//         else if (delta < 0 && absDelta <= earlyWindow)
//             timing = InteractTiming.Early;
//         else if (delta > 0 && absDelta <= earlyWindow)
//             timing = InteractTiming.Late;
//         else
//             timing = InteractTiming.Miss;

//         rewardSystem.ApplyScore(timing);

//         Destroy(gameObject, 0.2f);
//     }
// }
