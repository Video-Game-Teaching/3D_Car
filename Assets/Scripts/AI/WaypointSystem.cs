using UnityEngine;
using System.Collections.Generic;

public class WaypointSystem : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public List<Transform> waypoints = new List<Transform>();
    public bool loopWaypoints = true;
    public float waypointReachDistance = 5f;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    public Color waypointColor = Color.yellow;
    public Color lineColor = Color.white;

    private int currentWaypointIndex = 0;

    void Start()
    {
        // If no waypoints are assigned, try to find them automatically
        if (waypoints.Count == 0)
        {
            FindWaypointsAutomatically();
        }
    }

    void FindWaypointsAutomatically()
    {
        // Look for objects with "Waypoint" in their name
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        List<Transform> foundWaypoints = new List<Transform>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("waypoint"))
            {
                foundWaypoints.Add(obj.transform);
            }
        }

        // Sort waypoints by name to ensure correct order
        foundWaypoints.Sort((a, b) => a.name.CompareTo(b.name));
        waypoints = foundWaypoints;

        Debug.Log($"Found {waypoints.Count} waypoints automatically");
    }

    public Transform GetCurrentWaypoint()
    {
        if (waypoints.Count == 0) return null;
        return waypoints[currentWaypointIndex];
    }

    public Transform GetNextWaypoint()
    {
        if (waypoints.Count == 0) return null;
        
        int nextIndex = currentWaypointIndex + 1;
        if (nextIndex >= waypoints.Count)
        {
            if (loopWaypoints)
            {
                nextIndex = 0;
            }
            else
            {
                return null; // No more waypoints
            }
        }
        
        return waypoints[nextIndex];
    }

    public bool IsAtCurrentWaypoint(Vector3 position)
    {
        Transform current = GetCurrentWaypoint();
        if (current == null) return false;
        
        float distance = Vector3.Distance(position, current.position);
        return distance <= waypointReachDistance;
    }

    public void AdvanceToNextWaypoint()
    {
        if (waypoints.Count == 0) return;
        
        currentWaypointIndex++;
        if (currentWaypointIndex >= waypoints.Count)
        {
            if (loopWaypoints)
            {
                currentWaypointIndex = 0;
            }
            else
            {
                currentWaypointIndex = waypoints.Count - 1; // Stay at last waypoint
            }
        }
    }

    public float GetDistanceToCurrentWaypoint(Vector3 position)
    {
        Transform current = GetCurrentWaypoint();
        if (current == null) return float.MaxValue;
        
        return Vector3.Distance(position, current.position);
    }

    public Vector3 GetDirectionToCurrentWaypoint(Vector3 position)
    {
        Transform current = GetCurrentWaypoint();
        if (current == null) return Vector3.forward;
        
        Vector3 direction = (current.position - position).normalized;
        return direction;
    }

    public int GetCurrentWaypointIndex()
    {
        return currentWaypointIndex;
    }

    public int GetTotalWaypoints()
    {
        return waypoints.Count;
    }

    public bool HasReachedEnd()
    {
        return !loopWaypoints && currentWaypointIndex >= waypoints.Count - 1;
    }

    // Reset to first waypoint
    public void ResetToStart()
    {
        currentWaypointIndex = 0;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || waypoints.Count == 0) return;

        // Draw waypoints
        Gizmos.color = waypointColor;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawWireSphere(waypoints[i].position, waypointReachDistance);
                
                // Draw waypoint number
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(waypoints[i].position + Vector3.up * 2f, i.ToString());
                #endif
            }
        }

        // Draw lines between waypoints
        Gizmos.color = lineColor;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }

        // Draw line from last to first if looping
        if (loopWaypoints && waypoints.Count > 1)
        {
            if (waypoints[waypoints.Count - 1] != null && waypoints[0] != null)
            {
                Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
            }
        }

        // Highlight current waypoint
        if (GetCurrentWaypoint() != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GetCurrentWaypoint().position, waypointReachDistance * 1.5f);
        }
    }
}
