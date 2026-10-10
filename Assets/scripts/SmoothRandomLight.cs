using UnityEngine;

[RequireComponent(typeof(Light))]
public class SmoothRandomLight : MonoBehaviour
{
    [Header("Intensity Settings")]
    public float minIntensity = 0.8f;
    public float maxIntensity = 1.2f;

    [Header("Movement Settings")]
    public float speed = 1f;

    private Light lightSource;
    private float noiseOffset;

    void Awake()
    {
        lightSource = GetComponent<Light>();
        // Random offset so every light behaves differently
        noiseOffset = Random.Range(0f, 1000f);
    }

    void Update()
    {
        float noiseValue = Mathf.PerlinNoise(noiseOffset, Time.time * speed);
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, noiseValue);
        lightSource.intensity = intensity;
    }
}
