using System.Collections.Generic;
using UnityEngine;

public class Attackable : MonoBehaviour
{
    // === Public Enums ===
    public enum AttackDirection
    {
        TopToBottom, BottomToTop, LeftToRight, RightToLeft,
        BottomLeftToTopRight, TopRightToBottomLeft, BottomRightToTopLeft, TopLeftToBottomRight
    }

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
    private CombatController combatCtrl;

    private int currentIndex = 0;
    private bool successTriggered = false;
    private bool isPlayerInside = false;

    void Awake()
    {
        if (Application.isPlaying)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            combatCtrl = player.GetComponent<CombatController>();
        }
    }

    void Update()
    {
        if (!Application.isPlaying || successTriggered || currentIndex >= colliders.Count || player == null)
            return;

        var activeCollider = colliders[currentIndex];

        if (!activeCollider.enabled) return;

        if (IsPlayerInside(activeCollider))
        {
            isPlayerInside = true;

            // if (Input.GetKeyDown(KeyCode.E))
            // {
            //     LatestHitResult = DetermineHitResult(currentIndex);
            //     Debug.Log($"✅ {LatestHitResult} hit in collider {currentIndex + 1}");

            //     DisableAllOtherColliders();
            //     successTriggered = true;
            // }

            if (combatCtrl.attacked && combatCtrl.succes)
            {
                LatestHitResult = DetermineHitResult(currentIndex);
                Debug.Log($"✅ {LatestHitResult} hit in collider {currentIndex + 1}");

                DisableAllOtherColliders();
                successTriggered = true;
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

