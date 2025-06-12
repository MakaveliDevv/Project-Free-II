// using UnityEngine;

// public class BoxSpawner : MonoBehaviour
// {
//     [Header("Spawn Settings")]
//     public GameObject boxPrefabA; // First prefab
//     public GameObject boxPrefabB; // Second prefab
//     public float spawnInterval = 2f;
//     public float spawnDistanceZ = 20f;
//     public Vector2 spawnRangeX = new Vector2(-5f, 5f);
//     public Vector2 spawnRangeY = new Vector2(-3f, 3f);
//     public float boxMoveSpeed = 6f;

//     float _timer;

//     void Start()
//     {
//         SpawnBox();
//     }

//     void Update()
//     {
//         _timer += Time.deltaTime;
//         if (_timer >= spawnInterval)
//         {
//             _timer = 0f;
//             SpawnBox();
//         }
//     }

//     void SpawnBox()
//     {
//         float x = Random.Range(spawnRangeX.x, spawnRangeX.y);
//         float y = Random.Range(spawnRangeY.x, spawnRangeY.y);
//         Vector3 spawnPos = new(x, y, spawnDistanceZ);

//         // Randomly choose between prefab A or B
//         GameObject chosenPrefab = Random.value < 0.5f ? boxPrefabA : boxPrefabB;

//         GameObject box = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);
        
//         var mover = box.AddComponent<BoxMover>();
//         mover.speed = boxMoveSpeed;
//     }
// }


using UnityEngine;

public class BoxSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject interactable;
    public GameObject attackable;
    public float spawnInterval = 2f;
    public float spawnDistanceZ = 20f;
    public float boxMoveSpeed = 6f;

    [Header("Spawn Range")]
    public Vector2 spawnRangeX = new Vector2(-5f, 5f);
    public Vector2 spawnRangeY = new Vector2(-3f, 3f);

    [Header("Spawn Control")]
    public float maxOffsetFromPrevious = 2f;

    float _timer;
    Vector3 _lastSpawnPos;
    bool _hasSpawnedOnce = false;

    void Start()
    {
        SpawnBox(); // Spawn the first one
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            SpawnBox();
        }
    }

    void SpawnBox()
    {
        Vector3 newPos;

        if (!_hasSpawnedOnce)
        {
            // First box: random position
            float x = Random.Range(spawnRangeX.x, spawnRangeX.y);
            float y = Random.Range(spawnRangeY.x, spawnRangeY.y);
            newPos = new Vector3(x, y, spawnDistanceZ);
            _hasSpawnedOnce = true;
        }
        else
        {
            // Clamp next box within max offset of previous
            float x = Mathf.Clamp(
                _lastSpawnPos.x + Random.Range(-maxOffsetFromPrevious, maxOffsetFromPrevious),
                spawnRangeX.x, spawnRangeX.y
            );

            float y = Mathf.Clamp(
                _lastSpawnPos.y + Random.Range(-maxOffsetFromPrevious, maxOffsetFromPrevious),
                spawnRangeY.x, spawnRangeY.y
            );

            newPos = new Vector3(x, y, spawnDistanceZ);
        }

        // Randomly choose prefab A or B
        GameObject prefab = Random.value < 0.5f ? interactable : attackable;

        GameObject box = Instantiate(prefab, newPos, Quaternion.identity);
        // var mover = box.AddComponent<BoxMover>();
        // mover.speed = boxMoveSpeed;

        _lastSpawnPos = newPos;
    }
}
