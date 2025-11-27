using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Manages visibility of UI elements based on button input and game state.
/// Hold the assigned button to show balance indicators while advancing on the rope.
/// Indicators only appear when both conditions are met: button pressed AND player advancing.
/// </summary>
public class UIToggleManager : MonoBehaviour
{
    [Header("Input Configuration")]
    [Tooltip("Input action for toggling UI (e.g., Left Primary Button / X button)")]
    public InputActionReference toggleButton;

    [Header("UI Elements to Toggle")]
    [Tooltip("Balance text indicator from MovingPlank")]
    public TextMeshProUGUI balanceText;

    [Tooltip("T-Pose text indicator from MovingPlank")]
    public TextMeshProUGUI tPoseText;

    [Header("Game State Reference")]
    [Tooltip("Reference to MovingPlank to check if player is advancing on rope")]
    public MovingPlank movingPlank;

    [Header("Settings")]
    [Tooltip("Should indicators be visible when the game starts?")]
    public bool startVisible = false;

    private bool isButtonPressed = false;

    void Start()
    {
        // Set initial visibility based on startVisible setting
        SetIndicatorsVisibility(startVisible);
    }

    void OnEnable()
    {
        // Enable the input action when this component is enabled
        if (toggleButton != null && toggleButton.action != null)
        {
            toggleButton.action.Enable();
        }
    }

    void OnDisable()
    {
        // Disable the input action when this component is disabled
        if (toggleButton != null && toggleButton.action != null)
        {
            toggleButton.action.Disable();
        }
    }

    void Update()
    {
        CheckButtonInput();
    }

    /// <summary>
    /// Checks if the toggle button is being held down and updates visibility accordingly.
    /// Indicators only show when button is pressed AND player is advancing on the rope.
    /// </summary>
    void CheckButtonInput()
    {
        if (toggleButton == null || toggleButton.action == null)
        {
            return;
        }

        // Check if button is currently pressed
        bool currentlyPressed = toggleButton.action.IsPressed();

        // Check if player is advancing on rope
        bool isAdvancing = IsPlayerAdvancingOnRope();

        // Show indicators only when BOTH button is pressed AND player is advancing
        bool shouldShow = currentlyPressed && isAdvancing;

        // Update visibility if state changed
        if (shouldShow != isButtonPressed)
        {
            isButtonPressed = shouldShow;
            SetIndicatorsVisibility(shouldShow);
        }
    }

    /// <summary>
    /// Checks if the player is currently advancing on the rope.
    /// Returns true only when player is attached to plank AND plank is moving.
    /// </summary>
    /// <returns>True if player is advancing, false otherwise</returns>
    bool IsPlayerAdvancingOnRope()
    {
        if (movingPlank == null)
        {
            return false;
        }

        return movingPlank.IsPlayerAttached() && movingPlank.IsMoving();
    }

    /// <summary>
    /// Sets the visibility of both balance indicators
    /// </summary>
    /// <param name="visible">True to show, false to hide</param>
    void SetIndicatorsVisibility(bool visible)
    {
        if (balanceText != null && balanceText.gameObject != null)
        {
            balanceText.gameObject.SetActive(visible);
        }

        if (tPoseText != null && tPoseText.gameObject != null)
        {
            tPoseText.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Public method to manually show indicators (useful for other scripts or events)
    /// </summary>
    public void ShowIndicators()
    {
        SetIndicatorsVisibility(true);
    }

    /// <summary>
    /// Public method to manually hide indicators (useful for other scripts or events)
    /// </summary>
    public void HideIndicators()
    {
        SetIndicatorsVisibility(false);
    }

    /// <summary>
    /// Public method to check if indicators are currently visible
    /// </summary>
    public bool AreIndicatorsVisible()
    {
        if (balanceText != null && balanceText.gameObject != null)
        {
            return balanceText.gameObject.activeSelf;
        }
        return false;
    }
}
