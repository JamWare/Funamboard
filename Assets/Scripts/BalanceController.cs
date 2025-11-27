using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BalanceController : MonoBehaviour
{
    [Header("Controller References")]
    public Transform leftController;
    public Transform rightController;
    public Transform headTransform; // XR Camera transform
    
    [Header("Balance Settings")]
    public float balanceThreshold = 0.15f; // Maximum allowed deviation from perfect balance
    public float balanceSmoothing = 5f; // How quickly balance score changes
    
    [Header("Orientation Settings")]
    public float maxPointerAngleDeviation = 15f; // Max degrees from horizontal
    public float minControllerDistance = 1.2f; // Minimum 1.2m separation
    public bool requireBothPointers = true; // Whether both pointers need to be horizontal

    // Public properties
    public bool IsActive { get; set; } = false; // Controls whether balance system is running
    public float BalanceScore { get; private set; } = 1f; // 0 = completely unbalanced, 1 = perfect balance
    public float BalanceOffset { get; private set; } = 0f; // -1 = left side down, +1 = right side down
    public float DistanceScore { get; private set; } = 1f; // 0 = too close, 1 = good separation
    public float FinalScore => DistanceScore * BalanceScore; // Combined score for movement
    public bool IsBalanced => BalanceScore > balanceThreshold;
    public bool HasGoodDistance => DistanceScore > 0.8f;
    
    // Events
    public System.Action<float, float> OnBalanceChanged; // balance score, balance offset
    public System.Action<bool> OnBalanceLost; // true = left side, false = right side

    // Private variables
    private float currentBalanceScore = 1f;
    private float currentBalanceOffset = 0f;
    private float currentOrientationScore = 1f;
    private float currentDistanceScore = 1f;
    
    void Start()
    {
        if (!leftController || !rightController || !headTransform)
        {
            Debug.LogError("BalanceController: Missing controller or head references!");
        }
    }
    
    void Update()
    {
        // Only run balance calculations when active (player is on plank)
        if (!IsActive || !leftController || !rightController || !headTransform)
            return;

        CalculateBalance();
    }
    
    void CalculateBalance()
    {
        // 1. Calculate Orientation Score (pointer angles)
        Vector3 leftPointer = leftController.forward;
        Vector3 rightPointer = rightController.forward;
        
        // Check if pointers are horizontal (parallel to XZ plane)
        float leftAngleFromHorizontal = Vector3.Angle(leftPointer, Vector3.ProjectOnPlane(leftPointer, Vector3.up));
        float rightAngleFromHorizontal = Vector3.Angle(rightPointer, Vector3.ProjectOnPlane(rightPointer, Vector3.up));
        
        // Calculate orientation scores (1.0 when within maxPointerAngleDeviation, 0.0 when beyond)
        float leftOrientationScore = 1f - Mathf.Clamp01(leftAngleFromHorizontal / maxPointerAngleDeviation);
        float rightOrientationScore = 1f - Mathf.Clamp01(rightAngleFromHorizontal / maxPointerAngleDeviation);
        
        float targetOrientationScore;
        if (requireBothPointers)
        {
            // Both pointers need to be horizontal
            targetOrientationScore = Mathf.Min(leftOrientationScore, rightOrientationScore);
        }
        else
        {
            // At least one pointer needs to be horizontal
            targetOrientationScore = Mathf.Max(leftOrientationScore, rightOrientationScore);
        }
        
        // 2. Calculate Distance Score (1.2m separation)
        float controllerDistance = Vector3.Distance(leftController.position, rightController.position);
        float targetDistanceScore = Mathf.Clamp01(controllerDistance / minControllerDistance);
        
        // 3. Calculate Balance Score (height difference)
        Vector3 leftPos = leftController.position;
        Vector3 rightPos = rightController.position;
        float heightDifference = rightPos.y - leftPos.y;
        
        // Calculate balance offset (-1 to 1)
        float targetOffset = Mathf.Clamp(heightDifference * 3f, -1f, 1f); // More sensitive than before
        
        // Calculate balance score (1.0 when level, 0.0 when very unbalanced)
        float targetBalanceScore = 1f - Mathf.Abs(heightDifference * 4f); // More sensitive
        targetBalanceScore = Mathf.Clamp01(targetBalanceScore);
        
        // 4. Smooth all values
        currentDistanceScore = Mathf.Lerp(currentDistanceScore, targetDistanceScore, Time.deltaTime * balanceSmoothing);
        currentBalanceScore = Mathf.Lerp(currentBalanceScore, targetBalanceScore, Time.deltaTime * balanceSmoothing);
        currentBalanceOffset = Mathf.Lerp(currentBalanceOffset, targetOffset, Time.deltaTime * balanceSmoothing);
        
        // 5. Set public properties
        DistanceScore = currentDistanceScore;
        BalanceScore = currentBalanceScore;
        BalanceOffset = currentBalanceOffset;
        
        // 6. Calculate final combined score for movement (orientation × distance × balance)
        float finalScore = DistanceScore * BalanceScore;
        
        // Fire events with final score
        OnBalanceChanged?.Invoke(finalScore, BalanceOffset);
        
        // Check for balance loss
        if (finalScore < balanceThreshold && Mathf.Abs(BalanceOffset) > 0.1f)
        {
            OnBalanceLost?.Invoke(BalanceOffset < 0);
        }
    }

    // Public method to apply external balance disruption
    public void ApplyDisruption(float amount, float direction)
    {
        // direction: -1 = push left down, +1 = push right down
        currentBalanceOffset += amount * direction;
        currentBalanceOffset = Mathf.Clamp(currentBalanceOffset, -1f, 1f);
    }
    
    // Debug visualization - commented out to remove visual artifacts
    /*
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
    */
}