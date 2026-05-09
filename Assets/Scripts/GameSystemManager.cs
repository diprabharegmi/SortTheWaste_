using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSystemManager : MonoBehaviour
{
    [Header("UI References")]
    public Light directionalLight;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText; 
    public GameObject winScreen, loseScreen;

    [Header("Lives UI")]
    public GameObject[] lifeIcons; 

    [Header("Game Settings")]
    private int currentScore = 0;
    private int wrongAttempts = 0;
    private float gameTimer = 300f; 
    private bool isGameOver = false;

    [Header("Atmosphere Settings")]
    [Tooltip("Starting light - set to 0.3f for a 'cloudy day' look instead of dark night")]
    public float startingIntensity = 0.3f; 
    public float intensityBoost = 0.05f;    
    
    [Header("Fog & Background")]
    [Tooltip("Lowered to 0.008 for a very light mist")]
    public float startingFogDensity = 0.008f; 
    public float fogReduction = 0.001f;      
    // A lighter, softer grey to prevent the "dark void" feel
    public Color atmosphereColor = new Color(0.5f, 0.5f, 0.5f); 

    void Start() {
        Time.timeScale = 1; 
        
        // 1. Setup Lighting
        if(directionalLight != null) {
            directionalLight.intensity = startingIntensity; 
        }

        // 2. Setup Camera and Fog
        Camera mainCam = Camera.main;
        if(mainCam != null) {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = atmosphereColor;
        }

        RenderSettings.fog = true; 
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = startingFogDensity;
        RenderSettings.fogColor = atmosphereColor; 

        // 3. UI Setup
        if(winScreen != null) winScreen.SetActive(false);
        if(loseScreen != null) loseScreen.SetActive(false);
        
        UpdateScoreUI();
        UpdateLivesUI(); 
    }

    void Update() {
        if (isGameOver) return;
        gameTimer -= Time.deltaTime;
        if (timerText != null) {
            int minutes = Mathf.FloorToInt(gameTimer / 60);
            int seconds = Mathf.FloorToInt(gameTimer % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        if (gameTimer <= 0) EndGame(false, "Time Ran Out!");
    }

    public void CorrectSort() {
        if (isGameOver) return;
        currentScore += 10;
        UpdateScoreUI();
        
        if(directionalLight != null) directionalLight.intensity += intensityBoost;
        
        if(RenderSettings.fogDensity > 0) {
            RenderSettings.fogDensity -= fogReduction;
            if(RenderSettings.fogDensity < 0) RenderSettings.fogDensity = 0;
        }
        CheckWinCondition();
    }

    public void WrongSort() {
        if (isGameOver) return; 
        wrongAttempts++;
        UpdateLivesUI(); 
        if (wrongAttempts >= 3) EndGame(false, "Too many mistakes!");
    }

    void UpdateScoreUI() {
        if (scoreText != null) scoreText.text = "Score: " + currentScore;
    }

    void UpdateLivesUI() {
        if (lifeIcons == null || lifeIcons.Length == 0) return;
        for (int i = 0; i < lifeIcons.Length; i++) {
            Image iconImage = lifeIcons[i].GetComponent<Image>();
            if (iconImage != null) {
                iconImage.color = (i < wrongAttempts) ? new Color(0.2f, 0.2f, 0.2f, 0.6f) : Color.white;
            }
        }
    }

    void CheckWinCondition() {
        GameObject[] deg = GameObject.FindGameObjectsWithTag("Degradable");
        GameObject[] nonDeg = GameObject.FindGameObjectsWithTag("NonDegradable");
        if (deg.Length == 0 && nonDeg.Length == 0) EndGame(true, "City Cleaned!");
    }

    void EndGame(bool win, string msg) {
        if (isGameOver) return; 
        isGameOver = true;
        Time.timeScale = 0; 
        if (win && winScreen != null) winScreen.SetActive(true);
        else if (loseScreen != null) loseScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}