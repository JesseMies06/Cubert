using UnityEngine;

public class LoadingIcon : MonoBehaviour {
    public float rotationSpeed = 200f;

    void Update() {
        // Using localEulerAngles directly is often more stable for UI during hitches
        Vector3 currentRotation = transform.localEulerAngles;
        currentRotation.z -= rotationSpeed * Time.unscaledDeltaTime;
        transform.localEulerAngles = currentRotation;
    }
}