using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class TPoseDetector : MonoBehaviour
{
    [Header("Controller References")]
    public Transform leftController;
    public Transform rightController;
    public Transform headTransform; // XR Camera transform
    
    [Header("T-Pose Settings")]
    [Range(0f, 45f)]
    public float maxArmAngleDeviation = 15f; // Max angle deviation from horizontal
    [Range(0.3f, 1f)]
    public float minArmExtension = 0.7f; // Minimum arm extension (0-1, where 1 is fully extended)
    public float armLength = 0.6f; // Expected arm length in meters
    [Range(0f, 0.3f)]
    public float heightTolerance = 0.15f; // How much height difference is allowed between hands
    
    [Header("Detection Settings")]
    public float detectionSmoothing = 3f; // How quickly T-pose confidence changes
    public float holdDuration = 1f; // How long to hold T-pose before it's considered valid
    
    [Header("Feedback Settings")]
    public float feedbackHapticStrength = 0.3f;
    public float feedbackHapticDuration = 0.1f;
    public Color goodPoseColor = Color.green;
    public Color badPoseColor = Color.red;
    
    // Public properties
    public bool IsInTPose { get; private set; }
    public float TPoseConfidence { get; private set; } // 0-1, where 1 is perfect T-pose
    public float HoldProgress { get; private set; } // 0-1, progress towards holdDuration
    
    // Events
    public System.Action OnTPoseEntered;
    public System.Action OnTPoseExited;
    public System.Action<float> OnTPoseConfidenceChanged; // confidence value
    
    // Private variables
    private float currentConfidence = 0f;
    private float tPoseStartTime = -1f;
    private bool wasInTPose = false;
    private HapticImpulsePlayer leftHapticPlayer;
    private HapticImpulsePlayer rightHapticPlayer;
    private float lastFeedbackTime;
    
    void Start()
    {
        // Get HapticImpulsePlayer components for haptic feedback
        if (leftController)
            leftHapticPlayer = leftController.GetComponent<HapticImpulsePlayer>();
        if (rightController)
            rightHapticPlayer = rightController.GetComponent<HapticImpulsePlayer>();
            
        if (!leftController || !rightController || !headTransform)
        {
            Debug.LogError("TPoseDetector: Missing controller or head references!");
        }
    }
    
    void Update()
    {
        if (!leftController || !rightController || !headTransform)
            return;
            
        CalculateTPoseConfidence();
        UpdateTPoseState();
        ProvideFeedback();
    }
    
    void CalculateTPoseConfidence()
    {
        float confidence = 1f;
        
        // Get positions relative to head
        Vector3 headPos = headTransform.position;
        Vector3 leftPos = leftController.position;
        Vector3 rightPos = rightController.position;
        
        // 1. Check arm angles (should be horizontal)
        Vector3 leftArmVector = (leftPos - headPos).normalized;
        Vector3 rightArmVector = (rightPos - headPos).normalized;
        
        // Calculate angle from horizontal plane
        float leftAngle = Vector3.Angle(leftArmVector, Vector3.ProjectOnPlane(leftArmVector, Vector3.up));
        float rightAngle = Vector3.Angle(rightArmVector, Vector3.ProjectOnPlane(rightArmVector, Vector3.up));
        
        float angleScore = 1f - ((leftAngle + rightAngle) / (2f * maxArmAngleDeviation));
        angleScore = Mathf.Clamp01(angleScore);
        confidence *= angleScore;
        
        // 2. Check arm extension (distance from head)
        float leftDistance = Vector3.Distance(leftPos, headPos);
        float rightDistance = Vector3.Distance(rightPos, headPos);
        
        float leftExtension = Mathf.Clamp01(leftDistance / armLength);
        float rightExtension = Mathf.Clamp01(rightDistance / armLength);
        
        float extensionScore = 0f;
        if (leftExtension >= minArmExtension && rightExtension >= minArmExtension)
        {
            extensionScore = (leftExtension + rightExtension) / 2f;
        }
        confidence *= extensionScore;
        
        // 3. Check height alignment (both hands should be at similar height)
        float heightDiff = Mathf.Abs(leftPos.y - rightPos.y);
        float heightScore = 1f - Mathf.Clamp01(heightDiff / heightTolerance);
        confidence *= heightScore;
        
        // 4. Check symmetry (arms should be spread equally)
        Vector3 leftHorizontal = new Vector3(leftPos.x - headPos.x, 0, leftPos.z - headPos.z);
        Vector3 rightHorizontal = new Vector3(rightPos.x - headPos.x, 0, rightPos.z - headPos.z);
        
        // Arms should be roughly 180 degrees apart
        float armAngle = Vector3.Angle(leftHorizontal, rightHorizontal);
        float symmetryScore = armAngle / 180f; // Closer to 180 is better
        confidence *= symmetryScore;
        
        // 5. Check that controllers are roughly at shoulder height
        float expectedHeight = headPos.y - 0.15f; // Shoulders are typically 15cm below head
        float leftHeightDiff = Mathf.Abs(leftPos.y - expectedHeight);
        float rightHeightDiff = Mathf.Abs(rightPos.y - expectedHeight);
        float shoulderHeightScore = 1f - Mathf.Clamp01((leftHeightDiff + rightHeightDiff) / (2f * heightTolerance));
        confidence *= shoulderHeightScore;
        
        // Smooth the confidence value
        currentConfidence = Mathf.Lerp(currentConfidence, confidence, Time.deltaTime * detectionSmoothing);
        TPoseConfidence = currentConfidence;
        
        OnTPoseConfidenceChanged?.Invoke(TPoseConfidence);
    }
    
    void UpdateTPoseState()
    {
        // Consider it a T-pose if confidence is above 0.6 (more lenient)
        bool isCurrentlyInTPose = TPoseConfidence > 0.6f;
        
        if (isCurrentlyInTPose)
        {
            if (tPoseStartTime < 0)
            {
                tPoseStartTime = Time.time;
            }
            
            float holdTime = Time.time - tPoseStartTime;
            HoldProgress = Mathf.Clamp01(holdTime / holdDuration);
            
            if (holdTime >= holdDuration && !IsInTPose)
            {
                IsInTPose = true;
                OnTPoseEntered?.Invoke();
            }
        }
        else
        {
            tPoseStartTime = -1f;
            HoldProgress = 0f;
            
            if (IsInTPose)
            {
                IsInTPose = false;
                OnTPoseExited?.Invoke();
            }
        }
        
        wasInTPose = IsInTPose;
    }
    
    void ProvideFeedback()
    {
        // Provide haptic feedback when approaching correct pose
        if (TPoseConfidence > 0.4f && TPoseConfidence < 0.6f && !IsInTPose)
        {
            if (Time.time - lastFeedbackTime > 0.5f)
            {
                float hapticStrength = feedbackHapticStrength * TPoseConfidence;
                
                if (leftHapticPlayer)
                    leftHapticPlayer.SendHapticImpulse(hapticStrength, feedbackHapticDuration);
                if (rightHapticPlayer)
                    rightHapticPlayer.SendHapticImpulse(hapticStrength, feedbackHapticDuration);
                    
                lastFeedbackTime = Time.time;
            }
        }
    }
    
    // Helper method to get pose quality for UI
    public string GetPoseQualityFeedback()
    {
        if (IsInTPose)
            return "Perfect T-Pose!";
        else if (TPoseConfidence > 0.6f)
            return "Hold position...";
        else if (TPoseConfidence > 0.4f)
            return "Almost there!";
        else if (TPoseConfidence > 0.3f)
            return "Extend arms more";
        else if (TPoseConfidence > 0.15f)
            return "Raise arms to shoulder height";
        else
            return "Form T-Pose to begin";
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !leftController || !rightController || !headTransform)
            return;
            
        // Draw expected T-pose position
        Vector3 headPos = headTransform.position;
        Vector3 shoulderHeight = headPos + Vector3.down * 0.15f;
        
        Gizmos.color = Color.Lerp(badPoseColor, goodPoseColor, TPoseConfidence);
        
        // Draw expected arm positions
        Vector3 leftExpected = shoulderHeight + Vector3.left * armLength;
        Vector3 rightExpected = shoulderHeight + Vector3.right * armLength;
        
        Gizmos.DrawWireSphere(leftExpected, 0.1f);
        Gizmos.DrawWireSphere(rightExpected, 0.1f);
        Gizmos.DrawLine(leftExpected, rightExpected);
        
        // Draw actual controller positions
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(leftController.position, 0.05f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(rightController.position, 0.05f);
        
        // Draw connection lines
        Gizmos.color = Color.white * 0.5f;
        Gizmos.DrawLine(leftController.position, leftExpected);
        Gizmos.DrawLine(rightController.position, rightExpected);
    }
}