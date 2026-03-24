using UnityEngine;

public class HazardSpawner : MonoBehaviour {
    private GameObject prefab;
    private float speed;
    private Vector3 direction;
    private Transform player;

    public float minInterval;
    public float maxInterval;
    private float timer;

    public float spawnRangeZ = 25f; // New: If player is further than this, stop spawning

    public void Setup(GameObject hazardPrefab, float hazardSpeed, Vector3 dir, Transform playerRef) {
        prefab = hazardPrefab;
        speed = hazardSpeed;
        direction = dir;
        player = playerRef;
        timer = Random.Range(minInterval, maxInterval);
    }

    void Update() {
        if (player == null) return;

        // Check if player is nearby on the Z axis
        float distZ = Mathf.Abs(player.position.z - transform.position.z);
        if (distZ > spawnRangeZ) return; 

        timer -= Time.deltaTime;
        if (timer <= 0) {
            Spawn();
            timer = Random.Range(minInterval, maxInterval);
        }
    }

    void Spawn() {
        GameObject hazard = Instantiate(prefab, transform.position, Quaternion.identity);
        RollingHazard rh = hazard.GetComponent<RollingHazard>();
        if (rh != null) rh.Initialize(speed, direction);
    }
}