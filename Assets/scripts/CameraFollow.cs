using UnityEngine;

public class CameraFollow : MonoBehaviour {
    public Transform target;        // Drag your Player here
    public float smoothTime = 0.2f; // How "heavy" the camera feels
    public Vector3 offset = new Vector3(5, 5, -5); // The angle/distance from player

    private Vector3 currentVelocity = Vector3.zero;

    void Start() {
        // If you've already positioned the camera in the editor, 
        // this calculates the offset automatically.
        if (target != null) {
            // offset = transform.position - target.position; 
        }
    }

    // LateUpdate runs after the Player's Update/Coroutine movement
    void LateUpdate() {
        if (target == null) return;

        // Define our target position based on the player's position + the offset
        Vector3 targetPosition = target.position + offset;

        // Smoothly move the camera to that target position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }
}