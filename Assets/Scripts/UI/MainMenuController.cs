using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles button clicks in the MainMenu scene to navigate to different scenes
/// Attach this script to a GameObject in the MainMenu scene and assign the buttons in the Inspector
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Name of the Game scene to load when Play Game button is clicked")]
    public string gameSceneName = "Game";
    
    [Tooltip("Name of the Tutorial scene to load when Tutorial button is clicked")]
    public string tutorialSceneName = "Tutorial";

    [Header("Background Music")]
    [Tooltip("Background music clip to play in the main menu")]
    public AudioClip menuMusic;
    [Range(0f, 1f)]
    [Tooltip("Volume for background music (0-1)")]
    public float musicVolume = 0.5f;
    
    private AudioSource musicAudioSource;

    void Start()
    {
        // Set up background music
        SetupBackgroundMusic();
        StartBackgroundMusic();
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
        if (musicAudioSource != null && menuMusic != null)
        {
            musicAudioSource.clip = menuMusic;
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

    /// <summary>
    /// Called when the "Play Game" button is clicked
    /// Loads the Game scene
    /// </summary>
    public void OnPlayGameButtonClicked()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            Debug.Log($"Loading scene: {gameSceneName}");
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Game scene name is not set in MainMenuController!");
        }
    }

    /// <summary>
    /// Called when the "Tutorial" button is clicked
    /// Loads the Tutorial scene
    /// </summary>
    public void OnTutorialButtonClicked()
    {
        if (!string.IsNullOrEmpty(tutorialSceneName))
        {
            Debug.Log($"Loading scene: {tutorialSceneName}");
            SceneManager.LoadScene(tutorialSceneName);
        }
        else
        {
            Debug.LogError("Tutorial scene name is not set in MainMenuController!");
        }
    }
}

