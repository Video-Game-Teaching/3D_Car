using UnityEngine;
using TMPro;

public class EnhancedLapTimer : MonoBehaviour
{
    [Header("References")]
    public LapManager lapManager;
    public RaceTimer raceTimer;

    [Header("UI References")]
    public TextMeshProUGUI currentLapTimeText;     // 当前圈用时
    public TextMeshProUGUI lastLapTimeText;        // 上一圈用时
    public TextMeshProUGUI bestLapTimeText;        // 最佳圈速
    public TextMeshProUGUI deltaTimeText;          // 与最佳圈速的差距

    [Header("Display Settings")]
    public bool showCurrentLapTime = true;
    public bool showLastLapTime = true;
    public bool showBestLapTime = true;
    public bool showDeltaTime = true;

    [Header("Delta Display Colors")]
    public Color fasterColor = Color.green;        // 比最佳圈速快时的颜色
    public Color slowerColor = Color.red;          // 比最佳圈速慢时的颜色
    public Color neutralColor = Color.yellow;      // 首圈或无对比时的颜色

    [Header("Best Lap Flash Effect")]
    public bool flashOnNewBest = true;             // 创造新最佳圈速时是否闪烁
    public int flashCount = 3;
    public float flashDuration = 0.3f;

    // Internal tracking
    private float currentBestLapTime = Mathf.Infinity;
    private float lastCompletedLapTime = -1f;
    private int lastLapCount = 0;
    private bool isFlashing = false;

    void Start()
    {
        // Auto-find references if not assigned
        if (lapManager == null)
            lapManager = FindObjectOfType<LapManager>();
        
        if (raceTimer == null)
            raceTimer = FindObjectOfType<RaceTimer>();

        // Auto-find UI elements if not assigned
        AutoFindUIElements();

        // Initialize UI
        InitializeUI();
    }

    void Update()
    {
        UpdateCurrentLapTime();
        CheckForLapCompletion();
        UpdateDeltaTime();
    }

    // Auto-find UI elements by common names
    private void AutoFindUIElements()
    {
        if (currentLapTimeText == null)
        {
            GameObject obj = GameObject.Find("Current Lap Time Text") ?? GameObject.Find("CurrentLapTime");
            if (obj != null) currentLapTimeText = obj.GetComponent<TextMeshProUGUI>();
        }

        if (lastLapTimeText == null)
        {
            GameObject obj = GameObject.Find("Last Lap Time Text") ?? GameObject.Find("LastLapTime");
            if (obj != null) lastLapTimeText = obj.GetComponent<TextMeshProUGUI>();
        }

        if (bestLapTimeText == null)
        {
            GameObject obj = GameObject.Find("Best Lap Time Text") ?? GameObject.Find("BestLapTime");
            if (obj != null) bestLapTimeText = obj.GetComponent<TextMeshProUGUI>();
        }

        if (deltaTimeText == null)
        {
            GameObject obj = GameObject.Find("Delta Time Text") ?? GameObject.Find("DeltaTime");
            if (obj != null) deltaTimeText = obj.GetComponent<TextMeshProUGUI>();
        }
    }

    // Initialize UI with default values
    private void InitializeUI()
    {
        if (currentLapTimeText != null)
            currentLapTimeText.text = "Current: --:--.---";

        if (lastLapTimeText != null)
            lastLapTimeText.text = "Last: --:--.---";

        if (bestLapTimeText != null)
            bestLapTimeText.text = "Best: --:--.---";

        if (deltaTimeText != null)
        {
            deltaTimeText.text = "Δ --:--.---";
            deltaTimeText.color = neutralColor;
        }
    }

    // Update current lap time display (real-time)
    private void UpdateCurrentLapTime()
    {
        if (!showCurrentLapTime || currentLapTimeText == null) return;
        if (lapManager == null) return;

        float currentTime = lapManager.CurrentLapElapsed();
        
        if (currentTime >= 0f)
        {
            currentLapTimeText.text = $"Current: {FormatTime(currentTime)}";
        }
        else
        {
            currentLapTimeText.text = "Current: --:--.---";
        }
    }

    // Check if a new lap has been completed and update stats
    private void CheckForLapCompletion()
    {
        if (lapManager == null) return;

        // Check if lap count increased (new lap completed)
        if (lapManager.lapTimes.Count > lastLapCount)
        {
            lastLapCount = lapManager.lapTimes.Count;
            
            // Get the most recent lap time
            float newLapTime = lapManager.lapTimes[lapManager.lapTimes.Count - 1];
            lastCompletedLapTime = newLapTime;

            // Update last lap display
            UpdateLastLapTime(newLapTime);

            // Check if it's a new best lap
            if (newLapTime < currentBestLapTime)
            {
                currentBestLapTime = newLapTime;
                UpdateBestLapTime(newLapTime, true);
            }
            else
            {
                UpdateBestLapTime(currentBestLapTime, false);
            }
        }
    }

    // Update last lap time display
    private void UpdateLastLapTime(float lapTime)
    {
        if (!showLastLapTime || lastLapTimeText == null) return;

        lastLapTimeText.text = $"Last: {FormatTime(lapTime)}";
    }

    // Update best lap time display
    private void UpdateBestLapTime(float lapTime, bool isNewBest)
    {
        if (!showBestLapTime || bestLapTimeText == null) return;

        if (lapTime < Mathf.Infinity)
        {
            bestLapTimeText.text = $"Best: {FormatTime(lapTime)}";

            // Flash effect for new best lap
            if (isNewBest && flashOnNewBest && !isFlashing)
            {
                StartCoroutine(FlashBestLapTime());
            }
        }
        else
        {
            bestLapTimeText.text = "Best: --:--.---";
        }
    }

    // Update delta time (difference from best lap)
    private void UpdateDeltaTime()
    {
        if (!showDeltaTime || deltaTimeText == null) return;
        if (lapManager == null) return;

        // Only show delta if we have a best lap time
        if (currentBestLapTime >= Mathf.Infinity)
        {
            deltaTimeText.text = "Δ --:--.---";
            deltaTimeText.color = neutralColor;
            return;
        }

        float currentTime = lapManager.CurrentLapElapsed();
        
        if (currentTime >= 0f)
        {
            // Calculate delta (current time - best time)
            float delta = currentTime - currentBestLapTime;

            // Format with + or - sign
            string sign = delta >= 0 ? "+" : "";
            deltaTimeText.text = $"Δ {sign}{FormatTime(Mathf.Abs(delta))}";

            // Color based on performance
            if (delta < 0)
            {
                // Faster than best lap - green
                deltaTimeText.color = fasterColor;
            }
            else if (delta > 0)
            {
                // Slower than best lap - red
                deltaTimeText.color = slowerColor;
            }
            else
            {
                // Equal to best lap - neutral
                deltaTimeText.color = neutralColor;
            }
        }
        else
        {
            deltaTimeText.text = "Δ --:--.---";
            deltaTimeText.color = neutralColor;
        }
    }

    // Flash effect when new best lap is achieved
    private System.Collections.IEnumerator FlashBestLapTime()
    {
        if (bestLapTimeText == null) yield break;

        isFlashing = true;
        Color originalColor = bestLapTimeText.color;

        for (int i = 0; i < flashCount; i++)
        {
            // Flash to highlight color (yellow/gold)
            bestLapTimeText.color = Color.yellow;
            yield return new WaitForSeconds(flashDuration / 2f);

            // Flash back to original
            bestLapTimeText.color = originalColor;
            yield return new WaitForSeconds(flashDuration / 2f);
        }

        isFlashing = false;
    }

    // Format time as mm:ss.fff
    private string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds < 0f) return "00:00.000";

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        float seconds = timeInSeconds % 60f;
        return string.Format("{0:00}:{1:00.000}", minutes, seconds);
    }

    // Get current best lap time
    public float GetBestLapTime()
    {
        return currentBestLapTime < Mathf.Infinity ? currentBestLapTime : -1f;
    }

    // Get last completed lap time
    public float GetLastLapTime()
    {
        return lastCompletedLapTime;
    }

    // Reset all lap time statistics
    public void ResetStats()
    {
        currentBestLapTime = Mathf.Infinity;
        lastCompletedLapTime = -1f;
        lastLapCount = 0;
        InitializeUI();
    }

    // Public API: Get current lap time vs best lap delta
    public float GetCurrentDelta()
    {
        if (lapManager == null || currentBestLapTime >= Mathf.Infinity) return 0f;
        
        float currentTime = lapManager.CurrentLapElapsed();
        if (currentTime < 0f) return 0f;

        return currentTime - currentBestLapTime;
    }

    // Public API: Check if current lap is faster than best
    public bool IsCurrentLapFaster()
    {
        return GetCurrentDelta() < 0f;
    }
}