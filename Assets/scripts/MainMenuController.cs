using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour {
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject customizePanel;
    public GameObject loadingOverlay; 

    [Header("UI Text")]
    public TextMeshProUGUI highScoreText; // Drag your High Score TMP here

    [Header("UI Animation Settings")]
    public float bounceDuration = 0.3f;
    public float bounceScaleMultiplier = 1.1f; 

    [Header("Music/SFX Settings")]
    public Slider volumeSlider;
    public TextMeshProUGUI volumeText;
    public Slider sfxSlider;
    public TextMeshProUGUI sfxText;

    [Header("Visual Settings")]
    public Slider pixelSlider;
    public Toggle gameBoyToggle; 
    public TMP_Dropdown cameraDropdown; 

    [Header("Control Settings")]
    public TMP_Dropdown chargeKeyDropdown; 
    public TMP_Dropdown difficultyDropdown; 

    [Header("Start Sequence")]
    public Animator menuPlayerAnimator; 
    public Image screenFader;           
    public float sceneDelay = 2.0f;     
    public float fadeSpeed = 1.0f;

    [Header("Audio Sources")]
    public AudioSource menuMusicSource; 
    public AudioSource audioClick;
    public AudioSource audioRatchet;
    
    public List<AudioSource> otherSfxSources = new List<AudioSource>();

    private bool isStarting = false;

    private void Start() {
        Time.timeScale = 1f; 
        
        if (screenFader != null) {
            Color c = screenFader.color; c.a = 0;
            screenFader.color = c;
            screenFader.gameObject.SetActive(false);
        }
        if (loadingOverlay != null) loadingOverlay.SetActive(false);

        if (GameManager.Instance != null) {
            // Display High Score
            if (highScoreText != null) {
                highScoreText.text = "Best: " + GameManager.Instance.highScore.ToString();
            }

            if (volumeSlider != null) {
                volumeSlider.value = GameManager.Instance.savedVolume;
                UpdateMusicText(volumeSlider.value);
                ApplyMusicVolume(volumeSlider.value);
            }
            if (sfxSlider != null) {
                sfxSlider.value = GameManager.Instance.savedSfxVolume;
                UpdateSfxText(sfxSlider.value);
                ApplySfxVolume(sfxSlider.value);
            }
            if (pixelSlider != null) pixelSlider.value = GameManager.Instance.pixelSize;
            if (chargeKeyDropdown != null) chargeKeyDropdown.value = GameManager.Instance.chargeKeyIndex;
            if (difficultyDropdown != null) difficultyDropdown.value = GameManager.Instance.difficultyIndex;
            if (gameBoyToggle != null) gameBoyToggle.isOn = GameManager.Instance.useGameBoyFilter;
            if (cameraDropdown != null) cameraDropdown.value = GameManager.Instance.cameraMode;
        }

        if (mainMenuPanel != null && mainMenuPanel.activeSelf) {
            StartCoroutine(BouncePanel(mainMenuPanel));
        }
    }

    public void StartGame() {
        if (isStarting) return;
        StartCoroutine(StartSequence());
    }

    IEnumerator StartSequence() {
        isStarting = true;
        if (menuPlayerAnimator != null) menuPlayerAnimator.SetTrigger("Start");
        yield return new WaitForSeconds(sceneDelay);

        if (screenFader != null) {
            screenFader.gameObject.SetActive(true);
            float alpha = 0;
            while (alpha < 1) {
                alpha += Time.deltaTime * fadeSpeed;
                Color c = screenFader.color; c.a = alpha;
                screenFader.color = c;
                yield return null;
            }
        }

        if (loadingOverlay != null) {
            loadingOverlay.SetActive(true);
            loadingOverlay.transform.SetAsLastSibling();
        }

        yield return null; yield return null;
        AsyncOperation operation = SceneManager.LoadSceneAsync("Game");
        operation.allowSceneActivation = false; 
        while (operation.progress < 0.9f) yield return null; 
        yield return new WaitForSecondsRealtime(0.5f);
        operation.allowSceneActivation = true;
    }

    public void PlaySkinEmote(int skinIndex) {
        if (menuPlayerAnimator != null) {
            menuPlayerAnimator.SetInteger("SkinIndex", skinIndex);
            menuPlayerAnimator.SetTrigger("Emote");
        }
    }

    public void UiClickSound() => audioClick.Play();
    public void UiRatchetSound() => audioRatchet.Play();

    public void OnVolumeSliderChanged(float v) { 
        if (GameManager.Instance != null) { 
            GameManager.Instance.SetMasterVolume(v); 
            UpdateMusicText(v); 
            ApplyMusicVolume(v);
        } 
    }

    public void OnSfxSliderChanged(float v) { 
        if (GameManager.Instance != null) { 
            GameManager.Instance.SetSfxVolume(v); 
            UpdateSfxText(v); 
            ApplySfxVolume(v);
        } 
    }

    private void ApplyMusicVolume(float v) {
        if (menuMusicSource != null) menuMusicSource.volume = v;
    }

    private void ApplySfxVolume(float v) {
        if (audioClick != null) audioClick.volume = v;
        if (audioRatchet != null) audioRatchet.volume = v;
        foreach (AudioSource sfx in otherSfxSources) {
            if (sfx != null) sfx.volume = v;
        }
    }

    public void OnPixelSliderChanged(float v) { if (GameManager.Instance != null) { GameManager.Instance.SetPixelSize(v); } }
    public void OnGameBoyToggleChanged(bool enabled) { if (GameManager.Instance != null) GameManager.Instance.SetGameBoyFilter(enabled); }
    public void OnChargeKeyChanged(int index) { if (GameManager.Instance != null) { GameManager.Instance.SetChargeKeyIndex(index); } }
    public void OnDifficultyChanged(int index) { if (GameManager.Instance != null) { GameManager.Instance.SetDifficultyIndex(index); } }
    public void OnCameraModeChanged(int index) { if (GameManager.Instance != null) { GameManager.Instance.SetCameraMode(index); } }

    private void UpdateMusicText(float v) { if (volumeText != null) volumeText.text = v.ToString("F2"); }
    private void UpdateSfxText(float v) { if (sfxText != null) sfxText.text = v.ToString("F2"); }

    public void OpenSettings() { 
        mainMenuPanel.SetActive(false); 
        settingsPanel.SetActive(true); 
        StartCoroutine(BouncePanel(settingsPanel));
    }

    public void CloseSettings() { 
        mainMenuPanel.SetActive(true); 
        settingsPanel.SetActive(false); 
        StartCoroutine(BouncePanel(mainMenuPanel));
    }

    public void OpenCustomize() { 
        mainMenuPanel.SetActive(false); 
        customizePanel.SetActive(true); 
        StartCoroutine(BouncePanel(customizePanel));
    }

    public void CloseCustomize() { 
        mainMenuPanel.SetActive(true); 
        customizePanel.SetActive(false); 
        StartCoroutine(BouncePanel(mainMenuPanel));
    }

    public void QuitGame() => Application.Quit();

    IEnumerator BouncePanel(GameObject panel) {
        Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
        foreach (Transform child in panel.transform) {
            originalScales.Add(child, child.localScale);
            child.localScale = Vector3.zero; 
        }

        float elapsed = 0f;
        while (elapsed < bounceDuration) {
            elapsed += Time.deltaTime;
            float percent = elapsed / bounceDuration;
            float scaleFactor;
            if (percent < 0.8f) {
                float subPercent = percent / 0.8f;
                scaleFactor = Mathf.Lerp(0f, bounceScaleMultiplier, subPercent);
            } else {
                float subPercent = (percent - 0.8f) / 0.2f;
                scaleFactor = Mathf.Lerp(bounceScaleMultiplier, 1.0f, subPercent);
            }
            foreach (var entry in originalScales) {
                if (entry.Key != null) entry.Key.localScale = entry.Value * scaleFactor;
            }
            yield return null;
        }
        foreach (var entry in originalScales) {
            if (entry.Key != null) entry.Key.localScale = entry.Value;
        }
    }
}