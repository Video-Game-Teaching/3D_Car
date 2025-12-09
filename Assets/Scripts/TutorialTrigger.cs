using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Tooltip("1 = Movement (WASD), 2 = Drift (Space), 3 = Nitro (Shift), 4 = End")]
    public int tutorialStepIndex = 1;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Check if it's the player car
        if (other.CompareTag("Player"))
        {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.ShowTutorial(tutorialStepIndex);
                hasTriggered = true;
                
                // Disable this trigger so it doesn't happen again
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("TutorialManager instance not found!");
            }
        }
    }
}
