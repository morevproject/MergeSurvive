using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class InventoryItemDetails : MonoBehaviour
{
    public static InventoryItemDetails Instance;

    [Header("UI References")]
    public GameObject detailsPanel; 
    public Image itemIcon;
    public TextMeshProUGUI itemNameText; 
    public TextMeshProUGUI itemDescriptionText; 
    
    public Button findButton; 
    public Button equipButton; 
    public Button closeBackgroundButton; 

    private ItemData currentItem;

    private void Awake()
    {
        Instance = this;
        if (detailsPanel != null) detailsPanel.SetActive(false);
        
        if (findButton != null) findButton.onClick.AddListener(OnFindButtonClicked);
        if (equipButton != null) equipButton.onClick.AddListener(OnEquipButtonClicked);
        if (closeBackgroundButton != null) closeBackgroundButton.onClick.AddListener(ClosePanel);
    }

    public void SelectItem(ItemData item)
    {
        if (item == null) return;

        currentItem = item;
        
        if (detailsPanel != null) detailsPanel.SetActive(true);
        
        if (itemIcon != null) itemIcon.sprite = item.Icon;
        if (itemNameText != null) itemNameText.text = item.GetName();
        if (itemDescriptionText != null) itemDescriptionText.text = item.Description;

        UpdateButtonsVisibility(item);
    }

    private void UpdateButtonsVisibility(ItemData item)
    {
        // === УМНАЯ ЛОГИКА КНОПКИ "НАЙТИ" ===
        if (findButton != null)
        {
            bool canBeFound = false;
            // Спрашиваем Радар: "На этой локации это есть?"
            if (ResourceFinder.Instance != null)
            {
                canBeFound = ResourceFinder.Instance.CanBeFound(item.ItemName);
            }
            
            findButton.gameObject.SetActive(canBeFound);
        }

        // Кнопка "Надеть" (как мы делали в прошлом шаге)
        if (equipButton != null)
        {
            bool isEquippable = GetTargetSlotType(item).HasValue;
            equipButton.gameObject.SetActive(isEquippable);
        }
    }

    public void OnEquipButtonClicked()
    {
        if (currentItem == null) return;

        EquipmentSlotType? targetSlotType = GetTargetSlotType(currentItem);

        if (targetSlotType.HasValue)
        {
            if (EquipmentSlotUI.ActiveSlots.TryGetValue(targetSlotType.Value, out EquipmentSlotUI slot))
            {
                slot.EquipItem(currentItem);
                ClosePanel(); 
            }
            else
            {
                Debug.LogWarning($"[Inventory] Слот типа {targetSlotType.Value} не найден на сцене (возможно игрок не открыл профиль?)");
            }
        }
    }

    private EquipmentSlotType? GetTargetSlotType(ItemData item)
    {
        switch (item.itemType)
        {
            case ItemType.Weapon: return EquipmentSlotType.Weapon;
            case ItemType.Armor: return EquipmentSlotType.Armor;
            case ItemType.Ammo: return EquipmentSlotType.Ammo;
            case ItemType.Tool:
                if (item.toolType == ToolType.Axe) return EquipmentSlotType.ToolAxe;
                if (item.toolType == ToolType.Pickaxe) return EquipmentSlotType.ToolPickaxe;
                if (item.toolType == ToolType.Shovel) return EquipmentSlotType.ToolShovel;
                break;
        }
        return null; 
    }

    public void OnFindButtonClicked()
    {
        if (currentItem != null && ResourceFinder.Instance != null)
        {
            if (UIManager.Instance != null) UIManager.Instance.ToggleInventory();
            ResourceFinder.Instance.FindResourceNodes(currentItem.ItemName);
            ClosePanel();
        }
    }

    public void ClosePanel()
    {
        if (detailsPanel != null) detailsPanel.SetActive(false);
    }
}