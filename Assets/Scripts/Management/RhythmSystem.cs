using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Player;

[Serializable]
public enum BeatType { Attack, Interact }

[Serializable]
public class Beat
{
    public BeatType type;
    public float time;
    public AttackDirection attackDirection; 
}

public class RhythmSystem : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;

    [Header("Beat Map")]
    [Tooltip("List of beats (type and timestamp) to spawn boxes for")]
    [SerializeField] private List<Beat> beats = new List<Beat>();

    [Header("Prefabs")]
    [SerializeField] private GameObject attackBoxPrefab;
    [SerializeField] private GameObject interactBoxPrefab;

    [Header("Spawn Range (XY)")]
    [SerializeField] private Vector2 spawnRangeX = new(-5f, 5f);
    [SerializeField] private Vector2 spawnRangeY = new(-3f, 3f);

    [Header("Shrink Settings")]
    [SerializeField] private float boxShrinkDuration = 2f;
    [SerializeField] private float boxMinScale = 0.2f;

    [SerializeField] private float spawnOffsetZ = 1f;

    [Header("Spawn Spacing")]
    [SerializeField, Tooltip("Minimum XY distance between any two spawned boxes. Set 0 to disable.")]
    private float minSpawnSeparation = 0f;

    [SerializeField, Tooltip("How many random tries to find a valid separated spot before falling back.")]
    private int maxSpawnAttempts = 12;


    private GameObject player;

    private double songStartDSP;
    public static double SongStartDSP { get; private set; }

    private readonly List<Transform> spawnedBoxes = new();


    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Start()
    {
        musicSource.Play();
        songStartDSP = AudioSettings.dspTime;
        SongStartDSP = songStartDSP;

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        foreach (var beat in beats)
        {
            double waitUntil = songStartDSP + beat.time;
            double waitTime = waitUntil - AudioSettings.dspTime;
            if (waitTime > 0)
                yield return new WaitForSeconds((float)waitTime);

            SpawnBeat(beat);
        }
    }

    private void SpawnBeat(Beat beat)
    {
        Vector3 spawnPos = FindSpawnPosition(spawnRangeX, spawnRangeY, spawnOffsetZ);
        Vector3 spawnPosAttackable = FindSpawnPosition(spawnRangeX, spawnRangeY, spawnOffsetZ);

        GameObject box = null;

        switch (beat.type)
        {
            case BeatType.Attack:
                box = Instantiate(attackBoxPrefab, spawnPosAttackable, Quaternion.identity);
                var attack = box.GetComponent<Attackable>();
                if (attack != null)
                    attack.attackDirection = beat.attackDirection;

                // Rotate object so TOP (local +Y) points to A (no roll).
                Vector3 upDir = AttackDirectionToUp(beat.attackDirection);
                if (upDir.sqrMagnitude < 1e-6f) upDir = Vector3.up;
                box.transform.rotation = Quaternion.LookRotation(Vector3.forward, upDir);
                break;

            case BeatType.Interact:
                box = Instantiate(interactBoxPrefab, spawnPos, Quaternion.identity);
                var interact = box.GetComponent<Interactable>();
                if (interact != null)
                    interact.beatTime = beat.time;
                break;
        }

        if (box != null)
        {
            // Track for spacing
            spawnedBoxes.Add(box.transform);

            if (!box.TryGetComponent<ShrinkOverTime>(out var shrink))
                shrink = box.AddComponent<ShrinkOverTime>();
            shrink.shrinkDuration = boxShrinkDuration;
            shrink.minScale = boxMinScale;
        }
    }


    // private void SpawnBeat(Beat beat)
    // {
    //     Vector3 spawnPos = new(
    //         UnityEngine.Random.Range(spawnRangeX.x, spawnRangeX.y),
    //         UnityEngine.Random.Range(spawnRangeY.x, spawnRangeY.y),
    //         spawnOffsetZ);

    //     Vector3 spawnPosAttackable = new(
    //         UnityEngine.Random.Range(spawnRangeX.x, spawnRangeX.y),
    //         UnityEngine.Random.Range(spawnRangeY.x, spawnRangeY.y),
    //         spawnOffsetZ);

    //     GameObject box = null;

    //     switch (beat.type)
    //     {
    //         case BeatType.Attack:
    //             box = Instantiate(attackBoxPrefab, spawnPosAttackable, Quaternion.identity);
    //             var attack = box.GetComponent<Attackable>();
    //             if (attack != null)
    //                 attack.attackDirection = beat.attackDirection;
    //             // Rotate object towards the attack
    //             Vector3 upDir = AttackDirectionToUp(beat.attackDirection);
    //             if (upDir.sqrMagnitude < 1e-6f) upDir = Vector3.up;

    //             box.transform.rotation = Quaternion.LookRotation(Vector3.forward, upDir);
    //             break;

    //         case BeatType.Interact:
    //             box = Instantiate(interactBoxPrefab, spawnPos, Quaternion.identity);
    //             var interact = box.GetComponent<Interactable>();
    //             if (interact != null)
    //                 interact.beatTime = beat.time;
    //             break;
    //     }

    //     if (box != null)
    //     {
    //         if (!box.TryGetComponent<ShrinkOverTime>(out var shrink))
    //             shrink = box.AddComponent<ShrinkOverTime>();
    //         shrink.shrinkDuration = boxShrinkDuration;
    //         shrink.minScale = boxMinScale;
    //     }
    // }

    private static Vector3 AttackDirectionToUp(AttackDirection dir)
    {
        return dir switch
        {
            AttackDirection.TopToBottom => Vector3.down,// A is Bottom, B is Top
            AttackDirection.BottomToTop => Vector3.up,// A is Top,    B is Bottom
            AttackDirection.LeftToRight => Vector3.right,// A is Right,  B is Left
            AttackDirection.RightToLeft => Vector3.left,// A is Left,   B is Right
            AttackDirection.BottomLeftToTopRight => (Vector3.up + Vector3.right).normalized,// NE
            AttackDirection.TopRightToBottomLeft => (Vector3.down + Vector3.left).normalized,// SW
            AttackDirection.BottomRightToTopLeft => (Vector3.up + Vector3.left).normalized,// NW
            AttackDirection.TopLeftToBottomRight => (Vector3.down + Vector3.right).normalized,// SE
            _ => Vector3.up,
        };
    }
    
    private Vector3 FindSpawnPosition(Vector2 xRange, Vector2 yRange, float z)
    {
        // No spacing requested or nothing spawned yet
        if (minSpawnSeparation <= 0f || spawnedBoxes.Count == 0)
        {
            return new Vector3(
                UnityEngine.Random.Range(xRange.x, xRange.y),
                UnityEngine.Random.Range(yRange.x, yRange.y),
                z);
        }

        Vector3 bestPos = Vector3.zero;
        float bestMinDist = -1f;

        for (int i = 0; i < Mathf.Max(1, maxSpawnAttempts); i++)
        {
            var candidate = new Vector3(
                UnityEngine.Random.Range(xRange.x, xRange.y),
                UnityEngine.Random.Range(yRange.x, yRange.y),
                z);

            float minDist = float.MaxValue;
            Vector2 c2 = new(candidate.x, candidate.y);

            for (int j = 0; j < spawnedBoxes.Count; j++)
            {
                var t = spawnedBoxes[j];
                if (t == null) continue;

                Vector2 e2 = new Vector2(t.position.x, t.position.y);
                float d = Vector2.Distance(c2, e2);
                if (d < minDist) minDist = d;
                if (minDist < minSpawnSeparation) break; 
            }

            if (minDist >= minSpawnSeparation)
                return candidate;

            if (minDist > bestMinDist)
            {
                bestMinDist = minDist;
                bestPos = candidate;
            }
        }

        return bestPos;
    }
}
