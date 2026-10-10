using UnityEngine;

public class AudioFollower : MonoBehaviour {
    [Header("Follow Settings")]
    public Transform playerTransform;
    public bool followWhenDead = true;

    private Vector3 lastKnownPosition;

    void Start() {
        if (playerTransform == null) {
            // Automatically find the player by tag if not assigned
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform != null) {
            lastKnownPosition = playerTransform.position;
        }
    }

    void LateUpdate() {
        if (playerTransform != null && playerTransform.gameObject.activeInHierarchy) {
            // Follow the player's position
            transform.position = playerTransform.position;
            lastKnownPosition = playerTransform.position;
        } else {
            // If player is dead/inactive, stay at the last position 
            // so we still hear the world sounds from that spot.
            if (followWhenDead) {
                transform.position = lastKnownPosition;
            }
        }
    }
}