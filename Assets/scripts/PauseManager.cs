using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PauseManager : MonoBehaviour {
    public static bool isPaused = false;
    
    [Header("UI References")]
    public GameObject pauseMenuUI;
    public Image fadeImage; 
    public float fadeDuration = 1.0f;

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) {
            if (isPaused) {
                Resume();
            } else {
                Pause();
            }
        }

        if (isPaused && Input.GetKeyDown(KeyCode.Space)) {
            Resume();
        }
    }

    public void Resume() {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; 
        isPaused = false;
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Pause() {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; 
        isPaused = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void LoadMenu() {
        // We stop all other coroutines to prevent conflicts if the button is clicked twice
        Time.timeScale = 1f;
        StopAllCoroutines(); 
        StartCoroutine(FadeAndExit());
    }

    private IEnumerator FadeAndExit() {
        // 1. Make sure the image is active and starts transparent
        fadeImage.gameObject.SetActive(true);
        float timer = 0;
        Color c = fadeImage.color;
        c.a = 0;
        fadeImage.color = c;

        // 2. USE UNSCALED TIME
        // This ensures the fade happens at 1.0s even if the game is in slow-mo (Time.timeScale = 0.1)
        while (timer < fadeDuration) {
            timer += Time.unscaledDeltaTime; // <--- The Fix
            c.a = timer / fadeDuration;
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1;
        fadeImage.color = c;
        
        // 3. Reset everything for the next scene
        Time.timeScale = 1f; 
        isPaused = false;
        SceneManager.LoadScene("Menu"); 
    }

    public void QuitGame() {
        Application.Quit();
    }
}