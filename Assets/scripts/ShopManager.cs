using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text;

public class ShopManager : MonoBehaviour {
    [Header("UI References")]
    public TextMeshProUGUI actionButtonText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI skinNameText;
    public TextMeshProUGUI flavorTextDisplay; 
    public TextMeshProUGUI soundsListDisplay; 
    public Button actionButton;

    [Header("Icon Settings")]
    // Drag the text objects located under each selection icon here
    public TextMeshProUGUI[] skinIconLabels; 

    [Header("Preview Settings")]
    public Transform previewParent; 
    public SkinLoader playerSkinLoader; 
    private GameObject currentPreview;
    private int selectedIndex = 0;

    [Header("Effects & Audio")]
    public GameObject swapEffect;    // Particle when previewing a different skin
    public GameObject buyEffect;     // Particle when purchasing a skin
    public AudioSource shopAudioSource; // AudioSource to play the sounds
    public AudioClip buySound;       // Sound played on successful purchase
    public AudioClip swapSound;      // Sound played when changing preview
    public AudioClip equipSound;     // Sound played when equipping owned skin

    void Start() {
        string saved = PlayerPrefs.GetString("EquippedSkin", "");
        for (int i = 0; i < GameManager.Instance.allSkins.Length; i++) {
            if (GameManager.Instance.allSkins[i].skinName == saved) {
                selectedIndex = i;
                break;
            }
        }
        UpdateShopUI();
    }

    public void SelectSkin(int index) {
        // Only trigger swap effect if the selection actually changes
        if (selectedIndex != index) {
            SpawnEffect(swapEffect, swapSound);
        }

        selectedIndex = index;
        
        if (playerSkinLoader != null) {
            SkinItem current = GameManager.Instance.allSkins[selectedIndex];
            playerSkinLoader.PreviewSkin(current);
        }

        UpdateShopUI();
    }

    void UpdateShopUI() {
        // --- ICON LABELS LOGIC ---
        for (int i = 0; i < GameManager.Instance.allSkins.Length; i++) {
            if (i >= skinIconLabels.Length || skinIconLabels[i] == null) continue;

            SkinItem item = GameManager.Instance.allSkins[i];
            bool owned = PlayerPrefs.GetInt("Owned_" + item.skinName, 0) == 1 || item.isDefault;

            // If owned, show "OWNED". If not, leave it empty.
            if (owned) {
                skinIconLabels[i].text = "OWNED";
            } else {
                skinIconLabels[i].text = "";
            }
        }

        SkinItem current = GameManager.Instance.allSkins[selectedIndex];
        
        skinNameText.text = current.skinName;
        flavorTextDisplay.text = current.flavorText;

        StringBuilder sb = new StringBuilder();
        if (current.hasCustomJump || current.hasCustomCharge || current.hasCustomDeath) {
            sb.AppendLine("Custom sounds:");
            if (current.hasCustomJump) sb.AppendLine("-Jump");
            if (current.hasCustomCharge) sb.AppendLine("-Charge Jump");
            if (current.hasCustomDeath) sb.AppendLine("-Death (Hit)");
            soundsListDisplay.text = sb.ToString();
        } else {
            soundsListDisplay.text = ""; 
        }

        UpdatePreview(current.skinPrefab);

        bool isOwned = PlayerPrefs.GetInt("Owned_" + current.skinName, 0) == 1 || current.isDefault;
        bool isEquipped = PlayerPrefs.GetString("EquippedSkin", "") == current.skinName || (current.isDefault && PlayerPrefs.GetString("EquippedSkin", "") == "");

        if (isEquipped) {
            actionButtonText.text = "EQUIPPED";
            priceText.text = "OWNED"; 
            actionButton.interactable = false;
        } else if (isOwned) {
            actionButtonText.text = "EQUIP";
            priceText.text = "OWNED"; 
            actionButton.interactable = true;
        } else {
            actionButtonText.text = "BUY";
            priceText.text = current.price.ToString() + " COINS";
            actionButton.interactable = (GameManager.Instance.totalCoins >= current.price);
        }
    }

    void UpdatePreview(GameObject prefab) {
        if (previewParent == null) return;
        foreach (Transform child in previewParent) {
            Destroy(child.gameObject);
        }
        if (prefab != null) {
            currentPreview = Instantiate(prefab, previewParent.position, previewParent.rotation, previewParent);
            currentPreview.transform.localPosition = Vector3.zero;
        }
    }

    public void OnActionButtonPressed() {
        SkinItem current = GameManager.Instance.allSkins[selectedIndex];
        bool isOwned = PlayerPrefs.GetInt("Owned_" + current.skinName, 0) == 1 || current.isDefault;

        if (isOwned) {
            shopAudioSource.PlayOneShot(equipSound);
            EquipSkin(current.skinName);
        } else {
            BuySkin(current);
        }
        UpdateShopUI();
    }

    void BuySkin(SkinItem skin) {
        if (GameManager.Instance.totalCoins >= skin.price) {
            GameManager.Instance.AddCoin(-skin.price);
            PlayerPrefs.SetInt("Owned_" + skin.skinName, 1);
            
            SpawnEffect(buyEffect, buySound); // Play buy particle and sound
            EquipSkin(skin.skinName);
        }
    }

    void EquipSkin(string name) {
        PlayerPrefs.SetString("EquippedSkin", name);
        GameManager.Instance.equippedSkinName = name;
        PlayerPrefs.Save();
        
        if (playerSkinLoader != null) playerSkinLoader.LoadSkin();
    }

    // Helper method to spawn particles and play sound at the same time
    void SpawnEffect(GameObject effectPrefab, AudioClip clip) {
        // Handle Particle
        if (effectPrefab != null && previewParent != null) {
            GameObject fx = Instantiate(effectPrefab, previewParent.position, Quaternion.identity);
            Destroy(fx, 3f); 
        }

        // Handle Sound
        if (shopAudioSource != null && clip != null) {
            shopAudioSource.PlayOneShot(clip);
        }
    }

    public void OnBackButton() {
        string equipped = PlayerPrefs.GetString("EquippedSkin", "");
        for (int i = 0; i < GameManager.Instance.allSkins.Length; i++) {
            if (GameManager.Instance.allSkins[i].skinName == equipped || (GameManager.Instance.allSkins[i].isDefault && equipped == "")) {
                selectedIndex = i;
                break;
            }
        }

        if (playerSkinLoader != null) {
            playerSkinLoader.LoadSkin();
        }

        UpdateShopUI();
    }
}