using UnityEngine;
using TMPro;

public class RaceTimer : MonoBehaviour
{
    [Header("Race State")]
    public bool raceStarted = false;
    public bool raceFinished = false;
    public float raceStartTime = -1f;
    public float raceEndTime = -1f;
    public float totalRaceTime = -1f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    // Note: Final time display is now handled by GameManager
    // [SerializeField] private TextMeshProUGUI finalTimeText;  // DEPRECATED - use GameManager instead
    // [SerializeField] private GameObject finalTimePanel;      // DEPRECATED - use GameManager instead

    [Header("Display Settings")]
    [SerializeField] private bool showMilliseconds = true;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color finishedColor = Color.green;

    private void Start()
    {
        // Auto-find timer text if not assigned
        if (!timerText)
        {
            timerText = GetComponent<TextMeshProUGUI>();
        }

        // Initialize UI
        if (timerText)
        {
            timerText.text = "00:00.000";
            timerText.color = normalColor;
        }

        // Note: Final time panel is now managed by GameManager
    }

    private void Update()
    {
        // Only update timer when game is playing
        if (GameManager.Instance != null && !GameManager.Instance.IsGamePlaying())
        {
            return;
        }

        if (raceStarted && !raceFinished)
        {
            UpdateTimerDisplay();
        }
    }

    /// <summary>
    /// Called when the race starts (start gate is passed)
    /// </summary>
    public void StartRace()
    {
        // Don't start race if game is not in playing state
        if (GameManager.Instance != null && !GameManager.Instance.IsGamePlaying())
        {
            Debug.Log("[RaceTimer] Cannot start race - game is not in playing state!");
            return;
        }

        if (raceStarted) return; // Prevent multiple starts

        raceStarted = true;
        raceStartTime = Time.time;
        raceFinished = false;
        totalRaceTime = -1f;

        Debug.Log("[RaceTimer] Race started!");

        if (timerText)
        {
            timerText.color = normalColor;
        }
    }

    /// <summary>
    /// Called when the race finishes (end gate is passed)
    /// </summary>
    public void FinishRace()
    {
        if (!raceStarted || raceFinished) return; // Must be started and not already finished

        // Check if all checkpoints have been completed
        CheckpointManager checkpointManager = CheckpointManager.Instance;
        if (checkpointManager != null && !checkpointManager.AllCheckpointsCompleted())
        {
            Debug.Log("[RaceTimer] Cannot finish race - not all checkpoints completed! Progress: " +
                      checkpointManager.GetProgressString());
            checkpointManager.ShowIncompleteCheckpointsWarning();
            return;
        }

        raceFinished = true;
        raceEndTime = Time.time;
        totalRaceTime = raceEndTime - raceStartTime;

        Debug.Log($"[RaceTimer] Race finished! Total time: {FormatTime(totalRaceTime)}");

        // Update UI to show final time in the timer
        if (timerText)
        {
            timerText.text = FormatTime(totalRaceTime);
            // Don't change timer text color - keep it normal
        }

        // Note: Final time display and Game Over screen are now handled by GameManager
        // GameManager will detect race completion via LapManager and show the final time with flashing effect
    }

    /// <summary>
    /// Reset the race timer for a new race
    /// </summary>
    public void ResetRace()
    {
        raceStarted = false;
        raceFinished = false;
        raceStartTime = -1f;
        raceEndTime = -1f;
        totalRaceTime = -1f;

        if (timerText)
        {
            timerText.text = "00:00.000";
            // Keep timer text color as is - don't force it to normal color
        }

        // Note: Final time panel is now managed by GameManager

        Debug.Log("[RaceTimer] Race timer reset!");
    }

    /// <summary>
    /// Get the current elapsed race time
    /// </summary>
    public float GetCurrentRaceTime()
    {
        if (!raceStarted) return 0f;
        if (raceFinished) return totalRaceTime;
        return Time.time - raceStartTime;
    }

    /// <summary>
    /// Get the current elapsed race time as a formatted string
    /// </summary>
    public string GetCurrentRaceTimeFormatted()
    {
        return FormatTime(GetCurrentRaceTime());
    }

    /// <summary>
    /// Get the final race time (only valid after race is finished)
    /// </summary>
    public float GetFinalRaceTime()
    {
        return raceFinished ? totalRaceTime : -1f;
    }

    /// <summary>
    /// Check if the race is currently running
    /// </summary>
    public bool IsRaceRunning()
    {
        return raceStarted && !raceFinished;
    }

    /// <summary>
    /// Check if the race has finished
    /// </summary>
    public bool IsRaceFinished()
    {
        return raceFinished;
    }

    private void UpdateTimerDisplay()
    {
        if (!timerText) return;

        float currentTime = GetCurrentRaceTime();
        timerText.text = FormatTime(currentTime);
    }

    private string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds < 0f) return "00:00.000";

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        float seconds = timeInSeconds % 60f;

        if (showMilliseconds)
        {
            return string.Format("{0:00}:{1:00.000}", minutes, seconds);
        }
        else
        {
            return string.Format("{0:00}:{1:00}", minutes, Mathf.FloorToInt(seconds));
        }
    }

    // Public methods for external access
    public void SetTimerText(TextMeshProUGUI text) => timerText = text;
    public void SetShowMilliseconds(bool show) => showMilliseconds = show;

    // DEPRECATED: Final time display is now handled by GameManager
    // public void SetFinalTimeText(TextMeshProUGUI text) => finalTimeText = text;
    // public void SetFinalTimePanel(GameObject panel) => finalTimePanel = panel;
}
