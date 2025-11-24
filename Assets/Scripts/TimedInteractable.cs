using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class TimedInteractable : MonoBehaviour
{
    [Header("Timing Settings")]
    [SerializeField] private float quickClickThreshold = 0.3f;

    [Header("Destroy Settings")]
    [SerializeField] private bool useDestroyEffect = true;
    [SerializeField] private float destroyEffectDuration = 0.2f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private float selectStartTime;
    private bool isSelected = false;

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (!grabInteractable)
        {
            Debug.LogError($"TimedInteractable on {gameObject.name} requires XRGrabInteractable component!");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // Subscribe to grab events
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (grabInteractable)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Record when the object was grabbed
        selectStartTime = Time.time;
        isSelected = true;

        Debug.Log($"{gameObject.name} grabbed at {selectStartTime}");
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (!isSelected) return;

        // Calculate how long the object was held
        float holdDuration = Time.time - selectStartTime;
        isSelected = false;

        Debug.Log($"{gameObject.name} released after {holdDuration:F2}s");

        // Check if it was a quick click
        if (holdDuration < quickClickThreshold)
        {
            // Quick click - destroy the object
            HandleQuickClick();
        }
        else
        {
            // Long hold - normal grab behavior (object already released by XRI)
            Debug.Log($"{gameObject.name} was held long enough, normal release");
        }
    }

    void HandleQuickClick()
    {
        Debug.Log($"{gameObject.name} quick-clicked! Destroying...");

        if (useDestroyEffect)
        {
            // Optional: Add visual/audio effect before destroying
            StartCoroutine(DestroyWithEffect());
        }
        else
        {
            // Immediate destruction
            Destroy(gameObject);
        }
    }

    System.Collections.IEnumerator DestroyWithEffect()
    {
        // Optional: Shrink effect
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < destroyEffectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / destroyEffectDuration;

            // Shrink to zero
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);

            yield return null;
        }

        // Destroy after effect
        Destroy(gameObject);
    }
}
