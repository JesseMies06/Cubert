using UnityEngine;

public class CrumbleManager : MonoBehaviour {
    public LevelGenerator generator;
    public Transform player;

    [Header("Easy Settings")]
    public float easyBaseSpeed = 1f;
    public float easyThreshold = 12f;

    [Header("Normal Settings")]
    public float normalBaseSpeed = 2f;
    public float normalThreshold = 8f;

    [Header("Hard Settings")]
    public float hardBaseSpeed = 3.5f;
    public float hardThreshold = 5f;

    private float collapseZ = -5f;

    void Update() {
        if (!player || !generator) return;

        // Determine settings based on difficulty index from your GameManager
        float currentBaseSpeed = normalBaseSpeed;
        float currentThreshold = normalThreshold;

        if (GameManager.Instance != null) {
            // Using difficultyIndex to match your GameManager script
            switch (GameManager.Instance.difficultyIndex) {
                case 0:
                    currentBaseSpeed = easyBaseSpeed;
                    currentThreshold = easyThreshold;
                    break;
                case 1:
                    currentBaseSpeed = normalBaseSpeed;
                    currentThreshold = normalThreshold;
                    break;
                case 2:
                    currentBaseSpeed = hardBaseSpeed;
                    currentThreshold = hardThreshold;
                    break;
            }
        }

        float dist = player.position.z - collapseZ;
        float speed = (dist > currentThreshold) ? currentBaseSpeed + (dist - currentThreshold) * 1.5f : currentBaseSpeed;
        
        collapseZ += speed * Time.deltaTime;
        generator.CrumbleTilesBelow(collapseZ);
    }
}