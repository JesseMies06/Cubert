using UnityEngine;

public class FloatingIsland : MonoBehaviour {
    private float bobSpeed;
    private float bobAmount;
    private float offset;
    private Vector3 startPos;

    public void Setup(float speed, float amount) {
        bobSpeed = speed;
        bobAmount = amount;
        offset = Random.Range(0f, 100f); // Randomize start phase
        startPos = transform.position;
    }

    void Update() {
        // Smooth sine wave movement
        float newY = startPos.y + Mathf.Sin((Time.time + offset) * bobSpeed) * bobAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}