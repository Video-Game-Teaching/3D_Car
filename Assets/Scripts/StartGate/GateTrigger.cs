using UnityEngine;
using System.Collections.Generic;

public enum GateType { Start, Finish, StartAndFinish }

public class GateTrigger : MonoBehaviour
{
    public GateType gateType = GateType.Start;
    public LapManager lapManager;
    public RaceTimer raceTimer;                 // Reference to the race timer
    public Rigidbody playerRb;                 // 拖玩家车的 Rigidbody（强烈推荐）
    public string playerTag = "Player";        // 若没拖 playerRb，则用 Tag 识别
    public string aiCarTag = "AICar";          // AI car tag
    public bool trackAICars = true;            // Whether to track AI cars
    public Transform forwardRef;               // 门的朝向（留空用本物体）

    [Header("Debounce / Anti-Multi-Hit")]
    public float cooldownSeconds = 1.0f;       // 一次通过后的冷却时间
    private Dictionary<Rigidbody, float> lastPassTime = new();  // 每个刚体各自冷却

    void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (!rb) return;

        bool isPlayer =
            (playerRb && rb == playerRb) ||
            (!playerRb && rb.gameObject.CompareTag(playerTag));
        bool isAICar = trackAICars && rb.gameObject.CompareTag(aiCarTag);

        if (!isPlayer && !isAICar) return;

        // Check if player is in teleport protection period
        if (isPlayer)
        {
            CheckpointManager checkpointManager = CheckpointManager.Instance;
            if (checkpointManager != null && checkpointManager.IsPlayerInTeleportProtection())
            {
                return;
            }
        }

        // Cooldown check
        float last;
        if (lastPassTime.TryGetValue(rb, out last))
        {
            if (Time.time - last < cooldownSeconds) return;
        }

        if (!lapManager) lapManager = FindObjectOfType<LapManager>();
        if (!raceTimer) raceTimer = FindObjectOfType<RaceTimer>();
        if (!lapManager && !raceTimer) return;

        // Direction check
        Vector3 carVel = rb.velocity.sqrMagnitude > 0.1f ? rb.velocity : rb.transform.forward;
        Vector3 gateForward = (forwardRef ? forwardRef.forward : transform.forward).normalized;

        switch (gateType)
        {
            case GateType.Start:
                if (lapManager) lapManager.OnStartGatePassed(gateForward, carVel);
                if (raceTimer && !raceTimer.IsRaceRunning()) raceTimer.StartRace();
                break;
            case GateType.Finish:
                if (lapManager) lapManager.OnFinishGatePassed(gateForward, carVel);
                // Only finish the race timer if all laps are completed
                if (raceTimer && lapManager && lapManager.IsRaceCompleted())
                {
                    raceTimer.FinishRace();
                }
                break;
            case GateType.StartAndFinish:
                if (lapManager)
                {
                    // CRITICAL FIX: Only call OnFinishGatePassed if race has already started
                    // This prevents checkpoint validation error on the very first pass through the gate
                    if (lapManager.raceStarted)
                    {
                        lapManager.OnFinishGatePassed(gateForward, carVel);
                    }
                    lapManager.OnStartGatePassed(gateForward, carVel);
                }
                if (raceTimer)
                {
                    // Only finish the race timer if all laps are completed
                    if (lapManager && lapManager.IsRaceCompleted())
                    {
                        raceTimer.FinishRace();
                    }
                    else if (!raceTimer.IsRaceRunning())
                    {
                        raceTimer.StartRace();
                    }
                }
                break;
        }

        // Record pass time for cooldown
        lastPassTime[rb] = Time.time;
    }
}
