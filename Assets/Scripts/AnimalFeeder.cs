using UnityEngine;

public class AnimalFeeder : MonoBehaviour
{
    // On utilise un Singleton simple pour que n'importe qui puisse demander 
    // "Est-ce que je suis près de l'animal ?" sans avoir à faire de liens complexes.
    public static AnimalFeeder Instance;

    [Header("Zone de Nourrissage")]
    public bool isPlayerInZone = false;

    private void Awake()
    {
        Instance = this;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Assurez-vous que le XR Origin a le tag "Player"
        {
            isPlayerInZone = true;
            Debug.Log("Joueur près de l'animal.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            Debug.Log("Joueur parti.");
        }
    }
}