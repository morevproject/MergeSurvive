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
    
    // === НОВАЯ КНОПКА ДЛЯ ЕДЫ ===
    public Button eatButton; 
    
    public Button closeBackgroundButton; 

    private ItemData currentItem;

    private void Awake()
    {
        Instance = this;
        if (detailsPanel != null) detailsPanel.SetActive(false);
        
        if (findButton != null) findButton.onClick.AddListener(OnFindButtonClicked);
        if (equipButton != null) equipButton.onClick.AddListener(OnEquipButtonClicked);
        
        // Подписываем новую кнопку
        if (eatButton != null) eatButton.onClick.AddListener(OnEatButtonClicked);
        
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
        // 1. Умная логика кнопки "Найти"
        if (findButton != null)
        {
            bool canBeFound = false;
            if (ResourceFinder.Instance != null)
            {
                canBeFound = ResourceFinder.Instance.CanBeFound(item.ItemName);
            }
            findButton.gameObject.SetActive(canBeFound);
        }

        // 2. Кнопка "Надеть" (только для экипировки)
        if (equipButton != null)
        {
            bool isEquippable = GetTargetSlotType(item).HasValue;
            equipButton.gameObject.SetActive(isEquippable);
        }
        
        // === 3. УМНАЯ ЛОГИКА КНОПКИ "СЪЕСТЬ" ===
        // Показываем кнопку ТОЛЬКО если тип предмета — Еда
        if (eatButton != null)
        {
            eatButton.gameObject.SetActive(item.itemType == ItemType.Food);
        }
    }

    // === НОВЫЙ МЕТОД ПОЕДАНИЯ (Перенесен из AvatarDropZone) ===
    public void OnEatButtonClicked()
    {
        if (currentItem == null || currentItem.itemType != ItemType.Food) return;

        if (PlayerStats.Instance != null)
        {
            int cur = PlayerStats.Instance.currentHealth;
            int max = PlayerStats.Instance.maxHealth;

            // Проверяем, не полное ли здоровье
            if (cur < max)
            {
                // 1. Восстанавливаем ХП
                PlayerStats.Instance.Heal(currentItem.healAmount);
                Debug.Log($"[Inventory] Ням! {currentItem.GetName()} дал +{currentItem.healAmount} HP.");

                // 2. Забираем 1 штуку из инвентаря
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.RemoveItem(currentItem, 1);
                }

                // 3. Закрываем окошко инфо, так как предмет может закончиться
                ClosePanel(); 
            }
            else
            {
                Debug.Log("[Inventory] Отказ: Здоровье уже полное!");
                // Если у тебя есть система всплывающих текстовых подсказок, можно добавить её сюда:
                // FloatingTextManager.Show("Здоровье полное!");
            }
        }
    }

    public void OnEquipButtonClicked()
    {
        if (currentItem == null) return;

        EquipmentSlotType? targetSlotType = GetTargetSlotType(currentItem);

        if (targetSlotType.HasValue)
        {
            EquipmentSlotUI targetSlot = null;

            // 1. Быстрый путь: ищем в словаре (сработает, если профиль сейчас открыт)
            if (EquipmentSlotUI.ActiveSlots.TryGetValue(targetSlotType.Value, out EquipmentSlotUI activeSlot))
            {
                targetSlot = activeSlot;
            }
            else
            {
                // 2. Надежный путь: ищем слот принудительно среди ВСЕХ объектов сцены (даже скрытых!)
                // Параметр "true" заставляет Unity искать выключенные GameObject'ы
                EquipmentSlotUI[] allSlots = FindObjectsOfType<EquipmentSlotUI>(true);
            
                foreach (var s in allSlots)
                {
                    // Сравниваем тип найденного слота с тем типом, который нужен нашему предмету
                    if (s.slotType == targetSlotType.Value) 
                    {
                        targetSlot = s;
                        break;
                    }
                }
            }

            // Если нашли слот (а мы его теперь найдем в 99% случаев) — надеваем!
            if (targetSlot != null)
            {
                targetSlot.EquipItem(currentItem);
                ClosePanel(); 
            }
            // Варнинг мы убрали, игрок счастлив и может экипироваться прямо на ходу!
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