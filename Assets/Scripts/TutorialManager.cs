using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial UI Panels")]
    public GameObject step1_MovementPanel; // WASD
    public GameObject step2_DriftPanel;    // Space
    public GameObject step3_NitroPanel;    // Shift
    public GameObject step4_EndPanel;      // End of Tutorial

    [Header("Settings")]
    public bool showStep1OnStart = true;

    [Header("Background Music")]
    [Tooltip("Background music clip to play in the tutorial scene")]
    public AudioClip tutorialMusic;
    [Range(0f, 1f)]
    [Tooltip("Volume for background music (0-1)")]
    public float musicVolume = 0.5f;

    private bool isTutorialActive = false;
    private AudioSource musicAudioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Set up background music
        SetupBackgroundMusic();
        StartBackgroundMusic();

        // Ensure all panels are hidden at start
        if (step1_MovementPanel) step1_MovementPanel.SetActive(false);
        if (step2_DriftPanel) step2_DriftPanel.SetActive(false);
        if (step3_NitroPanel) step3_NitroPanel.SetActive(false);
        if (step4_EndPanel) step4_EndPanel.SetActive(false);

        if (showStep1OnStart)
        {
            // Small delay to let everything initialize
            StartCoroutine(ShowStep1Delayed());
        }
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
    /// Start playing the background music (looping)
    /// </summary>
    private void StartBackgroundMusic()
    {
        if (musicAudioSource != null && tutorialMusic != null)
        {
            musicAudioSource.clip = tutorialMusic;
            musicAudioSource.volume = musicVolume;
            musicAudioSource.Play();
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

    void OnDestroy()
    {
        // Stop music when leaving the scene
        StopBackgroundMusic();
    }

    private IEnumerator ShowStep1Delayed()
    {
        yield return new WaitForSeconds(0.5f);
        ShowTutorial(1);
    }

    public void ShowTutorial(int stepIndex)
    {
        if (isTutorialActive) return; // Already showing a tutorial

        GameObject panelToShow = null;

        switch (stepIndex)
        {
            case 1:
                panelToShow = step1_MovementPanel;
                break;
            case 2:
                panelToShow = step2_DriftPanel;
                break;
            case 3:
                panelToShow = step3_NitroPanel;
                break;
            case 4:
                panelToShow = step4_EndPanel;
                break;
            default:
                Debug.LogWarning($"Tutorial step {stepIndex} not defined.");
                return;
        }

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            PauseGame();
            isTutorialActive = true;
        }
    }

    public void CloseTutorial()
    {
        // Hide all panels
        if (step1_MovementPanel) step1_MovementPanel.SetActive(false);
        if (step2_DriftPanel) step2_DriftPanel.SetActive(false);
        if (step3_NitroPanel) step3_NitroPanel.SetActive(false);
        if (step4_EndPanel) step4_EndPanel.SetActive(false);

        ResumeGame();
        isTutorialActive = false;
    }

    [Header("Pause Menu")]
    public GameObject pauseMenuPanel;
    private bool isPausedByEsc = false;

    void Update()
    {
        // Only allow ESC pause if no tutorial is currently showing
        if (!isTutorialActive && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    public void TogglePauseMenu()
    {
        isPausedByEsc = !isPausedByEsc;

        if (isPausedByEsc)
        {
            Time.timeScale = 0f;
            if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
        }
        else
        {
            Time.timeScale = 1f;
            if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        }
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void StartMainGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game"); 
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        // Optionally disable car input if needed, but Time.timeScale 0 usually handles physics
        // If you need to disable input explicitly:
        // var car = FindObjectOfType<CarController>();
        // if (car) car.enabled = false;
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
        // var car = FindObjectOfType<CarController>();
        // if (car) car.enabled = true;
    }
}
