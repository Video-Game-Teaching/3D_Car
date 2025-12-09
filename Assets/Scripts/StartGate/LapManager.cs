using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class LapManager : MonoBehaviour
{
    [Header("Race Configuration")]
    public int totalLapsRequired = 3;  // Total laps required to finish the race

    [Header("Race State")]
    public int currentLap = 0;
    public bool raceStarted = false;
    public bool raceCompleted = false; // New: Track if all laps are completed

    [Header("Timing")]
    public float segmentStartTime = -1f;   // 本圈从起点门通过的时间戳
    public bool awaitingFinish = false;    // 是否在等待通过终点
    public List<float> lapTimes = new List<float>();  // 每圈用时（Start→Finish）

    [Header("UI")]
    public TextMeshProUGUI lapCountText;  // UI text to display current lap (e.g., "1/3")

    [Header("Optional")]
    public Transform player; // 若挂在全局物体上，拖拽玩家车

    public void OnStartGatePassed(Vector3 gateForward, Vector3 carVelocity)
    {
        // Don't allow lap progress if game is not in playing state
        if (GameManager.Instance != null && !GameManager.Instance.IsGamePlaying())
        {
            return;
        }

        if (!IsForward(gateForward, carVelocity)) return;
        if (raceCompleted) return;

        if (!raceStarted)
        {
            raceStarted = true;
            currentLap = 1;
            segmentStartTime = Time.time;
            awaitingFinish = true;
            UpdateLapUI();
        }
        else
        {
            // If player is still awaiting finish for current lap, ignore this trigger
            if (awaitingFinish) return;

            // At this point, player has finished previous lap (awaitingFinish = false)
            // and checkpoints were already validated and reset in OnFinishGatePassed
            // So we can safely start a new lap without additional checkpoint checks

            // Start a new lap only if we haven't exceeded total laps
            if (currentLap < totalLapsRequired)
            {
                currentLap++;
                segmentStartTime = Time.time;
                awaitingFinish = true;
                UpdateLapUI();
            }
        }
    }

    public void OnFinishGatePassed(Vector3 gateForward, Vector3 carVelocity)
    {
        // Don't allow lap progress if game is not in playing state
        if (GameManager.Instance != null && !GameManager.Instance.IsGamePlaying())
        {
            return;
        }

        if (!raceStarted) return;
        if (raceCompleted) return;
        if (!IsForward(gateForward, carVelocity)) return;
        if (!awaitingFinish) return;

        // Check if all checkpoints have been completed for this lap
        CheckpointManager checkpointManager = CheckpointManager.Instance;
        if (checkpointManager != null && !checkpointManager.AllCheckpointsCompleted())
        {
            checkpointManager.ShowIncompleteCheckpointsWarning();
            return;
        }

        if (segmentStartTime > 0f)
        {
            float t = Time.time - segmentStartTime;
            lapTimes.Add(t);
            awaitingFinish = false;

            // Check if this was the final lap
            if (currentLap >= totalLapsRequired)
            {
                raceCompleted = true;
                UpdateLapUI();
            }
            else
            {
                // Reset checkpoint tracking for next lap
                if (checkpointManager != null)
                {
                    checkpointManager.ResetForNewLap();
                }
            }
        }
    }

    bool IsForward(Vector3 gateForward, Vector3 carVelocity)
    {
        Vector3 v = carVelocity.sqrMagnitude > 0.01f ? carVelocity.normalized :
                    (player ? player.forward : transform.forward);
        float dot = Vector3.Dot(gateForward.normalized, v.normalized);
        return dot > 0f;
    }

    // --------------------------
    // ★ 当前圈计时 · 增强接口
    // --------------------------

    /// <summary>
    /// 当前圈已用时（秒）。未在计时状态返回 -1。
    /// </summary>
    public float CurrentLapElapsed()
    {
        if (!raceStarted || !awaitingFinish || segmentStartTime < 0f)
            return -1f;
        return Time.time - segmentStartTime;
    }

    /// <summary>
    /// 当前圈已用时（格式化字符串 mm:ss.fff）。未计时返回 "--:--.---"
    /// </summary>
    public string GetCurrentLapFormatted()
    {
        float elapsed = CurrentLapElapsed();
        if (elapsed < 0f) return "--:--.---";
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        float seconds = elapsed % 60f;
        return string.Format("{0:00}:{1:00.000}", minutes, seconds);
    }

    // 可选：获取最近一圈/最佳圈
    public float LastLapTime() => lapTimes.Count > 0 ? lapTimes[lapTimes.Count - 1] : -1f;
    public float BestLapTime()
    {
        if (lapTimes.Count == 0) return -1f;
        float best = lapTimes[0];
        for (int i = 1; i < lapTimes.Count; i++) if (lapTimes[i] < best) best = lapTimes[i];
        return best;
    }

    // --------------------------
    // ★ UI Update Methods
    // --------------------------

    /// <summary>
    /// Update the lap count UI - displays as "P1/P3" format
    /// Called every frame to ensure real-time updates
    /// </summary>
    private void UpdateLapUI()
    {
        if (lapCountText != null)
        {
            if (raceCompleted)
            {
                // Show completion with final lap count
                lapCountText.text = $"P{totalLapsRequired}/{totalLapsRequired}";
            }
            else if (raceStarted)
            {
                // Show current lap vs total (e.g., "P1/P3" for lap 1 of 3)
                lapCountText.text = $"P{currentLap}/{totalLapsRequired}";
            }
            else
            {
                // Before race starts, show "P0/P3"
                lapCountText.text = $"P0/{totalLapsRequired}";
            }
        }
        else
        {
            // Try to find the UI text if it's not assigned
            if (lapCountText == null)
            {
                GameObject lapTextObject = GameObject.Find("Lap Count Text");
                if (lapTextObject == null)
                {
                    lapTextObject = GameObject.Find("LapCountText");
                }
                if (lapTextObject == null)
                {
                    lapTextObject = GameObject.Find("Lap Text");
                }
                
                if (lapTextObject != null)
                {
                    lapCountText = lapTextObject.GetComponent<TextMeshProUGUI>();
                    if (lapCountText != null)
                    {
                        UpdateLapUI();
                    }
                }
            }
        }
    }

    // --------------------------
    // ★ Lap Management Methods
    // --------------------------

    /// <summary>
    /// Check if the race is completed (all laps finished)
    /// </summary>
    public bool IsRaceCompleted()
    {
        return raceCompleted;
    }

    /// <summary>
    /// Get current lap number
    /// </summary>
    public int GetCurrentLap()
    {
        return currentLap;
    }

    /// <summary>
    /// Get total laps required
    /// </summary>
    public int GetTotalLaps()
    {
        return totalLapsRequired;
    }

    /// <summary>
    /// Get lap progress as formatted string (e.g., "1/3")
    /// </summary>
    public string GetLapProgressString()
    {
        if (raceCompleted)
        {
            return $"{totalLapsRequired}/{totalLapsRequired}";
        }
        else if (raceStarted)
        {
            return $"{currentLap}/{totalLapsRequired}";
        }
        else
        {
            return $"0/{totalLapsRequired}";
        }
    }

    // --------------------------
    // ★ Unity Lifecycle Methods
    // --------------------------

    void Start()
    {
        // Try to auto-find lap count text if not assigned
        if (lapCountText == null)
        {
            GameObject lapTextObject = GameObject.Find("Lap Count Text");
            if (lapTextObject == null)
            {
                lapTextObject = GameObject.Find("LapCountText");
            }
            if (lapTextObject == null)
            {
                lapTextObject = GameObject.Find("Lap Text");
            }
            
            if (lapTextObject != null)
            {
                lapCountText = lapTextObject.GetComponent<TextMeshProUGUI>();
            }
        }
        
        // Initialize UI
        UpdateLapUI();
    }

    void Update()
    {
        // Update lap UI every frame to ensure real-time display
        // This is lightweight and ensures the UI is always current
        UpdateLapUI();
    }
}