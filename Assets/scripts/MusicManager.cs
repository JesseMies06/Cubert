using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour {
    [Header("Global Settings")]
    [Range(0f, 1f)] public float masterMusicVolume = 1f;
    public float fadeSpeed = 1.5f;

    private Dictionary<AudioClip, AudioSource> musicLayers = new Dictionary<AudioClip, AudioSource>();
    private AudioSource currentActiveSource;
    private LevelGenerator levelGen;
    private bool isDead = false; 

    void Start() {
        SyncVolume();
        levelGen = Object.FindFirstObjectByType<LevelGenerator>();
        
        if (levelGen != null) {
            foreach (var biome in levelGen.biomes) {
                if (biome.biomeMusic != null && !musicLayers.ContainsKey(biome.biomeMusic)) {
                    AudioSource newSource = gameObject.AddComponent<AudioSource>();
                    newSource.clip = biome.biomeMusic;
                    newSource.loop = true;
                    newSource.volume = 0f;
                    newSource.pitch = 1f;
                    newSource.playOnAwake = false;
                    newSource.Play();
                    musicLayers.Add(biome.biomeMusic, newSource);
                    
                    if (currentActiveSource == null && biome == levelGen.biomes[0]) {
                        currentActiveSource = newSource;
                        currentActiveSource.volume = masterMusicVolume;
                    }
                }
            }
        }
    }

    void Update() {
        if (isDead) return; 

        SyncVolume();
        foreach (var layer in musicLayers.Values) {
            if (layer == currentActiveSource && !IsFading()) {
                layer.volume = masterMusicVolume;
                layer.pitch = 1f; 
            } else if (!IsFading()) {
                layer.volume = 0f;
            }
        }
    }

    private void SyncVolume() {
        if (GameManager.Instance != null) {
            masterMusicVolume = GameManager.Instance.savedVolume;
        }
    }

    private bool IsFading() {
        foreach(var s in musicLayers.Values) {
            if (s.volume > 0 && s.volume < (masterMusicVolume - 0.01f)) return true;
        }
        return false;
    }

    public void UpdateBiomeMusic(AudioClip nextClip) {
        if (nextClip == null || isDead) return;
        if (musicLayers.TryGetValue(nextClip, out AudioSource targetSource)) {
            if (targetSource == currentActiveSource) return;
            // We use a specific Stop for the Fade coroutine only to avoid killing the death routine
            StopCoroutine("FadeToSource"); 
            StartCoroutine(FadeToSource(targetSource));
        }
    }

    IEnumerator FadeToSource(AudioSource targetSource) {
        float elapsed = 0;
        float duration = 1f / fadeSpeed;
        Dictionary<AudioSource, float> startVolumes = new Dictionary<AudioSource, float>();
        foreach (var source in musicLayers.Values) {
            startVolumes[source] = source.volume;
        }

        while (elapsed < duration) {
            if (isDead) yield break; 

            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            SyncVolume();
            foreach (var source in musicLayers.Values) {
                float targetVol = (source == targetSource) ? masterMusicVolume : 0f;
                source.volume = Mathf.Lerp(startVolumes[source], targetVol, percent);
            }
            yield return null;
        }
        currentActiveSource = targetSource;
        currentActiveSource.volume = masterMusicVolume;
    }

    public IEnumerator PitchDownRoutine() {
        if (isDead) yield break; 
        isDead = true; 
        
        StartCoroutine(FadeOutWorldSounds());

        // Capture every layer that is currently audible
        List<AudioSource> audibleLayers = new List<AudioSource>();
        Dictionary<AudioSource, float> startPitches = new Dictionary<AudioSource, float>();
        Dictionary<AudioSource, float> startVolumes = new Dictionary<AudioSource, float>();

        foreach (var layer in musicLayers.Values) {
            if (layer.volume > 0.01f) {
                audibleLayers.Add(layer);
                startPitches[layer] = layer.pitch;
                startVolumes[layer] = layer.volume;
            }
        }

        float elapsed = 0f;
        float duration = 1.5f; 

        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float percent = elapsed / duration;
            
            foreach (var layer in audibleLayers) {
                if (layer != null) {
                    layer.pitch = Mathf.Lerp(startPitches[layer], 0.4f, percent);
                    layer.volume = Mathf.Lerp(startVolumes[layer], startVolumes[layer] * 0.5f, percent);
                }
            }
            yield return null;
        }
        
        foreach (var layer in audibleLayers) {
            if (layer != null) layer.pitch = 0.4f;
        }
    }

    IEnumerator FadeOutWorldSounds() {
        AudioSource[] allSources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        float fadeDuration = 0.5f; 
        float elapsed = 0f;

        Dictionary<AudioSource, float> otherSources = new Dictionary<AudioSource, float>();
        foreach (var source in allSources) {
            if (source != null && !musicLayers.ContainsValue(source) && source.isPlaying) {
                otherSources[source] = source.volume;
            }
        }

        while (elapsed < fadeDuration) {
            elapsed += Time.unscaledDeltaTime;
            float percent = elapsed / fadeDuration;

            foreach (var kvp in otherSources) {
                if (kvp.Key != null) {
                    kvp.Key.volume = Mathf.Lerp(kvp.Value, 0f, percent);
                }
            }
            yield return null;
        }

        foreach (var source in otherSources.Keys) {
            if (source != null) source.Stop();
        }
    }
}