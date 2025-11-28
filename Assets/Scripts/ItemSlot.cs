using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x
using UnityEngine.InputSystem;

[RequireComponent(typeof(XRSimpleInteractable))]
[RequireComponent(typeof(BoxCollider))]
public class ItemSlot : MonoBehaviour
{
    [Header("Item Data")]
    public string itemName;
    public int quantity = 0;
    public Sprite itemSprite;
    public bool isFull;
    public bool isSelected;
    public string itemDescription;

    [Header("Item Description Data")] 
    public Image itemDescriptionImage;
    public TMP_Text itemDescriptionNameText;
    public TMP_Text itemDescriptionText;

    [Header("UI References")]
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image itemImage;
    [SerializeField] private GameObject selectedPanel;

    [Header("Inputs References")]
    // 1. Action pour SÉLECTIONNER (Trigger / UI Press)
    public InputActionReference selectAction; 
    
    // 2. Action pour UTILISER (Primary Button / Activate)
    public InputActionReference useAction; 
    
    // 3. Action pour DROP (Grip) -
    public InputActionReference dropAction;

    private XRSimpleInteractable _simpleInteractable;
    private InventoryManager _inventoryManager;
    private bool _isHovered;
    private Sprite _defaultImageSprite;

    private void Awake()
    {
        _simpleInteractable = GetComponent<XRSimpleInteractable>();
        _inventoryManager = FindFirstObjectByType<InventoryManager>();
    }

    private void OnEnable()
    {
        if (_simpleInteractable != null)
        {
            _simpleInteractable.hoverEntered.AddListener(OnHoverEnter);
            _simpleInteractable.hoverExited.AddListener(OnHoverExit);
        }
    }
    
    private void OnDisable()
    {
        if (_simpleInteractable != null)
        {
            _simpleInteractable.hoverEntered.RemoveListener(OnHoverEnter);
            _simpleInteractable.hoverExited.RemoveListener(OnHoverExit);
        }
    }

    private void Update()
    {
        if (_isHovered && selectAction != null && selectAction.action.WasPressedThisFrame())
        {
            Select();
        }

        if (isSelected)
        {
            if (useAction != null && useAction.action.WasPressedThisFrame())
            {
                Debug.Log($"Use action pressed on slot {gameObject.name}");
                UseItem();
            }

            if (dropAction != null && dropAction.action.WasPressedThisFrame())
            {
                Debug.Log($"Drop action pressed on slot {gameObject.name}");
                DropItem();
            }
            
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args) => _isHovered = true;
    private void OnHoverExit(HoverExitEventArgs args) => _isHovered = false;

    public void Select()
    {
        if (isSelected) return;

        isSelected = true;
        if (selectedPanel != null)
        {
            selectedPanel.SetActive(true);
            itemDescriptionImage.sprite = itemSprite;
            itemDescriptionNameText.text = itemName;
            itemDescriptionText.text = itemDescription;
        }

        if (_inventoryManager != null)
        {
            _inventoryManager.OnSlotSelected(this);
        }
        
        Debug.Log($"Slot {gameObject.name} sélectionné. En attente d'actions (Grip/Primary)...");
    }

    public void Deselect()
    {
        isSelected = false;
        if (selectedPanel != null) selectedPanel.SetActive(false);
    }

    private void UseItem()
    {
        Debug.Log($"Use item {itemName}");
        Debug.Log(isFull);
        if (!isFull) return;
        
        if (itemName == "Croissant")
        {
            if (AnimalFeeder.Instance != null && AnimalFeeder.Instance.isPlayerInZone)
            {
                if (GameManager.Instance != null)
                {
                    _inventoryManager.inventoryMenu.SetActive(false);
                    GameManager.Instance.FeedAnimal();
                }

                quantity = 0;
                if (quantity <= 0) ClearSlot();
                else quantityText.text = quantity.ToString();
            }
            else
            {
                Debug.Log("Trop loin de l'animal pour le nourrir !");
            }
        }
        else
        {
            Debug.Log($"Utilisé {itemName} (Pas pour l'animal)");
        }
    }


    private void DropItem()
    {
        _inventoryManager.DropItem();
    }

    public void AddItem(string newItemName, int newQuantity, Sprite newItemSprite, string newItemDescription)
    {
        _defaultImageSprite = itemSprite;
        itemName = newItemName;
        quantity = newQuantity;
        itemSprite = newItemSprite;
        itemDescription = newItemDescription;
        isFull = quantity >= 5;

        if (itemImage != null) itemImage.sprite = itemSprite;
        
        if (quantityText != null)
        {
            quantityText.text = quantity.ToString();
            quantityText.enabled = true;
        }
    }

    public void ClearSlot()
    {
        Debug.Log("Clearing slot: " + itemName);
        itemName = "";
        quantity = 0;
        itemSprite = _defaultImageSprite;
        isFull = false;
        isSelected = false;
        itemDescription = "";

        Debug.Log(_defaultImageSprite.name);
        itemImage.sprite = _defaultImageSprite;
        itemDescriptionImage.sprite = _defaultImageSprite;
        Debug.Log(itemDescriptionImage.sprite.name);
        itemDescriptionNameText.text = "";
        itemDescriptionText.text = "";
        
       
        quantityText.text = "";
        quantityText.enabled = false;
        if (selectedPanel != null) selectedPanel.SetActive(false);
    }
    
    public void UpdateQuantity()
    {
        if (quantityText != null)
        {
            quantityText.text = quantity.ToString();
        }
    }
}
