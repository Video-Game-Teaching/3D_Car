using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Handles sound effects for UI interactions (button clicks, hovers, etc.)
/// Attach this to a GameObject in your scene and assign the sound clips
/// </summary>
public class UISoundEffects : MonoBehaviour
{
    public static UISoundEffects Instance { get; private set; }

    [Header("UI Sound Effects")]
    [Tooltip("Sound played when a button is clicked")]
    public AudioClip buttonClickSound;
    [Tooltip("Sound played when hovering over a button")]
    public AudioClip buttonHoverSound;
    [Tooltip("Sound played when selecting a button (for keyboard navigation)")]
    public AudioClip buttonSelectSound;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [Tooltip("Volume for UI sound effects (0-1)")]
    public float sfxVolume = 0.7f;

    private AudioSource audioSource;
    private Selectable lastSelectedButton; // Track last selected button to avoid duplicate hover sounds

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes

        // Set up AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        audioSource.volume = sfxVolume;
    }

    void Update()
    {
        // Update volume
        audioSource.volume = sfxVolume;

        // Check for keyboard navigation selection changes
        CheckForSelectionChange();
    }

    /// <summary>
    /// Check if selection changed (for keyboard navigation)
    /// </summary>
    private void CheckForSelectionChange()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            Selectable currentSelected = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
            if (currentSelected != null && currentSelected != lastSelectedButton)
            {
                PlayButtonSelectSound();
                lastSelectedButton = currentSelected;
            }
        }
    }

    /// <summary>
    /// Play button click sound
    /// </summary>
    public void PlayButtonClickSound()
    {
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound, sfxVolume);
        }
    }

    /// <summary>
    /// Play button hover sound
    /// </summary>
    public void PlayButtonHoverSound()
    {
        if (buttonHoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonHoverSound, sfxVolume);
        }
    }

    /// <summary>
    /// Play button select sound (for keyboard navigation)
    /// </summary>
    public void PlayButtonSelectSound()
    {
        if (buttonSelectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonSelectSound, sfxVolume * 0.7f); // Slightly quieter for selection
        }
    }
}

