using UnityEngine;
using System.Collections.Generic;

public class AIRaceManager : MonoBehaviour
{
    [Header("AI Car Setup")]
    public GameObject aiCarPrefab; // Assign one of the car prefabs
    public Transform aiSpawnPoint; // Where to spawn the AI car
    public string aiCarTag = "AICar"; // Tag for the AI car
    
    [Header("Waypoint Setup")]
    public WaypointSystem waypointSystem;
    public bool createWaypointsAutomatically = true;
    public float waypointSpacing = 20f; // Distance between waypoints if creating automatically
    
    [Header("AI Settings")]
    [Range(0f, 1f)]
    public float aiDifficulty = 0.7f;
    public bool aiStartsWithPlayer = true;
    
    [Header("References")]
    public LapManager lapManager;
    public RaceTimer raceTimer;
    
    private GameObject aiCarInstance;
    private AIController aiController;
    private AIDrivingInput aiInput;
    private CarController aiCarController;
    
    void Start()
    {
        SetupAI();
    }
    
    void SetupAI()
    {
        // Find required components
        if (lapManager == null)
            lapManager = FindObjectOfType<LapManager>();
        if (raceTimer == null)
            raceTimer = FindObjectOfType<RaceTimer>();
        
        // Create waypoint system if needed
        if (waypointSystem == null)
        {
            CreateWaypointSystem();
        }
        
        // Spawn AI car
        SpawnAICar();
        
        // Configure AI
        ConfigureAI();
    }
    
    void CreateWaypointSystem()
    {
        GameObject waypointSystemObj = new GameObject("AI Waypoint System");
        waypointSystem = waypointSystemObj.AddComponent<WaypointSystem>();
        
        if (createWaypointsAutomatically)
        {
            CreateWaypointsAlongTrack();
        }
    }
    
    void CreateWaypointsAlongTrack()
    {
        // This is a simple waypoint creation system
        // In a real implementation, you might want to use the RoadArchitect system
        // or manually place waypoints in the scene
        
        List<Transform> waypoints = new List<Transform>();
        
        // Create waypoints in a simple oval track
        // You can modify this to match your actual track layout
        Vector3 center = Vector3.zero;
        float radius = 50f;
        int waypointCount = 20;
        
        for (int i = 0; i < waypointCount; i++)
        {
            float angle = (360f / waypointCount) * i * Mathf.Deg2Rad;
            Vector3 position = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
            
            GameObject waypoint = new GameObject($"Waypoint_{i:D2}");
            waypoint.transform.position = position;
            waypoint.transform.parent = waypointSystem.transform;
            waypoints.Add(waypoint.transform);
        }
        
        waypointSystem.waypoints = waypoints;
        waypointSystem.loopWaypoints = true;
        
        Debug.Log($"Created {waypointCount} waypoints for AI");
    }
    
    void SpawnAICar()
    {
        if (aiCarPrefab == null)
        {
            Debug.LogError("AI Car Prefab not assigned!");
            return;
        }
        
        // Determine spawn position
        Vector3 spawnPosition = Vector3.zero;
        if (aiSpawnPoint != null)
        {
            spawnPosition = aiSpawnPoint.position;
        }
        else
        {
            // Try to find a spawn position near the start gate
            GateTrigger startGate = FindObjectOfType<GateTrigger>();
            if (startGate != null)
            {
                spawnPosition = startGate.transform.position + startGate.transform.right * 5f; // Spawn to the side
            }
        }
        
        // Calculate rotation to face first waypoint
        Quaternion spawnRotation = Quaternion.identity;
        if (waypointSystem != null && waypointSystem.waypoints.Count > 0)
        {
            Vector3 directionToFirstWaypoint = (waypointSystem.waypoints[0].position - spawnPosition).normalized;
            directionToFirstWaypoint.y = 0; // Keep it horizontal
            if (directionToFirstWaypoint != Vector3.zero)
            {
                spawnRotation = Quaternion.LookRotation(directionToFirstWaypoint);
            }
        }
        else if (aiSpawnPoint != null)
        {
            spawnRotation = aiSpawnPoint.rotation;
        }
        
        // Spawn the AI car
        aiCarInstance = Instantiate(aiCarPrefab, spawnPosition, spawnRotation);
        aiCarInstance.name = "AI Car";
        aiCarInstance.tag = aiCarTag;
        
        // Get components
        aiController = aiCarInstance.GetComponent<AIController>();
        aiInput = aiCarInstance.GetComponent<AIDrivingInput>();
        aiCarController = aiCarInstance.GetComponent<CarController>();
        
        // Add components if they don't exist
        if (aiController == null)
        {
            aiController = aiCarInstance.AddComponent<AIController>();
        }
        if (aiInput == null)
        {
            aiInput = aiCarInstance.AddComponent<AIDrivingInput>();
        }
        if (aiCarController == null)
        {
            aiCarController = aiCarInstance.AddComponent<CarController>();
        }
        
        // Configure AI controller
        aiController.waypointSystem = waypointSystem;
        aiController.aiInput = aiInput;
        aiController.carController = aiCarController;
        
        Debug.Log("AI Car spawned and configured");
    }
    
    void ConfigureAI()
    {
        if (aiController == null) return;
        
        // Set AI difficulty
        aiController.SetDifficulty(aiDifficulty);
        
        // Start racing if configured to do so
        if (aiStartsWithPlayer)
        {
            aiController.StartRacing();
        }
    }
    
    public void StartAIRacing()
    {
        if (aiController != null)
        {
            aiController.StartRacing();
        }
    }
    
    public void StopAIRacing()
    {
        if (aiController != null)
        {
            aiController.StopRacing();
        }
    }
    
    public void SetAIDifficulty(float difficulty)
    {
        aiDifficulty = Mathf.Clamp01(difficulty);
        if (aiController != null)
        {
            aiController.SetDifficulty(aiDifficulty);
        }
    }
    
    public GameObject GetAICar()
    {
        return aiCarInstance;
    }
    
    public AIController GetAIController()
    {
        return aiController;
    }
    
    // Method to manually add waypoints (useful for custom tracks)
    public void AddWaypoint(Vector3 position)
    {
        if (waypointSystem == null) return;
        
        GameObject waypoint = new GameObject($"Waypoint_{waypointSystem.waypoints.Count:D2}");
        waypoint.transform.position = position;
        waypoint.transform.parent = waypointSystem.transform;
        waypointSystem.waypoints.Add(waypoint.transform);
    }
    
    // Method to clear all waypoints
    public void ClearWaypoints()
    {
        if (waypointSystem == null) return;
        
        foreach (Transform waypoint in waypointSystem.waypoints)
        {
            if (waypoint != null)
            {
                DestroyImmediate(waypoint.gameObject);
            }
        }
        waypointSystem.waypoints.Clear();
    }
    
    void OnDrawGizmos()
    {
        // Draw AI spawn point
        if (aiSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(aiSpawnPoint.position, 2f);
            Gizmos.DrawWireCube(aiSpawnPoint.position, Vector3.one * 4f);
        }
        
        // Draw AI car if spawned
        if (aiCarInstance != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(aiCarInstance.transform.position, 3f);
        }
    }
}
