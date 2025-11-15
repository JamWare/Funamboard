using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class BalanceController : MonoBehaviour
{
    [Header("Controller References")]
    public Transform leftController;
    public Transform rightController;
    public Transform headTransform; // XR Camera transform
    
    [Header("Balance Settings")]
    public float balanceThreshold = 0.15f; // Maximum allowed deviation from perfect balance
    public float balanceSmoothing = 5f; // How quickly balance score changes
    
    [Header("Haptic Feedback")]
    public float minHapticStrength = 0.1f;
    public float maxHapticStrength = 0.8f;
    public float hapticDuration = 0.1f;
    public float hapticInterval = 0.5f; // Time between haptic pulses
    
    // Public properties
    public float BalanceScore { get; private set; } = 1f; // 0 = completely unbalanced, 1 = perfect balance
    public float BalanceOffset { get; private set; } = 0f; // -1 = left side down, +1 = right side down
    public bool IsBalanced => BalanceScore > balanceThreshold;
    
    // Events
    public System.Action<float, float> OnBalanceChanged; // balance score, balance offset
    public System.Action<bool> OnBalanceLost; // true = left side, false = right side
    
    // Private variables
    private HapticImpulsePlayer leftHapticPlayer;
    private HapticImpulsePlayer rightHapticPlayer;
    private float lastHapticTime;
    private float currentBalanceScore = 1f;
    private float currentBalanceOffset = 0f;
    
    void Start()
    {
        // Get HapticImpulsePlayer components for haptic feedback
        if (leftController)
            leftHapticPlayer = leftController.GetComponent<HapticImpulsePlayer>();
        if (rightController)
            rightHapticPlayer = rightController.GetComponent<HapticImpulsePlayer>();
            
        if (!leftController || !rightController || !headTransform)
        {
            Debug.LogError("BalanceController: Missing controller or head references!");
        }
    }
    
    void Update()
    {
        if (!leftController || !rightController || !headTransform)
            return;
            
        CalculateBalance();
        UpdateHapticFeedback();
    }
    
    void CalculateBalance()
    {
        // Calculate controller positions relative to head
        Vector3 leftPos = leftController.position;
        Vector3 rightPos = rightController.position;
        Vector3 headPos = headTransform.position;
        
        // Get the height difference of controllers relative to head height
        float leftHeight = leftPos.y - headPos.y;
        float rightHeight = rightPos.y - headPos.y;
        
        // Calculate horizontal distance from head (for T-pose detection)
        Vector3 leftHorizontal = new Vector3(leftPos.x - headPos.x, 0, leftPos.z - headPos.z);
        Vector3 rightHorizontal = new Vector3(rightPos.x - headPos.x, 0, rightPos.z - headPos.z);
        
        float leftDistance = leftHorizontal.magnitude;
        float rightDistance = rightHorizontal.magnitude;
        
        // Calculate height difference between controllers
        float heightDifference = rightHeight - leftHeight;
        
        // Calculate balance offset (-1 to 1)
        float targetOffset = Mathf.Clamp(heightDifference * 2f, -1f, 1f);
        
        // Smooth the balance offset
        currentBalanceOffset = Mathf.Lerp(currentBalanceOffset, targetOffset, Time.deltaTime * balanceSmoothing);
        BalanceOffset = currentBalanceOffset;
        
        // Calculate balance score based on how level the controllers are
        float targetScore = 1f - Mathf.Abs(heightDifference * 2f);
        targetScore = Mathf.Clamp01(targetScore);
        
        // Also factor in horizontal symmetry (arms should be equally extended)
        float distanceDifference = Mathf.Abs(leftDistance - rightDistance);
        float distanceScore = 1f - Mathf.Clamp01(distanceDifference * 2f);
        targetScore *= distanceScore;
        
        // Smooth the balance score
        currentBalanceScore = Mathf.Lerp(currentBalanceScore, targetScore, Time.deltaTime * balanceSmoothing);
        BalanceScore = currentBalanceScore;
        
        // Fire events
        OnBalanceChanged?.Invoke(BalanceScore, BalanceOffset);
        
        // Check for balance loss
        if (BalanceScore < balanceThreshold && Mathf.Abs(BalanceOffset) > 0.1f)
        {
            OnBalanceLost?.Invoke(BalanceOffset < 0);
        }
    }
    
    void UpdateHapticFeedback()
    {
        if (Time.time - lastHapticTime < hapticInterval)
            return;
            
        if (BalanceScore < balanceThreshold)
        {
            // Calculate haptic strength based on how unbalanced we are
            float imbalance = 1f - BalanceScore;
            float hapticStrength = Mathf.Lerp(minHapticStrength, maxHapticStrength, imbalance);
            
            // Vibrate the controller on the side that's out of balance
            if (BalanceOffset < -0.1f && leftHapticPlayer != null)
            {
                // Left side is lower - vibrate left controller
                leftHapticPlayer.SendHapticImpulse(hapticStrength, hapticDuration);
            }
            else if (BalanceOffset > 0.1f && rightHapticPlayer != null)
            {
                // Right side is lower - vibrate right controller
                rightHapticPlayer.SendHapticImpulse(hapticStrength, hapticDuration);
            }
            
            lastHapticTime = Time.time;
        }
    }
    
    // Public method to apply external balance disruption
    public void ApplyDisruption(float amount, float direction)
    {
        // direction: -1 = push left down, +1 = push right down
        currentBalanceOffset += amount * direction;
        currentBalanceOffset = Mathf.Clamp(currentBalanceOffset, -1f, 1f);
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !leftController || !rightController || !headTransform)
            return;
            
        // Draw controller positions
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(leftController.position, 0.1f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(rightController.position, 0.1f);
        
        // Draw balance line
        Gizmos.color = Color.Lerp(Color.red, Color.green, BalanceScore);
        Gizmos.DrawLine(leftController.position, rightController.position);
        
        // Draw head position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(headTransform.position, 0.15f);
    }
}