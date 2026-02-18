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

    [Header("List Settings")]
    public Transform contentContainer; 
    public GameObject soulItemPrefab;  

    // Текущая вкладка (0..6)
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

        RefreshUI();
    }
    
    // МЕТОД ДЛЯ КНОПОК
    public void SetTab(int tabIndex)
    {
        currentTabIndex = tabIndex;
        // Debug.Log($"[UI] Вкладка: {tabIndex}");
        RefreshSoulList();
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

        // Чистим список
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        if (CraftingManager.Instance == null) return;

        var recipes = CraftingManager.Instance.GetDiscoveredRecipes();
        int playerTier = (PlayerStats.Instance != null) ? PlayerStats.Instance.WorkbenchTier : 1;

        foreach (var recipe in recipes)
        {
            bool showRecipe = false;

            // --- ЛОГИКА 7 ВКЛАДОК ---
            switch (currentTabIndex)
            {
                case 0: // Всё
                    showRecipe = true;
                    break;
                case 1: // Ресурсы
                    showRecipe = (recipe.category == ItemCategory.Resource);
                    break;
                case 2: // Инструменты
                    showRecipe = (recipe.category == ItemCategory.Tool);
                    break;
                case 3: // Оружие
                    showRecipe = (recipe.category == ItemCategory.Weapon);
                    break;
                case 4: // Броня
                    showRecipe = (recipe.category == ItemCategory.Armor);
                    break;
                case 5: // Еда (Food)
                    showRecipe = (recipe.category == ItemCategory.Food);
                    break;
                case 6: // Боеприпасы (Ammo)
                    showRecipe = (recipe.category == ItemCategory.Ammo);
                    break;
                case 7: // Станции (Stations)
                    showRecipe = (recipe.category == ItemCategory.Stations);
                    break;
            }
            // ------------------------

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