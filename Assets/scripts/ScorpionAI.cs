using UnityEngine;
using System.Collections;

public class ScorpionAI : MonoBehaviour {
    [Header("Movement Settings")]
    public float moveSpeed = 3.5f; 
    public float retreatMultiplier = 1.2f;
    public float boardWidth = 4.5f;

    [Header("Probability Settings")]
    [Range(0f, 1f)] public float idleChance = 0.3f; 
    public Vector2 idleTimeRange = new Vector2(0.5f, 1.5f);

    [Header("Visuals & FX")]
    public Animator anim;
    public string walkBoolName = "isWalking";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip moveSound;
    public AudioClip attackSound; 

    private bool isMoving = false;
    private bool isFalling = false;
    private Rigidbody rb;

    void Start() {
        if (anim == null) anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        
        if (rb != null) rb.isKinematic = true;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        StartCoroutine(ScorpionBrain());
    }

    // --- TRIGGER BASED KILL ---
    // Works like Rolling Hazards: kills on overlap
    private void OnTriggerEnter(Collider other) {
        if (isFalling) return;

        if (other.CompareTag("Player")) {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null) {
                if (audioSource != null && attackSound != null) {
                    audioSource.PlayOneShot(attackSound);
                }
                pc.DieByHazard(transform.forward);
            }
        }
    }

    void FixedUpdate() {
        if (isFalling) return;

        RaycastHit hit;
        Vector3 boxCenter = transform.position + Vector3.up * 0.5f;
        Vector3 boxHalfExtents = new Vector3(0.3f, 0.1f, 0.3f); 

        bool groundFound = Physics.BoxCast(boxCenter, boxHalfExtents, Vector3.down, out hit, transform.rotation, 1.2f, Physics.AllLayers, QueryTriggerInteraction.Collide);

        if (groundFound) {
            FallingTile ft = hit.collider.GetComponentInParent<FallingTile>();
            if (ft != null && ft.isFalling) {
                StartFalling();
                return;
            }

            if (hit.collider.CompareTag("Death") || hit.collider.CompareTag("Quicksand")) {
                StartFalling();
                return;
            }
        } else {
            if (!isMoving) StartFalling();
        }
    }

    IEnumerator ScorpionBrain() {
        while (!isFalling) {
            float rng = Random.value;

            if (rng < idleChance) { 
                if(anim) anim.SetBool(walkBoolName, false);
                yield return new WaitForSeconds(Random.Range(idleTimeRange.x, idleTimeRange.y));
            } 
            else { 
                Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
                ShuffleArray(directions);

                foreach (Vector3 dir in directions) {
                    Vector3 target = transform.position + dir;
                    if (CanMoveTo(target)) {
                        yield return StartCoroutine(MoveToTile(target));
                        break; 
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }
            yield return null;
        }
    }

    bool CanMoveTo(Vector3 target) {
        if (Mathf.Abs(target.x) > boardWidth) return false;

        Collider[] hitColliders = Physics.OverlapSphere(target + Vector3.up * 0.5f, 0.4f);
        foreach (var hit in hitColliders) {
            if (hit.CompareTag("Wall") || hit.CompareTag("JumpableWall")) return false;
        }

        RaycastHit floorHit;
        if (Physics.Raycast(target + Vector3.up * 1f, Vector3.down, out floorHit, 2f)) {
            if (floorHit.collider.CompareTag("Death") || floorHit.collider.CompareTag("Quicksand")) return false;
            FallingTile ft = floorHit.collider.GetComponentInParent<FallingTile>();
            if (ft != null && ft.isFalling) return false;
        } else {
            return false;
        }
        return true;
    }

    IEnumerator MoveToTile(Vector3 target) {
        isMoving = true;
        Vector3 direction = (target - transform.position).normalized;
        
        if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);
        if(anim) anim.SetBool(walkBoolName, true);
        if(audioSource != null && moveSound != null) audioSource.PlayOneShot(moveSound);

        while (Vector3.Distance(transform.position, target) > 0.01f) {
            if (isFalling) yield break;
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = new Vector3(Mathf.Round(target.x), 0.5f, Mathf.Round(target.z));
        if(anim) anim.SetBool(walkBoolName, false);
        isMoving = false;
    }

    void StartFalling() {
        if (isFalling) return;
        isFalling = true;
        StopAllCoroutines();
        
        if(anim) {
            anim.SetBool(walkBoolName, false);
            anim.enabled = false; 
        }

        if (rb != null) {
            rb.isKinematic = false; 
            rb.useGravity = true;
            rb.AddTorque(new Vector3(Random.value, Random.value, Random.value) * 30f, ForceMode.Impulse);
        }
        Destroy(gameObject, 2.5f);
    }

    void ShuffleArray(Vector3[] array) {
        for (int i = 0; i < array.Length; i++) {
            int rnd = Random.Range(0, array.Length);
            Vector3 temp = array[rnd];
            array[rnd] = array[i];
            array[i] = temp;
        }
    }
}