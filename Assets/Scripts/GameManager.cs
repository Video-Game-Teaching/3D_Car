using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        StartMenu,      // Showing start menu
        Playing,        // Game is running
        Paused,         // Game is paused
        GameOver        // Race finished
    }
    public GameState currentState = GameState.StartMenu;

    [Header("UI Panels")]
    public GameObject startMenuPanel;
    public GameObject pauseMenuPanel;
    public GameObject gameOverPanel;

    [Header("Game Over UI")]
    public TextMeshProUGUI finalTimeText;
    public TextMeshProUGUI gameOverTitleText;

    [Header("References")]
    public RaceTimer raceTimer;
    public LapManager lapManager;
    private CarController playerCar;

    [Header("Audio")]
    public AudioClip backgroundMusic; // Background music that loops during gameplay
    [Range(0f, 1f)]
    public float musicVolume = 0.5f; // Volume for background music (0-1)
    private AudioSource musicAudioSource; // AudioSource component for background music

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Auto-find UI Panels if not assigned..
        AutoBindUIPanels();

        // Find references if not assigned
        if (raceTimer == null)
        {
            raceTimer = FindObjectOfType<RaceTimer>();
        }
        if (lapManager == null)
        {
            lapManager = FindObjectOfType<LapManager>();
        }

        // Find player car
        playerCar = FindObjectOfType<CarController>();

        // Set up background music AudioSource
        SetupBackgroundMusic();

        // Initialize to start menu
        SetGameState(GameState.StartMenu);
    }

    /// <summary>
    /// Set up AudioSource component for background music
    /// </summary>
    private void SetupBackgroundMusic()
    {
        // Get or create AudioSource for background music
        musicAudioSource = GetComponent<AudioSource>();
        if (musicAudioSource == null)
        {
            musicAudioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure AudioSource for background music
        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = true; // Loop the music
        musicAudioSource.volume = musicVolume;
        musicAudioSource.spatialBlend = 0f; // 2D sound (0 = 2D, 1 = 3D)
    }

    /// <summary>
    /// Automatically find and assign UI panels by name if not manually assigned
    /// </summary>
    private void AutoBindUIPanels()
    {
        // Find Start Menu Panel
        if (startMenuPanel == null)
        {
            startMenuPanel = GameObject.Find("StartMenuPanel");
        }

        // Find Pause Menu Panel
        if (pauseMenuPanel == null)
        {
            pauseMenuPanel = GameObject.Find("PauseMenuPanel");
        }

        // Find Game Over Panel
        if (gameOverPanel == null)
        {
            gameOverPanel = GameObject.Find("GameOverPanel");
        }

        // Note: We don't control the original gameplay UI (timer, lap counter, etc.)
        // Those UI elements remain active throughout the game

        // Auto-find Final Time Text (by name first to avoid confusion)
        if (finalTimeText == null && gameOverPanel != null)
        {
            Transform finalTimeTransform = FindChildRecursive(gameOverPanel.transform, "FinalTimeText");
            if (finalTimeTransform != null)
            {
                finalTimeText = finalTimeTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        // Auto-find Game Over Title Text (by name)
        if (gameOverTitleText == null && gameOverPanel != null)
        {
            Transform titleTransform = FindChildRecursive(gameOverPanel.transform, "GameOverTitle");
            if (titleTransform != null)
            {
                gameOverTitleText = titleTransform.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    /// <summary>
    /// Recursively search for a child object by name
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }
            Transform result = FindChildRecursive(child, childName);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    void Update()
    {
        // Handle ESC key for pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        // Check if race is completed
        if (currentState == GameState.Playing && lapManager != null && lapManager.IsRaceCompleted())
        {
            ShowGameOver();
        }
    }

    public void SetGameState(GameState newState)
    {
        currentState = newState;

        // Hide menu panels first
        if (startMenuPanel) startMenuPanel.SetActive(false);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        // Show appropriate panel and set time scale
        switch (newState)
        {
            case GameState.StartMenu:
                if (startMenuPanel) startMenuPanel.SetActive(true);
                Time.timeScale = 0f;  // Freeze game
                SetCarInputEnabled(false);
                // Stop background music when returning to start menu
                StopBackgroundMusic();
                break;

            case GameState.Playing:
                // Original gameplay UI remains visible
                Time.timeScale = 1f;  // Normal game speed
                SetCarInputEnabled(true);
                // Start background music when game starts playing
                StartBackgroundMusic();
                break;

            case GameState.Paused:
                if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
                // Original gameplay UI remains visible in background
                Time.timeScale = 0f;  // Freeze game
                SetCarInputEnabled(false);
                break;

            case GameState.GameOver:
                if (gameOverPanel) gameOverPanel.SetActive(true);
                // Original gameplay UI remains visible in background
                Time.timeScale = 0f;  // Freeze game
                SetCarInputEnabled(false);
                // Stop background music when game ends
                StopBackgroundMusic();
                break;
        }

    }



    // ----------------------- Button Handlers

    public void OnStartButtonPressed()
    {
        SetGameState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;  // Reset time scale before reloading
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // Make sure your main menu scene is named "MainMenu"
    }

    // ----------------------- Game Over Logic

    private void ShowGameOver()
    {
        // Calculate and display final time
        if (finalTimeText != null && lapManager != null)
        {
            // Calculate total race time from all laps
            float totalTime = 0f;
            foreach (float lapTime in lapManager.lapTimes)
            {
                totalTime += lapTime;
            }
            finalTimeText.text = $"Final Time: {FormatTime(totalTime)}";
        }

        // Show Game Over panel
        SetGameState(GameState.GameOver);
    }

    private void SetCarInputEnabled(bool enabled)
    {
        if (playerCar != null)
        {
            playerCar.enabled = enabled;
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds < 0f) return "00:00.000";

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        float seconds = timeInSeconds % 60f;
        return string.Format("{0:00}:{1:00.000}", minutes, seconds);
    }


    // -------------------------Public API for Game State Queries
    public bool IsGamePlaying()
    {
        return currentState == GameState.Playing;
    }

    public bool IsGamePaused()
    {
        return currentState == GameState.Paused;
    }

    public bool IsGameOver()
    {
        return currentState == GameState.GameOver;
    }

    // ------------------------- Background Music Methods

    /// <summary>
    /// Start playing the background music (looping)
    /// </summary>
    private void StartBackgroundMusic()
    {
        if (musicAudioSource != null && backgroundMusic != null)
        {
            // Only start if not already playing
            if (!musicAudioSource.isPlaying)
            {
                musicAudioSource.clip = backgroundMusic;
                musicAudioSource.volume = musicVolume;
                musicAudioSource.Play();
            }
        }
    }

    /// <summary>
    /// Stop the background music
    /// </summary>
    private void StopBackgroundMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
    }
}
