using UnityEngine;

public class CloudMovement : MonoBehaviour {
    private float moveSpeed;
    private float bobSpeed;
    private float bobAmount;
    private float offset;
    private Vector3 startPos;

    public void Setup(float speed, float bSpeed, float bAmount) {
        moveSpeed = speed;
        bobSpeed = bSpeed;
        bobAmount = bAmount;
        offset = Random.Range(0f, 100f);
        startPos = transform.position;
    }

    void Update() {
        // Independent movement "backwards" (Negative Z)
        startPos += Vector3.back * moveSpeed * Time.deltaTime;

        // Combine with a gentle bobbing so they don't look like static blocks
        float newY = startPos.y + Mathf.Sin((Time.time + offset) * bobSpeed) * bobAmount;
        
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // Optional: Self-destruct if they go too far back to save memory
        if (transform.position.z < -20f) {
            Destroy(gameObject);
        }
    }
}