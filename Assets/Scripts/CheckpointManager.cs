using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro; // TextMeshPro

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    public List<GameObject> checkpoints;
    private int nextCheckpointIndex = 1; // Start from Node1 instead of Node0
    private HashSet<int> visitedCheckpoints = new HashSet<int>(); // Track visited checkpoints

    public delegate void OnCheckpointPassed(int checkpointIndex);
    public event OnCheckpointPassed onCheckpointPassed;

    // UI text for checkpoint message
    public TextMeshProUGUI checkpointText;
    private float displayDuration = 2f; // display duration
    private float currentDisplayTime = 0f;
    public GameObject WrongCheckpointMessage;

    // Prevent multiple simultaneous teleport countdowns
    private bool isTeleportCountdownActive = false;
    private Coroutine activeCountdownCoroutine = null;

    // Track when player was last teleported to prevent gate re-triggering
    private float lastTeleportTime = -10f;
    public float teleportProtectionDuration = 3f; // Seconds of protection after teleport

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        checkpoints = GameObject.FindObjectsOfType<GameObject>()
                                .Where(obj => obj.name.StartsWith("Node"))
                                .OrderBy(obj => GetCheckpointNumber(obj.name))
                                .ToList();

        foreach (var checkpoint in checkpoints)
        {
            // Move checkpoint to Default layer (0) to avoid interfering with Ground layer raycasts
            // This prevents the car's terrain detection from hitting the checkpoint collider
            checkpoint.layer = 0;

            Collider col = checkpoint.GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider boxCollider = checkpoint.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                // Give the checkpoint a reasonable thickness (Z-axis) to ensure stable trigger detection
                // Width: 40, Height: 10, Depth: 5
                boxCollider.size = new Vector3(40f, 10f, 5f);
            }

            if (checkpoint.GetComponent<CheckpointTrigger>() == null)
            {
                checkpoint.AddComponent<CheckpointTrigger>();
            }
        }

        if (checkpointText == null)
        {
            checkpointText = GameObject.Find("Checkpoint Text").GetComponent<TextMeshProUGUI>();
        }

        // Mark Node0 as visited (starting position)
        visitedCheckpoints.Add(0);
    }

    void Update()
    {
        if (currentDisplayTime > 0)
        {
            currentDisplayTime -= Time.deltaTime;
            if (currentDisplayTime <= 0)
            {
                checkpointText.gameObject.SetActive(false);
            }
        }
    }

    public void CheckpointHit(GameObject checkpointObject)
    {
        // Find the index of the hit checkpoint
        int hitCheckpointIndex = checkpoints.IndexOf(checkpointObject);
        if (hitCheckpointIndex < 0) return;

        // Always ignore Node0 (starting line)
        if (hitCheckpointIndex == 0) return;

        // If the checkpoint has already been visited in this lap, ignore it (no penalty)
        if (visitedCheckpoints.Contains(hitCheckpointIndex)) return;

        // Determine expected index
        int expectedIndex = nextCheckpointIndex;

        if (hitCheckpointIndex == expectedIndex)
        {
            // Mark this checkpoint as visited
            visitedCheckpoints.Add(hitCheckpointIndex);

            // show checkpoint message
            ShowCheckpointMessage(expectedIndex);

            if (onCheckpointPassed != null)
            {
                onCheckpointPassed(expectedIndex);
            }

            // Move to next expected index
            nextCheckpointIndex = expectedIndex + 1;
        }
        else
        {
            // Handle wrong checkpoint
            HandleWrongCheckpoint(checkpointObject);
        }
    }

    private void HandleWrongCheckpoint(GameObject checkpointObject)
    {
        // If a countdown is already active, ignore this new wrong checkpoint trigger
        if (isTeleportCountdownActive) return;

        // Capture the current state BEFORE player can continue moving
        int teleportTargetIndex = nextCheckpointIndex - 1;
        if (teleportTargetIndex < 0)
        {
            teleportTargetIndex = 0;
        }

        WrongCheckpointMessage.SetActive(true);

        // Start countdown and track it
        isTeleportCountdownActive = true;
        activeCountdownCoroutine = StartCoroutine(ShowFlashingCountdown(teleportTargetIndex));
    }

    private IEnumerator ShowFlashingCountdown(int teleportTargetIndex)
    {
        float remainingTime = 5f;
        TextMeshProUGUI wrongText = WrongCheckpointMessage.GetComponentInChildren<TextMeshProUGUI>();

        while (remainingTime > 0)
        {
            if (wrongText != null)
            {
                int secondsLeft = Mathf.CeilToInt(remainingTime);
                wrongText.text = $"You are taking a shortcut. \n You will be teleported back to your last passed checkpoint in {secondsLeft} seconds.";

                // flashing
                float alpha = 0.7f + 0.3f * Mathf.Sin(Time.time * 4f);
                wrongText.color = new Color(wrongText.color.r, wrongText.color.g, wrongText.color.b, alpha);
            }

            yield return null;
            remainingTime -= Time.deltaTime;
        }

        if (WrongCheckpointMessage != null)
        {
            WrongCheckpointMessage.SetActive(false);
        }

        TeleportToCheckpoint(teleportTargetIndex);

        // Reset the countdown flag after teleport is complete
        isTeleportCountdownActive = false;
        activeCountdownCoroutine = null;
    }

    private void TeleportToLastCheckpoint()
    {
        // Wrapper method for compatibility - calculates last checkpoint index and calls TeleportToCheckpoint
        int lastPassedIndex = nextCheckpointIndex - 1;
        if (lastPassedIndex < 0)
        {
            lastPassedIndex = 0;
        }
        TeleportToCheckpoint(lastPassedIndex);
    }

    private void TeleportToCheckpoint(int checkpointIndex)
    {
        // Find player car
        GameObject playerCar = null;

        // Find all objects with "Player" tag and pick the one with CarController
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        if (playerObjects != null && playerObjects.Length > 0)
        {
            foreach (GameObject obj in playerObjects)
            {
                if (obj.GetComponent<CarController>() != null)
                {
                    playerCar = obj;
                    break;
                }
            }
            if (playerCar == null && playerObjects.Length > 0)
            {
                playerCar = playerObjects[0];
            }
        }

        // Fallback: find directly by CarController component
        if (playerCar == null)
        {
            CarController carController = FindObjectOfType<CarController>();
            if (carController != null)
            {
                playerCar = carController.gameObject;
            }
        }

        if (playerCar == null)
        {
            Debug.LogError("[CheckpointManager] Unable to find player car for teleportation!");
            return;
        }

        // Ensure the index is valid
        if (checkpointIndex < 0 || checkpointIndex >= checkpoints.Count)
        {
            checkpointIndex = Mathf.Clamp(checkpointIndex, 0, checkpoints.Count - 1);
        }

        GameObject targetCheckpoint = checkpoints[checkpointIndex];
        Vector3 teleportPosition = targetCheckpoint.transform.position;

        // Calculate rotation to face the NEXT checkpoint in the sequence
        Quaternion teleportRotation;
        int nextCheckpointIndexAfterTeleport = checkpointIndex + 1;

        if (nextCheckpointIndexAfterTeleport < checkpoints.Count)
        {
            // Calculate direction from current checkpoint to next checkpoint
            GameObject nextCheckpoint = checkpoints[nextCheckpointIndexAfterTeleport];
            Vector3 directionToNext = (nextCheckpoint.transform.position - targetCheckpoint.transform.position).normalized;

            // Use only horizontal direction (XZ plane) to avoid tilting the car
            directionToNext.y = 0;
            if (directionToNext.sqrMagnitude > 0.01f)
            {
                teleportRotation = Quaternion.LookRotation(directionToNext);
            }
            else
            {
                teleportRotation = targetCheckpoint.transform.rotation;
            }
        }
        else
        {
            teleportRotation = targetCheckpoint.transform.rotation;
        }

        // Get Rigidbody and prepare for teleport
        Rigidbody rb = playerCar.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Execute teleport (raised to avoid ground collision)
        Vector3 safeTeleportPosition = teleportPosition + Vector3.up * 3f;
        playerCar.transform.position = safeTeleportPosition;
        playerCar.transform.rotation = teleportRotation;

        // Record teleport time to activate protection period
        lastTeleportTime = Time.time;

        // Re-enable physics after a short delay
        if (rb != null)
        {
            StartCoroutine(RestorePhysicsAfterTeleport(rb));
        }
    }

    private IEnumerator RestorePhysicsAfterTeleport(Rigidbody rb)
    {
        // Wait for physics to settle
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Re-enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    private void ShowCheckpointMessage(int checkpointIndex)
    {
        if (checkpointText != null)
        {
            string timeStr = "";
            var raceTimer = FindObjectOfType<RaceTimer>();   // 简单获取场景里的 RaceTimer
            if (raceTimer != null && raceTimer.IsRaceRunning())
            {
                timeStr = raceTimer.GetCurrentRaceTimeFormatted();
            }

            checkpointText.text = $"Checkpoint {checkpointIndex} Passed!\nTime: {timeStr}";
            checkpointText.gameObject.SetActive(true);
            currentDisplayTime = displayDuration;
        }
    }


    private int GetCheckpointNumber(string checkpointName)
    {
        // Extracts the number from a checkpoint name like "Node1"
        string numberPart = new string(checkpointName.Where(char.IsDigit).ToArray());
        if (int.TryParse(numberPart, out int number))
        {
            return number;
        }
        return -1;
    }

    // Check if all checkpoints have been passed (excluding Node0)
    public bool AllCheckpointsCompleted()
    {
        int requiredCheckpoints = checkpoints.Count - 1; // Exclude Node0
        int visitedCount = 0;
        for (int i = 1; i < checkpoints.Count; i++)
        {
            if (visitedCheckpoints.Contains(i))
            {
                visitedCount++;
            }
        }
        return visitedCount >= requiredCheckpoints;
    }

    // Get the current progress (how many checkpoints passed out of total)
    public string GetProgressString()
    {
        int visitedCount = 0;
        for (int i = 1; i < checkpoints.Count; i++)
        {
            if (visitedCheckpoints.Contains(i))
            {
                visitedCount++;
            }
        }
        int totalCheckpoints = checkpoints.Count - 1; // Exclude Node0
        return $"{visitedCount}/{totalCheckpoints}";
    }

    // Show warning message when trying to finish without completing all checkpoints
    public void ShowIncompleteCheckpointsWarning()
    {
        if (isTeleportCountdownActive) return;

        if (WrongCheckpointMessage != null)
        {
            WrongCheckpointMessage.SetActive(true);
            isTeleportCountdownActive = true;
            activeCountdownCoroutine = StartCoroutine(ShowIncompleteWarningCountdown());
        }
    }

    private IEnumerator ShowIncompleteWarningCountdown()
    {
        float remainingTime = 5f;
        TextMeshProUGUI wrongText = WrongCheckpointMessage.GetComponentInChildren<TextMeshProUGUI>();

        while (remainingTime > 0)
        {
            if (wrongText != null)
            {
                int secondsLeft = Mathf.CeilToInt(remainingTime);
                string progress = GetProgressString();
                wrongText.text = $"You haven't completed all checkpoints yet!\nProgress: {progress}\nYou will be teleported back to your last passed checkpoint in {secondsLeft} seconds.";

                // flashing
                float alpha = 0.7f + 0.3f * Mathf.Sin(Time.time * 4f);
                wrongText.color = new Color(wrongText.color.r, wrongText.color.g, wrongText.color.b, alpha);
            }

            yield return null;
            remainingTime -= Time.deltaTime;
        }

        if (WrongCheckpointMessage != null)
        {
            WrongCheckpointMessage.SetActive(false);
        }

        TeleportToLastCheckpoint();

        // Reset the countdown flag after teleport is complete
        isTeleportCountdownActive = false;
        activeCountdownCoroutine = null;
    }

    // Reset checkpoint tracking for a new lap
    public void ResetForNewLap()
    {
        visitedCheckpoints.Clear();
        visitedCheckpoints.Add(0); // Mark Node0 as visited
        nextCheckpointIndex = 1;
    }

    /// <summary>
    /// Check if player is currently in teleport protection period
    /// </summary>
    public bool IsPlayerInTeleportProtection()
    {
        return (Time.time - lastTeleportTime) < teleportProtectionDuration;
    }
}
