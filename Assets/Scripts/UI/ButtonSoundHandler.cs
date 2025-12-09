using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Component to attach to individual buttons to add sound effects
/// Automatically handles click and hover sounds
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSoundHandler : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Sound Settings")]
    [Tooltip("Override default click sound for this button")]
    public AudioClip customClickSound;
    [Tooltip("Override default hover sound for this button")]
    public AudioClip customHoverSound;
    [Tooltip("Play hover sound on mouse enter")]
    public bool playHoverSound = true;
    [Tooltip("Play click sound on button click")]
    public bool playClickSound = true;

    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        
        // Add click listener if not already added
        if (button != null && playClickSound)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    /// <summary>
    /// Called when mouse enters the button
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playHoverSound && button != null && button.interactable)
        {
            if (UISoundEffects.Instance != null)
            {
                if (customHoverSound != null)
                {
                    // Play custom hover sound
                    if (UISoundEffects.Instance.GetComponent<AudioSource>() != null)
                    {
                        UISoundEffects.Instance.GetComponent<AudioSource>().PlayOneShot(customHoverSound);
                    }
                }
                else
                {
                    // Play default hover sound
                    UISoundEffects.Instance.PlayButtonHoverSound();
                }
            }
        }
    }

    /// <summary>
    /// Called when button is clicked
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // This is handled by OnButtonClicked to ensure it works with both mouse and keyboard
    }

    /// <summary>
    /// Called when button is clicked (works for both mouse and keyboard)
    /// </summary>
    private void OnButtonClicked()
    {
        if (playClickSound && button != null && button.interactable)
        {
            if (UISoundEffects.Instance != null)
            {
                if (customClickSound != null)
                {
                    // Play custom click sound
                    if (UISoundEffects.Instance.GetComponent<AudioSource>() != null)
                    {
                        UISoundEffects.Instance.GetComponent<AudioSource>().PlayOneShot(customClickSound);
                    }
                }
                else
                {
                    // Play default click sound
                    UISoundEffects.Instance.PlayButtonClickSound();
                }
            }
        }
    }

    void OnDestroy()
    {
        // Remove listener when destroyed
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}


