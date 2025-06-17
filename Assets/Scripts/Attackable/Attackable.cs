using System.Collections.Generic;
using Assets.Scripts.Player;
using UnityEngine;

public class Attackable : MonoBehaviour
{
    private RewardSystem rewardSystem;

    public enum HitResult
    {
        Miss, Good, Perfect, Late
    }

    // === Public Variables ===
    [HideInInspector] public AttackDirection attackDirection;
    public HitResult LatestHitResult { get; private set; } = HitResult.Miss;

    [Header("Shrink Ratio Thresholds")]
    public HitRange perfectRange = new() { max = 1f, min = 0.7f };
    public HitRange goodRange = new() { max = 0.7f, min = 0.4f };
    public HitRange lateRange = new() { max = 0.4f, min = 0.2f };


    // === Private Variables ===
    private ShrinkOverTime shrinker;
    public Dictionary<string, AttackDirection> directions = new()
    {
        { "N-S", AttackDirection.TopToBottom },
        { "S-N", AttackDirection.BottomToTop },
        { "W-E", AttackDirection.LeftToRight },
        { "E-W", AttackDirection.RightToLeft },
        { "SW-NE", AttackDirection.BottomLeftToTopRight },
        { "NE-SW", AttackDirection.TopRightToBottomLeft },
        { "SE-NW", AttackDirection.BottomRightToTopLeft },
        { "NW-SE", AttackDirection.TopLeftToBottomRight },
    };

    private GameObject player;
    private Player Player;

    private bool successTriggered = false;

    void Awake()
    {
        if (Application.isPlaying)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            Player = player.GetComponent<Player>();
            rewardSystem = FindFirstObjectByType<RewardSystem>();
        }
    }

    void Start()
    {
        shrinker = GetComponent<ShrinkOverTime>();
    }

    void Update()
    {
        if (successTriggered || player == null || shrinker == null) return;

        if (Player.combatContrl.attacked && Player.combatContrl.success)
        {
            float ratio = shrinker.GetCurrentSizeRatio();
            LatestHitResult = DetermineHitResult(ratio);

            Debug.Log($"✅ {LatestHitResult} hit at size ratio {ratio:F2}");
            rewardSystem.ApplyScore(LatestHitResult);

            successTriggered = true;
            Player.combatContrl.attacked = false;
            Player.combatContrl.success = false;

            Destroy(gameObject, 0.2f);
        }
    }

    private HitResult DetermineHitResult(float ratio)
    {
        if (perfectRange.InRange(ratio))
            return HitResult.Perfect;
        else if (goodRange.InRange(ratio))
            return HitResult.Good;
        else if (lateRange.InRange(ratio))
            return HitResult.Late;
        else
            return HitResult.Miss;
    }

    // private HitResult DetermineHitResult(float ratio)
    // {
    //     // Divide the range (1 → minScale) into 3 equal segments
    //     float perfectThreshold = Mathf.Lerp(1f, shrinker.minScale, 1f / 3f);
    //     float goodThreshold = Mathf.Lerp(1f, shrinker.minScale, 2f / 3f);

    //     if (ratio >= goodThreshold)
    //         return HitResult.Perfect; // Box is still large
    //     else if (ratio >= perfectThreshold)
    //         return HitResult.Good;    // Mid-size
    //     else
    //         return HitResult.Late;    // Box is small
    // }
}

