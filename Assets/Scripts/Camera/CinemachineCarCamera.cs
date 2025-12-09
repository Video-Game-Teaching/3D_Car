using UnityEngine;
using Cinemachine;

public class CinemachineCarCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform carTarget; // Drag your car here
    
    [Header("Camera Settings")]
    public Vector3 followOffset = new Vector3(0, 5, -10);
    public Vector3 lookAtOffset = new Vector3(0, 1.5f, 0);
    public float damping = 1f;
    
    [Header("Cinemachine Components")]
    public CinemachineVirtualCamera virtualCamera;
    
    private Transform lookAtTarget;
    
    void Start()
    {
        // Find car automatically if not assigned
        if (carTarget == null)
        {
            CarController carController = FindObjectOfType<CarController>();
            if (carController != null)
            {
                carTarget = carController.transform;
            }
        }
        
        // Get or create virtual camera
        if (virtualCamera == null)
        {
            virtualCamera = GetComponent<CinemachineVirtualCamera>();
            if (virtualCamera == null)
            {
                virtualCamera = gameObject.AddComponent<CinemachineVirtualCamera>();
            }
        }
        
        // Set up the camera
        SetupCamera();
    }
    
    void SetupCamera()
    {
        if (carTarget == null || virtualCamera == null) return;
        
        // Create a look-at target
        if (lookAtTarget == null)
        {
            GameObject lookAtObj = new GameObject("CameraLookAtTarget");
            lookAtObj.transform.SetParent(carTarget);
            lookAtObj.transform.localPosition = lookAtOffset;
            lookAtTarget = lookAtObj.transform;
        }
        
        // Set up the virtual camera
        virtualCamera.Follow = carTarget;
        virtualCamera.LookAt = lookAtTarget;
        
        // Configure the transposer (camera position)
        var transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer == null)
        {
            transposer = virtualCamera.AddCinemachineComponent<CinemachineTransposer>();
        }
        
        transposer.m_FollowOffset = followOffset;
        transposer.m_XDamping = damping;
        transposer.m_YDamping = damping;
        transposer.m_ZDamping = damping;
        
        // Configure the composer (camera aim)
        var composer = virtualCamera.GetCinemachineComponent<CinemachineComposer>();
        if (composer == null)
        {
            composer = virtualCamera.AddCinemachineComponent<CinemachineComposer>();
        }
        
        composer.m_TrackedObjectOffset = lookAtOffset;
        composer.m_HorizontalDamping = damping;
        composer.m_VerticalDamping = damping;
        composer.m_ScreenX = 0.5f; // Center horizontally
        composer.m_ScreenY = 0.4f; // Slightly above center
        
        // Set priority
        virtualCamera.Priority = 10;
    }
    
    // Public methods for runtime adjustments
    public void SetCarTarget(Transform target)
    {
        carTarget = target;
        if (virtualCamera != null)
        {
            virtualCamera.Follow = target;
            if (lookAtTarget != null)
            {
                lookAtTarget.SetParent(target);
                lookAtTarget.localPosition = lookAtOffset;
            }
        }
    }
    
    public void SetFollowOffset(Vector3 offset)
    {
        followOffset = offset;
        if (virtualCamera != null)
        {
            var transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_FollowOffset = followOffset;
            }
        }
    }
    
    public void SetLookAtOffset(Vector3 offset)
    {
        lookAtOffset = offset;
        if (lookAtTarget != null)
        {
            lookAtTarget.localPosition = lookAtOffset;
        }
        if (virtualCamera != null)
        {
            var composer = virtualCamera.GetCinemachineComponent<CinemachineComposer>();
            if (composer != null)
            {
                composer.m_TrackedObjectOffset = lookAtOffset;
            }
        }
    }
    
    public void SetDamping(float dampingValue)
    {
        damping = dampingValue;
        if (virtualCamera != null)
        {
            var transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_XDamping = damping;
                transposer.m_YDamping = damping;
                transposer.m_ZDamping = damping;
            }
            
            var composer = virtualCamera.GetCinemachineComponent<CinemachineComposer>();
            if (composer != null)
            {
                composer.m_HorizontalDamping = damping;
                composer.m_VerticalDamping = damping;
            }
        }
    }
}
