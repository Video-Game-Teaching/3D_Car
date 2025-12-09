using UnityEngine;
using System.Collections.Generic;

public class AILapManager : MonoBehaviour
{
    [Header("AI Race State")]
    public int currentLap = 0;
    public bool raceStarted = false;
    public bool raceFinished = false;
    
    [Header("Timing")]
    public float segmentStartTime = -1f;
    public bool awaitingFinish = false;
    public List<float> lapTimes = new List<float>();
    public float totalRaceTime = 0f;
    
    [Header("References")]
    public Rigidbody aiRigidbody;
    public string aiTag = "AICar";
    
    private void Start()
    {
        if (aiRigidbody == null)
        {
            aiRigidbody = GetComponent<Rigidbody>();
        }
        
        if (aiRigidbody == null)
        {
            GameObject aiCar = GameObject.FindGameObjectWithTag(aiTag);
            if (aiCar != null)
            {
                aiRigidbody = aiCar.GetComponent<Rigidbody>();
            }
        }
    }
    
    public void OnStartGatePassed(Vector3 gateForward, Vector3 carVelocity)
    {
        if (!IsForward(gateForward, carVelocity)) return;
        
        if (!raceStarted)
        {
            raceStarted = true;
            currentLap = 1;
            segmentStartTime = Time.time;
            awaitingFinish = true;
            Debug.Log($"AI Race started! Lap {currentLap} timer started.");
        }
        else if (!raceFinished)
        {
            currentLap++;
            segmentStartTime = Time.time;
            awaitingFinish = true;
            Debug.Log($"AI New Lap: {currentLap}. Timer restarted at StartGate.");
        }
    }
    
    public void OnFinishGatePassed(Vector3 gateForward, Vector3 carVelocity)
    {
        if (!raceStarted || raceFinished) return;
        if (!IsForward(gateForward, carVelocity)) return;
        if (!awaitingFinish) return;
        
        if (segmentStartTime > 0f)
        {
            float lapTime = Time.time - segmentStartTime;
            lapTimes.Add(lapTime);
            awaitingFinish = false;
            totalRaceTime += lapTime;
            
            Debug.Log($"AI Lap {currentLap} finished! Time = {lapTime:F3}s");
            
            // Check if race is finished (you can adjust this based on your race rules)
            if (currentLap >= 3) // Example: 3 laps
            {
                raceFinished = true;
                Debug.Log($"AI Race finished! Total time = {totalRaceTime:F3}s");
            }
        }
    }
    
    bool IsForward(Vector3 gateForward, Vector3 carVelocity)
    {
        Vector3 v = carVelocity.sqrMagnitude > 0.01f ? carVelocity.normalized : transform.forward;
        float dot = Vector3.Dot(gateForward.normalized, v.normalized);
        return dot > 0f;
    }
    
    public float CurrentLapElapsed()
    {
        if (!raceStarted || !awaitingFinish || segmentStartTime < 0f)
            return -1f;
        return Time.time - segmentStartTime;
    }
    
    public string CurrentLapTimeString()
    {
        float elapsed = CurrentLapElapsed();
        if (elapsed < 0f) return "--:--.---";
        
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        float seconds = elapsed % 60f;
        return $"{minutes:D2}:{seconds:F3}";
    }
    
    public float GetBestLapTime()
    {
        if (lapTimes.Count == 0) return -1f;
        
        float bestTime = lapTimes[0];
        for (int i = 1; i < lapTimes.Count; i++)
        {
            if (lapTimes[i] < bestTime)
            {
                bestTime = lapTimes[i];
            }
        }
        return bestTime;
    }
    
    public float GetAverageLapTime()
    {
        if (lapTimes.Count == 0) return -1f;
        
        float total = 0f;
        foreach (float time in lapTimes)
        {
            total += time;
        }
        return total / lapTimes.Count;
    }
    
    public void ResetRace()
    {
        currentLap = 0;
        raceStarted = false;
        raceFinished = false;
        segmentStartTime = -1f;
        awaitingFinish = false;
        lapTimes.Clear();
        totalRaceTime = 0f;
    }
}
