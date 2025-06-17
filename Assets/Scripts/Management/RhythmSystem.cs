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
                var attack = box.GetComponent<Attackable>();
                if (attack != null)
                    attack.attackDirection = beat.attackDirection;
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
            if (!box.TryGetComponent<ShrinkOverTime>(out var shrink))
                shrink = box.AddComponent<ShrinkOverTime>();
            shrink.shrinkDuration = boxShrinkDuration;
            shrink.minScale = boxMinScale;
        }
    }
}
