using System.Collections.Generic;
using Assets.Scripts.Player;
using UnityEngine;

public class Attackable : MonoBehaviour
{
    private RewardSystem rewardSystem;

    public enum HitResult
    {
        None, Early, Good, Perfect, Late
    }

    // === Public Variables ===
    public AttackDirection attackDirection;
    public Vector3 colSize = new(1, 1, 1);
    public Vector3 colOffset = Vector3.zero;
    public HitResult LatestHitResult { get; private set; } = HitResult.None;

    // === Serialized Private Variables ===
    [SerializeField] private List<BoxCollider> colliders = new();

    // === Private Variables ===
    public readonly Dictionary<string, AttackDirection> directions = new()
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

    private int currentIndex = 0;
    private bool successTriggered = false;
    private bool isPlayerInside = false;

    void Awake()
    {
        if (Application.isPlaying)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            Player = player.GetComponent<Player>();
            rewardSystem = FindFirstObjectByType <RewardSystem>(); 
        }
    }

    void Update()
    {
        if (successTriggered || currentIndex >= colliders.Count || player == null)
            return;

        var activeCollider = colliders[currentIndex];

        if (!activeCollider.enabled) return;

        if (IsPlayerInside(activeCollider))
        {
            isPlayerInside = true;

            if (Player.combatContrl.attacked && Player.combatContrl.success)
            {
                LatestHitResult = DetermineHitResult(currentIndex);
                Debug.Log($"✅ {LatestHitResult} hit in collider {currentIndex + 1}");
                rewardSystem.ApplyScore(LatestHitResult);

                DisableAllOtherColliders();
                successTriggered = true;
                Player.combatContrl.attacked = false;
                Player.combatContrl.success = false;
            }
        }
        else if (isPlayerInside)
        {
            Debug.Log($"❌ Failed in collider {currentIndex + 1}");

            activeCollider.enabled = false;
            currentIndex++;
            isPlayerInside = false;
        }
    }

    private HitResult DetermineHitResult(int index)
    {
        return index switch
        {
            0 => HitResult.Early,
            1 => HitResult.Good,
            2 => HitResult.Perfect,
            3 => HitResult.Late,
            _ => HitResult.None
        };
    }

    private bool IsPlayerInside(BoxCollider col)
    {
        return col.bounds.Contains(player.transform.position);
    }

    private void DisableAllOtherColliders()
    {
        for (int i = 0; i < colliders.Count; i++)
        {
            if (i != currentIndex)
            {
                colliders[i].enabled = false;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (var col in colliders)
        {
            if (col != null)
                Gizmos.DrawWireCube(col.transform.position + col.center, col.size);
        }
    }
}

