using UnityEngine;

public class RewardSystem : MonoBehaviour
{
    [SerializeField] private ScoreDisplay scoreDisplay;

    public int TotalScore { get; private set; } = 0;
    public int ComboMultiplier { get; private set; } = 1;
    public int PerfectComboCount { get; private set; } = 0;

    public int PerfectCount { get; private set; }
    public int GoodCount { get; private set; }
    public int EarlyCount { get; private set; }
    public int LateCount { get; private set; }
    public int HighestComboAchieved { get; private set; }

    /// <summary>
    /// Calculates base score for a given HitResult.
    /// </summary>
    public int CalculateBasePoints(Attackable.HitResult hitResult)
    {
        return hitResult switch
        {
            Attackable.HitResult.Perfect => 100,
            Attackable.HitResult.Good => 75,
            Attackable.HitResult.Early => 50,
            Attackable.HitResult.Late => 10,
            _ => 0
        };
    }

    /// <summary>
    /// Applies the score with combo multiplier and updates the total.
    /// </summary>
    // public void ApplyScore(Attackable.HitResult result)
    // {
    //     UpdateCombo(result);

    //     int basePoints = CalculateBasePoints(result);
    //     int finalPoints = basePoints * ComboMultiplier;

    //     TotalScore += finalPoints;

    //     Debug.Log($"🎯 Hit: {result} | Base: {basePoints} | Combo x{ComboMultiplier} → +{finalPoints} points | Total: {TotalScore}");

    //     if (scoreDisplay != null) { scoreDisplay.UpdateUI(result); }
    // }

    public void ApplyScore(Attackable.HitResult result)
    {
        UpdateCombo(result);
        TrackHitCount(result);

        int basePoints = CalculateBasePoints(result);
        int finalPoints = basePoints * ComboMultiplier;

        TotalScore += finalPoints;

        if (ComboMultiplier > HighestComboAchieved)
            HighestComboAchieved = ComboMultiplier;

        Debug.Log($"🎯 Hit: {result} | Base: {basePoints} | Combo x{ComboMultiplier} → +{finalPoints} | Total: {TotalScore}");

        if (scoreDisplay != null)
            scoreDisplay.UpdateUI(result);
    }

    /// <summary>
    /// Handles combo logic: increment on Perfect, reset otherwise.
    /// </summary>
    private void UpdateCombo(Attackable.HitResult result)
    {
        if (result == Attackable.HitResult.Perfect)
        {
            PerfectComboCount++;
            ComboMultiplier = 1 + PerfectComboCount / 2; // e.g., 2 perfects = x2, 4 perfects = x3
        }
        else
        {
            PerfectComboCount = 0;
            ComboMultiplier = 1;
        }
    }

    private void TrackHitCount(Attackable.HitResult result)
    {
        switch (result)
        {
            case Attackable.HitResult.Perfect: PerfectCount++; break;
            case Attackable.HitResult.Good: GoodCount++; break;
            case Attackable.HitResult.Early: EarlyCount++; break;
            case Attackable.HitResult.Late: LateCount++; break;
        }
    }

    public void ResetScore()
    {
        TotalScore = 0;
        ComboMultiplier = 1;
        PerfectComboCount = 0;
    }
}
