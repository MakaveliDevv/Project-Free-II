using UnityEngine;

public class RewardSystem : MonoBehaviour
{
    [SerializeField] private float comboMaxDelay = 2f;
    [SerializeField] private float comboWarningTime = 1.5f;

    public bool IsComboAboutToExpire => Time.time - lastComboTime > comboWarningTime
                                     && Time.time - lastComboTime <= comboMaxDelay;

    public int TotalScore { get; private set; } = 0;

    public int GoodComboCount { get; private set; } = 0;
    public int PerfectComboCount { get; private set; } = 0;

    public int HighestGoodCombo { get; private set; } = 0;
    public int HighestPerfectCombo { get; private set; } = 0;

    private float lastComboTime = -100f;
    private bool goodComboActive = false;
    private bool perfectComboActive = false;
    private bool goodComboPaused = false;

    private int pendingGood = 0;
    private int pendingPerfect = 0;


    private ScoreDisplay scoreDisplay;

    void Awake()
    {
        scoreDisplay = FindFirstObjectByType<ScoreDisplay>();
    }

    public void ApplyScore(Attackable.HitResult result)
    {
        bool comboRunning = goodComboActive || perfectComboActive || pendingGood > 0 || pendingPerfect > 0;
        if (comboRunning && Time.time - lastComboTime > comboMaxDelay)
        {
            Debug.Log("<color=red>⏳ Combo window expired – resetting combos.</color>");
            ResetCombos();
        }

        int basePoints = CalculateBasePoints(result);
        TotalScore += basePoints;

        HandleComboLogic(result);

        Debug.Log($"🎯 Hit: {result} | +{basePoints} | Score: {TotalScore} | Good x{GoodComboCount} | Perfect x{PerfectComboCount}");

        if (scoreDisplay != null)
            scoreDisplay.UpdateUI(result);

        lastComboTime = Time.time;
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

