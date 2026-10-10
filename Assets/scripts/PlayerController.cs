using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {
    [Header("Original Jump/Move Settings")]
    public float baseJumpTime = 0.15f;
    public float jumpHeight = 0.6f;
    public float slideSpeed = 10f;
    public float rotationSpeed = 25f; 
    public float chargeTime = 0.5f;           
    public float maxChargeHoldTime = 2.0f;    
    public float chargeJumpHeight = 1.2f;
    public float chargeJumpDuration = 0.25f;
    public float defaultGroundY = 1.1f; 
    public float chargeSquashDepth = 0.2f; 

    [Header("Buffering")]
    private Vector3 bufferedDir = Vector3.zero;

    [Header("Visual Effects")]
    public GameObject chargeJumpEffect; 
    public float effectYOffset = -0.2f;
    public float chargeJumpEffectY = 0.5f; // NEW: Configurable spawn height for the jump effect
    public GameObject chargeMarker; 
    [Range(0f, 1f)]
    public float maxMarkerAlpha = 0.5f; 
    public Color safeColor = Color.white;
    public Color hazardColor = Color.red;
    private List<Material> markerMaterials = new List<Material>();

    [Header("Charge Visuals")]
    public float maxShakeIntensity = 0.2f;    
    [ColorUsage(true, true)]
    public Color readyEmissionColor = Color.white;
    private Material playerMaterial;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioSource slideAudioSource; 
    public AudioSource jumpAudioSource; 
    public AudioClip jumpSound;
    public AudioClip chargingSound;
    public AudioClip chargeJumpSound;
    public AudioClip iceSlideSound;      
    public AudioClip hazardDeathSound; 
    public AudioClip waterDeathSound;
    public AudioClip lavaDeathSound;    
    public AudioClip fallingEdgeSound;  
    public AudioClip quicksandSinkLoop;
    public AudioClip bananaSlipSound;
    public float maxChargePitch = 1.5f;
    public float jumpPitchRange = 0.1f; 

    [Header("Death & UI")]
    public GameObject gameOverUI;       
    public GameObject deadPlayerPrefab; 
    public GameObject hazardDeathEffectPrefab; 
    public GameObject waterFallEffectPrefab; 
    public float hitForce = 20f;
    public float restartDelay = 1.0f; 

    [Header("Quicksand Settings")]
    public float quicksandDeathTime = 2.0f;
    private float currentQuicksandTimer = 0f;
    private bool isTrapped = false;

    private bool isStunned = false; 

    private LevelGenerator generator;
    private EnvironmentManager envManager;
    private bool isMoving = false, isSliding = false, isCharging = false;
    private bool isDead = false;
    private string currentBiomeTag = "";
    private int maxZReached = 0;
    private Coroutine slideFadeCoroutine;
    private Coroutine currentMoveCoroutine; 

    private SkinLoader skinLoader;

    private Vector3 currentChargeDir = Vector3.zero;
    private KeyCode chargeKey = KeyCode.LeftShift;

    void Start() {
        generator = Object.FindFirstObjectByType<LevelGenerator>();
        envManager = Object.FindFirstObjectByType<EnvironmentManager>();
        skinLoader = GetComponent<SkinLoader>();
        
        if (gameOverUI != null) gameOverUI.SetActive(false);

        Renderer rNode = GetComponentInChildren<Renderer>();
        if (rNode != null) playerMaterial = rNode.material;

        if (chargeMarker != null) {
            Renderer[] rends = chargeMarker.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in rends) {
                markerMaterials.Add(r.material);
            }
            SetMarkerAlpha(0f);
            chargeMarker.SetActive(true);
        }

        if (slideAudioSource == null) {
            slideAudioSource = gameObject.AddComponent<AudioSource>();
            slideAudioSource.playOnAwake = false;
            slideAudioSource.loop = true;
            slideAudioSource.volume = 0f; 
        }

        if (jumpAudioSource == null) {
            jumpAudioSource = gameObject.AddComponent<AudioSource>();
            jumpAudioSource.playOnAwake = false;
        }

        if (GameManager.Instance != null) chargeKey = GameManager.Instance.GetChargeKeyCode();
    }

    void Update() {
        if (PauseManager.isPaused) return;
        
        SyncSfxVolume(); 
        if (isDead || isStunned) return; 
        CheckForHazards();

        if (isCharging) return;

        if (!isSliding && !isMoving && Input.GetKey(chargeKey)) {
            StartCoroutine(ChargeRoutine());
            return;
        }

        if (isTrapped && !isMoving) {
            currentQuicksandTimer += Time.deltaTime;
            transform.position += Vector3.down * 0.15f * Time.deltaTime;
            if (currentQuicksandTimer >= quicksandDeathTime) {
                StopQuicksandSound();
                StartCoroutine(FallToDeath(true));
                return;
            }
        }

        if (isMoving) {
            bufferedDir = GetPriorityDirection();
            return;
        }

        if (isTrapped) return;

        if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 1.6f)) {
            StartCoroutine(FallToDeath(false)); 
            return;
        }

        Vector3 dir = GetPriorityDirection();
        if (dir != Vector3.zero) Move(dir);
    }

    private void SyncSfxVolume() {
        if (GameManager.Instance != null) {
            float vol = GameManager.Instance.savedSfxVolume;
            if (!isSliding && slideFadeCoroutine == null) slideAudioSource.volume = 0; 
            else if (isSliding && slideFadeCoroutine == null) slideAudioSource.volume = vol;

            jumpAudioSource.volume = vol;
            if (isCharging || isTrapped) audioSource.volume = vol;
        }
    }

    Vector3 GetPriorityDirection() {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (h != 0) return new Vector3(h, 0, 0);
        if (v != 0) return new Vector3(0, 0, v);
        return Vector3.zero;
    }

    void StartQuicksandSound() {
        if (audioSource != null && quicksandSinkLoop != null) {
            if (audioSource.clip != quicksandSinkLoop) {
                audioSource.clip = quicksandSinkLoop;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
    }

    void StopQuicksandSound() {
        if (audioSource != null && audioSource.clip == quicksandSinkLoop) {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.loop = false;
        }
    }

    void Move(Vector3 dir) {
        StopQuicksandSound(); 
        Quaternion targetRotation = Quaternion.LookRotation(dir);
        
        Vector3 target = transform.position + dir;
        target.x = Mathf.Round(target.x);
        target.z = Mathf.Round(target.z);
        target.y = GetTargetYAtPosition(target);

        if (CheckForWall(transform.position, dir, 1f)) { 
            currentMoveCoroutine = StartCoroutine(RotateTowards(targetRotation));
            if (isSliding) {
                if (slideFadeCoroutine != null) StopCoroutine(slideFadeCoroutine);
                slideFadeCoroutine = StartCoroutine(FadeSlideAudio(0f, 0.1f));
            }
            isSliding = false; 
            return; 
        }
        
        if (isSliding) {
            currentMoveCoroutine = StartCoroutine(SlideRoutine(target, targetRotation));
        } else {
            float mult = 1.0f;
            BiomeSettings b = generator.GetBiomeByName(currentBiomeTag);
            if(b != null) mult = b.jumpTimeMultiplier;
            currentMoveCoroutine = StartCoroutine(JumpRoutine(target, baseJumpTime * mult, jumpHeight, targetRotation));
        }
    }

    IEnumerator RotateTowards(Quaternion targetRot) {
        while (Quaternion.Angle(transform.rotation, targetRot) > 0.1f) {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetRot;
    }

    void CheckForHazards() {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.4f, Physics.AllLayers, QueryTriggerInteraction.Collide);
        foreach (var hit in hitColliders) {
            Flamethrower ft = hit.GetComponent<Flamethrower>();
            if (ft == null) ft = hit.GetComponentInParent<Flamethrower>();
            if (ft != null && ft.isFiring) {
                DieByHazard(ft.transform.forward);
                break;
            }
        }
    }

    IEnumerator ChargeRoutine() {
        isCharging = true;
        bufferedDir = Vector3.zero; 
        currentChargeDir = transform.forward;
        Vector3 oldScale = transform.localScale;
        
        float startY = isTrapped ? transform.position.y : defaultGroundY;
        Vector3 basePosition = new Vector3(Mathf.Round(transform.position.x), startY, Mathf.Round(transform.position.z));
        transform.position = basePosition;

        float timer = 0;

        if (audioSource != null && chargingSound != null) {
            audioSource.clip = chargingSound;
            audioSource.loop = true;
            audioSource.pitch = 1f;
            audioSource.Play();
        }

        while (Input.GetKey(chargeKey) && timer < maxChargeHoldTime) {
            if (isDead) { 
                StopChargeAudio();
                ResetVisuals(oldScale, basePosition);
                yield break; 
            }

            if (isTrapped) {
                currentQuicksandTimer += Time.deltaTime;
                basePosition += Vector3.down * 0.15f * Time.deltaTime;
                if (currentQuicksandTimer >= quicksandDeathTime) {
                    StopChargeAudio();
                    ResetVisuals(oldScale, basePosition);
                    isCharging = false;
                    StartCoroutine(FallToDeath(true));
                    yield break;
                }
            }

            timer += Time.deltaTime;
            float chargePercent = Mathf.Clamp01(timer / chargeTime);
            float tension = timer / maxChargeHoldTime;

            if (audioSource != null) audioSource.pitch = Mathf.Lerp(1f, maxChargePitch, tension);
            
            float currentShake = tension * maxShakeIntensity;
            Vector3 shakeOffset = Random.insideUnitSphere * currentShake;
            shakeOffset.y = 0; 

            float currentSquashY = Mathf.Lerp(0, chargeSquashDepth, chargePercent);
            transform.position = basePosition + shakeOffset + (Vector3.down * currentSquashY);

            if (playerMaterial != null) {
                playerMaterial.SetColor("_EmissionColor", timer >= chargeTime ? readyEmissionColor : Color.black);
                if(timer >= chargeTime) playerMaterial.EnableKeyword("_EMISSION");
            }

            transform.localScale = Vector3.Lerp(oldScale, new Vector3(oldScale.x, oldScale.y * 0.7f, oldScale.z), chargePercent);
            
            Vector3 inputDir = GetPriorityDirection();
            if (inputDir != Vector3.zero) currentChargeDir = inputDir;

            Quaternion targetRot = Quaternion.LookRotation(currentChargeDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            if (chargeMarker != null) {
                float currentAlpha = chargePercent * maxMarkerAlpha;
                Vector3 moveDir = currentChargeDir;
                Vector3 previewTarget = basePosition + (moveDir * 2f);
                previewTarget.y = 0.55f; 

                bool canJump = true;

                if (Physics.Raycast(new Vector3(basePosition.x, 1f, basePosition.z), moveDir, out RaycastHit hit1, 1f)) {
                    if (hit1.collider.CompareTag("Wall")) {
                        canJump = false;
                    } else if (hit1.collider.CompareTag("JumpableWall")) {
                        if (CheckForWall(basePosition + moveDir, moveDir, 1f)) canJump = false;
                    }
                } else {
                    if (CheckForWall(basePosition + moveDir, moveDir, 1f)) {
                        previewTarget = basePosition + moveDir;
                        previewTarget.y = 0.55f;
                        if (CheckForWall(basePosition, moveDir, 1f)) canJump = false;
                    }
                }

                if (!canJump) {
                    UpdateMarkerVisuals(hazardColor, 0f); 
                } else {
                    if (Physics.Raycast(new Vector3(previewTarget.x, 2f, previewTarget.z), Vector3.down, out RaycastHit groundHit, 3f)) {
                        if (groundHit.collider.CompareTag("Death")) UpdateMarkerVisuals(hazardColor, currentAlpha);
                        else UpdateMarkerVisuals(safeColor, currentAlpha);
                    }
                    chargeMarker.transform.position = previewTarget;
                }
            }
            yield return null;
        }

        StopChargeAudio();
        ResetVisuals(oldScale, basePosition);
        isCharging = false;

        if (timer >= chargeTime && !isDead) {
            isTrapped = false;
            currentQuicksandTimer = 0f;
            StopQuicksandSound();
            Vector3 finalMoveDir = currentChargeDir;
            
            Vector3 target = basePosition + (finalMoveDir * 2f);
            target.x = Mathf.Round(target.x);
            target.z = Mathf.Round(target.z);
            target.y = GetTargetYAtPosition(target); 

            bool validJump = true;
            if (Physics.Raycast(new Vector3(basePosition.x, 1f, basePosition.z), finalMoveDir, out RaycastHit wallHit, 1f)) {
                if (wallHit.collider.CompareTag("Wall")) validJump = false;
                else if (wallHit.collider.CompareTag("JumpableWall")) {
                    if (CheckForWall(basePosition + finalMoveDir, finalMoveDir, 1f)) validJump = false;
                }
            } 
            else {
                if (CheckForWall(basePosition + finalMoveDir, finalMoveDir, 1f)) {
                    target = basePosition + finalMoveDir;
                    target.x = Mathf.Round(target.x);
                    target.z = Mathf.Round(target.z);
                    target.y = GetTargetYAtPosition(target);
                    if (CheckForWall(basePosition, finalMoveDir, 1f)) validJump = false;
                }
            }
            
            if (validJump) {
                PlayJumpSound(chargeJumpSound);
                if (chargeJumpEffect != null) {
                    // NEW: Spawns at the configured height (chargeJumpEffectY) instead of an offset from basePosition
                    Vector3 spawnPos = new Vector3(basePosition.x, chargeJumpEffectY, basePosition.z);
                    GameObject fx = Instantiate(chargeJumpEffect, spawnPos, Quaternion.Euler(-90, 0, 0));
                    Destroy(fx, 2f);
                }
                StartCoroutine(FadeOutMarker());
                currentMoveCoroutine = StartCoroutine(JumpRoutine(target, chargeJumpDuration, chargeJumpHeight, Quaternion.LookRotation(finalMoveDir)));
            } else {
                StartCoroutine(FadeOutMarker());
            }
        } else {
            StartCoroutine(FadeOutMarker());
        }
    }

    void PlayJumpSound(AudioClip clip) {
        if (jumpAudioSource != null && clip != null) {
            AudioClip clipToPlay = clip;
            if (skinLoader != null && skinLoader.activeSkinData != null) {
                if (clip == jumpSound && skinLoader.activeSkinData.hasCustomJump) {
                    clipToPlay = skinLoader.activeSkinData.jumpSound;
                } else if (clip == chargeJumpSound && skinLoader.activeSkinData.hasCustomCharge) {
                    clipToPlay = skinLoader.activeSkinData.chargeJumpSound;
                }
            }

            jumpAudioSource.pitch = 1.0f + Random.Range(-jumpPitchRange, jumpPitchRange);
            jumpAudioSource.PlayOneShot(clipToPlay, GameManager.Instance != null ? GameManager.Instance.savedSfxVolume : 1f);
        }
    }

    void StopChargeAudio() {
        if (audioSource != null && audioSource.clip == chargingSound) {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.pitch = 1f;
        }
    }

    void ResetVisuals(Vector3 oldScale, Vector3 basePos) {
        transform.localScale = oldScale;
        transform.position = basePos; 
        if (playerMaterial != null) playerMaterial.SetColor("_EmissionColor", Color.black);
    }

    float GetTargetYAtPosition(Vector3 pos) {
        if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 4f)) {
            if (hit.collider.transform.position.y < -0.1f) return defaultGroundY - 0.2f;
        }
        return defaultGroundY;
    }

    void UpdateMarkerVisuals(Color col, float alpha) {
        foreach (Material mat in markerMaterials) if (mat != null) { Color c = col; c.a = alpha; mat.color = c; }
    }

    void SetMarkerAlpha(float alpha) {
        foreach (Material mat in markerMaterials) if (mat != null) { Color c = mat.color; c.a = alpha; mat.color = c; }
    }

    IEnumerator FadeOutMarker() {
        if (markerMaterials.Count == 0) yield break;
        float startAlpha = markerMaterials[0].color.a;
        Color startCol = markerMaterials[0].color;
        float elapsed = 0f;
        float duration = 0.2f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            UpdateMarkerVisuals(startCol, Mathf.Lerp(startAlpha, 0f, elapsed / duration));
            yield return null;
        }
    }

    IEnumerator JumpRoutine(Vector3 target, float duration, float height, Quaternion targetRot) {
        if (!isCharging) PlayJumpSound(jumpSound);
        isMoving = true;
        Vector3 start = transform.position;
        float elapsed = 0;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;
            transform.position = Vector3.Lerp(start, target, p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * height;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetRot; 
        FinalizeMove(target);
    }

    IEnumerator SlideRoutine(Vector3 target, Quaternion targetRot) {
        isMoving = true;
        Vector3 start = transform.position;
        float duration = 1f / slideSpeed;
        float elapsed = 0;
        float sfxVol = GameManager.Instance != null ? GameManager.Instance.savedSfxVolume : 1f;
        if (slideAudioSource != null && iceSlideSound != null) {
            if (!slideAudioSource.isPlaying || slideAudioSource.clip != iceSlideSound) {
                slideAudioSource.clip = iceSlideSound;
                slideAudioSource.Play();
            }
            if (slideFadeCoroutine != null) StopCoroutine(slideFadeCoroutine);
            slideFadeCoroutine = StartCoroutine(FadeSlideAudio(sfxVol, 0.1f));
        }
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetRot;
        FinalizeMove(target);
    }

    IEnumerator FadeSlideAudio(float targetVolume, float duration) {
        if (slideAudioSource == null) yield break;
        float startVol = slideAudioSource.volume;
        float time = 0;
        while (time < duration) {
            time += Time.deltaTime;
            slideAudioSource.volume = Mathf.Lerp(startVol, targetVolume, time / duration);
            yield return null;
        }
        slideAudioSource.volume = targetVolume;
        if (targetVolume <= 0) {
            slideAudioSource.Stop();
            slideFadeCoroutine = null;
        }
    }

    void FinalizeMove(Vector3 target) {
        transform.position = new Vector3(Mathf.Round(target.x), target.y, Mathf.Round(target.z));
        
        isMoving = false;
        int cz = Mathf.RoundToInt(transform.position.z);
        if (cz > maxZReached) { 
            maxZReached = cz; 
            if(envManager != null) envManager.UpdateScore(maxZReached); 
        }

        PostMoveCheck();

        if (isDead) return;

        if (!isSliding && !isTrapped && Input.GetKey(chargeKey)) {
            bufferedDir = Vector3.zero; 
            StartCoroutine(ChargeRoutine());
            return;
        }

        if (!isTrapped && !isSliding && bufferedDir != Vector3.zero) {
            Vector3 nextMove = bufferedDir;
            bufferedDir = Vector3.zero;
            Move(nextMove);
        }
    }

    void PostMoveCheck() {
        if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 1.6f)) {
            bufferedDir = Vector3.zero;
            StartCoroutine(FallToDeath(false)); 
            return;
        }

        string currentTag = hit.collider.tag;

        if (currentTag == "Death" || currentTag == "Quicksand") {
            bufferedDir = Vector3.zero;
        }

        if (currentTag == "Quicksand") {
            isTrapped = true;
            currentQuicksandTimer = 0f;
            StartQuicksandSound();
            return;
        }

        if (currentTag == "Death") { 
            StartCoroutine(FallToDeath(true)); 
            return; 
        }

        if (currentTag != currentBiomeTag && currentTag != "Untagged" && currentTag != "Wall" && currentTag != "JumpableWall") {
            currentBiomeTag = currentTag;
            if (envManager != null) envManager.UpdateVisuals(generator.GetBiomeByName(currentTag));
        }

        if (currentTag == "Ice") {
            isSliding = true;
            bufferedDir = Vector3.zero; 
            Move(transform.forward);
        } else {
            if (isSliding) {
                if (slideFadeCoroutine != null) StopCoroutine(slideFadeCoroutine);
                slideFadeCoroutine = StartCoroutine(FadeSlideAudio(0f, 0.15f));
            }
            isSliding = false;
            isTrapped = false;
            StopQuicksandSound();
        }
    }

    bool CheckForWall(Vector3 origin, Vector3 dir, float dist) {
        if (Physics.Raycast(new Vector3(origin.x, 1f, origin.z), dir, out RaycastHit hit, dist)) {
            return hit.collider.CompareTag("Wall") || hit.collider.CompareTag("JumpableWall");
        }
        return false;
    }

    public IEnumerator FallToDeath(bool wasOnLiquid) {
        if (isDead) yield break;
        isDead = true; 
        isMoving = true;
        bufferedDir = Vector3.zero; 

        MusicManager mm = Object.FindFirstObjectByType<MusicManager>();
        if (mm != null) {
            mm.StopAllCoroutines();
            mm.StartCoroutine(mm.PitchDownRoutine());
        }

        Vector3 deathPosition = transform.position;
        float sfxVol = GameManager.Instance != null ? GameManager.Instance.savedSfxVolume : 1f;

        if (wasOnLiquid && waterFallEffectPrefab != null) {
            Vector3 splashPos = new Vector3(deathPosition.x, 0.5f, deathPosition.z);
            GameObject splash = Instantiate(waterFallEffectPrefab, splashPos, Quaternion.Euler(-90, 0, 0));
            Destroy(splash, 2f);
        }

        StopQuicksandSound(); 
        StopChargeAudio();
        if (slideAudioSource != null) slideAudioSource.Stop();

        if (wasOnLiquid) {
            if (audioSource != null && waterDeathSound != null) audioSource.PlayOneShot(waterDeathSound, sfxVol);
        } else {
            if (audioSource != null && fallingEdgeSound != null) audioSource.PlayOneShot(fallingEdgeSound, sfxVol);
        }

        float fallVelocity = 0f; 
        float gravity = 35f;
        
        while (transform.position.y > -8f) { 
            fallVelocity += gravity * Time.unscaledDeltaTime;
            transform.position += Vector3.down * fallVelocity * Time.unscaledDeltaTime; 
            if (wasOnLiquid && transform.position.y <= 0.1f) {
                transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
                break; 
            }
            yield return null; 
        }

        FinalDeathCleanup();
    }

    public void DieByHazard(Vector3 hazardDirection) {
        if (isDead) return;
        isDead = true;
        bufferedDir = Vector3.zero;

        MusicManager mm = Object.FindFirstObjectByType<MusicManager>();
        if (mm != null) {
            mm.StopAllCoroutines();
            mm.StartCoroutine(mm.PitchDownRoutine());
        }

        StopQuicksandSound(); StopChargeAudio();
        if (slideAudioSource != null) slideAudioSource.Stop();

        float sfxVol = GameManager.Instance != null ? GameManager.Instance.savedSfxVolume : 1f;

        AudioClip clipToPlay = hazardDeathSound;
        if (skinLoader != null && skinLoader.activeSkinData != null && skinLoader.activeSkinData.hasCustomDeath) {
            clipToPlay = skinLoader.activeSkinData.deathSound;
        }

        if (audioSource != null && clipToPlay != null) audioSource.PlayOneShot(clipToPlay, sfxVol);

        if (hazardDeathEffectPrefab != null) Instantiate(hazardDeathEffectPrefab, transform.position, Quaternion.identity);
        if (deadPlayerPrefab != null) {
            GameObject dummy = Instantiate(deadPlayerPrefab, transform.position, transform.rotation);
            Rigidbody rb = dummy.GetComponent<Rigidbody>();
            if (rb != null) {
                Vector3 forceDir = (hazardDirection + Vector3.up * 0.7f).normalized;
                rb.AddForce(forceDir * hitForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * hitForce, ForceMode.Impulse);
            }
        }
        FinalDeathCleanup();
    }

    void FinalDeathCleanup() {
        // ADD THIS LINE HERE
        if (GameManager.Instance != null) {
            GameManager.Instance.SubmitScore(maxZReached);
        }

        if (gameOverUI != null) gameOverUI.SetActive(true);
        StartCoroutine(EnableRestartAfterDelay());
        GetComponent<Collider>().enabled = false;
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
    }

    private IEnumerator EnableRestartAfterDelay() {
        yield return new WaitForSeconds(restartDelay);
        if (generator != null) generator.EnableRestart();
    }

    public void SlipOnBanana() {
        if (isDead || isStunned) return;
        StartCoroutine(BananaSlipRoutine());
    }

    IEnumerator BananaSlipRoutine() {
        isStunned = true;
        isMoving = true; 

        if (audioSource != null && bananaSlipSound != null) {
            audioSource.PlayOneShot(bananaSlipSound);
        }

        float elapsed = 0;
        float duration = 1.0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            transform.Rotate(Vector3.up, 720f * Time.deltaTime);
            yield return null;
        }

        isStunned = false;
        isMoving = false;
    }
}