// using UnityEngine;

// public class RewardSystem : MonoBehaviour
// {
//     [SerializeField] private ScoreDisplay scoreDisplay;

//     public int TotalScore { get; private set; } = 0;
//     public int ComboMultiplier { get; private set; } = 1;
//     public int PerfectComboCount { get; private set; } = 0;

//     public int PerfectCount { get; private set; }
//     public int GoodCount { get; private set; }
//     public int EarlyCount { get; private set; }
//     public int LateCount { get; private set; }
//     public int HighestComboAchieved { get; private set; }

//     /// <summary>
//     /// Calculates base score for a given HitResult.
//     /// </summary>
//     public int CalculateBasePoints(Attackable.HitResult hitResult)
//     {
//         return hitResult switch
//         {
//             Attackable.HitResult.Perfect => 100,
//             Attackable.HitResult.Good => 75,
//             Attackable.HitResult.Early => 50,
//             Attackable.HitResult.Late => 10,
//             _ => 0
//         };
//     }

//     /// <summary>
//     /// Applies the score with combo multiplier and updates the total.
//     /// </summary>
//     // public void ApplyScore(Attackable.HitResult result)
//     // {
//     //     UpdateCombo(result);

//     //     int basePoints = CalculateBasePoints(result);
//     //     int finalPoints = basePoints * ComboMultiplier;

//     //     TotalScore += finalPoints;

//     //     Debug.Log($"🎯 Hit: {result} | Base: {basePoints} | Combo x{ComboMultiplier} → +{finalPoints} points | Total: {TotalScore}");

//     //     if (scoreDisplay != null) { scoreDisplay.UpdateUI(result); }
//     // }

//     public void ApplyScore(Attackable.HitResult result)
//     {
//         UpdateCombo(result);
//         TrackHitCount(result);

//         int basePoints = CalculateBasePoints(result);
//         int finalPoints = basePoints * ComboMultiplier;

//         TotalScore += finalPoints;

//         if (ComboMultiplier > HighestComboAchieved)
//             HighestComboAchieved = ComboMultiplier;

//         Debug.Log($"🎯 Hit: {result} | Base: {basePoints} | Combo x{ComboMultiplier} → +{finalPoints} | Total: {TotalScore}");

//         if (scoreDisplay != null)
//             scoreDisplay.UpdateUI(result);
//     }

//     /// <summary>
//     /// Handles combo logic: increment on Perfect, reset otherwise.
//     /// </summary>
//     private void UpdateCombo(Attackable.HitResult result)
//     {
//         if (result == Attackable.HitResult.Perfect)
//         {
//             PerfectComboCount++;
//             ComboMultiplier = 1 + PerfectComboCount / 2; // e.g., 2 perfects = x2, 4 perfects = x3
//         }
//         else
//         {
//             PerfectComboCount = 0;
//             ComboMultiplier = 1;
//         }
//     }

//     private void TrackHitCount(Attackable.HitResult result)
//     {
//         switch (result)
//         {
//             case Attackable.HitResult.Perfect: PerfectCount++; break;
//             case Attackable.HitResult.Good: GoodCount++; break;
//             case Attackable.HitResult.Early: EarlyCount++; break;
//             case Attackable.HitResult.Late: LateCount++; break;
//         }
//     }

//     public void ResetScore()
//     {
//         TotalScore = 0;
//         ComboMultiplier = 1;
//         PerfectComboCount = 0;
//     }
// }


// using UnityEngine;

// public class RewardSystem : MonoBehaviour
// {
//     public int TotalScore { get; private set; } = 0;

//     public int GoodComboCount { get; private set; } = 0;
//     public int PerfectComboCount { get; private set; } = 0;

//     public int HighestGoodCombo { get; private set; } = 0;
//     public int HighestPerfectCombo { get; private set; } = 0;

//     private bool goodComboActive = false;
//     private bool perfectComboActive = false;
//     private bool goodComboPaused = false;

//     private int pendingPerfect = 0;

//     private ScoreDisplay scoreDisplay;

//     void Awake()
//     {
//         scoreDisplay = FindFirstObjectByType <ScoreDisplay>();
//     }

//     public void ApplyScore(Attackable.HitResult result)
//     {
//         int basePoints = CalculateBasePoints(result);
//         TotalScore += basePoints;

//         HandleComboLogic(result);

//         Debug.Log($"🎯 Hit: {result} | +{basePoints} | Score: {TotalScore} | Good x{GoodComboCount} | Perfect x{PerfectComboCount}");

//         if (scoreDisplay != null)
//             scoreDisplay.UpdateUI(result);
//     }

//     private int CalculateBasePoints(Attackable.HitResult result)
//     {
//         return result switch
//         {
//             Attackable.HitResult.Perfect => 100,
//             Attackable.HitResult.Good => 75,
//             Attackable.HitResult.Early => 50,
//             Attackable.HitResult.Late => 10,
//             _ => 0
//         };
//     }

//     private void HandleComboLogic(Attackable.HitResult result)
//     {
//         switch (result)
//         {
//             case Attackable.HitResult.Perfect:
//                 if (perfectComboActive)
//                 {
//                     PerfectComboCount++;
//                     UpdateHighestCombo();
//                 }
//                 else
//                 {
//                     if (pendingPerfect == 1)
//                     {
//                         // This is the second perfect → start combo
//                         perfectComboActive = true;
//                         PerfectComboCount = 2; // first one was pending, second is this one
//                         pendingPerfect = 0;
//                         goodComboActive = false;
//                         goodComboPaused = false;
//                         GoodComboCount = 0;
//                     }
//                     else
//                     {
//                         pendingPerfect = 1;

//                         if (goodComboActive)
//                         {
//                             goodComboPaused = true;
//                         }
//                     }
//                 }
//                 break;

//             case Attackable.HitResult.Good:
//                 if (perfectComboActive)
//                 {
//                     // Perfect streak broken → stop perfect combo
//                     perfectComboActive = false;
//                     pendingPerfect = 0;
//                     PerfectComboCount = 0;

//                     // Resume paused good combo
//                     if (goodComboPaused)
//                     {
//                         goodComboPaused = false;
//                         goodComboActive = true;
//                         GoodComboCount++;
//                         UpdateHighestCombo();
//                     }
//                     else
//                     {
//                         // No paused combo → start fresh good combo
//                         goodComboActive = true;
//                         GoodComboCount = 1;
//                     }
//                 }
//                 else if (goodComboActive || goodComboPaused)
//                 {
//                     goodComboActive = true;
//                     goodComboPaused = false;
//                     GoodComboCount++;
//                     UpdateHighestCombo();
//                 }
//                 else
//                 {
//                     goodComboActive = true;
//                     GoodComboCount = 1;
//                 }

//                 pendingPerfect = 0;
//                 break;

//             case Attackable.HitResult.Early:
//             case Attackable.HitResult.Late:
//             case Attackable.HitResult.None:
//                 // Any of these → reset everything
//                 goodComboActive = false;
//                 goodComboPaused = false;
//                 perfectComboActive = false;
//                 pendingPerfect = 0;

//                 GoodComboCount = 0;
//                 PerfectComboCount = 0;
//                 break;
//         }
//     }

//     private void UpdateHighestCombo()
//     {
//         if (GoodComboCount > HighestGoodCombo)
//             HighestGoodCombo = GoodComboCount;

//         if (PerfectComboCount > HighestPerfectCombo)
//             HighestPerfectCombo = PerfectComboCount;
//     }

//     public void ResetScore()
//     {
//         TotalScore = 0;
//         GoodComboCount = 0;
//         PerfectComboCount = 0;
//         HighestGoodCombo = 0;
//         HighestPerfectCombo = 0;

//         goodComboActive = false;
//         goodComboPaused = false;
//         perfectComboActive = false;
//         pendingPerfect = 0;
//     }
// }

using UnityEngine;

public class RewardSystem : MonoBehaviour
{
    public int TotalScore { get; private set; } = 0;

    public int GoodComboCount { get; private set; } = 0;
    public int PerfectComboCount { get; private set; } = 0;

    public int HighestGoodCombo { get; private set; } = 0;
    public int HighestPerfectCombo { get; private set; } = 0;

    private bool goodComboActive = false;
    private bool perfectComboActive = false;
    private bool goodComboPaused = false;

    private int pendingGood = 0;
    private int pendingPerfect = 0;

    private ScoreDisplay scoreDisplay;

    void Awake()
    {
        scoreDisplay = FindFirstObjectByType <ScoreDisplay>();
    }

    public void ApplyScore(Attackable.HitResult result)
    {
        int basePoints = CalculateBasePoints(result);
        TotalScore += basePoints;

        HandleComboLogic(result);

        Debug.Log($"🎯 Hit: {result} | +{basePoints} | Score: {TotalScore} | Good x{GoodComboCount} | Perfect x{PerfectComboCount}");

        if (scoreDisplay != null)
            scoreDisplay.UpdateUI(result);
    }

    private int CalculateBasePoints(Attackable.HitResult result)
    {
        return result switch
        {
            Attackable.HitResult.Perfect => 100,
            Attackable.HitResult.Good => 75,
            Attackable.HitResult.Early => 50,
            Attackable.HitResult.Late => 10,
            _ => 0
        };
    }

    private void HandleComboLogic(Attackable.HitResult result)
    {
        switch (result)
        {
            case Attackable.HitResult.Perfect:
                if (perfectComboActive)
                {
                    PerfectComboCount++;
                    // Debug.Log($"🔥 Perfect combo continued! Count: {PerfectComboCount}");
                    Debug.Log($"<color=magenta>🔥 Perfect combo continued! Count: {PerfectComboCount}</color>");
                    UpdateHighestCombo();
                }
                else if (pendingPerfect == 1)
                {
                    // Debug.Log($"🔥 Perfect combo started! Count: 2");
                    Debug.Log("<color=magenta>🔥 Perfect combo started! Count: 2</color>");

                    // Second perfect → start combo
                    perfectComboActive = true;
                    PerfectComboCount = 2;
                    pendingPerfect = 0;

                    // Kill good combo
                    goodComboActive = false;
                    goodComboPaused = false;
                    pendingGood = 0;
                    GoodComboCount = 0;
                }
                else
                {
                    pendingPerfect = 1;

                    if (goodComboActive)
                        goodComboPaused = true;
                }

                break;

            case Attackable.HitResult.Good:
                if (perfectComboActive)
                {
                    // Debug.Log($"💥 Good combo continued! Count: {GoodComboCount}");
                    Debug.Log($"<color=cyan>💥 Good combo continued! Count: {GoodComboCount}</color>");

                    // Interrupt perfect combo
                    perfectComboActive = false;
                    // Debug.Log("❌ Perfect combo ended.");
                    Debug.Log("<color=red>❌ Perfect combo ended.</color>");

                    pendingPerfect = 0;
                    PerfectComboCount = 0;

                    if (goodComboPaused)
                    {
                        goodComboPaused = false;
                        goodComboActive = true;
                        GoodComboCount++;
                        UpdateHighestCombo();
                    }
                    else
                    {
                        pendingGood = 1;
                        goodComboActive = false;
                        GoodComboCount = 0;
                    }
                }
                else if (goodComboActive)
                {
                    GoodComboCount++;
                    UpdateHighestCombo();
                }
                else if (pendingGood == 1)
                {
                    // Debug.Log($"💥 Good combo started! Count: 2");
                    Debug.Log("<color=cyan>💥 Good combo started! Count: 2</color>");

                    // Second good hit → start combo
                    goodComboActive = true;
                    GoodComboCount = 2;
                    pendingGood = 0;
                }
                else
                {
                    pendingGood = 1;

                    // If perfect combo was paused, we resume good
                    if (goodComboPaused)
                    {
                        goodComboPaused = false;
                        goodComboActive = true;
                        GoodComboCount = 1;
                    }
                }

                pendingPerfect = 0;
                break;

            case Attackable.HitResult.Early:
            case Attackable.HitResult.Late:
            case Attackable.HitResult.None:
                ResetCombos();
                break;
        }
    }

    private void ResetCombos()
    {
        goodComboActive = false;
        goodComboPaused = false;
        perfectComboActive = false;

        pendingGood = 0;
        pendingPerfect = 0;

        GoodComboCount = 0;
        PerfectComboCount = 0;

        // if (goodComboActive) Debug.Log("❌ Good combo ended.");
        // if (perfectComboActive) Debug.Log("❌ Perfect combo ended.");
        if (goodComboActive) Debug.Log("<color=red>❌ Good combo ended.</color>");
        if (perfectComboActive) Debug.Log("<color=red>❌ Perfect combo ended.</color>");
    }

    private void UpdateHighestCombo()
    {
        if (GoodComboCount > HighestGoodCombo)
            HighestGoodCombo = GoodComboCount;

        if (PerfectComboCount > HighestPerfectCombo)
            HighestPerfectCombo = PerfectComboCount;
    }

    public void ResetScore()
    {
        TotalScore = 0;
        ResetCombos();
        HighestGoodCombo = 0;
        HighestPerfectCombo = 0;
    }
}

