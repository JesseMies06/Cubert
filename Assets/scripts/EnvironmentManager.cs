using UnityEngine;
using TMPro;

public class EnvironmentManager : MonoBehaviour {
    public Light directionalLight;
    public TextMeshProUGUI biomeText;
    public TextMeshProUGUI scoreText; 
    public float transitionSpeed = 1.5f;

    [Header("Camera Sync")]
    public Camera[] allCameras; 

    [Header("Audio")]
    public MusicManager musicManager;

    private Color targetLightColor;
    private float targetIntensity;
    private Color targetBGColor;

    public void SetInitialBiome(BiomeSettings biome) {
        targetLightColor = biome.lightColor;
        targetIntensity = biome.lightIntensity;
        targetBGColor = biome.backgroundColor;
        
        directionalLight.color = targetLightColor;
        directionalLight.intensity = targetIntensity;
        
        foreach (Camera cam in allCameras) {
            if (cam != null) cam.backgroundColor = targetBGColor;
        }

        if (biomeText != null) biomeText.text = biome.name;
        if (scoreText != null) scoreText.text = "0";

        if (musicManager != null && biome.biomeMusic != null) {
            musicManager.UpdateBiomeMusic(biome.biomeMusic);
        }
    }

    // This method is called by LevelGenerator once the player reaches the row
    public void TransitionToBiome(BiomeSettings biome) {
        UpdateVisuals(biome);
    }

    public void UpdateVisuals(BiomeSettings biome) {
        targetLightColor = biome.lightColor;
        targetIntensity = biome.lightIntensity;
        targetBGColor = biome.backgroundColor;
        
        if (biomeText != null) biomeText.text = biome.name;

        if (musicManager != null && biome.biomeMusic != null) {
            musicManager.UpdateBiomeMusic(biome.biomeMusic);
        }
    }

    void Update() {
        directionalLight.color = Color.Lerp(directionalLight.color, targetLightColor, Time.deltaTime * transitionSpeed);
        directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, targetIntensity, Time.deltaTime * transitionSpeed);
        
        foreach (Camera cam in allCameras) {
            if (cam != null) {
                cam.backgroundColor = Color.Lerp(cam.backgroundColor, targetBGColor, Time.deltaTime * transitionSpeed);
            }
        }
    }

    public void UpdateScore(int newScore) {
        if (scoreText != null) {
            scoreText.text = newScore.ToString();
            scoreText.transform.localScale = Vector3.one * 1.2f;
        }
    }

    void LateUpdate() {
        if (scoreText != null) {
            scoreText.transform.localScale = Vector3.Lerp(scoreText.transform.localScale, Vector3.one, Time.deltaTime * 5f);
        }
    }
}