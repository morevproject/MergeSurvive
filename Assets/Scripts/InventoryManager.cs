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

    [Header("Tabs UI (Color Coded)")]
    public Image[] tabButtons; 
    public Sprite[] activeTabSprites;   // Массив ярких картинок (по порядку)
    public Sprite[] inactiveTabSprites; // Массив тусклых картинок (по порядку)

    [Header("Data")]
    public List<InventorySlot> inventorySlots = new List<InventorySlot>();

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
        UpdateTabVisuals(); 
    }

    private void OnDisable()
    {
        ResetFilter();
    }

    public void AddItem(ItemData item, int quantity = 1)
    {
        if (item == null) return;

        InventorySlot existingSlot = null;
        
        if (item.IsStackable)
        {
            existingSlot = inventorySlots.Find(x => x.itemData.ItemName == item.ItemName && x.stackSize < item.MaxStackSize);
        }

        if (existingSlot != null)
        {
            existingSlot.AddToStack(quantity);
        }
        else
        {
            inventorySlots.Add(new InventorySlot(item, quantity));
        }

        if (CraftingManager.Instance != null) CraftingManager.Instance.CheckUnlocks(item);

        UpdateInventoryUI();

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.CheckGathering(); 
        }
    }

    public void RemoveItem(ItemData item, int quantity = 1)
    {
        InventorySlot slot = inventorySlots.Find(x => x.itemData.ItemName == item.ItemName);
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
        else Debug.LogError($"InventoryManager: Не найден предмет {resourceName} в Resources!");
    }

    public void SetCategory(int categoryIndex)
    {
        currentCategoryIndex = categoryIndex;
        UpdateTabVisuals(); 
        ListItems();
    }
    
    // === ОБНОВЛЕННЫЙ МЕТОД С МАССИВАМИ ===
    private void UpdateTabVisuals()
    {
        if (tabButtons == null || activeTabSprites == null || inactiveTabSprites == null) return;

        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null && i < activeTabSprites.Length && i < inactiveTabSprites.Length)
            {
                // Берем картинку из массива под тем же номером 'i'
                tabButtons[i].sprite = (i == currentCategoryIndex) ? activeTabSprites[i] : inactiveTabSprites[i];
            }
        }
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
                    if ((int)slot.itemData.itemType != (currentCategoryIndex - 1)) continue;
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
        
        UpdateTabVisuals(); 

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
        UpdateTabVisuals();
        ListItems();
    }

    public void ClearInventory()
    {
        inventorySlots.Clear();
        UpdateInventoryUI();
    }

    public int GetItemCount(ItemData itemData)
    {
        if (itemData != null) return GetTotalAmountByName(itemData.ItemName);
        return 0;
    }

    public int GetItemCountByName(string itemName)
    {
        foreach (var slot in inventorySlots) 
        {
            if (slot.itemData != null && slot.itemData.ItemName == itemName)
                return slot.stackSize; 
        }
        return 0;
    }

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