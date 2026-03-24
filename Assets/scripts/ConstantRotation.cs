using UnityEngine;

public class ConstantRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Degrees per second to rotate around the Y axis")]
    public float rotationSpeed = 20f;

    // Update is called once per frame
    void Update()
    {
        // Rotate the object around its local Y axis
        // Time.deltaTime ensures the speed is consistent regardless of frame rate
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }
}