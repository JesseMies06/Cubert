using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    [Header("Saved Settings")]
    public float savedVolume = 1f;
    public float savedSfxVolume = 1f;
    public int chargeKeyIndex = 0; 
    public int difficultyIndex = 1; 
    public float pixelSize = 4.0f;
    public bool useGameBoyFilter = false; 
    public int cameraMode = 0; // 0: Normal, 1: First Person, 2: Third Person
    public int totalCoins = 0;
    public int highScore = 0; // High Score tracking

    [Header("Skin System")]
    public string equippedSkinName = "";
    public SkinItem[] allSkins; // Fill this list in the inspector in both scenes

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
            // Coins and Skins are loaded here
            totalCoins = PlayerPrefs.GetInt("SavedCoins", 0);   
            equippedSkinName = PlayerPrefs.GetString("EquippedSkin", "");
        } else {
            Destroy(gameObject);
        }
    }

    // Settings Setters
    public void SetMasterVolume(float v) { savedVolume = v; SaveSettings(); }
    public void SetSfxVolume(float v) { savedSfxVolume = v; SaveSettings(); }
    public void SetChargeKeyIndex(int index) { chargeKeyIndex = index; SaveSettings(); }
    public void SetDifficultyIndex(int index) { difficultyIndex = index; SaveSettings(); }
    public void SetPixelSize(float v) { pixelSize = v; SaveSettings(); }
    public void SetGameBoyFilter(bool enabled) { useGameBoyFilter = enabled; SaveSettings(); }
    
    public void SetCameraMode(int index) {
        cameraMode = index;
        SaveSettings();
    }

    public void SaveSettings() {
        PlayerPrefs.SetFloat("MusicVolume", savedVolume);
        PlayerPrefs.SetFloat("SfxVolume", savedSfxVolume);
        PlayerPrefs.SetInt("ChargeKeyIndex", chargeKeyIndex);
        PlayerPrefs.SetInt("DifficultyIndex", difficultyIndex);
        PlayerPrefs.SetFloat("PixelSize", pixelSize);
        PlayerPrefs.SetInt("GameBoyFilter", useGameBoyFilter ? 1 : 0);
        PlayerPrefs.SetInt("CameraMode", cameraMode);
        PlayerPrefs.SetString("EquippedSkin", equippedSkinName);
        
        // Added highscore to the main save block
        PlayerPrefs.SetInt("HighScore", highScore); 
        
        PlayerPrefs.Save();
    }

    public void LoadSettings() {
        savedVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        savedSfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1f);
        chargeKeyIndex = PlayerPrefs.GetInt("ChargeKeyIndex", 0);
        difficultyIndex = PlayerPrefs.GetInt("DifficultyIndex", 1);
        pixelSize = PlayerPrefs.GetFloat("PixelSize", 4.0f);
        useGameBoyFilter = PlayerPrefs.GetInt("GameBoyFilter", 0) == 1;
        cameraMode = PlayerPrefs.GetInt("CameraMode", 0);
        equippedSkinName = PlayerPrefs.GetString("EquippedSkin", "");
        
        // Added highscore to the main load block
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    public KeyCode GetChargeKeyCode() {
        switch (chargeKeyIndex) {
            case 0: return KeyCode.LeftShift;
            case 1: return KeyCode.Space;
            case 2: return KeyCode.Q;
            default: return KeyCode.LeftShift;
        }
    }

    public void AddCoin(int amount) {
        totalCoins += amount;
        if (totalCoins < 0) totalCoins = 0;
        PlayerPrefs.SetInt("SavedCoins", totalCoins);
        PlayerPrefs.Save();
    }

    // Method to update High Score - Call this when the game ends
    public void SubmitScore(int score) {
        if (score > highScore) {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
            Debug.Log("New High Score Saved: " + highScore);
        }
    }

    [ContextMenu("DEBUG: Reset Everything")]
    public void ResetEverything() {
        PlayerPrefs.DeleteAll(); 
        totalCoins = 0;
        highScore = 0;
        equippedSkinName = "";
        
        LoadSettings(); 
        
        PlayerPrefs.Save();
        Debug.Log("<color=red><b>All Data Wiped!</b></color> Coins and High Score reset.");
    }

    [ContextMenu("DEBUG: Add 100 Coins")]
    public void AddDebugCoins() {
        AddCoin(100);
        Debug.Log("<color=green><b>Added 100 Coins!</b></color> Current Total: " + totalCoins);
    }
}