using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

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
    
    [Header("Player Attachment")]
    public Transform xrOrigin; // Assign your XR Origin here
    public GameObject locomotionSystem; // Assign the Locomotion game object here
    public float attachmentHeight = 0.1f; // How high above plank to place player
    public float detectionRange = 3f; // Range to detect button press
    
    [Header("XR Input")]
    public InputActionReference triggerAction; // Assign XRI RightHand/Primary Button action
    
    // Private variables
    private float currentPosition = 0f; // 0 to 1 along rope
    private bool playerAttached = false;
    private Vector3 originalXRPosition;
    private Transform playerParent;
    private float ropeLength;
    
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

        // Position plank at start of rope
        UpdatePlankPosition();
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
        
        // Start movement
        StartMovement();
        
        Debug.Log("Player attached to plank and movement started!");
    }
    
    public void DetachPlayerFromPlank()
    {
        if (!xrOrigin || !playerAttached) return;
        
        playerAttached = false;
        isMoving = false;
        
        // Unparent the XR Origin
        xrOrigin.SetParent(playerParent);
        
        // Re-enable player movement
        EnablePlayerMovement();
        
        Debug.Log("Player detached from plank and movement stopped!");
    }
    
    void Update()
    {
        HandleInput();

        if (isMoving)
        {
            MovePlankAlongRope();
        }

        UpdatePlankPosition();
    }
    
    void MovePlankAlongRope()
    {
        if (!startPoint || !endPoint) return;
        
        // Calculate movement delta
        float movementDelta = (baseSpeed * Time.deltaTime) / ropeLength;
        
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
