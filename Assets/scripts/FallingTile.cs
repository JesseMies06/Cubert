using UnityEngine;
using System.Collections;

public class FallingTile : MonoBehaviour {
    // Changed to public so MonkeyAI can check if the ground is safe
    public bool isFalling = false;
    
    private float fallVelocity = 0f;
    private float gravity = 30f; // Fast but smooth acceleration

    public void StartFalling() {
        if (isFalling) return;
        isFalling = true;
        StartCoroutine(CrumbleSequence());
    }

    IEnumerator CrumbleSequence() {
        // Add a random delay so they don't all drop at the exact same frame
        float randomDelay = Random.Range(0f, 0.5f);
        yield return new WaitForSeconds(randomDelay);

        // Subtle shake before falling
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        while (elapsed < 0.15f) {
            transform.position = originalPos + Random.insideUnitSphere * 0.04f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;

        // Smooth accelerated fall
        while (transform.position.y > -15f) {
            fallVelocity += gravity * Time.deltaTime;
            transform.position += Vector3.down * fallVelocity * Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}