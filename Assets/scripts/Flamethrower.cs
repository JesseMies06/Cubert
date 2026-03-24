using UnityEngine;
using System.Collections;

public class Flamethrower : MonoBehaviour {
    public ParticleSystem flameParticles;
    public Light chargeLight;       
    public float chargeTime = 1.5f, fireDuration = 1.5f, cooldownTime = 3.0f;
    
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip warningBeepSound;
    public AudioClip fireLoopSound;

    [HideInInspector] public bool isFiring = false;

    private bool hasBeepedThisFlash = false;

    void Start() {
        // Randomize direction: 0, 90, 180, or 270 degrees
        float[] rotations = { 0f, 90f, 180f, 270f };
        transform.rotation = Quaternion.Euler(0, rotations[Random.Range(0, rotations.Length)], 0);

        if (flameParticles == null) flameParticles = GetComponentInChildren<ParticleSystem>();
        if (chargeLight == null) chargeLight = GetComponentInChildren<Light>();
        
        StartCoroutine(TrapCycle());
    }

    IEnumerator TrapCycle() {
        while (true) {
            float elapsed = 0;

            while (elapsed < chargeTime) {
                elapsed += Time.deltaTime;
                
                if (chargeLight != null) {
                    // Use the same PingPong logic you had
                    float intensity = Mathf.PingPong(Time.time * 10, 5f);
                    chargeLight.intensity = intensity;

                    // SYNC SOUND TO LIGHT START:
                    // Trigger beep when light is near 0 and starting to rise
                    if (intensity > 0.1f && !hasBeepedThisFlash) {
                        if (audioSource != null && warningBeepSound != null) {
                            audioSource.PlayOneShot(warningBeepSound);
                        }
                        hasBeepedThisFlash = true; 
                    }
                    
                    // Reset flag once the light has dimmed back down significantly
                    // This allows it to beep again on the next upward pulse
                    if (intensity < 0.05f) {
                        hasBeepedThisFlash = false;
                    }
                }
                
                yield return null;
            }

            // Small buffer to prevent the loop sound from immediately cutting off the last beep
            yield return new WaitForSeconds(0.05f);

            isFiring = true;
            if (flameParticles != null) flameParticles.Play();
            
            // Start fire loop sound
            if (audioSource != null && fireLoopSound != null) {
                audioSource.clip = fireLoopSound;
                audioSource.loop = true;
                audioSource.Play();
            }

            yield return new WaitForSeconds(fireDuration);
            
            isFiring = false;
            if (flameParticles != null) flameParticles.Stop();
            if (chargeLight != null) chargeLight.intensity = 0;

            // Stop fire loop sound
            if (audioSource != null) {
                audioSource.Stop();
                audioSource.loop = false;
            }

            yield return new WaitForSeconds(cooldownTime);
        }
    }
}