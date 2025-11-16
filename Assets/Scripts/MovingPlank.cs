using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class MovingPlank : MonoBehaviour
{
    [Header("Rope Path Settings")]
    public Transform startPoint;
    public Transform endPoint;
    public AnimationCurve ropeSagCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);
    public float maxSag = 2f;
    
    [Header("Movement Settings")]
    public float baseSpeed = 2f;
    public bool isMoving = false;
    public bool movingForward = true; // Direction of travel
    
    [Header("Balance-Based Movement")]
    public bool useBalanceSystem = true;
    [Range(0f, 1f)]
    public float minBalanceForMovement = 0.3f; // Minimum balance score needed to move
    [Range(0f, 1f)]
    public float balanceSpeedMultiplier = 1f; // How much balance affects speed (0=no effect, 1=full effect)
    [Range(0f, 1f)]
    public float minSpeedWhenUnbalanced = 0.2f; // Minimum speed multiplier when unbalanced (instead of stopping)
    public AnimationCurve balanceSpeedCurve = AnimationCurve.Linear(0, 0, 1, 1); // Maps balance score to speed multiplier
    
    [Header("Player Attachment")]
    public Transform xrOrigin; // Assign your XR Origin here
    public GameObject locomotionSystem; // Assign the Locomotion game object here
    public Transform leftController; // Left hand controller
    public Transform rightController; // Right hand controller
    public Transform headTransform; // XR Camera
    public float attachmentHeight = 0.1f; // How high above plank to place player
    public float detectionRange = 3f; // Range to detect button press
    
    [Header("XR Input")]
    public InputActionReference triggerAction; // Assign XRI RightHand/Primary Button action
    
    [Header("UI Feedback")]
    public TextMeshProUGUI balanceText; // Optional UI text for balance feedback
    public TextMeshProUGUI tPoseText; // Optional UI text for T-pose feedback
    
    [Header("Progressive Difficulty")]
    public bool difficultyProgressionEnabled = true;
    [Range(1f, 10f)]
    public float difficultyMultiplier = 1f; // Current difficulty level
    [Range(0.01f, 0.5f)]
    public float difficultyIncreaseRate = 0.1f; // How much difficulty increases per interval
    [Range(5f, 30f)]
    public float difficultyIncreaseInterval = 10f; // How often difficulty increases (seconds)
    [Range(1f, 10f)]
    public float maxDifficultyMultiplier = 3f; // Maximum difficulty cap
    
    [Header("Disruption Control")]
    [Range(0f, 10f)]
    public float gracePeriodAfterBalance = 3f; // Delay after achieving balance before disruptions
    [Range(0f, 1f)]
    public float baseDisruptionChance = 0.5f; // Base probability of disruption occurring
    [Range(0f, 2f)]
    public float disruptionChanceMultiplier = 1.5f; // How much difficulty affects disruption chance
    
    // Balance components
    private BalanceController balanceController;
    private TPoseDetector tPoseDetector;
    private BalanceDisruptor balanceDisruptor;
    private VisionShiftController visionShiftController;
    
    // Private variables
    private float currentPosition = 0f; // 0 to 1 along rope
    private bool playerAttached = false;
    private Vector3 originalXRPosition;
    private Transform playerParent;
    private float ropeLength;
    private float currentSpeed = 0f;
    
    // Difficulty progression tracking
    private float lastDifficultyIncreaseTime;
    private float journeyStartTime;
    private float lastBalancedTime;
    private bool wasBalanced = false;
    
    void Start()
    {
        // Calculate rope length for movement calculations
        if (startPoint && endPoint)
        {
            ropeLength = Vector3.Distance(startPoint.position, endPoint.position);
        }

        // Store original player setup
        if (xrOrigin)
        {
            originalXRPosition = xrOrigin.position;
            playerParent = xrOrigin.parent;
        }

        // Setup balance system if enabled
        if (useBalanceSystem)
        {
            SetupBalanceSystem();
        }

        // Position plank at start of rope
        UpdatePlankPosition();
    }
    
    void SetupBalanceSystem()
    {
        // Check required references
        if (!leftController || !rightController || !headTransform)
        {
            Debug.LogError("MovingPlank: Missing controller or head references! Please assign:");
            if (!leftController) Debug.LogError("- Left Controller is missing");
            if (!rightController) Debug.LogError("- Right Controller is missing");
            if (!headTransform) Debug.LogError("- Head Transform (Camera) is missing");
            useBalanceSystem = false;
            return;
        }
        
        Debug.Log($"Setting up balance system with controllers: Left={leftController.name}, Right={rightController.name}, Head={headTransform.name}");
        
        // Create balance controller (main system)
        balanceController = gameObject.AddComponent<BalanceController>();
        balanceController.leftController = leftController;
        balanceController.rightController = rightController;
        balanceController.headTransform = headTransform;
        
        // Create balance disruptor
        balanceDisruptor = gameObject.AddComponent<BalanceDisruptor>();
        
        // Create vision shift controller
        visionShiftController = gameObject.AddComponent<VisionShiftController>();
        visionShiftController.xrCamera = headTransform;
        
        // Subscribe to balance events
        if (balanceController)
        {
            balanceController.OnBalanceChanged += OnBalanceChanged;
        }
        
        Debug.Log("Simplified balance system initialized successfully!");
    }
    
    void HandleInput()
    {
        // Get right trigger button press using Input System
        bool inputDetected = false;

        if (triggerAction != null && triggerAction.action != null)
        {
            inputDetected = triggerAction.action.WasPressedThisFrame();
        }

        // Handle attachment/detachment with same button
        if (inputDetected && !playerAttached && IsPlayerNearPlank())
        {
            // Button pressed when not on board: get on the board
            AttachPlayerToPlank();
        }
        else if (inputDetected && playerAttached)
        {
            // Button pressed when on board: get off the board
            DetachPlayerFromPlank();
        }
    }
    
    bool IsPlayerNearPlank()
    {
        if (!xrOrigin) return false;
        
        float distance = Vector3.Distance(xrOrigin.position, transform.position);
        return distance <= detectionRange;
    }
    
    public void AttachPlayerToPlank()
    {
        if (!xrOrigin || playerAttached) return;
        
        playerAttached = true;
        
        // Parent the XR Origin to the plank
        xrOrigin.SetParent(transform);
        
        // Position player on top of plank
        Vector3 plankTop = transform.position + Vector3.up * attachmentHeight;
        xrOrigin.position = plankTop;
        
        // Disable player movement while on plank
        DisablePlayerMovement();
        
        // Reset difficulty progression
        if (difficultyProgressionEnabled)
        {
            difficultyMultiplier = 1f;
            lastDifficultyIncreaseTime = Time.time;
            journeyStartTime = Time.time;
            lastBalancedTime = Time.time;
            UpdateDisruptorSettings();
        }
        
        // Start movement (will be controlled by balance if system is enabled)
        if (!useBalanceSystem)
        {
            StartMovement();
            Debug.Log("Player attached to plank and movement started!");
        }
        else
        {
            // Activate balance system when player is on plank
            if (balanceController)
            {
                balanceController.IsActive = true;
            }
            Debug.Log("Player attached to plank - Meet balance requirements to move!");
        }
    }
    
    public void DetachPlayerFromPlank()
    {
        if (!xrOrigin || !playerAttached) return;
        
        playerAttached = false;
        isMoving = false;
        
        // Deactivate balance system when player leaves plank
        if (balanceController)
        {
            balanceController.IsActive = false;
        }
        
        // Unparent the XR Origin
        xrOrigin.SetParent(playerParent);
        
        // Re-enable player movement
        EnablePlayerMovement();
        
        Debug.Log("Player detached from plank and movement stopped!");
    }
    
    void Update()
    {
        HandleInput();

        if (playerAttached && useBalanceSystem)
        {
            UpdateBalanceBasedMovement();
            UpdateUIFeedback();
            
            if (difficultyProgressionEnabled)
            {
                UpdateDifficultyProgression();
            }
        }
        else if (isMoving)
        {
            MovePlankAlongRope();
        }

        UpdatePlankPosition();
    }
    
    void UpdateBalanceBasedMovement()
    {
        if (!balanceController) return;
        
        // Use the combined final score (orientation × distance × balance)
        float finalScore = balanceController.FinalScore;
        
        // Always allow movement, but adjust speed based on balance
        isMoving = true;
        
        // Check if player meets all requirements (50% threshold)
        if (finalScore > 0.5f)
        {
            // Calculate speed based on final combined score
            float speedMultiplier = balanceSpeedCurve.Evaluate(finalScore);
            currentSpeed = baseSpeed * speedMultiplier * balanceSpeedMultiplier;
            
            // Ensure at least 10% speed when balanced
            currentSpeed = Mathf.Max(currentSpeed, baseSpeed * 0.1f);
        }
        else
        {
            // Apply minimum speed when unbalanced (instead of stopping)
            currentSpeed = baseSpeed * minSpeedWhenUnbalanced;
        }
        
        // Actually move the plank
        MovePlankAlongRope();
    }
    
    void UpdateUIFeedback()
    {
        if (balanceText != null && balanceController != null)
        {
            // Show detailed breakdown of all scores
            string balanceInfo = $"Orientation: {(balanceController.OrientationScore * 100f):F0}%\n" +
                               $"Distance: {(balanceController.DistanceScore * 100f):F0}%\n" +
                               $"Balance: {(balanceController.BalanceScore * 100f):F0}%\n" +
                               $"Side: {(balanceController.BalanceOffset < -0.1f ? "Left ↓" : balanceController.BalanceOffset > 0.1f ? "Right ↓" : "Centered")}";
            
            if (difficultyProgressionEnabled)
            {
                balanceInfo += $"\nDifficulty: {difficultyMultiplier:F1}x";
                if (IsInGracePeriod())
                {
                    float remaining = gracePeriodAfterBalance - GetTimeSinceBalanced();
                    balanceInfo += $"\nGrace Period: {remaining:F1}s";
                }
            }
            
            balanceText.text = balanceInfo;
        }
        
        if (tPoseText != null && balanceController != null)
        {
            // Priority-based instructions for new system
            if (!balanceController.HasGoodOrientation)
            {
                tPoseText.text = "Point controllers forward and horizontal!";
            }
            else if (!balanceController.HasGoodDistance)
            {
                tPoseText.text = "Spread arms wider (1.2m apart)!";
            }
            else if (!balanceController.IsBalanced)
            {
                if (balanceController.BalanceOffset < -0.1f)
                    tPoseText.text = "Raise your LEFT arm!";
                else if (balanceController.BalanceOffset > 0.1f)
                    tPoseText.text = "Raise your RIGHT arm!";
                else
                    tPoseText.text = "Keep controllers level!";
            }
            else
            {
                // All requirements met
                float finalScore = balanceController.OrientationScore * balanceController.DistanceScore * balanceController.BalanceScore;
                if (finalScore > 0.5f)
                {
                    if (isMoving)
                        tPoseText.text = "Moving! Maintain horizontal pointers!";
                    else
                        tPoseText.text = "Perfect pose - Ready to move!";
                }
                else
                {
                    tPoseText.text = "Good pose! Keep position steady...";
                }
            }
        }
    }
    
    void MovePlankAlongRope()
    {
        if (!startPoint || !endPoint) return;
        
        // Calculate movement delta
        float speed = useBalanceSystem ? currentSpeed : baseSpeed;
        float movementDelta = (speed * Time.deltaTime) / ropeLength;
        
        // Apply movement based on direction
        if (movingForward)
        {
            currentPosition += movementDelta;
            
            // Check if reached end point
            if (currentPosition >= 1f)
            {
                currentPosition = 1f;
                ReachedEndpoint();
            }
        }
        else
        {
            currentPosition -= movementDelta;
            
            // Check if reached start point
            if (currentPosition <= 0f)
            {
                currentPosition = 0f;
                ReachedEndpoint();
            }
        }
    }
    
    void ReachedEndpoint()
    {
        // Stop movement and detach player
        isMoving = false;

        // Automatically detach player when reaching endpoint
        DetachPlayerFromPlank();

        // Reverse direction for next time
        movingForward = !movingForward;

        Debug.Log($"Reached endpoint! Player automatically detached. Next travel will go {(movingForward ? "forward" : "backward")}");
    }
    
    void UpdatePlankPosition()
    {
        if (!startPoint || !endPoint) return;
        
        // Calculate position along rope with sag
        Vector3 basePosition = Vector3.Lerp(startPoint.position, endPoint.position, currentPosition);
        float sag = ropeSagCurve.Evaluate(currentPosition) * maxSag;
        Vector3 finalPosition = basePosition + Vector3.down * sag;
        
        // Update plank position
        transform.position = finalPosition;
        
        // Orient plank to face along rope direction
        Vector3 direction = GetRopeDirection(currentPosition);
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
    
    Vector3 GetRopeDirection(float t)
    {
        // Calculate tangent direction at current position
        float delta = 0.01f;
        float t1 = Mathf.Clamp01(t - delta);
        float t2 = Mathf.Clamp01(t + delta);
        
        Vector3 pos1 = Vector3.Lerp(startPoint.position, endPoint.position, t1);
        Vector3 pos2 = Vector3.Lerp(startPoint.position, endPoint.position, t2);
        
        // Add sag to both positions
        pos1 += Vector3.down * (ropeSagCurve.Evaluate(t1) * maxSag);
        pos2 += Vector3.down * (ropeSagCurve.Evaluate(t2) * maxSag);
        
        return (pos2 - pos1).normalized;
    }
    
    // Public methods for external control
    public void StartMovement()
    {
        if (playerAttached)
        {
            isMoving = true;
            Debug.Log($"Started moving {(movingForward ? "forward" : "backward")}");
        }
    }

    public void StopMovement()
    {
        isMoving = false;
        Debug.Log("Movement stopped");
    }
    
    public void SetSpeed(float newSpeed)
    {
        baseSpeed = newSpeed;
    }
    
    public bool IsPlayerAttached()
    {
        return playerAttached;
    }
    
    public bool IsMoving()
    {
        return isMoving;
    }
    
    // Difficulty progression methods
    void UpdateDifficultyProgression()
    {
        if (!balanceController) return;
        
        // Track balance state changes
        bool currentlyBalanced = balanceController.FinalScore > 0.5f;
        if (currentlyBalanced && !wasBalanced)
        {
            // Just achieved balance
            lastBalancedTime = Time.time;
        }
        wasBalanced = currentlyBalanced;
        
        // Update difficulty over time
        if (Time.time - lastDifficultyIncreaseTime >= difficultyIncreaseInterval)
        {
            lastDifficultyIncreaseTime = Time.time;
            difficultyMultiplier = Mathf.Min(difficultyMultiplier + difficultyIncreaseRate, maxDifficultyMultiplier);
            UpdateDisruptorSettings();
            
            Debug.Log($"Difficulty increased to {difficultyMultiplier:F2}x");
        }
    }
    
    void UpdateDisruptorSettings()
    {
        if (!balanceDisruptor) return;
        
        // Apply difficulty multiplier to disruption settings
        // Stronger disruptions
        balanceDisruptor.gustMinStrength = 0.2f * difficultyMultiplier;
        balanceDisruptor.gustMaxStrength = 0.6f * difficultyMultiplier;
        balanceDisruptor.driftMinStrength = 0.1f * difficultyMultiplier;
        balanceDisruptor.driftMaxStrength = 0.3f * difficultyMultiplier;
        balanceDisruptor.oscillationAmplitude = 0.15f * difficultyMultiplier;
        
        // More frequent disruptions (shorter delays)
        float frequencyMultiplier = 1f / difficultyMultiplier;
        balanceDisruptor.minTimeBetweenDisruptions = 5f * frequencyMultiplier;
        balanceDisruptor.maxTimeBetweenDisruptions = 15f * frequencyMultiplier;
        
        // Update grace period and chance in disruptor
        UpdateDisruptorGracePeriod();
    }
    
    void UpdateDisruptorGracePeriod()
    {
        if (!balanceDisruptor) return;
        
        // Tell disruptor about grace period status
        float timeSinceBalanced = Time.time - lastBalancedTime;
        bool inGracePeriod = wasBalanced && (timeSinceBalanced < gracePeriodAfterBalance);
        
        // Calculate current disruption chance
        float currentChance = baseDisruptionChance * (1f + (difficultyMultiplier - 1f) * disruptionChanceMultiplier);
        currentChance = Mathf.Clamp01(currentChance);
        
        // Update disruption chance in disruptor
        balanceDisruptor.SetDisruptionChance(currentChance);
        
        // Grace period is now handled in BalanceDisruptor's Update method
        // by checking movingPlank.IsInGracePeriod()
    }
    
    public float GetTimeSinceBalanced()
    {
        return Time.time - lastBalancedTime;
    }
    
    public bool IsInGracePeriod()
    {
        return wasBalanced && (GetTimeSinceBalanced() < gracePeriodAfterBalance);
    }
    
    // Movement control methods
    void DisablePlayerMovement()
    {
        // Disable the entire locomotion system to prevent all player movement
        if (locomotionSystem != null)
        {
            locomotionSystem.SetActive(false);
        }
    }

    void EnablePlayerMovement()
    {
        // Re-enable the locomotion system
        if (locomotionSystem != null)
        {
            locomotionSystem.SetActive(true);
        }
    }
    
    // Balance event handlers
    void OnBalanceChanged(float balanceScore, float balanceOffset)
    {
        // This is called whenever balance changes
        // The movement speed is already being updated in UpdateBalanceBasedMovement
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (balanceController)
        {
            balanceController.OnBalanceChanged -= OnBalanceChanged;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw rope path in editor
        if (startPoint && endPoint)
        {
            Gizmos.color = Color.yellow;
            
            // Draw rope segments with sag
            int segments = 20;
            Vector3 lastPos = startPoint.position;
            
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 basePos = Vector3.Lerp(startPoint.position, endPoint.position, t);
                float sag = ropeSagCurve.Evaluate(t) * maxSag;
                Vector3 currentPos = basePos + Vector3.down * sag;
                
                Gizmos.DrawLine(lastPos, currentPos);
                lastPos = currentPos;
            }
        }
        
        // Draw detection range
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
