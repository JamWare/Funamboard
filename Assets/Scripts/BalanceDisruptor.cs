using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class BalanceDisruptor : MonoBehaviour
{
    [Header("Disruption Settings")]
    public bool enableDisruptions = true;
    [Range(1f, 30f)]
    public float minTimeBetweenDisruptions = 5f;
    [Range(2f, 60f)]
    public float maxTimeBetweenDisruptions = 15f;
    
    [Header("Disruption Types")]
    public bool enableSuddenGusts = true;
    public bool enableGradualDrift = true;
    public bool enableOscillations = true;
    
    [Header("Sudden Gust Settings")]
    [Range(0.1f, 0.5f)]
    public float gustMinStrength = 0.2f;
    [Range(0.3f, 1f)]
    public float gustMaxStrength = 0.6f;
    [Range(0.1f, 1f)]
    public float gustDuration = 0.3f;
    
    [Header("Gradual Drift Settings")]
    [Range(0.05f, 0.3f)]
    public float driftMinStrength = 0.1f;
    [Range(0.1f, 0.5f)]
    public float driftMaxStrength = 0.3f;
    [Range(1f, 5f)]
    public float driftDuration = 2f;
    
    [Header("Oscillation Settings")]
    [Range(0.05f, 0.3f)]
    public float oscillationAmplitude = 0.15f;
    [Range(0.5f, 3f)]
    public float oscillationFrequency = 1f;
    [Range(2f, 10f)]
    public float oscillationDuration = 4f;
    
    [Header("Warning Settings")]
    public bool provideWarning = true;
    public float warningTime = 0.5f; // Time before disruption
    public float warningHapticStrength = 0.2f;
    public float warningHapticDuration = 0.2f;
    
    [Header("Probability Settings")]
    [Range(0f, 1f)]
    public float disruptionChance = 1f; // Probability that disruption will occur
    private bool useChanceBasedTrigger = false; // Will be set by MovingPlank
    
    // References
    private BalanceController balanceController;
    private HapticImpulsePlayer leftHapticPlayer;
    private HapticImpulsePlayer rightHapticPlayer;
    
    // Events
    public System.Action<DisruptionType> OnDisruptionStarted;
    public System.Action OnDisruptionEnded;
    public System.Action OnDisruptionWarning;
    
    // Private variables
    private Coroutine disruptionCoroutine;
    private bool isDisrupting = false;
    private float nextDisruptionTime;
    
    // Grace period tracking
    private MovingPlank movingPlank;
    private float baseGustMinStrength;
    private float baseGustMaxStrength;
    private float baseDriftMinStrength;
    private float baseDriftMaxStrength;
    private float baseOscillationAmplitude;
    
    public enum DisruptionType
    {
        SuddenGust,
        GradualDrift,
        Oscillation
    }
    
    void Start()
    {
        balanceController = GetComponent<BalanceController>();
        if (!balanceController)
        {
            Debug.LogError("BalanceDisruptor requires BalanceController component!");
            enabled = false;
            return;
        }
        
        // Get MovingPlank reference
        movingPlank = GetComponent<MovingPlank>();
        
        // Store base values for difficulty scaling
        baseGustMinStrength = gustMinStrength;
        baseGustMaxStrength = gustMaxStrength;
        baseDriftMinStrength = driftMinStrength;
        baseDriftMaxStrength = driftMaxStrength;
        baseOscillationAmplitude = oscillationAmplitude;
        
        // Get controller references for haptic feedback
        if (balanceController.leftController)
            leftHapticPlayer = balanceController.leftController.GetComponent<HapticImpulsePlayer>();
        if (balanceController.rightController)
            rightHapticPlayer = balanceController.rightController.GetComponent<HapticImpulsePlayer>();
            
        ScheduleNextDisruption();
    }
    
    void Update()
    {
        if (!enableDisruptions || isDisrupting)
            return;
            
        // Check if in grace period
        if (movingPlank && movingPlank.IsInGracePeriod())
            return;
            
        // Check if it's time for next disruption
        if (Time.time >= nextDisruptionTime)
        {
            // Roll dice for chance-based triggering
            bool shouldTrigger = true;
            if (movingPlank && movingPlank.difficultyProgressionEnabled)
            {
                shouldTrigger = Random.value <= disruptionChance;
            }
            
            if (shouldTrigger)
            {
                StartRandomDisruption();
            }
            else
            {
                // Failed roll, schedule next attempt
                ScheduleNextDisruption();
            }
        }
    }
    
    void ScheduleNextDisruption()
    {
        float delay = Random.Range(minTimeBetweenDisruptions, maxTimeBetweenDisruptions);
        nextDisruptionTime = Time.time + delay;
    }
    
    void StartRandomDisruption()
    {
        if (!balanceController.IsBalanced)
            return; // Don't disrupt if already unbalanced
            
        // Build list of enabled disruption types
        System.Collections.Generic.List<DisruptionType> availableTypes = new System.Collections.Generic.List<DisruptionType>();
        
        if (enableSuddenGusts) availableTypes.Add(DisruptionType.SuddenGust);
        if (enableGradualDrift) availableTypes.Add(DisruptionType.GradualDrift);
        if (enableOscillations) availableTypes.Add(DisruptionType.Oscillation);
        
        if (availableTypes.Count == 0)
            return;
            
        // Choose random disruption type
        DisruptionType chosenType = availableTypes[Random.Range(0, availableTypes.Count)];
        
        // Start the disruption
        if (disruptionCoroutine != null)
            StopCoroutine(disruptionCoroutine);
            
        switch (chosenType)
        {
            case DisruptionType.SuddenGust:
                disruptionCoroutine = StartCoroutine(SuddenGustCoroutine());
                break;
            case DisruptionType.GradualDrift:
                disruptionCoroutine = StartCoroutine(GradualDriftCoroutine());
                break;
            case DisruptionType.Oscillation:
                disruptionCoroutine = StartCoroutine(OscillationCoroutine());
                break;
        }
    }
    
    IEnumerator SuddenGustCoroutine()
    {
        isDisrupting = true;
        
        // Warning phase
        if (provideWarning)
        {
            OnDisruptionWarning?.Invoke();
            ProvideWarningFeedback();
            yield return new WaitForSeconds(warningTime);
        }
        
        OnDisruptionStarted?.Invoke(DisruptionType.SuddenGust);
        
        // Choose random direction and strength
        float direction = Random.Range(0, 2) == 0 ? -1f : 1f; // Left or right
        float strength = Random.Range(gustMinStrength, gustMaxStrength);
        
        // Apply sudden disruption
        float startTime = Time.time;
        while (Time.time - startTime < gustDuration)
        {
            float t = (Time.time - startTime) / gustDuration;
            float currentStrength = strength * (1f - t); // Fade out
            balanceController.ApplyDisruption(currentStrength * Time.deltaTime, direction);
            yield return null;
        }
        
        isDisrupting = false;
        OnDisruptionEnded?.Invoke();
        ScheduleNextDisruption();
    }
    
    IEnumerator GradualDriftCoroutine()
    {
        isDisrupting = true;
        
        // Warning phase
        if (provideWarning)
        {
            OnDisruptionWarning?.Invoke();
            ProvideWarningFeedback();
            yield return new WaitForSeconds(warningTime);
        }
        
        OnDisruptionStarted?.Invoke(DisruptionType.GradualDrift);
        
        // Choose random direction and strength
        float direction = Random.Range(0, 2) == 0 ? -1f : 1f;
        float strength = Random.Range(driftMinStrength, driftMaxStrength);
        
        // Apply gradual drift
        float startTime = Time.time;
        while (Time.time - startTime < driftDuration)
        {
            float t = (Time.time - startTime) / driftDuration;
            // Bell curve for smooth in and out
            float curve = Mathf.Sin(t * Mathf.PI);
            float currentStrength = strength * curve;
            balanceController.ApplyDisruption(currentStrength * Time.deltaTime, direction);
            yield return null;
        }
        
        isDisrupting = false;
        OnDisruptionEnded?.Invoke();
        ScheduleNextDisruption();
    }
    
    IEnumerator OscillationCoroutine()
    {
        isDisrupting = true;
        
        // Warning phase
        if (provideWarning)
        {
            OnDisruptionWarning?.Invoke();
            ProvideWarningFeedback();
            yield return new WaitForSeconds(warningTime);
        }
        
        OnDisruptionStarted?.Invoke(DisruptionType.Oscillation);
        
        // Apply oscillating disruption
        float startTime = Time.time;
        while (Time.time - startTime < oscillationDuration)
        {
            float t = (Time.time - startTime) / oscillationDuration;
            float fade = 1f - t; // Fade out over time
            
            // Sinusoidal oscillation
            float oscillation = Mathf.Sin((Time.time - startTime) * oscillationFrequency * 2f * Mathf.PI);
            float strength = oscillationAmplitude * oscillation * fade;
            
            balanceController.ApplyDisruption(strength * Time.deltaTime, 1f);
            yield return null;
        }
        
        isDisrupting = false;
        OnDisruptionEnded?.Invoke();
        ScheduleNextDisruption();
    }
    
    void ProvideWarningFeedback()
    {
        // Quick haptic pulses to both controllers as warning
        if (leftHapticPlayer)
            leftHapticPlayer.SendHapticImpulse(warningHapticStrength, warningHapticDuration);
        if (rightHapticPlayer)
            rightHapticPlayer.SendHapticImpulse(warningHapticStrength, warningHapticDuration);
    }
    
    // Public methods for external control
    public void TriggerDisruption(DisruptionType type)
    {
        if (isDisrupting)
            return;
            
        if (disruptionCoroutine != null)
            StopCoroutine(disruptionCoroutine);
            
        switch (type)
        {
            case DisruptionType.SuddenGust:
                disruptionCoroutine = StartCoroutine(SuddenGustCoroutine());
                break;
            case DisruptionType.GradualDrift:
                disruptionCoroutine = StartCoroutine(GradualDriftCoroutine());
                break;
            case DisruptionType.Oscillation:
                disruptionCoroutine = StartCoroutine(OscillationCoroutine());
                break;
        }
    }
    
    public void SetDisruptionChance(float chance)
    {
        disruptionChance = Mathf.Clamp01(chance);
    }
    
    public void RestoreBaseValues()
    {
        gustMinStrength = baseGustMinStrength;
        gustMaxStrength = baseGustMaxStrength;
        driftMinStrength = baseDriftMinStrength;
        driftMaxStrength = baseDriftMaxStrength;
        oscillationAmplitude = baseOscillationAmplitude;
    }
    
    public void StopAllDisruptions()
    {
        if (disruptionCoroutine != null)
        {
            StopCoroutine(disruptionCoroutine);
            disruptionCoroutine = null;
        }
        isDisrupting = false;
    }
    
    void OnDisable()
    {
        StopAllDisruptions();
    }
}