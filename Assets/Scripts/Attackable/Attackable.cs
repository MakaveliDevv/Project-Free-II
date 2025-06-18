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

    private Collider childTriggerCollider;

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

    private bool hasReachedTarget = false;
    private bool hasExitedAfterTarget = false;


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
        childTriggerCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (successTriggered || player == null || shrinker == null) return;

        var target = Player.moveContrl.movementSystem.targetAttackable;

        if (!hasReachedTarget && target == this && Player.moveContrl.movementSystem.HasReachedTargetPoint())
        {
            hasReachedTarget = true;
            Debug.Log("✅ Target point reached");
        }

        if (hasReachedTarget && hasExitedAfterTarget)
        {
            Debug.Log("💥 Destroying box after exit");
            Destroy(gameObject);
            hasExitedAfterTarget = false;
        }

        if (Player.combatContrl.attacked && Player.combatContrl.success)
        {
            bool swag = target == this;
            Debug.Log($"same attackable -> {swag}");
            if (target != this) return;
            
            float ratio = shrinker.GetCurrentSizeRatio();
            LatestHitResult = DetermineHitResult(ratio);
            rewardSystem.ApplyScore(LatestHitResult);

            successTriggered = true;
            Player.combatContrl.attacked = false;
            Player.combatContrl.success = false;

            Destroy(gameObject, 0.2f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            bool isInside = childTriggerCollider.bounds.Contains(other.transform.position);

            if (!isInside && hasReachedTarget && !hasExitedAfterTarget)
            {
                hasExitedAfterTarget = true;
            }
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
}

