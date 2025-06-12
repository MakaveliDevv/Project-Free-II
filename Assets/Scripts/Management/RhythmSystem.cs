// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using Assets.Scripts.Player; 

// [Serializable]
// public enum BeatType { Attack, Interact }

// [Serializable]
// public class Beat
// {
//     public BeatType type;
//     public float time; // Time (in seconds) from song start
//     public AttackDirection attackDirection; // Used only for Attack beats
// }

// /// <summary>
// /// Spawns rhythm-based boxes (attackable or interactable) in sync with the music.
// /// Calculates spawn timing so boxes reach the player on-beat.
// /// </summary>
// public class RhythmSystem : MonoBehaviour
// {
//     [Header("Audio")]
//     [Tooltip("AudioSource with the rhythm track (it will play on Start)")]
//     [SerializeField] private AudioSource musicSource;

//     [Header("Beat Map")]
//     [Tooltip("List of beats (type and timestamp) to spawn boxes for")]
//     [SerializeField] private List<Beat> beats = new List<Beat>();

//     [Header("Prefabs & Movement")]
//     [Tooltip("Prefab with Attackable component and timing colliders configured")]
//     [SerializeField] private GameObject attackBoxPrefab;
//     [Tooltip("Prefab with InteractableRhythmBox component and timing colliders configured")]
//     [SerializeField] private GameObject interactBoxPrefab;

//     [Tooltip("Z distance at which boxes spawn")]
//     [SerializeField] private float spawnDistanceZ = 20f;
//     [Tooltip("Speed at which boxes move toward the player (must match BoxMover.speed)")]
//     [SerializeField] private float boxMoveSpeed = 6f;

//     [Header("Spawn Position Range")]
//     [SerializeField] private Vector2 spawnRangeX = new Vector2(-5f, 5f);
//     [SerializeField] private Vector2 spawnRangeY = new Vector2(-3f, 3f);

//     private double songStartDSP;
//     private float travelTime;

//     public static double SongStartDSP { get; private set; }

//     void Start()
//     {
//         travelTime = spawnDistanceZ / boxMoveSpeed;

//         musicSource.Play();
//         songStartDSP = AudioSettings.dspTime;

//         StartCoroutine(SpawnRoutine());
//     }

//     private IEnumerator SpawnRoutine()
//     {
//         foreach (var beat in beats)
//         {
//             double targetSpawnDSP = songStartDSP + beat.time - travelTime;
//             double waitTime = targetSpawnDSP - AudioSettings.dspTime;
//             if (waitTime > 0)
//                 yield return new WaitForSeconds((float)waitTime);

//             SpawnBeat(beat);
//         }
//     }
    
//     private void SpawnBeat(Beat beat)
//     {
//         Vector3 spawnPos = new(
//             UnityEngine.Random.Range(spawnRangeX.x, spawnRangeX.y),
//             UnityEngine.Random.Range(spawnRangeY.x, spawnRangeY.y),
//             spawnDistanceZ);

//         GameObject box = null;
//         switch (beat.type)
//         {
//             case BeatType.Attack:
//                 box = Instantiate(attackBoxPrefab, spawnPos, Quaternion.identity);
//                 var attackable = box.GetComponent<Attackable>();
//                 if (attackable != null)
//                     attackable.attackDirection = beat.attackDirection;
//                 break;

//             case BeatType.Interact:
//                 box = Instantiate(interactBoxPrefab, spawnPos, Quaternion.identity);
//                 var iBox = box.GetComponent<InteractableRhythmBox>();
//                 if (iBox != null)
//                     iBox.beatTime = beat.time;
//                 break;
//         }

//         // if (box != null)
//         // {
//         //     if (!box.TryGetComponent<Interactable>(out var mover))
//         //         mover = box.AddComponent<Interactable>();
//         //     mover.speed = boxMoveSpeed;
//         // }
//     }
// }


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
    public float time; // Time (in seconds) from song start
    public AttackDirection attackDirection; // Used only for Attack beats
}

public class RhythmSystem : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("AudioSource with the rhythm track (it will play on Start)")]
    [SerializeField] private AudioSource musicSource;

    [Header("Beat Map")]
    [Tooltip("List of beats (type and timestamp) to spawn boxes for")]
    [SerializeField] private List<Beat> beats = new List<Beat>();

    [Header("Prefabs")]
    [Tooltip("Prefab with AttackableRhythmBox component")]
    [SerializeField] private GameObject attackBoxPrefab;
    [Tooltip("Prefab with InteractableRhythmBox component")]
    [SerializeField] private GameObject interactBoxPrefab;

    [Header("Spawn Range (XY only)")]
    [SerializeField] private Vector2 spawnRangeX = new Vector2(-5f, 5f);
    [SerializeField] private Vector2 spawnRangeY = new Vector2(-3f, 3f);

    [Header("Shrink Settings")]
    [SerializeField] private float boxShrinkDuration = 2f;
    [SerializeField] private float boxMinScale = 0.2f;

    private double songStartDSP;
    public static double SongStartDSP { get; private set; }

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
        Vector3 spawnPos = new(
            UnityEngine.Random.Range(spawnRangeX.x, spawnRangeX.y),
            UnityEngine.Random.Range(spawnRangeY.x, spawnRangeY.y),
            0f);

        GameObject box = null;

        switch (beat.type)
        {
            case BeatType.Attack:
                box = Instantiate(attackBoxPrefab, spawnPos, Quaternion.identity);
                var attack = box.GetComponent<AttackableRhythmBox>();
                if (attack != null)
                    attack.requiredDirection = beat.attackDirection;
                break;

            case BeatType.Interact:
                box = Instantiate(interactBoxPrefab, spawnPos, Quaternion.identity);
                var interact = box.GetComponent<InteractableRhythmBox>();
                if (interact != null)
                    interact.beatTime = beat.time;
                break;
        }

        if (box != null)
        {
            if (!box.TryGetComponent<ShrinkOverTime>(out var shrink))
                shrink = box.AddComponent<ShrinkOverTime>();
            shrink.shrinkDuration = boxShrinkDuration;
            shrink.minScale = boxMinScale;
        }
    }
}
