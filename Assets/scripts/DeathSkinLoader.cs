using UnityEngine;

public class DeathSkinLoader : MonoBehaviour {
    void Awake() {
        ApplyDeathSkin();
    }

    void ApplyDeathSkin() {
        if (GameManager.Instance == null) return;

        string skinName = GameManager.Instance.equippedSkinName;
        SkinItem activeSkin = null;

        // Find the skin data
        foreach (var skin in GameManager.Instance.allSkins) {
            if (skin.skinName == skinName) {
                activeSkin = skin;
                break;
            }
        }

        // Fallback to default if nothing found
        if (activeSkin == null && GameManager.Instance.allSkins.Length > 0) {
            activeSkin = GameManager.Instance.allSkins[0];
        }

        if (activeSkin != null && activeSkin.skinPrefab != null) {
            // 1. Remove the "placeholder" model on the dead prefab
            foreach (Transform child in transform) {
                // Assuming your dead prefab has a placeholder named "Visual"
                if (child.name.Contains("Visual")) {
                    Destroy(child.gameObject);
                }
            }

            // 2. Spawn the correct skin model
            GameObject visual = Instantiate(activeSkin.skinPrefab, transform.position, transform.rotation, transform);
            visual.transform.localPosition = Vector3.zero;
            
            // 3. (Optional) Disable scripts on the spawned visual so it doesn't try to move
            MonoBehaviour[] scripts = visual.GetComponentsInChildren<MonoBehaviour>();
            foreach (var s in scripts) s.enabled = false;
        }
    }
}