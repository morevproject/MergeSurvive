using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

[System.Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int stackSize; // В твоем коде это называется stackSize

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
    public int maxSlots = 20;

    // Событие обновления инвентаря
    public delegate void OnInventoryChange();
    public event OnInventoryChange onInventoryChange;

    // --- СОСТОЯНИЕ ФИЛЬТРОВ ---
    private List<Recipe.RecipeIngredient> craftRequirementsFilter = null; 
    private int currentCategoryIndex = 0; 
    private string currentSearchQuery = ""; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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

    // =========================================================
    // ОСНОВНЫЕ МЕТОДЫ
    // =========================================================

    public void AddItem(ItemData item, int quantity = 1)
    {
        if (item == null) return;

        InventorySlot existingSlot = null;
        if (item.IsStackable)
        {
            existingSlot = inventorySlots.Find(x => x.itemData == item && x.stackSize < item.MaxStackSize);
        }

        if (existingSlot != null) existingSlot.AddToStack(quantity);
        else
        {
            if (inventorySlots.Count < maxSlots) inventorySlots.Add(new InventorySlot(item, quantity));
            else { Debug.LogWarning("Инвентарь полон!"); return; }
        }

        if (CraftingManager.Instance != null) CraftingManager.Instance.CheckUnlocks(item);

        UpdateInventoryUI();
    }

    public void RemoveItem(ItemData item, int quantity = 1)
    {
        InventorySlot slot = inventorySlots.Find(x => x.itemData == item);
        if (slot != null)
        {
            slot.RemoveFromStack(quantity);
            if (slot.stackSize <= 0) inventorySlots.Remove(slot);
        }
        UpdateInventoryUI();
    }

    public void AddResource(string resourceName, int amount)
    {
        ItemData item = Resources.Load<ItemData>("Items/" + resourceName);
        if (item == null) item = Resources.Load<ItemData>(resourceName);

        if (item != null) AddItem(item, amount);
        else Debug.LogError($"InventoryManager: Не найден предмет {resourceName}");
    }

    // =========================================================
    // ЛОГИКА ДЛЯ КРАФТА
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
            {
                GameStateManager.Instance.SetState(GameStateManager.GameState.MenuOpen);
            }
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

    private void UpdateInventoryUI()
    {
        onInventoryChange?.Invoke();
        ListItems();
    }

    public void ListItems()
    {
        if (itemContent == null || inventoryItemPrefab == null) return;

        foreach (Transform child in itemContent) Destroy(child.gameObject);

        if (craftRequirementsFilter != null)
        {
            foreach (var req in craftRequirementsFilter)
            {
                int currentAmount = GetTotalAmountByName(req.resourceName);
                
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
                    CreateVisualSlot(itemData, currentAmount, req.amount);
                }
            }
        }
        else
        {
            foreach (var slot in inventorySlots)
            {
                if (currentCategoryIndex != 0)
                {
                    ItemType requiredType = (ItemType)(currentCategoryIndex - 1);
                    if (slot.itemData.itemType != requiredType) continue;
                }

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

        bool isMissing = (requiredAmount > 0) && (currentAmount < requiredAmount);

        var slotUI = obj.GetComponent<ResourceSlotUI>();
        if (slotUI != null)
        {
            slotUI.UpdateSlot(item.GetName(), item.Icon, currentAmount, item);
            if (isMissing) slotUI.SetHighlight(true); 
        }
        
        var draggable = obj.GetComponent<UIDraggableItem>();
        if (draggable != null) draggable.SetSlotData(new InventorySlot(item, currentAmount));
    }

    public int GetItemCount(ItemData itemData)
    {
        // Используем твой же надежный метод поиска по имени
        if (itemData != null)
        {
            return GetTotalAmountByName(itemData.ItemName);
        }
        return 0;
    }

    // --- МЕТОДЫ ДЛЯ СОХРАНЕНИЙ ---
    public void ClearInventory()
    {
        inventorySlots.Clear(); // Очищаем список
        UpdateInventoryUI();    // Обновляем картинку на экране
    }
}