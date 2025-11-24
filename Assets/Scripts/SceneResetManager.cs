using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneResetManager : MonoBehaviour
{
    [Header("Input References")]
    public InputActionReference leftSecondaryButton;
    public InputActionReference rightSecondaryButton;

    // Button press tracking
    private bool leftSecondaryPressed = false;
    private bool rightSecondaryPressed = false;

    void Start()
    {
        Debug.Log("SceneResetManager initialized - Press B+Y to reset scene");
    }

    void Update()
    {
        CheckResetInput();
    }

    void CheckResetInput()
    {
        // Check left secondary button (Y)
        if (leftSecondaryButton != null && leftSecondaryButton.action != null)
        {
            leftSecondaryPressed = leftSecondaryButton.action.IsPressed();
        }

        // Check right secondary button (B)
        if (rightSecondaryButton != null && rightSecondaryButton.action != null)
        {
            rightSecondaryPressed = rightSecondaryButton.action.IsPressed();
        }

        // Trigger reset if both are pressed
        if (leftSecondaryPressed && rightSecondaryPressed)
        {
            ResetScene();
        }
    }

    public void ResetScene()
    {
        Debug.Log("=== RELOADING SCENE ===");

        // Reload the current active scene
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    // Public method for manual reset trigger
    public void TriggerReset()
    {
        ResetScene();
    }
}
