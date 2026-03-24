using UnityEngine;

public class SfxAutoVolume : MonoBehaviour {
    private AudioSource source;
    private float baseVolume;

    void Start() {
        source = GetComponent<AudioSource>();
        if (source != null) {
            // We store the volume you set in the Inspector as the "base"
            // This allows some sounds to be naturally quieter than others
            baseVolume = source.volume;
        }
    }

    void Update() {
        if (source != null && GameManager.Instance != null) {
            // This ensures that if the slider moves, the sound changes immediately
            source.volume = baseVolume * GameManager.Instance.savedSfxVolume;
        }
    }
}