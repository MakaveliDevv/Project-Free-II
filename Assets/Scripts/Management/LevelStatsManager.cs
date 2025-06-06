// using UnityEngine;

// public class LevelStatsManager : MonoBehaviour
// {
//     public static LevelStatsManager Instance { get; private set; }

//     public int FinalScore { get; private set; }
//     public int HighestCombo { get; private set; }

//     public int PerfectHits { get; private set; }
//     public int GoodHits { get; private set; }
//     public int EarlyHits { get; private set; }
//     public int LateHits { get; private set; }

//     private void Awake()
//     {
//         if (Instance != null && Instance != this)
//             Destroy(gameObject);
//         else
//             Instance = this;
//     }

//     /// <summary>
//     /// Call this once at the end of a level to record all current stats.
//     /// </summary>
//     public void CaptureStats(RewardSystem rewardSystem)
//     {
//         FinalScore = rewardSystem.TotalScore;
//         HighestCombo = rewardSystem.HighestComboAchieved;

//         PerfectHits = rewardSystem.PerfectCount;
//         GoodHits = rewardSystem.GoodCount;
//         EarlyHits = rewardSystem.EarlyCount;
//         LateHits = rewardSystem.LateCount;

//         Debug.Log($"📊 Final Stats Captured:\n" +
//                   $"- Score: {FinalScore}\n" +
//                   $"- Highest Combo: {HighestCombo}\n" +
//                   $"- Perfect: {PerfectHits}, Good: {GoodHits}, Early: {EarlyHits}, Late: {LateHits}");
//     }

//     /// <summary>
//     /// Optional: reset all stats if needed.
//     /// </summary>
//     public void ResetStats()
//     {
//         FinalScore = 0;
//         HighestCombo = 0;

//         PerfectHits = 0;
//         GoodHits = 0;
//         EarlyHits = 0;
//         LateHits = 0;
//     }
// }
