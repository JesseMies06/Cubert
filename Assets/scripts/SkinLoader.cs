using UnityEngine;
using UnityEngine.SceneManagement;

public class SkinLoader : MonoBehaviour {
    public SkinItem activeSkinData;

    [Header("Visual Adjustment")]
    public float yOffset = 0f; 

    [Header("Menu Animation")]
    public Animator menuAnimator; // Assign the Player animator here in the Menu scene

    void Awake() {
        LoadSkin();
    }

    public void LoadSkin() {
        if (GameManager.Instance == null || GameManager.Instance.allSkins.Length == 0) return;

        string skinToLoad = PlayerPrefs.GetString("EquippedSkin", "");
        SkinItem foundSkin = null;
        int foundIndex = 0;

        for (int i = 0; i < GameManager.Instance.allSkins.Length; i++) {
            var skin = GameManager.Instance.allSkins[i];
            if (skin.skinName == skinToLoad || (skin.isDefault && skinToLoad == "")) {
                foundSkin = skin;
                foundIndex = i;
                break;
            }
        }

        if (foundSkin == null) foundSkin = GameManager.Instance.allSkins[0];
        activeSkinData = foundSkin;

        ApplyVisual(foundSkin);

        // Optional: Trigger emote on load if in menu
        if (menuAnimator != null) {
            menuAnimator.SetInteger("SkinIndex", foundIndex);
            menuAnimator.SetTrigger("Emote");
        }
    }

    public void PreviewSkin(SkinItem skin) {
        if (skin == null) return;
        ApplyVisual(skin);
        
        // Find index for the emote
        if (menuAnimator != null && GameManager.Instance != null) {
            for (int i = 0; i < GameManager.Instance.allSkins.Length; i++) {
                if (GameManager.Instance.allSkins[i] == skin) {
                    menuAnimator.SetInteger("SkinIndex", i);
                    menuAnimator.SetTrigger("Emote");
                    break;
                }
            }
        }
    }

    private void ApplyVisual(SkinItem skin) {
        foreach (Transform child in transform) {
            if (child.name.Contains("Visual")) {
                Destroy(child.gameObject);
            }
        }

        if (skin.skinPrefab != null) {
            GameObject visual = Instantiate(skin.skinPrefab, transform.position, transform.rotation, transform);
            visual.transform.localPosition = new Vector3(0, yOffset, 0);
            visual.name = "SkinVisual"; 
        }
    }
}