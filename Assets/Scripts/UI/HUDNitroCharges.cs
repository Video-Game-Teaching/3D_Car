using UnityEngine;
using UnityEngine.UI;

public class HUDNitroCharges : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CarController carController;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Nitro Bar Segments")]
    [SerializeField] private Image[] nitroBarSegments = new Image[3];  // Three segments for the nitro bar
    [SerializeField] private Color activeColor = new Color(0f, 0.5f, 1f);  // Blue color for active
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);  // Gray color for empty

    [Header("Display Options")]
    [SerializeField] private bool autoHideWhenZero = false;

    private int lastChargesDisplayed = -1;

    void Reset()
    {
        if (!carController) carController = FindAnyObjectByType<CarController>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        // Auto-find CarController if not assigned
        if (!carController)
        {
            carController = FindAnyObjectByType<CarController>();
            if (!carController)
            {
                Debug.LogWarning("HUDNitroCharges: CarController not found! Please assign it in the Inspector.");
            }
        }

        // Validate nitro bar segments
        if (nitroBarSegments == null || nitroBarSegments.Length != 3)
        {
            Debug.LogWarning("HUDNitroCharges: Nitro bar segments array must have exactly 3 Image components assigned!");
        }

        // Initialize display
        UpdateDisplay(0);
    }

    void Update()
    {
        if (!carController) return;

        int currentCharges = carController.nitroChargesStored;

        // Only update if charges changed
        if (currentCharges != lastChargesDisplayed)
        {
            UpdateDisplay(currentCharges);
            lastChargesDisplayed = currentCharges;
        }
    }

    private void UpdateDisplay(int charges)
    {
        // Update nitro bar segments (3 segments)
        if (nitroBarSegments != null && nitroBarSegments.Length == 3)
        {
            for (int i = 0; i < 3; i++)
            {
                if (nitroBarSegments[i] != null)
                {
                    // Segment is blue if we have at least i+1 charges, gray otherwise
                    if (i < charges)
                    {
                        nitroBarSegments[i].color = activeColor;  // Blue for active
                    }
                    else
                    {
                        nitroBarSegments[i].color = inactiveColor;  // Gray for empty
                    }
                }
            }
        }

        // Handle canvas group visibility
        if (canvasGroup && autoHideWhenZero)
        {
            float targetAlpha = (charges == 0) ? 0f : 1f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, 10f * Time.unscaledDeltaTime);
        }
    }

    // Public method to set car controller reference
    public void SetCarController(CarController controller)
    {
        carController = controller;
    }
}

