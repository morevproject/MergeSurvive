using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class CraftingPanel : MonoBehaviour
{
    public static CraftingPanel Instance; 

    [Header("Core References")]
    public CraftingSlot[] slots;          
    public CraftingResultSlot resultSlot; 

    [Header("Action UI")]
    public GameObject infoPanel;      
    public Text selectedNameText;     
    public Button createButton;       
    public Text createButtonText;     

    [Header("Notifications")]
    public GameObject warningPanel;   
    public Text warningText;          

    public bool IsCrafting { get; private set; } = false; 
    private Recipe selectedRecipe; 

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (infoPanel != null) infoPanel.SetActive(false);
        if (warningPanel != null) warningPanel.SetActive(false);
        
        if (createButton != null)
        {
            createButton.onClick.RemoveAllListeners();
            createButton.onClick.AddListener(OnActionClicked);
        }
    }
    
    // При выключении панели сбрасываем фильтр в инвентаре
    /*void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ResetFilter();
        }
    }*/

    // =========================================================
    // ЛОГИКА ВЫБОРА И СОЗДАНИЯ
    // =========================================================

    public void SelectRecipe(Recipe recipe)
    {
        if (IsCrafting) return;

        selectedRecipe = recipe;
        
        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
            if (selectedNameText != null) selectedNameText.text = recipe.recipeName;
            
            if (createButtonText != null)
            {
                createButtonText.text = recipe.isBaseResource ? "Найти" : "Создать";
            }
        }
    }

    void OnActionClicked()
    {
        if (selectedRecipe == null) return;

        if (selectedRecipe.isBaseResource)
        {
            // Поиск
            if (ResourceFinder.Instance != null)
            {
                ResourceFinder.Instance.FindResourceNodes(selectedRecipe.outputResourceName);
            }
            else Debug.LogError("ResourceFinder не найден!");
        }
        else
        {
            // Крафт
            TryCraftItem();
        }
    }

    void TryCraftItem()
    {
        if (InventoryManager.Instance == null) return;

        // 1. Проверяем, есть ли ресурсы
        bool hasEnough = InventoryManager.Instance.HasResources(selectedRecipe.requiredIngredients);

        if (hasEnough)
        {
            // А. РЕСУРСОВ ХВАТАЕТ
            
            // 1. Списываем ресурсы
            InventoryManager.Instance.ConsumeResources(selectedRecipe.requiredIngredients);
            
            // 2. Добавляем в очередь (таймер на верстаке)
            if (WorldCraftingQueue.Instance != null)
            {
                WorldCraftingQueue.Instance.AddToQueue(selectedRecipe);
            }
            else
            {
                // Fallback: Если очереди нет, выдаем сразу
                Debug.LogWarning("Очередь крафта не найдена, выдаю сразу.");
                ItemData res = Resources.Load<ItemData>("Items/" + selectedRecipe.outputResourceName);
                if (res == null) res = Resources.Load<ItemData>(selectedRecipe.outputResourceName);
                InventoryManager.Instance.AddItem(res, selectedRecipe.outputAmount);
            }
            
            // Верстак НЕ закрываем, чтобы игрок мог крафтить дальше
            Debug.Log($"Крафт запущен: {selectedRecipe.recipeName}");
        }
        else
        {
            // Б. НЕ ХВАТАЕТ РЕСУРСОВ
            
            Debug.Log("Не хватает ресурсов! Переключаюсь на Инвентарь.");
            
            // 1. Показываем в инвентаре только нужные ресурсы (с пульсацией)
            InventoryManager.Instance.ShowMissingResourcesMode(selectedRecipe.requiredIngredients);
            
            // 2. Закрываем Верстак
            gameObject.SetActive(false);
        }
    }

    // =========================================================
    // ЛОГИКА ИССЛЕДОВАНИЯ (MERGE)
    // =========================================================

    public void CheckCrafting()
    {
        if (IsCrafting) return;

        List<ItemData> collectedItems = new List<ItemData>();
        bool hasItems = false;

        foreach (var slot in slots)
        {
            ItemData item = slot.GetResourceInSlot();
            if (item != null)
            {
                hasItems = true;
                collectedItems.Add(item);
            }
        }

        if (!hasItems)
        {
            if (resultSlot != null) resultSlot.ClearSlot();
            return;
        }

        Recipe foundRecipe = CraftingManager.Instance.TryMergeItems(collectedItems);
    
        if (foundRecipe != null)
        {
            StartCoroutine(ProcessDiscovery(foundRecipe));
        }
        else
        {
            if (resultSlot != null) resultSlot.ClearSlot();
        }
    }

    IEnumerator ProcessDiscovery(Recipe recipe)
    {
        IsCrafting = true;

        if (resultSlot != null) resultSlot.SetResult(recipe); 
        
        yield return new WaitForSeconds(1.0f);

        int currentWbTier = CraftingManager.Instance.currentWorkbenchTier;
        int recipeTier = (int)recipe.itemTier;

        if (recipeTier > currentWbTier + 1)
        {
            int neededTier = recipeTier - 1; 
            if (neededTier < 0) neededTier = 0;
            StartCoroutine(ShowWarning($"Требуется Верстак Ур. {neededTier}"));
        }
        else
        {
            CraftingManager.Instance.DiscoverRecipe(recipe);
        }

        foreach (var slot in slots) slot.ClearSlot(); 
        if (resultSlot != null) resultSlot.ClearSlot(); 

        IsCrafting = false;
        CheckCrafting(); 
    }

    IEnumerator ShowWarning(string message)
    {
        if (warningPanel != null && warningText != null)
        {
            warningText.text = message;
            warningPanel.SetActive(true);
            yield return new WaitForSeconds(2.0f);
            warningPanel.SetActive(false);
        }
    }
}