using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Item : MonoBehaviour
{
    [SerializeField]
    private string itemName;
    
    [SerializeField]
    private int quantity;

    [SerializeField] 
    private Sprite sprite;

    [TextArea]
    [SerializeField] 
    private string itemDescription;
    
    private InventoryManager _inventoryManager;
    private XRGrabInteractable _grabInteractable;

    
    private void Start()
    {
        _inventoryManager = GameObject.Find("Inventory Canvas").GetComponent<InventoryManager>();
        Debug.Log(_inventoryManager.name);
        
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _grabInteractable.selectEntered.AddListener(OnGrabbed);
    }
    
    private void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        AddToInventory();
    }

    private void AddToInventory()
    {
        _inventoryManager.AddItem(itemName, quantity, sprite, itemDescription);
        Destroy(gameObject);
    }
}
