using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper script to handle button clicks and UI interactions
/// Attach this to individual buttons or use directly from Unity's Button component
/// </summary>
public class UIMenuController : MonoBehaviour
{
    // These methods can be called from Unity Button onClick events

    public void OnStartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStartButtonPressed();
        }
    }

    public void OnResumeGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }

    public void OnPauseGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
    }

    public void OnRestartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }
}
