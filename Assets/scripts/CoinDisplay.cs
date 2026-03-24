using UnityEngine;
using TMPro;
using System.Collections;

public class CoinDisplay : MonoBehaviour {
    public TextMeshProUGUI coinText;
    public string prefix = "Coins: ";
    
    [Header("Bounce Settings")]
    public float bounceScale = 1.2f;
    public float bounceDuration = 0.2f;

    private int lastCoinCount;
    private Vector3 originalScale;
    private Coroutine bounceRoutine;

    void Start() {
        if (coinText != null) originalScale = coinText.transform.localScale;
        if (GameManager.Instance != null) lastCoinCount = GameManager.Instance.totalCoins;
    }

    void Update() {
        if (GameManager.Instance != null && coinText != null) {
            int currentCoins = GameManager.Instance.totalCoins;
            coinText.text = prefix + currentCoins.ToString();

            // Trigger bounce if coins increased
            if (currentCoins > lastCoinCount) {
                if (bounceRoutine != null) StopCoroutine(bounceRoutine);
                bounceRoutine = StartCoroutine(BounceEffect());
            }
            lastCoinCount = currentCoins;
        }
    }

    IEnumerator BounceEffect() {
        // Scale up
        float elapsed = 0f;
        while (elapsed < bounceDuration / 2) {
            elapsed += Time.unscaledDeltaTime;
            coinText.transform.localScale = Vector3.Lerp(originalScale, originalScale * bounceScale, elapsed / (bounceDuration / 2));
            yield return null;
        }
        // Scale down
        elapsed = 0f;
        while (elapsed < bounceDuration / 2) {
            elapsed += Time.unscaledDeltaTime;
            coinText.transform.localScale = Vector3.Lerp(originalScale * bounceScale, originalScale, elapsed / (bounceDuration / 2));
            yield return null;
        }
        coinText.transform.localScale = originalScale;
    }
}