using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    private bool hasTriggered = false; // 防止重复触发

    void OnTriggerEnter(Collider other)
    {
        // Assuming the player car has the tag "Player"
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true; // 标记为已触发
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.CheckpointHit(gameObject);
            }
        }
    }

    // 当汽车离开触发器时，重置标志
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            hasTriggered = false;
        }
    }
}
