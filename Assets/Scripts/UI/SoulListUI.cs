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

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // --- ГЛАВНОЕ ИСПРАВЛЕНИЕ ЗДЕСЬ ---
    private void OnEnable()
    {
        // 1. Подписываемся на событие открытия рецепта
        CraftingManager.OnRecipeDiscovered += OnRecipeDiscoveredHandler;
        
        // 2. Обновляемся при включении (вдруг что-то открыли, пока мы спали)
        RefreshUI();
    }

    private void OnDisable()
    {
        // Отписываемся, чтобы не было ошибок
        CraftingManager.OnRecipeDiscovered -= OnRecipeDiscoveredHandler;
    }

    // Этот метод сработает АВТОМАТИЧЕСКИ, когда CraftingManager скажет "Открыто!"
    private void OnRecipeDiscoveredHandler(Recipe recipe)
    {
        // Debug.Log($"[UI] Ура! Новый рецепт {recipe.recipeName}, обновляю список!");
        RefreshUI();
    }
    // ---------------------------------

    private void Start()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeClicked);

        // На старте тоже обновляем
        RefreshUI();
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

        if (currentGearsText != null) 
            currentGearsText.text = $"{currentGears}"; 

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
            
            if (upgradeCostText != null) 
                upgradeCostText.text = $"Улучшить Верстак за {nextCost}";
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

        // Удаляем старые иконки
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        if (CraftingManager.Instance == null) return;

        var recipes = CraftingManager.Instance.GetDiscoveredRecipes();
        int playerTier = (PlayerStats.Instance != null) ? PlayerStats.Instance.WorkbenchTier : 1;

        foreach (var recipe in recipes)
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