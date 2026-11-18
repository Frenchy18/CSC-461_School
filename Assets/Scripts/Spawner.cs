using UnityEngine;

public class Spawner : MonoBehaviour
{
    // Prefab of the current object to spawn
    public GameObject movingObject;
    // The initial position where object will spawn
    public Transform spawnPoint;
    // Configure the available lanes and colors in the Inspector.
    public Transform[] spawnPoints;
    public GameObject[] movingObjects;
    // How often objects spawn
    [Min(0.01f)] public float beat = 60f / 130f;
    // accumulate the time between spawns
    private float timer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if(timer >= Mathf.Max(0.01f, beat))
        {
            Transform point = spawnPoints != null && spawnPoints.Length > 0
                ? spawnPoints[Random.Range(0, spawnPoints.Length)] : spawnPoint;
            GameObject prefab = movingObjects != null && movingObjects.Length > 0
                ? movingObjects[Random.Range(0, movingObjects.Length)] : movingObject;
            if (point == null || prefab == null) return;

            // Instantiate a movingObject in the position of the spawnPoint
            GameObject movingObj = Instantiate(prefab, point.position, point.rotation);
            // Rotate the object in the appropriate direction
            movingObj.transform.Rotate(Vector3.forward, 45 * Random.Range(0, 8), Space.Self);
            // Count the beat
            timer %= Mathf.Max(0.01f, beat);
        }
    }
}
