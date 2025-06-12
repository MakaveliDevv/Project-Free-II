// using UnityEngine;
// using Assets.Scripts.Player;

// public class AttackableRhythmBox : MonoBehaviour
// {
//     public AttackDirection requiredDirection;

//     [Header("Windows around ideal size")]
//     public float perfectThreshold = 0.05f;
//     public float goodThreshold = 0.1f;
//     public float earlyLateThreshold = 0.2f;

//     private RewardSystem rewardSystem;
//     private ShrinkOverTime shrinker;
//     private Player player;
//     private bool hasBeenAttacked = false;

//     void Awake()
//     {
//         rewardSystem = FindFirstObjectByType<RewardSystem>();
//         shrinker = GetComponent<ShrinkOverTime>();
//         player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
//     }

//     void Update()
//     {
//         if (hasBeenAttacked || player == null || player.combatContrl == null) return;

//         if (player.combatContrl.attacked && player.combatContrl.success)
//         {
//             var attempted = player.combatContrl.GetAttemptedAttackDirection();
//             if (attempted.HasValue && attempted.Value == requiredDirection)
//             {
//                 hasBeenAttacked = true;

//                 float sizeRatio = shrinker.GetCurrentSizeRatio();
//                 float distanceFromIdeal = Mathf.Abs(sizeRatio - 0.5f);

//                 Attackable.HitResult result;
//                 if (distanceFromIdeal <= perfectThreshold)
//                     result = Attackable.HitResult.Perfect;
//                 else if (distanceFromIdeal <= goodThreshold)
//                     result = Attackable.HitResult.Good;
//                 else if (sizeRatio > 0.5f && distanceFromIdeal <= earlyLateThreshold)
//                     result = Attackable.HitResult.Early;
//                 else if (sizeRatio < 0.5f && distanceFromIdeal <= earlyLateThreshold)
//                     result = Attackable.HitResult.Late;
//                 else
//                     result = Attackable.HitResult.None;

//                 rewardSystem.ApplyScore(result);
//                 Destroy(gameObject, 0.2f);

//                 // reset attack flag so we don’t double-score
//                 player.combatContrl.attacked = false;
//                 player.combatContrl.success = false;
//             }
//         }
//     }
// }

using Assets.Scripts.Player;
using UnityEngine;

public class AttackableRhythmBox : MonoBehaviour
{
    public AttackDirection requiredDirection;

    [Header("Windows around ideal size")]
    public float perfectThreshold = 0.05f;
    public float goodThreshold = 0.1f;
    public float earlyLateThreshold = 0.2f;

    private RewardSystem rewardSystem;
    private ShrinkOverTime shrinker;
    private Player player;
    private bool hasBeenAttacked = false;

    void Awake()
    {
        rewardSystem = FindFirstObjectByType<RewardSystem>();
        shrinker = GetComponent<ShrinkOverTime>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    }

    void Update()
    {
        if (hasBeenAttacked || player == null || player.combatContrl == null) return;

        if (player.combatContrl.attacked && player.combatContrl.success)
        {
            var attempted = player.combatContrl.GetAttemptedAttackDirection();
            if (attempted.HasValue && attempted.Value == requiredDirection)
            {
                hasBeenAttacked = true;

                float sizeRatio = shrinker.GetCurrentSizeRatio();
                float distanceFromIdeal = Mathf.Abs(sizeRatio - 0.5f);

                Attackable.HitResult result;
                if (distanceFromIdeal <= perfectThreshold)
                    result = Attackable.HitResult.Perfect;
                else if (distanceFromIdeal <= goodThreshold)
                    result = Attackable.HitResult.Good;
                else if (sizeRatio > 0.5f && distanceFromIdeal <= earlyLateThreshold)
                    result = Attackable.HitResult.Early;
                else if (sizeRatio < 0.5f && distanceFromIdeal <= earlyLateThreshold)
                    result = Attackable.HitResult.Late;
                else
                    result = Attackable.HitResult.None;

                rewardSystem.ApplyScore(result);
                Destroy(gameObject, 0.2f);

                // reset attack flag so we don’t double-score
                player.combatContrl.attacked = false;
                player.combatContrl.success = false;
            }
        }
    }
}