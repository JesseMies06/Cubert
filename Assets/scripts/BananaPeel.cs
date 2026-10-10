using UnityEngine;
using System.Collections;

public class BananaPeel : MonoBehaviour {
    [HideInInspector] public bool isBeingThrown = false; // Set by the Monkey
    private bool isFalling = false;
    private Rigidbody rb;

    void Start() {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // 1. Random Rotation on the Y axis
        transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        // 2. Snap to Ground immediately to prevent floating
        // We cast a ray from slightly above the spawn point downwards
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f)) {
            // Check if we hit a floor/tile (ignoring triggers)
            if (!hit.collider.isTrigger) {
                transform.position = hit.point;
            }
        }
    }

    void FixedUpdate() {
        // If we are still mid-air from a monkey throw, don't check for ground yet!
        if (isFalling || isBeingThrown) return;

        RaycastHit hit;
        // Check ground directly below
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 1.5f)) {
            FallingTile ft = hit.collider.GetComponentInParent<FallingTile>();
            
            if (ft != null && ft.isFalling) {
                StartFalling();
            }
        } else {
            // No ground? Start falling.
            StartFalling();
        }
    }

    private void OnTriggerEnter(Collider other) {
        // Only slip if the banana has landed and isn't currently falling into the abyss
        if (other.CompareTag("Player") && !isFalling && !isBeingThrown) {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null) {
                pc.SlipOnBanana();
                Destroy(gameObject); 
            }
        }
    }

    void StartFalling() {
        if (isFalling) return;
        isFalling = true;

        if (rb != null) {
            rb.isKinematic = false; 
            rb.useGravity = true;
            rb.AddTorque(new Vector3(Random.value, Random.value, Random.value) * 15f, ForceMode.Impulse);
        }

        Destroy(gameObject, 2.5f);
    }
}