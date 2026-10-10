using UnityEngine;
using System.Collections.Generic;

public class RollingHazard : MonoBehaviour {
    [Header("Behavior Toggles")]
    public bool shouldRoll = true;          // Toggle for visual rotation
    public bool shouldFallInLiquids = true; // Toggle for Water/Quicksand
    public bool shouldFallAtEdges = true;   // Toggle for Map Boundaries

    [Header("Visuals")]
    public GameObject wallShatterPrefab; 
    
    private float speed;
    private Vector3 direction;
    private bool isFalling = false;
    private float fallVelocity = 0f;
    
    private List<Material> hazardMaterials = new List<Material>();
    private Transform modelContainer; 
    private float mapBoundary = 4.5f;

    // SFX Logic
    private AudioSource movementAudio;

    public void Initialize(float moveSpeed, Vector3 moveDir) {
        speed = moveSpeed;
        direction = moveDir;
        transform.forward = direction;

        // Setup AudioSource for rolling loop
        movementAudio = GetComponent<AudioSource>();
        if (movementAudio != null) {
            movementAudio.loop = true;
            if (GameManager.Instance != null) movementAudio.volume = GameManager.Instance.savedSfxVolume;
            movementAudio.Play();
        }

        // Ensure we have a shatter effect loaded
        if (wallShatterPrefab == null) {
            wallShatterPrefab = Resources.Load<GameObject>("HitEffect");
        }

        // Get the model container (child 0) for rotation
        if (transform.childCount > 0) {
            modelContainer = transform.GetChild(0);
        }

        // Collect all materials to handle alpha fading at edges
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers) {
            hazardMaterials.Add(rend.material);
        }
        
        // Start semi-transparent as it spawns outside the map
        SetAlpha(0.5f);
    }

    void Update() {
        // Sync rolling sound volume in real-time
        if (movementAudio != null && GameManager.Instance != null) {
            movementAudio.volume = GameManager.Instance.savedSfxVolume;
        }

        if (isFalling) {
            // Smooth accelerated fall (Uses unscaled to keep moving during death freeze)
            fallVelocity += 30f * Time.unscaledDeltaTime;
            transform.position += Vector3.down * fallVelocity * Time.unscaledDeltaTime;

            if (transform.position.y < -10f) Destroy(gameObject);
            return;
        }

        // Standard movement
        transform.position += direction * speed * Time.deltaTime;

        // Visual rotation of the model
        if (shouldRoll && modelContainer != null) {
            Vector3 rollAxis = Vector3.Cross(Vector3.up, direction);
            modelContainer.Rotate(rollAxis, speed * 150f * Time.deltaTime, Space.World);
        }

        // Alpha fading based on map boundaries
        float currentX = transform.position.x;
        if (Mathf.Abs(currentX) <= mapBoundary) {
            SetAlpha(1.0f);
        } else {
            SetAlpha(0.2f);
        }

        // Self-destruct if it travels way off screen
        if (Mathf.Abs(currentX) > 25f) {
            Destroy(gameObject);
        }
        
        CheckCollisions();
    }

    void SetAlpha(float alpha) {
        foreach (Material mat in hazardMaterials) {
            if (mat.HasProperty("_Color")) {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
        }
    }

    void CheckCollisions() {
        float currentX = transform.position.x;
        bool isInsideMap = Mathf.Abs(currentX) <= mapBoundary;

        // 1. WATER / QUICKSAND / HOLE DETECTION
        if (shouldFallInLiquids && isInsideMap) {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f)) {
                if (hit.collider.CompareTag("Death")) { 
                    isFalling = true; 
                    return; 
                }
            }
        }

        // 2. EDGE DETECTION (Falling when exiting the map)
        if (shouldFallAtEdges) {
            bool movingRight = direction.x > 0;
            bool pastRightEdge = currentX > mapBoundary && movingRight;
            bool pastLeftEdge = currentX < -mapBoundary && !movingRight;

            if (pastRightEdge || pastLeftEdge) {
                // Check one last time if there is ground
                if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit edgeHit, 1.5f)) {
                    isFalling = true;
                    return;
                }
            }
        }

        // 3. WALL SMASH LOGIC
        if (Physics.Raycast(transform.position, direction, out RaycastHit wallHit, 0.8f)) {
            if (wallHit.collider.CompareTag("Wall") || wallHit.collider.CompareTag("JumpableWall")) {
                if (wallShatterPrefab != null) {
                    GameObject effect = Instantiate(wallShatterPrefab, wallHit.collider.transform.position, Quaternion.identity);
                    
                    // Sync the spawned effect sound volume
                    AudioSource effectSource = effect.GetComponent<AudioSource>();
                    if (effectSource != null && GameManager.Instance != null) {
                        effectSource.volume = GameManager.Instance.savedSfxVolume;
                    }
                }
                Destroy(wallHit.collider.gameObject);
            }
        }
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null) {
                pc.DieByHazard(this.direction);
            }
        }
    }
}