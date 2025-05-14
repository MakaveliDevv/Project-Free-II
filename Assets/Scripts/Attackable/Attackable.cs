using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[ExecuteAlways]
public class Attackable : MonoBehaviour
{
    public enum AttackDirection
    {

        TopToBottom,           // ↓
        BottomToTop,           // ↑
        LeftToRight,           // →
        RightToLeft,           // ←
        BottomLeftToTopRight,  // ↗
        TopRightToBottomLeft,  // ↙
        BottomRightToTopLeft,  // ↖
        TopLeftToBottomRight   // ↘
    }

    public AttackDirection attackDirection;

    // Map "A-B" → AttackDirection
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

    private BoxCollider col;
    public Vector3 colSize;
    public Vector3 colOffset;


    void Awake()
    {
        col = GetComponent<BoxCollider>();
    }

    void Start()
    {
        col.isTrigger = true;
        col.size = colSize;
        col.center = colOffset;
    }

    void OnValidate()
    {
        if (col == null)
            col = GetComponent<BoxCollider>();

        if (col != null)
        {
            col.isTrigger = false;
            col.size = colSize;
            col.center = colOffset;
        }
    }

    void Update()
    {

    }
}



