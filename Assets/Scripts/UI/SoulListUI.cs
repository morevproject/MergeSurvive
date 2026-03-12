using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SoulListUI : MonoBehaviour
{
    public static SoulListUI Instance;

    [Header("Upgrade UI")]
    public Button upgradeButton;     
    public Text upgradeCostText;     
    public Text currentGearsText;    
    public GameObject maxLevelText;  

    [Header("Tabs UI (Color Coded)")]
    public Image[] tabButtons; 
    public Sprite[] activeTabSprites;   // Массив ярких картинок (по порядку)
    public Sprite[] inactiveTabSprites; // Массив тусклых картинок (по порядку)

    [Header("List Settings")]
    public Transform contentContainer; 
    public GameObject soulItemPrefab;  

    private int currentTabIndex = 0; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnEnable()
    {
        CraftingManager.OnRecipeDiscovered += OnRecipeDiscoveredHandler;
        RefreshUI();
    }

    private void OnDisable()
    {
        CraftingManager.OnRecipeDiscovered -= OnRecipeDiscoveredHandler;
    }

    private void OnRecipeDiscoveredHandler(Recipe recipe)
    {
        RefreshUI();
    }

    private void Start()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeClicked);

        UpdateTabVisuals(); 
        RefreshUI();
    }
    
    public void SetTab(int tabIndex)
    {
        currentTabIndex = tabIndex;
        UpdateTabVisuals(); 
        RefreshSoulList();
    }

    // === ОБНОВЛЕННЫЙ МЕТОД С МАССИВАМИ ===
    private void UpdateTabVisuals()
    {
        if (tabButtons == null || activeTabSprites == null || inactiveTabSprites == null) return;

        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null && i < activeTabSprites.Length && i < inactiveTabSprites.Length)
            {
                tabButtons[i].sprite = (i == currentTabIndex) ? activeTabSprites[i] : inactiveTabSprites[i];
            }
        }
    }

    public void RefreshUI()
    {
        UpdateUpgradeInfo();
        RefreshSoulList();
    }

    private void UpdateUpgradeInfo()
    {
        if (PlayerStats.Instance == null) return;

        int currentGears = PlayerStats.Instance.Gears;
        int nextCost = PlayerStats.Instance.GetNextUpgradeCost();

        if (currentGearsText != null) currentGearsText.text = $"{currentGears}"; 

        if (nextCost == -1) 
        {
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
            if (maxLevelText != null) maxLevelText.SetActive(true);
        }
        else
        {
            if (upgradeButton != null)
            {
                upgradeButton.gameObject.SetActive(true);
                upgradeButton.interactable = (currentGears >= nextCost);
            }
            if (maxLevelText != null) maxLevelText.SetActive(false);
            if (upgradeCostText != null) upgradeCostText.text = $"Улучшить Верстак за {nextCost}";
        }
    }

    public void OnUpgradeClicked()
    {
        if (PlayerStats.Instance.TryUpgradeWorkbench())
        {
            RefreshUI(); 
        }
    }

    private void RefreshSoulList()
    {
        if (contentContainer == null || soulItemPrefab == null) return;

        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        if (CraftingManager.Instance == null) return;

        var recipes = CraftingManager.Instance.GetDiscoveredRecipes();
        int playerTier = (PlayerStats.Instance != null) ? PlayerStats.Instance.WorkbenchTier : 1;

        foreach (var recipe in recipes)
        {
            bool showRecipe = false;

            switch (currentTabIndex)
            {
                case 0: showRecipe = true; break;
                case 1: showRecipe = (recipe.category == ItemCategory.Resource); break;
                case 2: showRecipe = (recipe.category == ItemCategory.Tool); break;
                case 3: showRecipe = (recipe.category == ItemCategory.Weapon); break;
                case 4: showRecipe = (recipe.category == ItemCategory.Armor); break;
                case 5: showRecipe = (recipe.category == ItemCategory.Food); break;
                case 6: showRecipe = (recipe.category == ItemCategory.Ammo); break;
                case 7: showRecipe = (recipe.category == ItemCategory.Stations); break;
            }

            if (showRecipe)
            {
                GameObject obj = Instantiate(soulItemPrefab, contentContainer);
                SoulItemUI ui = obj.GetComponent<SoulItemUI>();
                
                if (ui != null)
                {
                    bool isLocked = recipe.requiredWorkbenchTier > playerTier;
                    ui.Setup(recipe, isLocked); 
                }
            }
        }
    }
}