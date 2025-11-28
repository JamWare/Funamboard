using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    [Header("Menu Configuration")]
    public GameObject inventoryMenu;
    public ItemSlot[] itemSlots; // Assurez-vous que ce tableau est rempli dans l'inspecteur

    [Header("XR Input")]
    public InputActionReference triggerAction;

    private bool _menuActivated;
    private InputAction _action;

    // Garde en mémoire le slot actuellement sélectionné
    private ItemSlot _currentSelectedSlot;

    private void Start()
    {
        if (triggerAction != null)
        {
            _action = triggerAction.action;
            _action.Enable();
        }

        // Initialisation : s'assurer que rien n'est sélectionné visuellement au départ
        foreach (var slot in itemSlots)
        {
            slot.Deselect();
        }
    }

    private void Update()
    {
        if (_action == null || !_action.WasPressedThisFrame()) return;

        ToggleInventory();
    }

    private void ToggleInventory()
    {
        _menuActivated = !_menuActivated;
        inventoryMenu.SetActive(_menuActivated);
        
        // Time.timeScale = _menuActivated ? 0f : 1f; 
    }

    // Méthode appelée par un ItemSlot quand il est cliqué
    public void OnSlotSelected(ItemSlot slotClicked)
    {

        if (_currentSelectedSlot != null && _currentSelectedSlot != slotClicked)
        {
            _currentSelectedSlot.Deselect();
        }

        _currentSelectedSlot = slotClicked;
        
    }

    public void AddItem(string itemName, int quantity, Sprite sprite, string itemDescription)
    {
        foreach (var itemSlot in itemSlots)
        {
            if (itemSlot.itemName == itemName && !itemSlot.isFull)
            {
                itemSlot.quantity += quantity;
                itemSlot.UpdateQuantity();
                if (itemSlot.quantity == 5) itemSlot.isFull = true;
                return;
            }
            if (itemSlot.quantity > 0 || itemSlot.isFull)
            {
                continue;
            }

            itemSlot.AddItem(itemName, quantity, sprite, itemDescription);
            return;
        }
        
        Debug.Log("Inventaire plein !");
    }
    
    public void DropItem()
    {
        if (_currentSelectedSlot == null || _currentSelectedSlot.quantity == 0)
        {
            Debug.Log("Aucun item sélectionné à jeter !");
            return;
        }
        
        Debug.Log(_currentSelectedSlot.itemName);
        Debug.Log(_currentSelectedSlot.quantity);
        _currentSelectedSlot.ClearSlot();
        _currentSelectedSlot = null;
    }
}
