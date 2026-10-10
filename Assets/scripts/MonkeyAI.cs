using UnityEngine;
using System.Collections;

public class MonkeyAI : MonoBehaviour {
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float retreatMultiplier = 1.5f;
    public float boardWidth = 4.5f;

    [Header("Action Timing")]
    public float eatingDuration = 2.0f; 
    public Vector2 idleTimeRange = new Vector2(1f, 2f);
    public Vector2 sleepTimeRange = new Vector2(10f, 20f);

    [Header("Probability Settings")]
    [Range(0f, 1f)] public float sleepChance = 0.05f;  
    [Range(0f, 1f)] public float actionChance = 0.15f; 
    [Range(0f, 1f)] public float throwBananaChance = 0.4f; 

    [Header("Banana Settings")]
    public GameObject bananaPeelPrefab; 
    public string eatTriggerName = "EatBanana";
    [Range(0, 10)] public int minThrowDistance = 3;
    [Range(0, 10)] public int maxThrowDistance = 7;

    [Header("Visuals & FX")]
    public ParticleSystem sleepZ; 
    public Animator anim;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip bananaThrowSound;
    public AudioClip eatSound;

    private bool isMoving = false;
    private bool isFalling = false;
    private bool isEating = false;
    private Rigidbody rb;

    void Start() {
        if (anim == null) anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        
        if (rb != null) rb.isKinematic = true;
        if (sleepZ != null) sleepZ.Stop();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        StartCoroutine(MonkeyBrain());
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

            if (hit.collider.CompareTag("Death") || hit.collider.CompareTag("Ice") || hit.collider.CompareTag("Quicksand")) {
                StartFalling();
                return;
            }
        } else {
            if (!isMoving) StartFalling();
        }
    }

    IEnumerator MonkeyBrain() {
        while (!isFalling) {
            if (isEating) {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            float rng = Random.value;

            if (rng < sleepChance) { 
                if(anim) anim.SetBool("isSleeping", true);
                if(sleepZ != null) sleepZ.Play();

                float sleepDuration = Random.Range(sleepTimeRange.x, sleepTimeRange.y);
                float elapsed = 0f;
                while (elapsed < sleepDuration && !isFalling) {
                    elapsed += 0.1f;
                    yield return new WaitForSeconds(0.1f);
                }
                
                if(sleepZ != null) sleepZ.Stop();
                if(anim) anim.SetBool("isSleeping", false);
            } 
            else if (rng < sleepChance + actionChance) { 
                if (Random.value < throwBananaChance && bananaPeelPrefab != null) {
                    yield return StartCoroutine(EatBananaRoutine());
                } else {
                    yield return new WaitForSeconds(Random.Range(idleTimeRange.x, idleTimeRange.y));
                }
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
                yield return new WaitForSeconds(0.2f);
            }
            yield return null;
        }
    }

    IEnumerator EatBananaRoutine() {
        isEating = true;
        if (anim) anim.SetTrigger(eatTriggerName);
        yield return new WaitForSeconds(eatingDuration); 
        isEating = false;
    }

    public void LaunchPeelEvent() {
        if (isFalling || bananaPeelPrefab == null) return;

        if (audioSource != null && bananaThrowSound != null) {
            audioSource.PlayOneShot(bananaThrowSound);
        }

        float randomForward = Random.Range(minThrowDistance, maxThrowDistance);
        float randomSide = Random.Range(-2, 2);
        
        float targetX = Mathf.Round(transform.position.x + randomSide);
        float targetZ = Mathf.Round(transform.position.z + randomForward);
        targetX = Mathf.Clamp(targetX, -boardWidth, boardWidth);

        Vector3 snappedTarget = new Vector3(targetX, 0.5f, targetZ);

        bool isOccupied = false;
        Collider[] blockers = Physics.OverlapSphere(snappedTarget, 0.4f);
        foreach (var col in blockers) {
            if (col.CompareTag("Wall") || col.CompareTag("JumpableWall") || col.CompareTag("Player")) {
                isOccupied = true;
                break;
            }
        }
        if (isOccupied) return;

        if (Physics.Raycast(snappedTarget + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 3f)) {
            if (!hit.collider.CompareTag("Death")) {
                GameObject peel = Instantiate(bananaPeelPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                Vector3 finalFloorPos = new Vector3(snappedTarget.x, hit.point.y + 0.05f, snappedTarget.z);
                StartCoroutine(PeelJumpAnimation(peel, finalFloorPos));
            }
        }
    }

    IEnumerator PeelJumpAnimation(GameObject peel, Vector3 target) {
        // NEW: Tell the banana it is currently being thrown so it doesn't try to "fall" mid-air
        BananaPeel bp = peel.GetComponent<BananaPeel>();
        if (bp != null) bp.isBeingThrown = true;

        float elapsed = 0;
        float duration = 0.8f;
        Vector3 startPos = peel.transform.position;
        while (elapsed < duration && peel != null) {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;
            peel.transform.position = Vector3.Lerp(startPos, target, p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * 2f;
            peel.transform.Rotate(Vector3.up, 500f * Time.deltaTime);
            yield return null;
        }

        // NEW: It has landed, now it can start checking if the floor crumbles
        if (bp != null) bp.isBeingThrown = false;
    }

    bool CanMoveTo(Vector3 target) {
        if (Mathf.Abs(target.x) > boardWidth) return false;

        Collider[] hitColliders = Physics.OverlapSphere(target + Vector3.up * 0.5f, 0.4f, Physics.AllLayers, QueryTriggerInteraction.Collide);
        foreach (var hit in hitColliders) {
            if (hit.CompareTag("Wall") || hit.CompareTag("JumpableWall")) return false;
            if (hit.CompareTag("Player")) return false;
        }

        RaycastHit floorHit;
        if (Physics.Raycast(target + Vector3.up * 1f, Vector3.down, out floorHit, 2f, Physics.AllLayers, QueryTriggerInteraction.Collide)) {
            if (floorHit.collider.CompareTag("Death") || floorHit.collider.CompareTag("Ice") || floorHit.collider.CompareTag("Quicksand")) return false;
            FallingTile ft = floorHit.collider.GetComponentInParent<FallingTile>();
            if (ft != null && ft.isFalling) return false;
        } else {
            return false;
        }
        return true;
    }

    IEnumerator MoveToTile(Vector3 target) {
        isMoving = true;
        Vector3 startPosition = transform.position;
        Vector3 direction = (target - transform.position).normalized;
        
        if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);
        if(anim) anim.SetBool("isWalking", true);

        while (Vector3.Distance(transform.position, target) > 0.01f) {
            if (isFalling) yield break;

            if (Physics.CheckSphere(target + Vector3.up * 0.5f, 0.3f)) {
                Collider[] cols = Physics.OverlapSphere(target + Vector3.up * 0.5f, 0.3f);
                foreach(var col in cols) {
                    if(col.CompareTag("Player")) {
                        yield return StartCoroutine(Retreat(startPosition));
                        isMoving = false;
                        yield break; 
                    }
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = new Vector3(Mathf.Round(target.x), target.y, Mathf.Round(target.z));
        if(anim) anim.SetBool("isWalking", false);
        isMoving = false;
    }

    IEnumerator Retreat(Vector3 retreatTarget) {
        while (Vector3.Distance(transform.position, retreatTarget) > 0.01f) {
            if (isFalling) yield break;
            transform.position = Vector3.MoveTowards(transform.position, retreatTarget, moveSpeed * retreatMultiplier * Time.deltaTime);
            yield return null;
        }
        transform.position = retreatTarget;
        if(anim) anim.SetBool("isWalking", false);
    }

    void StartFalling() {
        if (isFalling) return;
        isFalling = true;
        isEating = false;
        StopAllCoroutines();
        
        if(sleepZ != null) sleepZ.Stop();
        if(anim) {
            anim.SetBool("isWalking", false);
            anim.SetBool("isSleeping", false);
            anim.enabled = false; 
        }

        if (rb != null) {
            rb.isKinematic = false; 
            rb.useGravity = true;
            rb.AddTorque(new Vector3(Random.value, Random.value, Random.value) * 20f, ForceMode.Impulse);
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

    public void EatBananSound() => audioSource.PlayOneShot(eatSound);
}