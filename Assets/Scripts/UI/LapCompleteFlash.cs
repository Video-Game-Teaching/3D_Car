using UnityEngine;
using TMPro;
using System.Collections;

public class LapCompleteFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [Tooltip("Number of times the text will flash")]
    public int flashCount = 3;

    [Tooltip("Duration of each flash cycle (on + off)")]
    public float flashCycleDuration = 0.5f;

    [Tooltip("Text to display when lap is completed")]
    public string lapCompleteMessage = "LAP {0} FINISHED!";

    [Header("UI References")]
    public TextMeshProUGUI flashText;

    [Header("Visual Settings")]
    [Tooltip("Color of the flash text")]
    public Color flashColor = Color.yellow;

    [Tooltip("Font size for flash text")]
    public float flashFontSize = 60f;

    [Tooltip("Position offset from center (0,0 = center of screen)")]
    public Vector2 screenPosition = new Vector2(0, 100);

    private bool isFlashing = false;
    private LapManager lapManager;

    void Start()
    {
        // Find LapManager
        lapManager = FindObjectOfType<LapManager>();

        // Auto-find or create flash text UI
        if (flashText == null)
        {
            // Try to find existing flash text
            GameObject flashTextObj = GameObject.Find("Lap Complete Flash Text");
            if (flashTextObj != null)
            {
                flashText = flashTextObj.GetComponent<TextMeshProUGUI>();
            }

            // If not found, create one
            if (flashText == null)
            {
                Debug.LogWarning("[LapCompleteFlash] Flash text not found. Please assign it in the inspector or create a UI Text element named 'Lap Complete Flash Text'.");
            }
        }

        // Initialize flash text (hide it)
        if (flashText != null)
        {
            flashText.gameObject.SetActive(false);
            flashText.fontSize = flashFontSize;
            flashText.color = flashColor;
            flashText.alignment = TextAlignmentOptions.Center;

            // Set position
            RectTransform rectTransform = flashText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = screenPosition;
            }
        }

        // Subscribe to lap completion event
        if (lapManager != null)
        {
            // We'll trigger the flash when a lap is completed
            StartCoroutine(MonitorLapCompletion());
        }
    }

    /// Monitor lap completion and trigger flash effect
    private IEnumerator MonitorLapCompletion()
    {
        int lastCompletedLap = 0;

        while (true)
        {
            // Wait for next frame
            yield return null;

            // Check if a new lap has been completed
            if (lapManager != null && lapManager.lapTimes.Count > lastCompletedLap)
            {
                lastCompletedLap = lapManager.lapTimes.Count;

                // Trigger flash for the completed lap
                TriggerLapCompleteFlash(lastCompletedLap);
            }

            // Stop monitoring if race is completed
            if (lapManager != null && lapManager.IsRaceCompleted())
            {
                yield break;
            }
        }
    }

    /// Trigger the lap complete flash effect
    public void TriggerLapCompleteFlash(int completedLapNumber)
    {
        if (isFlashing) return; // Prevent multiple simultaneous flashes
        if (flashText == null) return;

        StartCoroutine(FlashRoutine(completedLapNumber));
    }

    /// Flash animation coroutine
    private IEnumerator FlashRoutine(int lapNumber)
    {
        isFlashing = true;

        // Set the message
        string message = string.Format(lapCompleteMessage, lapNumber);
        flashText.text = message;

        // Calculate flash timing
        float flashOnTime = flashCycleDuration * 0.5f;
        float flashOffTime = flashCycleDuration * 0.5f;

        // Flash multiple times
        for (int i = 0; i < flashCount; i++)
        {
            // Flash ON
            flashText.gameObject.SetActive(true);

            // Smooth fade in (optional - makes it less jarring)
            yield return FadeTextAlpha(0f, 1f, flashOnTime * 0.3f);

            // Hold visible
            yield return new WaitForSeconds(flashOnTime * 0.7f);

            // Smooth fade out (optional)
            yield return FadeTextAlpha(1f, 0f, flashOffTime * 0.5f);

            // Flash OFF
            flashText.gameObject.SetActive(false);

            // Wait before next flash (only if not the last flash)
            if (i < flashCount - 1)
            {
                yield return new WaitForSeconds(flashOffTime * 0.5f);
            }
        }

        // Ensure text is hidden at the end
        flashText.gameObject.SetActive(false);

        isFlashing = false;
    }

    /// Smoothly fade text alpha for smoother visual effect
    private IEnumerator FadeTextAlpha(float startAlpha, float endAlpha, float duration)
    {
        if (flashText == null) yield break;

        float elapsed = 0f;
        Color originalColor = flashText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Lerp alpha
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, t);
            flashText.color = new Color(originalColor.r, originalColor.g, originalColor.b, currentAlpha);

            yield return null;
        }

        // Ensure final alpha is set
        flashText.color = new Color(originalColor.r, originalColor.g, originalColor.b, endAlpha);
    }

    /// Public method to manually trigger flash (for testing or external calls)
    public void ManualTriggerFlash(int lapNumber)
    {
        TriggerLapCompleteFlash(lapNumber);
    }
}