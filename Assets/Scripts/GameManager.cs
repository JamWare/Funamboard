using UnityEngine;
using TMPro; // Pour le texte

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Victoire")]
    public GameObject victoryCanvas;
    public TMP_Text feedbackText;
    
    private int croissantsEaten = 0;
    private const int TARGET_CROISSANTS = 5;

    private void Awake()
    {
        Instance = this;
        if (victoryCanvas != null) victoryCanvas.SetActive(false);
    }

    public void FeedAnimal()
    {
        croissantsEaten = 5;

        if (croissantsEaten >= TARGET_CROISSANTS)
        {
            WinGame();
        }
    }

    private void WinGame()
    {
        Debug.Log("VICTOIRE !");
        if (victoryCanvas != null) victoryCanvas.SetActive(true);
        if (feedbackText != null) feedbackText.text = "Victoire";
    }
}