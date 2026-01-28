using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SoulListUI : MonoBehaviour
{
    // Такой же порядок, как в Инвентаре
    public enum FilterTab 
    { 
        All = 0, 
        Resources = 1, 
        Tools = 2, 
        Weapons = 3, 
        Armor = 4, 
        Food = 5, 
        Ammo = 6
    }

    public GameObject soulSlotPrefab; 
    public TMP_InputField searchInputField;

    private FilterTab currentTab = FilterTab.All;
    private string currentSearchText = "";
    
    private Dictionary<GameObject, Recipe> spawnedSlots = new Dictionary<GameObject, Recipe>();

    void Start()
    {
        Invoke("RefreshList", 0.2f); 
        if (searchInputField != null)
            searchInputField.onValueChanged.AddListener(OnSearchTextChanged);
    }

    void OnEnable()
    {
        if (CraftingManager.Instance != null) RefreshList();
        CraftingManager.OnRecipeDiscovered += HandleRecipeDiscovered;
    }

    void OnDisable()
    {
        CraftingManager.OnRecipeDiscovered -= HandleRecipeDiscovered;
    }

    private void HandleRecipeDiscovered(Recipe recipe) => RefreshList();

    // --- ФИЛЬТРАЦИЯ ---

    public void SetTab(int tabIndex)
    {
        currentTab = (FilterTab)tabIndex;
        ApplyFilters();
    }

    public void OnSearchTextChanged(string text)
    {
        currentSearchText = text.ToLower();
        ApplyFilters();
    }

    void ApplyFilters()
    {
        foreach (var kvp in spawnedSlots)
        {
            GameObject slotObj = kvp.Key;
            Recipe recipe = kvp.Value;

            if (slotObj == null) continue;

            // 1. Поиск по имени
            bool matchesSearch = string.IsNullOrEmpty(currentSearchText) || 
                                 recipe.recipeName.ToLower().Contains(currentSearchText);

            // 2. Категория
            bool matchesTab = CheckCategory(recipe);

            slotObj.SetActive(matchesSearch && matchesTab);
        }
    }

    bool CheckCategory(Recipe recipe)
    {
        if (currentTab == FilterTab.All) return true;

        switch (currentTab)
        {
            case FilterTab.Resources: return recipe.category == ItemCategory.Resource;
            case FilterTab.Tools:     return recipe.category == ItemCategory.Tool;
            case FilterTab.Weapons:   return recipe.category == ItemCategory.Weapon;
            case FilterTab.Armor:     return recipe.category == ItemCategory.Armor;
            case FilterTab.Food:      return recipe.category == ItemCategory.Food;
            case FilterTab.Ammo:   return recipe.category == ItemCategory.Ammo;
            default: return true;
        }
    }

    public void RefreshList()
    {
        if (CraftingManager.Instance == null || soulSlotPrefab == null) return;

        foreach (Transform child in transform) Destroy(child.gameObject);
        spawnedSlots.Clear();

        var discovered = CraftingManager.Instance.GetDiscoveredRecipes();

        foreach (var recipe in discovered)
        {
            if (recipe == null) continue;

            GameObject slot = Instantiate(soulSlotPrefab, transform);
            spawnedSlots.Add(slot, recipe);

            var slotUI = slot.GetComponent<ResourceSlotUI>();
            if (slotUI != null)
                slotUI.UpdateSlot(recipe.outputResourceName, recipe.icon, 1, null);

            var soulItem = slot.GetComponent<SoulItemUI>();
            if (soulItem != null) soulItem.Setup(recipe);
        }
        
        ApplyFilters();
    }
}