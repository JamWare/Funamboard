using UnityEngine;

public class VisionShiftController : MonoBehaviour
{
    [Header("Camera References")]
    public Transform xrCamera; // The XR Camera transform
    public Transform cameraOffset; // Optional: Camera Offset parent (if using XR Origin hierarchy)
    
    [Header("Tilt Settings")]
    [Range(0f, 30f)]
    public float maxTiltAngle = 20f; // Maximum tilt angle in degrees
    [Range(1f, 10f)]
    public float tiltSmoothness = 3f; // How smoothly the tilt changes
    [Range(0f, 1f)]
    public float tiltSensitivity = 0.8f; // How sensitive tilt is to balance offset
    
    [Header("Tilt Behavior")]
    public bool enableTilt = true;
    public bool tiltOnlyWhenUnbalanced = true;
    public float minBalanceScoreForTilt = 0.7f; // Only tilt when balance score is below this
    
    [Header("Additional Effects")]
    public bool addSwayMotion = true;
    [Range(0f, 0.1f)]
    public float swayAmplitude = 0.02f;
    [Range(0.5f, 3f)]
    public float swayFrequency = 1.5f;
    
    // References
    private BalanceController balanceController;
    
    // Private variables
    private Quaternion originalRotation;
    private Transform tiltTransform; // The transform we'll actually tilt
    private float currentTiltAngle = 0f;
    private float swayPhase = 0f;
    
    void Start()
    {
        // Find BalanceController
        balanceController = FindFirstObjectByType<BalanceController>();
        if (!balanceController)
        {
            Debug.LogError("VisionShiftController: No BalanceController found in scene!");
            enabled = false;
            return;
        }
        
        // Determine what transform to tilt
        if (cameraOffset != null)
        {
            // If camera offset exists, tilt that instead of the camera directly
            tiltTransform = cameraOffset;
        }
        else if (xrCamera != null)
        {
            // Create a parent transform for the camera to avoid conflicts with tracking
            GameObject tiltObject = new GameObject("VisionTiltOffset");
            tiltObject.transform.SetParent(xrCamera.parent);
            tiltObject.transform.position = xrCamera.position;
            tiltObject.transform.rotation = xrCamera.rotation;
            
            xrCamera.SetParent(tiltObject.transform);
            tiltTransform = tiltObject.transform;
        }
        else
        {
            Debug.LogError("VisionShiftController: No camera reference provided!");
            enabled = false;
            return;
        }
        
        // Store original rotation
        originalRotation = tiltTransform.localRotation;
        
        // Subscribe to balance events
        if (balanceController != null)
        {
            balanceController.OnBalanceChanged += OnBalanceChanged;
        }
    }
    
    void Update()
    {
        if (!enableTilt || tiltTransform == null)
            return;
            
        ApplyTilt();
        
        if (addSwayMotion)
        {
            ApplySway();
        }
    }
    
    void OnBalanceChanged(float balanceScore, float balanceOffset)
    {
        // This is called by the BalanceController when balance changes
        // We'll use this to calculate target tilt
    }
    
    void ApplyTilt()
    {
        float targetTiltAngle = 0f;
        
        if (balanceController != null)
        {
            // Check if we should apply tilt
            bool shouldTilt = !tiltOnlyWhenUnbalanced || 
                             balanceController.BalanceScore < minBalanceScoreForTilt;
            
            if (shouldTilt)
            {
                // Calculate target tilt based on balance offset
                // Negative offset = left side down = tilt right (positive angle)
                // Positive offset = right side down = tilt left (negative angle)
                targetTiltAngle = -balanceController.BalanceOffset * maxTiltAngle * tiltSensitivity;
                
                // Apply additional scaling based on how unbalanced we are
                float imbalance = 1f - balanceController.BalanceScore;
                targetTiltAngle *= imbalance;
            }
        }
        
        // Smoothly interpolate to target tilt
        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetTiltAngle, Time.deltaTime * tiltSmoothness);
        
        // Apply tilt rotation (around forward axis for left/right tilt)
        Quaternion tiltRotation = Quaternion.AngleAxis(currentTiltAngle, Vector3.forward);
        tiltTransform.localRotation = originalRotation * tiltRotation;
    }
    
    void ApplySway()
    {
        if (balanceController == null || balanceController.IsBalanced)
            return;
            
        // Add subtle swaying motion when unbalanced
        swayPhase += Time.deltaTime * swayFrequency;
        
        float swayX = Mathf.Sin(swayPhase) * swayAmplitude;
        float swayY = Mathf.Sin(swayPhase * 0.7f) * swayAmplitude * 0.5f; // Different frequency for Y
        
        // Scale sway by imbalance
        float imbalance = 1f - balanceController.BalanceScore;
        Vector3 swayOffset = new Vector3(swayX, swayY, 0f) * imbalance;
        
        // Apply sway as additional rotation
        Quaternion swayRotation = Quaternion.Euler(swayOffset.y * 10f, 0f, swayOffset.x * 10f);
        tiltTransform.localRotation *= swayRotation;
    }
    
    // Public methods for external control
    public void ResetTilt()
    {
        currentTiltAngle = 0f;
        if (tiltTransform != null)
        {
            tiltTransform.localRotation = originalRotation;
        }
    }
    
    public void SetTiltEnabled(bool enabled)
    {
        enableTilt = enabled;
        if (!enabled)
        {
            ResetTilt();
        }
    }
    
    public float GetCurrentTiltAngle()
    {
        return currentTiltAngle;
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (balanceController != null)
        {
            balanceController.OnBalanceChanged -= OnBalanceChanged;
        }
        
        // Clean up the tilt transform if we created it
        if (tiltTransform != null && tiltTransform.name == "VisionTiltOffset")
        {
            // Re-parent the camera to its original parent before destroying tilt object
            if (xrCamera != null && xrCamera.parent == tiltTransform)
            {
                xrCamera.SetParent(tiltTransform.parent);
            }
            
            Destroy(tiltTransform.gameObject);
        }
    }
    
    // Debug visualization - commented out to remove visual artifacts
    /*
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || tiltTransform == null)
            return;
            
        // Draw current tilt direction
        Gizmos.color = Color.yellow;
        Vector3 tiltDirection = tiltTransform.right * Mathf.Sign(currentTiltAngle);
        Gizmos.DrawRay(tiltTransform.position, tiltDirection * 0.5f);
        
        // Draw tilt angle arc
        Gizmos.color = Color.red;
        float angleStep = 5f;
        Vector3 lastPoint = tiltTransform.position + tiltTransform.forward * 0.5f;
        
        for (float angle = -maxTiltAngle; angle <= maxTiltAngle; angle += angleStep)
        {
            Quaternion rotation = Quaternion.AngleAxis(angle, tiltTransform.forward);
            Vector3 point = tiltTransform.position + rotation * tiltTransform.right * 0.5f;
            Gizmos.DrawLine(lastPoint, point);
            lastPoint = point;
        }
    }
    */
}