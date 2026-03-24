using UnityEngine;

[System.Serializable]
public class SkinItem {
    public string skinName;
    [TextArea(3, 5)] public string flavorText; // The fun description
    public int price;
    public GameObject skinPrefab;
    public bool isDefault = false;

    [Header("Custom Sounds")]
    public AudioClip jumpSound;
    public bool hasCustomJump;
    
    public AudioClip chargeJumpSound;
    public bool hasCustomCharge;
    
    public AudioClip deathSound;
    public bool hasCustomDeath;
}