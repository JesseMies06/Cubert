using UnityEngine;

public class CameraModeManager : MonoBehaviour {
    [Header("Camera GameObjects")]
    public GameObject normalCam;
    public GameObject thirdPersonCam;

    void Update() {
        if (GameManager.Instance == null) return;

        int mode = GameManager.Instance.cameraMode;

        // Toggle visibility based on mode
        if (normalCam != null) normalCam.SetActive(mode == 0);
        if (thirdPersonCam != null) thirdPersonCam.SetActive(mode == 1);
    }
}