using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

[System.Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int stackSize; 

    public InventorySlot(ItemData item, int amount)
    {
        itemData = item;
        stackSize = amount;
    }

    public void AddToStack(int amount) => stackSize += amount;
    public void RemoveFromStack(int amount) => stackSize -= amount;
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("UI References")]
    public Transform itemContent;
    public GameObject inventoryItemPrefab;
    public GameObject inventoryUI; 

    [Header("Data")]
    public List<InventorySlot> inventorySlots = new List<InventorySlot>();
    // public int maxSlots = 20; // <--- УДАЛИЛИ ОГРАНИЧЕНИЕ

    public delegate void OnInventoryChange();
    public event OnInventoryChange onInventoryChange;

    private List<Recipe.RecipeIngredient> craftRequirementsFilter = null; 
    private int currentCategoryIndex = 0; 
    private string currentSearchQuery = ""; 

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateInventoryUI();
    }

    private void OnDisable()
    {
        ResetFilter();
    }

    // =========================================================
    // ОСНОВНЫЕ МЕТОДЫ (Add/Remove)
    // =========================================================

    public void AddItem(ItemData item, int quantity = 1)
    {
        if (item == null) return;

        InventorySlot existingSlot = null;
        
        // Пытаемся найти слот для стака
        if (item.IsStackable)
        {
            // Ищем слот, где есть этот предмет и место еще есть (< maxStack)
            existingSlot = inventorySlots.Find(x => x.itemData.ItemName == item.ItemName && x.stackSize < item.MaxStackSize);
        }

        if (existingSlot != null)
        {
            existingSlot.AddToStack(quantity);
        }
        else
        {
            // --- ФИКС: УБРАЛИ ПРОВЕРКУ НА maxSlots ---
            // Просто добавляем новый слот. ScrollView сам растянется.
            inventorySlots.Add(new InventorySlot(item, quantity));
            // -----------------------------------------
        }

        // Проверяем анлоки рецептов
        if (CraftingManager.Instance != null) CraftingManager.Instance.CheckUnlocks(item);

        UpdateInventoryUI();

        // Универсальные триггеры туториала (проверяем всё при любом пополнении инвентаря)
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.CheckGathering(); // Проверит палки и камни
        }
    }

    public void RemoveItem(ItemData item, int quantity = 1)
    {
        // Ищем слот по имени, так надежнее
        InventorySlot slot = inventorySlots.Find(x => x.itemData.ItemName == item.ItemName);
        if (slot != null)
        {
            slot.RemoveFromStack(quantity);
            if (slot.stackSize <= 0) inventorySlots.Remove(slot);
        }
        UpdateInventoryUI();
    }

    // Метод для добавления по имени (для читов/дропа)
    public void AddResource(string resourceName, int amount)
    {
        ItemData item = Resources.Load<ItemData>("Items/" + resourceName);
        // Fallback: пробуем искать без папки Items/
        if (item == null) item = Resources.Load<ItemData>(resourceName);

        if (item != null) AddItem(item, amount);
        else Debug.LogError($"InventoryManager: Не найден предмет {resourceName} в Resources!");
    }

    // =========================================================
    // УПРАВЛЕНИЕ UI
    // =========================================================

    public void SetCategory(int categoryIndex)
    {
        currentCategoryIndex = categoryIndex;
        ListItems();
    }
    
    public void UpdateSearch(string query)
    {
        currentSearchQuery = query.ToLower();
        ListItems();
    }

    private void UpdateInventoryUI()
    {
        onInventoryChange?.Invoke();
        ListItems();
    }

    public void ListItems()
    {
        if (itemContent == null || inventoryItemPrefab == null) return;

        foreach (Transform child in itemContent) Destroy(child.gameObject);

        // 1. РЕЖИМ "ПОКАЗАТЬ НЕДОСТАЮЩЕЕ" (Для крафта)
        if (craftRequirementsFilter != null)
        {
            foreach (var req in craftRequirementsFilter)
            {
                int currentAmount = GetTotalAmountByName(req.resourceName);
                
                // Пытаемся найти ItemData, чтобы отобразить иконку
                ItemData itemData = null;
                var existingSlot = inventorySlots.Find(x => x.itemData.ItemName == req.resourceName);
                if (existingSlot != null) itemData = existingSlot.itemData;
                else
                {
                    itemData = Resources.Load<ItemData>("Items/" + req.resourceName);
                    if (itemData == null) itemData = Resources.Load<ItemData>(req.resourceName);
                }

                if (itemData != null)
                {
                    CreateVisualSlot(itemData, currentAmount, req.amount); // req.amount здесь работает как "Target"
                }
            }
        }
        // 2. ОБЫЧНЫЙ РЕЖИМ
        else
        {
            foreach (var slot in inventorySlots)
            {
                // Фильтр по категории
                if (currentCategoryIndex != 0)
                {
                    // (ItemType 0 = Resource, 1 = Tool...)
                    // Проверяем соответствие типа. (currentCategoryIndex - 1) т.к. 0 это "ВСЕ"
                    if ((int)slot.itemData.itemType != (currentCategoryIndex - 1)) continue;
                }

                // Поиск
                if (!string.IsNullOrEmpty(currentSearchQuery))
                {
                    if (!slot.itemData.GetName().ToLower().Contains(currentSearchQuery)) continue;
                }

                CreateVisualSlot(slot.itemData, slot.stackSize, 0);
            }
        }
    }

    void CreateVisualSlot(ItemData item, int currentAmount, int requiredAmount)
    {
        GameObject obj = Instantiate(inventoryItemPrefab, itemContent);
        obj.SetActive(true);

        // Если requiredAmount > 0, значит мы в режиме "показать нехватку"
        bool isMissing = (requiredAmount > 0) && (currentAmount < requiredAmount);

        var slotUI = obj.GetComponent<ResourceSlotUI>();
        if (slotUI != null)
        {
            slotUI.UpdateSlot(item.GetName(), item.Icon, currentAmount, item);
            if (isMissing) slotUI.SetHighlight(true); // Красная рамка
        }
        
        var draggable = obj.GetComponent<UIDraggableItem>();
        if (draggable != null) draggable.SetSlotData(new InventorySlot(item, currentAmount));
    }

    // =========================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ (Крафт, Сохранение)
    // =========================================================

    public int GetTotalAmountByName(string resourceName)
    {
        return inventorySlots.Where(x => x.itemData.ItemName == resourceName).Sum(x => x.stackSize);
    }

    public bool HasResources(List<Recipe.RecipeIngredient> requirements)
    {
        foreach (var req in requirements)
        {
            if (GetTotalAmountByName(req.resourceName) < req.amount) return false;
        }
        return true;
    }

    public void ConsumeResources(List<Recipe.RecipeIngredient> requirements)
    {
        foreach (var req in requirements) RemoveResource(req.resourceName, req.amount);
    }

    public void RemoveResource(string resourceName, int amount)
    {
        int remaining = amount;
        // Идем с конца, чтобы безопасно удалять слоты
        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            if (inventorySlots[i].itemData.ItemName == resourceName)
            {
                int take = Mathf.Min(remaining, inventorySlots[i].stackSize);
                inventorySlots[i].RemoveFromStack(take);
                remaining -= take;
                if (inventorySlots[i].stackSize <= 0) inventorySlots.RemoveAt(i);
                if (remaining <= 0) break;
            }
        }
        UpdateInventoryUI();
    }

    public void ShowMissingResourcesMode(List<Recipe.RecipeIngredient> requirements)
    {
        craftRequirementsFilter = requirements;
        currentCategoryIndex = 0; 
        currentSearchQuery = "";

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(true);
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.SetState(GameStateManager.GameState.MenuOpen);
        }
        
        ListItems();
        StopAllCoroutines();
        StartCoroutine(AutoResetFilterRoutine());
    }

    IEnumerator AutoResetFilterRoutine()
    {
        yield return new WaitForSeconds(3.0f);
        ResetFilter();
    }

    public void ResetFilter()
    {
        craftRequirementsFilter = null;
        currentCategoryIndex = 0;
        currentSearchQuery = "";
        ListItems();
    }

    public void ClearInventory()
    {
        inventorySlots.Clear();
        UpdateInventoryUI();
    }
    
    // --- ВОТ ЭТИ МЕТОДЫ Я ВЕРНУЛ ---

    public int GetItemCount(ItemData itemData)
    {
        if (itemData != null) return GetTotalAmountByName(itemData.ItemName);
        return 0;
    }

    // Получить количество предмета по ИМЕНИ (строке) - старый метод, который мог использоваться где-то еще
    public int GetItemCountByName(string itemName)
    {
        foreach (var slot in inventorySlots) 
        {
            if (slot.itemData != null && slot.itemData.ItemName == itemName)
                return slot.stackSize; 
        }
        return 0;
    }

    // Найти саму ItemData по имени (нужно для SaveManager и Debug Console)
    public ItemData GetItemDataByName(string itemName)
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.itemData != null && slot.itemData.ItemName == itemName)
                return slot.itemData;
        }
        return null; 
    }
}