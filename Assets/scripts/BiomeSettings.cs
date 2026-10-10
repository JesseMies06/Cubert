using UnityEngine;

[System.Serializable]
public class BiomeSettings {
    public string name;
    
    [Header("Prefabs")]
    public GameObject ground;
    public GameObject wall;
    public GameObject water;
    public GameObject uniqueBridge;

    [Header("Cave Settings")]
    public bool isCave = false;
    public GameObject caveWallBack;  
    public int caveWallHeight = 5;   

    [Header("Wall Variety")]
    public GameObject wall2; 
    [Range(0, 100)]
    public float wall2Ratio = 20f; 
    public bool wall2IsJumpable = false;

    [Header("Environment Visuals")]
    public Color lightColor = Color.white;
    public float lightIntensity = 1.0f;
    public Color backgroundColor = Color.gray;

    [Header("Generation Logic")]
    [Range(0f, 1f)] public float wallChance = 0.2f;
    [Range(0f, 1f)] public float riverChance = 0.1f;
    
    [Header("Biome Mechanics")]
    public float jumpTimeMultiplier = 1.0f; 

    [Header("Hazard Settings")]
    public GameObject hazardPrefab;
    [Range(0f, 1f)] public float hazardChance = 0.1f; // Only for side-spawning biomes
    public float hazardSpeed = 6f;
    public float minSpawnInterval = 3.0f; // Increase these to slow down bat spawns
    public float maxSpawnInterval = 6.0f;

    [Header("Water Effects")]
    public GameObject waterEffectPrefab; 
    [Range(0f, 1f)] public float waterEffectChance = 0.2f;

    [Header("Atmosphere")]
    public bool canSpawnClouds = true; 
    public GameObject[] cloudPrefabs; 

    [Header("Coin Spawning")]
    public GameObject coinPrefab;
    [Range(0f, 1f)] public float coinSpawnRate = 0.15f;

    [Header("Monkey Spawning")]
    public bool spawnMonkeys;
    public GameObject monkeyPrefab;
    [Range(0f, 1f)] public float monkeySpawnRate = 0.1f;

    [Header("Scorpion Spawning")]
    public bool spawnEnemies;
    public GameObject enemyPrefab;
    [Range(0f, 1f)] public float enemySpawnRate = 0.1f;
    
    [Header("Audio")]
    public AudioClip biomeMusic;
}