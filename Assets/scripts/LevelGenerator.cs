using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class LevelGenerator : MonoBehaviour {
    public BiomeSettings[] biomes;
    public Transform player;
    public EnvironmentManager envManager;

    [Header("Grid Settings")]
    public int mapWidth = 9;
    public int renderDistance = 20;
    public int thirdPersonRenderDistance = 50; // Increased distance for 3rd person

    [Header("Biome Progression")]
    public int minBiomeRows = 20;
    public int maxBiomeRows = 40;
    public int transitionLength = 8;

    [Header("Safe Zone Settings")]
    public int safeZoneDepth = 10; 

    [Header("Crumble Tracking")]
    public float lastDeleteZ = 0; 
    public float islandDeleteOffset = 15f; 

    [Header("Background Islands")]
    public bool spawnIslands = true;
    public float islandSpawnChance = 0.15f;
    public int islandTileAmount = 8; 
    public float islandScale = 0.6f;
    public float bobSpeed = 1.5f;
    public float bobAmount = 0.3f;

    [Header("Left Side Settings")]
    public float leftMinDistX = 15f;
    public float leftMaxDistX = 25f;
    public float leftMinY = -2f;
    public float leftMaxY = 2f;

    [Header("Right Side Settings")]
    public float rightMinDistX = 10f;
    public float rightMaxDistX = 15f;
    public float rightMinY = 0f;
    public float rightMaxY = 5f;

    [Header("Cloud Settings")]
    public float cloudSpawnChance = 0.1f;
    public float cloudMinY = 3f;
    public float cloudMaxY = 6f;
    public float cloudMoveSpeed = 2f;

    [Header("Fade UI")]
    public Image fadeImage;
    public float fadeDuration = 1.0f;

    private int currentZ = 0;
    private int safeColumn;
    private List<GameObject> activeTiles = new List<GameObject>();
    private List<GameObject> activeIslands = new List<GameObject>(); 
    private bool canRestart = false;
    
    private BiomeSettings currentBiome;
    private BiomeSettings targetBiome;
    private int rowsInCurrentBiome = 0;
    private int targetBiomeLength;
    private bool isTransitioning = false;
    private int transitionStep = 0;

    private float caveHazardTimer = 0f;

    private struct BiomeChangePoint {
        public int zTrigger;
        public BiomeSettings settings;
    }
    private List<BiomeChangePoint> upcomingBiomeChanges = new List<BiomeChangePoint>();

    void Start() {
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        safeColumn = mapWidth / 2;
        
        // Adjust render distance based on Camera Mode (1 is Third Person)
        if (GameManager.Instance != null && GameManager.Instance.cameraMode == 1) {
            renderDistance = thirdPersonRenderDistance;
        }

        if (biomes == null || biomes.Length == 0) return;

        currentBiome = biomes[0];
        targetBiome = biomes[0];
        targetBiomeLength = Random.Range(minBiomeRows, maxBiomeRows);
        
        if(envManager != null) envManager.SetInitialBiome(currentBiome);

        for (int i = 0; i < 8; i++) GenerateRow(true); 
        for (int i = 0; i < renderDistance; i++) GenerateRow(false);

        if (player != null) player.position = new Vector3(0, 1.1f, 1f);
        caveHazardTimer = Random.Range(currentBiome.minSpawnInterval, currentBiome.maxSpawnInterval);

        if (fadeImage != null) StartCoroutine(FadeOutStart());
    }

    IEnumerator FadeOutStart() {
        float elapsed = 0f;
        Color c = fadeImage.color;
        while (elapsed < fadeDuration) {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
    }

    void Update() {
        CheckDebugInput();
        if (canRestart) {
            if (Input.GetKeyDown(KeyCode.Space)) RestartGame();
            return;
        }

        if (player == null || !player.gameObject.activeInHierarchy) return;

        if (upcomingBiomeChanges.Count > 0 && player.position.z >= upcomingBiomeChanges[0].zTrigger) {
            if (envManager != null) envManager.TransitionToBiome(upcomingBiomeChanges[0].settings);
            upcomingBiomeChanges.RemoveAt(0);
        }

        if (player.position.z > currentZ - renderDistance) GenerateRow(false);

        if (currentBiome.isCave && currentBiome.hazardPrefab != null) {
            HandleCaveHazardTimer();
        }

        CrumbleOldIslands();

        float limit = (mapWidth / 2f) + 0.5f;
        if (Mathf.Abs(player.position.x) > limit) {
            player.GetComponent<PlayerController>()?.StartCoroutine("FallToDeath", false);
        }
    }

    void HandleCaveHazardTimer() {
        if (player.position.z < safeZoneDepth) return;
        caveHazardTimer -= Time.deltaTime;
        if (caveHazardTimer <= 0) {
            SpawnDirectCaveHazard();
            caveHazardTimer = Random.Range(currentBiome.minSpawnInterval, currentBiome.maxSpawnInterval);
            if (caveHazardTimer <= 0.1f) caveHazardTimer = 2.0f;
        }
    }

    void SpawnDirectCaveHazard() {
        int halfWidth = mapWidth / 2;
        int randomLane = Random.Range(-halfWidth, halfWidth + 1);
        float xPos = (float)randomLane;
        float spawnZ = player.position.z + renderDistance + 5f; 
        Vector3 spawnPos = new Vector3(xPos, 1.0f, spawnZ);
        GameObject bat = Instantiate(currentBiome.hazardPrefab, spawnPos, Quaternion.identity);
        RollingHazard rh = bat.GetComponent<RollingHazard>();
        if (rh != null) rh.Initialize(currentBiome.hazardSpeed, Vector3.back);
        Destroy(bat, 15f);
    }

    void CheckDebugInput() {
        for (int i = 0; i < 9; i++) {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) && i < biomes.Length) {
                targetBiome = biomes[i];
                isTransitioning = true;
                transitionStep = 0;
                upcomingBiomeChanges.Add(new BiomeChangePoint { zTrigger = (int)player.position.z + 2, settings = targetBiome });
            }
        }
    }

    public void EnableRestart() {
        canRestart = true;
        StartCoroutine(SlowMotionFreeze());
    }

    IEnumerator SlowMotionFreeze() {
        float duration = 1.5f;
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(1.0f, 0.0f, elapsed / duration);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }
        Time.timeScale = 0f;
    }

    void GenerateRow(bool forceAllGround) {
        DetermineBiomeState();
        int previousSafe = safeColumn;
        safeColumn = Mathf.Clamp(safeColumn + Random.Range(-1, 2), 0, mapWidth - 1);

        for (int x = 0; x < mapWidth; x++) {
            Vector3 pos = new Vector3(x - (mapWidth / 2), 0, currentZ);
            BiomeSettings tileBiome = currentBiome;
            if (isTransitioning) {
                float transitionChance = (float)transitionStep / transitionLength;
                if (Random.value < transitionChance) tileBiome = targetBiome;
            }

            bool isOnSafePath = (x == safeColumn || (x == previousSafe && currentZ > 0));
            bool isRiver = !forceAllGround && (Random.value < tileBiome.riverChance);
            bool isWall = !forceAllGround && !isOnSafePath && (Random.value < tileBiome.wallChance);

            GameObject prefab;
            if (isOnSafePath) prefab = (isRiver && tileBiome.uniqueBridge != null) ? tileBiome.uniqueBridge : tileBiome.ground;
            else if (isRiver) prefab = tileBiome.water;
            else if (isWall) prefab = (tileBiome.wall2 != null && Random.Range(0f, 100f) < tileBiome.wall2Ratio) ? tileBiome.wall2 : tileBiome.wall;
            else prefab = tileBiome.ground;

            SpawnTile(prefab, pos, tileBiome);

            bool canSpawnProps = !forceAllGround && prefab == tileBiome.ground;

            // MONKEY SPAWNING LOGIC
            if (canSpawnProps && tileBiome.spawnMonkeys && !isOnSafePath) {
                if (Random.value < tileBiome.monkeySpawnRate && tileBiome.monkeyPrefab != null) {
                    Instantiate(tileBiome.monkeyPrefab, pos + Vector3.up, Quaternion.identity);
                    canSpawnProps = false; // Don't spawn a coin on a monkey
                }
            }

            // ENEMY SPAWNING LOGIC (Scorpions, etc.)
            if (canSpawnProps && tileBiome.spawnEnemies && !isOnSafePath) {
                if (Random.value < tileBiome.enemySpawnRate && tileBiome.enemyPrefab != null) {
                    Vector3 enemySpawnPos = new Vector3(Mathf.Round(pos.x), 0.5f, Mathf.Round(pos.z));
                    Instantiate(tileBiome.enemyPrefab, enemySpawnPos, Quaternion.identity);
                    canSpawnProps = false; // Don't spawn a coin on a scorpion
                }
            }

            // COIN SPAWNING LOGIC
            if (canSpawnProps) {
                if (Random.value < tileBiome.coinSpawnRate && tileBiome.coinPrefab != null) {
                    // Spawned 0.6f over ground to be at player chest/waist height
                    Vector3 coinPos = new Vector3(pos.x, 0.6f, pos.z);
                    Instantiate(tileBiome.coinPrefab, coinPos, Quaternion.identity);
                }
            }
        }

        if (currentBiome.isCave && currentBiome.caveWallBack != null) {
            Vector3 leftPos = new Vector3(-(mapWidth / 2) - 1, 0, currentZ);
            SpawnCaveBoundary(currentBiome.caveWallBack, leftPos, currentBiome);
        }

        if (!forceAllGround && spawnIslands && Random.value < islandSpawnChance && !currentBiome.isCave) SpawnBackgroundIsland();

        if (!forceAllGround && currentBiome.canSpawnClouds && currentBiome.cloudPrefabs != null && currentBiome.cloudPrefabs.Length > 0 && !currentBiome.isCave) {
            if (Random.value < cloudSpawnChance) SpawnCloud(currentBiome);
        }

        if (!forceAllGround && currentZ > safeZoneDepth && currentBiome.hazardPrefab != null && !currentBiome.isCave) {
            if (Random.value < currentBiome.hazardChance) SpawnSideHazard(currentZ, currentBiome);
        }

        currentZ++;
    }

    void SpawnTile(GameObject prefab, Vector3 pos, BiomeSettings biome) {
        GameObject baseTile;
        bool isWaterPrefab = (prefab == biome.water);
        
        if (isWaterPrefab) {
            if (prefab.CompareTag("Quicksand")) {
                pos.y = 0f; 
                baseTile = Instantiate(prefab, pos, Quaternion.identity, transform);
                baseTile.tag = "Quicksand";
            } else {
                pos.y = -0.2f; 
                baseTile = Instantiate(prefab, pos, Quaternion.identity, transform);
                baseTile.tag = (biome.name == "Snow") ? "Ice" : "Death";
            }
        } 
        else if (prefab == biome.uniqueBridge) {
            pos.y = -0.2f;
            baseTile = Instantiate(prefab, pos, Quaternion.identity, transform);
            baseTile.tag = biome.name; 
        }
        else {
            baseTile = Instantiate(biome.ground, pos, Quaternion.identity, transform);
            baseTile.tag = biome.name; 
        }

        if (isWaterPrefab && biome.waterEffectPrefab != null && Random.value < biome.waterEffectChance) {
            Vector3 effectPos = baseTile.transform.position + new Vector3(0, 0.2f, 0);
            Instantiate(biome.waterEffectPrefab, effectPos, Quaternion.identity, baseTile.transform);
        }

        baseTile.AddComponent<FallingTile>();
        activeTiles.Add(baseTile);

        if (prefab == biome.wall || prefab == biome.wall2) {
            float randomYRotation = Random.Range(0, 4) * 90f;
            Quaternion wallRotation = Quaternion.Euler(0, randomYRotation, 0);
            GameObject top = Instantiate(prefab, pos + Vector3.up, wallRotation, baseTile.transform);
            if (top.GetComponent<Flamethrower>() != null) top.tag = "Wall";
            else if (prefab == biome.wall2 && biome.wall2IsJumpable) top.tag = "JumpableWall";
            else top.tag = "Wall"; 
        }
    }

    void SpawnSideHazard(int zPos, BiomeSettings biome) {
        bool fromLeft = Random.value > 0.5f;
        float xPos = fromLeft ? -(mapWidth / 2f) - 6f : (mapWidth / 2f) + 6f;
        GameObject spawnerObj = new GameObject("SideHazardSpawner_Z" + zPos);
        spawnerObj.transform.position = new Vector3(xPos, 1.0f, zPos);
        HazardSpawner spawner = spawnerObj.AddComponent<HazardSpawner>();
        spawner.minInterval = biome.minSpawnInterval;
        spawner.maxInterval = biome.maxSpawnInterval;
        spawner.Setup(biome.hazardPrefab, biome.hazardSpeed, fromLeft ? Vector3.right : Vector3.left, player);
        spawnerObj.transform.parent = this.transform;
    }

    void SpawnCaveBoundary(GameObject prefab, Vector3 pos, BiomeSettings biome) {
        GameObject baseTile = Instantiate(biome.ground, pos, Quaternion.identity, transform);
        baseTile.AddComponent<FallingTile>();
        activeTiles.Add(baseTile);
        int height = Mathf.Max(1, biome.caveWallHeight);
        for (int i = 0; i < height; i++) {
            Vector3 wallPos = pos + Vector3.up * (1f + i); 
            GameObject wallObj = Instantiate(prefab, wallPos, Quaternion.identity, baseTile.transform);
            wallObj.tag = "Wall"; 
        }
    }

    void SpawnCloud(BiomeSettings biome) {
        GameObject cloudModel = biome.cloudPrefabs[Random.Range(0, biome.cloudPrefabs.Length)];
        float x = Random.Range(-leftMaxDistX, rightMaxDistX);
        float y = Random.Range(cloudMinY, cloudMaxY);
        Vector3 spawnPos = new Vector3(x, y, currentZ + renderDistance);
        GameObject cloud = Instantiate(cloudModel, spawnPos, Quaternion.identity);
        float s = Random.Range(0.7f, 1.4f);
        cloud.transform.localScale = Vector3.one * s;
        CloudMovement cm = cloud.AddComponent<CloudMovement>();
        float variance = Random.Range(0.8f, 1.2f);
        cm.Setup(cloudMoveSpeed * variance, bobSpeed * 0.5f, bobAmount);
    }

    void SpawnBackgroundIsland() {
        bool goLeft = Random.value > 0.5f;
        float x, y;
        if (goLeft) {
            x = -Random.Range(Mathf.Abs(leftMinDistX), Mathf.Abs(leftMaxDistX));
            y = Random.Range(leftMinY, leftMaxY);
        } else {
            x = Random.Range(Mathf.Abs(rightMinDistX), Mathf.Abs(rightMaxDistX));
            y = Random.Range(rightMinY, rightMaxY);
        }
        Vector3 islandCenter = new Vector3(x, y, currentZ);
        GameObject islandParent = new GameObject("Island_Z" + currentZ);
        islandParent.transform.position = islandCenter;
        islandParent.transform.localScale = Vector3.one * islandScale;
        HashSet<Vector3> occupied = new HashSet<Vector3>();
        List<Vector3> edgeTiles = new List<Vector3>(); 
        Vector3 start = Vector3.zero;
        occupied.Add(start);
        edgeTiles.Add(start);
        for (int i = 0; i < islandTileAmount; i++) {
            int index = Random.Range(0, edgeTiles.Count);
            Vector3 growFrom = edgeTiles[index];
            GameObject tile = Instantiate(currentBiome.ground, islandParent.transform.position + (growFrom * islandScale), Quaternion.identity, islandParent.transform);
            if (Random.value < 0.3f) Instantiate(currentBiome.wall, tile.transform.position + (Vector3.up * islandScale), Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0), tile.transform);
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            foreach (Vector3 d in directions) {
                Vector3 neighbor = growFrom + d;
                if (!occupied.Contains(neighbor)) edgeTiles.Add(neighbor);
            }
            edgeTiles.RemoveAt(index);
        }
        islandParent.AddComponent<FloatingIsland>().Setup(bobSpeed, bobAmount);
        activeIslands.Add(islandParent); 
    }

    void DetermineBiomeState() {
        if (!isTransitioning) {
            rowsInCurrentBiome++;
            if (rowsInCurrentBiome >= targetBiomeLength) {
                isTransitioning = true;
                transitionStep = 0;
                targetBiome = biomes[Random.Range(0, biomes.Length)];
                upcomingBiomeChanges.Add(new BiomeChangePoint { zTrigger = currentZ, settings = targetBiome });
            }
        } else {
            transitionStep++;
            if (transitionStep >= transitionLength) {
                isTransitioning = false;
                currentBiome = targetBiome;
                rowsInCurrentBiome = 0;
                targetBiomeLength = Random.Range(minBiomeRows, maxBiomeRows);
            }
        }
    }

    public void CrumbleTilesBelow(float z) {
        lastDeleteZ = z;
        for (int i = activeTiles.Count - 1; i >= 0; i--) {
            if (activeTiles[i] != null && activeTiles[i].transform.position.z < z) {
                FallingTile ft = activeTiles[i].GetComponent<FallingTile>();
                if (ft != null) ft.StartFalling();
                activeTiles.RemoveAt(i);
            }
        }
    }

    void CrumbleOldIslands() {
        if (player == null) return;
        float killZ = player.position.z - islandDeleteOffset;
        for (int i = activeIslands.Count - 1; i >= 0; i--) {
            if (activeIslands[i] != null && activeIslands[i].transform.position.z < killZ) {
                Destroy(activeIslands[i]);
                activeIslands.RemoveAt(i);
            }
        }
    }

    public BiomeSettings GetBiomeByName(string bName) {
        string lookUp = (bName == "Ice") ? "Snow" : (bName == "Quicksand" ? "Desert" : bName);
        foreach (var b in biomes) if (b.name == lookUp) return b;
        return biomes[0];
    }

    public void RestartGame() {
        if (fadeImage != null) StartCoroutine(FadeInRestart());
        else ActualRestart();
    }

    IEnumerator FadeInRestart() {
        float elapsed = 0f;
        Color c = fadeImage.color;
        while (elapsed < fadeDuration) {
            elapsed += Time.unscaledDeltaTime; 
            c.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        ActualRestart();
    }

    void ActualRestart() {
        Time.timeScale = 1.0f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}