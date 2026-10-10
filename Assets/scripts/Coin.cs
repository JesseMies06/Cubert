using UnityEngine;
using System.Collections;

public class Coin : MonoBehaviour {
    [Header("Settings")]
    public AudioClip collectSound;

    private bool isFalling = false;
    private Rigidbody rb;

    void Start() {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    void FixedUpdate() {
        if (isFalling) return;

        // Simplified Raycast to check the tile directly below
        RaycastHit hit;
        // Starting the ray slightly inside the coin and pointing down
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 1.5f)) {
            // Check for the FallingTile component on the object we hit (or its parent)
            FallingTile ft = hit.collider.GetComponentInParent<FallingTile>();
            
            // If the tile exists and has started its falling sequence
            if (ft != null && ft.isFalling) {
                StartFalling();
            }
        } else {
            // If there's absolutely nothing under the coin, it should fall
            StartFalling();
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && !isFalling) {
            if (GameManager.Instance != null) {
                GameManager.Instance.AddCoin(1);
            }

            if (collectSound != null) {
                AudioSource.PlayClipAtPoint(collectSound, transform.position, GameManager.Instance.savedSfxVolume);
            }

            Destroy(gameObject);
        }
    }

    void StartFalling() {
        if (isFalling) return;
        isFalling = true;

        if (rb != null) {
            rb.isKinematic = false; 
            rb.useGravity = true;
            // Apply the same tumble as the monkeys
            rb.AddTorque(new Vector3(Random.value, Random.value, Random.value) * 10f, ForceMode.Impulse);
        }

        // Clean up the object after it falls out of view
        Destroy(gameObject, 2.5f);
    }
}