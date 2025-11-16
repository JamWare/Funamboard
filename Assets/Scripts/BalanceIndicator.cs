using UnityEngine;
using UnityEngine.UI;

public class BalanceIndicator : MonoBehaviour
{
    [Header("Indicator Settings")]
    [SerializeField] private float distanceFromPlayer = 2f;
    [SerializeField] private float verticalOffset = 0f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float maxRotation = 45f;
    [SerializeField] private float rotationSmoothing = 5f;
    
    [Header("Movement Settings")]
    [SerializeField] private float positionSmoothing = 10f;
    
    [Header("Visibility Settings")]
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private bool onlyShowWhenOnPlank = true;
    
    [Header("References")]
    private Transform headTransform;
    private BalanceController balanceController;
    private MovingPlank movingPlank;
    private CanvasGroup canvasGroup;
    private Image indicatorImage;
    
    private float currentRotation = 0f;
    private bool shouldBeVisible = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    
    void Awake()
    {
        indicatorImage = GetComponentInChildren<Image>();
        if (!indicatorImage)
        {
            indicatorImage = GetComponent<Image>();
        }
        
        if (!indicatorImage)
        {
            Debug.LogError("BalanceIndicator requires an Image component on this GameObject or its children!");
            enabled = false;
            return;
        }
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
    }
    
    public void Initialize(Transform head, BalanceController balance, MovingPlank plank)
    {
        headTransform = head;
        balanceController = balance;
        movingPlank = plank;
        
        if (!headTransform)
        {
            Debug.LogError("BalanceIndicator: Head transform is required!");
            enabled = false;
        }
    }
    
    void Update()
    {
        if (!headTransform || !indicatorImage) return;
        
        UpdateVisibility();
        UpdatePosition();
        UpdateRotation();
        UpdateFade();
    }
    
    void UpdateVisibility()
    {
        if (onlyShowWhenOnPlank && movingPlank)
        {
            shouldBeVisible = movingPlank.IsPlayerAttached();
        }
        else
        {
            shouldBeVisible = true;
        }
    }
    
    void UpdatePosition()
    {
        Vector3 forward = headTransform.forward;
        forward.y = 0;
        forward.Normalize();
        
        if (forward.magnitude < 0.01f)
        {
            forward = headTransform.forward;
        }
        
        targetPosition = headTransform.position + forward * distanceFromPlayer;
        targetPosition.y = headTransform.position.y + verticalOffset;
        
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionSmoothing);
        
        targetRotation = Quaternion.LookRotation(transform.position - headTransform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * positionSmoothing);
    }
    
    void UpdateRotation()
    {
        if (!balanceController || !indicatorImage) return;
        
        float balanceOffset = balanceController.BalanceOffset;
        
        float targetImageRotation = balanceOffset * maxRotation;
        
        currentRotation = Mathf.Lerp(currentRotation, targetImageRotation, Time.deltaTime * rotationSmoothing);
        
        indicatorImage.transform.localRotation = Quaternion.Euler(0, 0, -currentRotation);
    }
    
    void UpdateFade()
    {
        if (!canvasGroup) return;
        
        float targetAlpha = shouldBeVisible ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }
}