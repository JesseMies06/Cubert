using UnityEngine;

public class CrumbleFollower : MonoBehaviour {
    private LevelGenerator generator;
    
    [Header("Movement Settings")]
    public float zOffset = -2f;    // Sit slightly behind the crumble line
    public float smoothSpeed = 5f; // How fast the sound catches up
    public float fixedY = 1.0f;    // Keep the sound at player height

    void Start() {
        generator = Object.FindFirstObjectByType<LevelGenerator>();
        if (generator == null) {
            Debug.LogError("CrumbleFollower: No LevelGenerator found in scene!");
        }
    }

    void LateUpdate() {
        if (generator == null) return;

        // 1. Get the Z from the generator
        float targetZ = generator.lastDeleteZ + zOffset;

        // 2. Follow the player's X (so sound is centered behind them)
        float targetX = 0;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) {
            targetX = player.transform.position.x;
        }

        // 3. Create the target position (ONLY Z and X change, Y stays fixed)
        Vector3 targetPos = new Vector3(targetX, fixedY, targetZ);
        
        // 4. Move the object
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);

        // Optional: Uncomment to see the movement in the console
        // Debug.Log($"Crumble Audio Position: {transform.position}");
    }
}