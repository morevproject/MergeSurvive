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
            if (ResourceFinder.Instance != null)
            {
                ResourceFinder.Instance.FindResourceNodes(selectedRecipe.outputResourceName);
            }
            else Debug.LogError("ResourceFinder не найден!");
        }
        else
        {
            TryCraftItem();
        }
    }

    void TryCraftItem()
    {
        if (InventoryManager.Instance == null) return;

        bool hasEnough = InventoryManager.Instance.HasResources(selectedRecipe.requiredIngredients);

        if (hasEnough)
        {
            InventoryManager.Instance.ConsumeResources(selectedRecipe.requiredIngredients);
            
            if (WorldCraftingQueue.Instance != null)
            {
                WorldCraftingQueue.Instance.AddToQueue(selectedRecipe);
            }
            else
            {
                Debug.LogWarning("Очередь крафта не найдена, выдаю сразу.");
                ItemData res = Resources.Load<ItemData>("Items/" + selectedRecipe.outputResourceName);
                if (res == null) res = Resources.Load<ItemData>(selectedRecipe.outputResourceName);
                InventoryManager.Instance.AddItem(res, selectedRecipe.outputAmount);
            }
            Debug.Log($"Крафт запущен: {selectedRecipe.recipeName}");
        }
        else
        {
            Debug.Log("Не хватает ресурсов! Переключаюсь на Инвентарь.");
            InventoryManager.Instance.ShowMissingResourcesMode(selectedRecipe.requiredIngredients);
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
        
        // ВЕРНУЛИ ОБЫЧНЫЙ ТАЙМЕР (так как время теперь идет)
        yield return new WaitForSeconds(1.0f); 

        // Очистка и логика (оставляем твой безопасный вариант с try-catch или обычный)
        foreach (var slot in slots) slot.ClearSlot(); 
        if (resultSlot != null) resultSlot.ClearSlot();

        // Теперь безопасно пытаемся добавить рецепт
        try 
        {
            if (CraftingManager.Instance != null)
            {
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
                    // Если тут произойдет ошибка (например в SoulListUI), верстак уже чист
                    CraftingManager.Instance.DiscoverRecipe(recipe);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("ОШИБКА ПРИ ОТКРЫТИИ РЕЦЕПТА: " + e.Message);
        }

        IsCrafting = false;
        
        // Проверяем, не положил ли игрок что-то новое
        CheckCrafting(); 
    }

    IEnumerator ShowWarning(string message)
    {
        if (warningPanel != null && warningText != null)
        {
            warningText.text = message;
            warningPanel.SetActive(true);
            // Тут тоже используем Realtime
            yield return new WaitForSecondsRealtime(2.0f);
            warningPanel.SetActive(false);
        }
    }
}