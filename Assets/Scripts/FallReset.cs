using UnityEngine;

public class FallReset : MonoBehaviour
{
    [Header("Paramètres")]
    [Tooltip("Hauteur Y à laquelle le joueur est considéré comme tombé")]
    public float fallThreshold = 10f; 

    [Tooltip("Point de réapparition (Transform vide à placer dans la scène)")]
    public Transform respawnPoint;

    // On met souvent le CharacterController ici pour éviter les bugs de physique au reset
    private CharacterController _characterController;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Vérification simple de la hauteur
        if (transform.position.y < fallThreshold)
        {
            RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        // Désactiver temporairement le CharacterController est CRUCIAL
        // Sinon Unity empêche souvent le déplacement forcé
        if (_characterController != null) 
            _characterController.enabled = false;

        // Téléportation
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            // Optionnel : Reset la rotation aussi
            transform.rotation = respawnPoint.rotation;
        }
        else
        {
            // Fallback : Reset au point zéro si aucun point n'est assigné
            transform.position = Vector3.zero;
        }

        // Réactiver le contrôleur
        if (_characterController != null) 
            _characterController.enabled = true;

        Debug.Log("Joueur tombé ! Respawn effectué.");
    }
}