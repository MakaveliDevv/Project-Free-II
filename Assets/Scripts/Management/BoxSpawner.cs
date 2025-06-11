using UnityEngine;

public class BoxSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject boxPrefab;
    public float spawnInterval = 2f;
    public float spawnDistanceZ = 20f;
    public Vector2 spawnRangeX = new Vector2(-5f, 5f);
    public Vector2 spawnRangeY = new Vector2(-3f, 3f);
    public float boxMoveSpeed = 6f;

    float _timer;

    void Start()
    {
        SpawnBox();
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
        float x = Random.Range(spawnRangeX.x, spawnRangeX.y);
        float y = Random.Range(spawnRangeY.x, spawnRangeY.y);
        Vector3 spawnPos = new(x, y, spawnDistanceZ);

        GameObject b = Instantiate(boxPrefab, spawnPos, Quaternion.identity);
        // add simple mover
        var mover = b.AddComponent<BoxMover>();
        mover.speed = boxMoveSpeed;
    }
}

