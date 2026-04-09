using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CraftingDetailsUI : MonoBehaviour
{
    public static CraftingDetailsUI Instance;

    [Header("UI Panels")]
    public GameObject detailsWindow; 
    public Transform ingredientsContainer; 
    public GameObject ingredientPrefab; 

    [Header("Info")]
    public Image resultIcon;
    public TextMeshProUGUI resultNameText;
    public TextMeshProUGUI resultDescriptionText;

    [Header("Buttons")]
    public Button createButton;
    public Button closeButton;
    public Button backgroundCloseButton; 
    
    public Button clearSlotsButton; 

    private Recipe currentRecipe;
    
    private List<CraftingIngredientUI> spawnedIngredients = new List<CraftingIngredientUI>();
    private float updateTimer = 0f;

    private bool isWorkbenchTierMet = true;

    private void Awake()
    {
        Instance = this;
        if (detailsWindow != null) detailsWindow.SetActive(false);

        if (createButton != null) 
        {
            createButton.onClick.RemoveAllListeners();
            createButton.onClick.AddListener(OnCreateButtonClicked);
        }
        
        if (closeButton != null) closeButton.onClick.AddListener(CloseWindow);
        if (backgroundCloseButton != null) backgroundCloseButton.onClick.AddListener(CloseWindow);
    }

    private void Update()
    {
        if (detailsWindow != null && detailsWindow.activeInHierarchy && currentRecipe != null)
        {
            updateTimer += Time.deltaTime;
            // Проверяем каждые 0.5 сек (в том числе и место в очереди!)
            if (updateTimer >= 0.5f) 
            {
                updateTimer = 0f;
                UpdateDynamicAmounts();
            }
        }
    }

    public void ShowRecipe(Recipe recipe)
 {
     if (recipe == null) return;
     currentRecipe = recipe;

     if (detailsWindow != null) detailsWindow.SetActive(true);

     ItemData resultItem = Resources.Load<ItemData>("Items/" + recipe.outputResourceName);
     if (resultItem == null) resultItem = Resources.Load<ItemData>(recipe.outputResourceName);

     if (resultItem != null)
     {
         if (resultIcon != null) resultIcon.sprite = resultItem.Icon;
         if (resultNameText != null) resultNameText.text = resultItem.GetName();

         // Задаем базовое описание
         if (resultDescriptionText != null) resultDescriptionText.text = resultItem.Description;
     }
     else
     {
         if (resultNameText != null) resultNameText.text = recipe.recipeName;
     }

     // === НОВАЯ ЛОГИКА: ПРОВЕРКА ВЕРСТАКА ===
     // ВНИМАНИЕ: Я использую PlayerPrefs для примера. 
     // Замени "WorkbenchTier" на ту переменную, где ты реально хранишь уровень верстака!
     int currentWorkbenchTier = PlayerPrefs.GetInt("WorkbenchTier", 1); 
     isWorkbenchTierMet = currentWorkbenchTier >= recipe.requiredWorkbenchTier;

     if (!isWorkbenchTierMet)
     {
         // Если верстак слабый, просто приклеиваем красное предупреждение в конец описания
         if (resultDescriptionText != null) 
         {
             resultDescriptionText.text += $"\n\n<color=red><b>Необходим верстак {recipe.requiredWorkbenchTier} уровня</b></color>";
         }
     }
     // =======================================

     bool isCraftable = !recipe.isBaseResource && recipe.requiredIngredients.Count > 0;

     if (createButton != null)
     {
         createButton.gameObject.SetActive(isCraftable);
     }

     RefreshIngredientsList();
 }

    private void RefreshIngredientsList()
    {
        if (ingredientsContainer == null || ingredientPrefab == null || currentRecipe == null) return;

        for (int i = ingredientsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = ingredientsContainer.GetChild(i);
            child.SetParent(null); 
            Destroy(child.gameObject);
        }
        spawnedIngredients.Clear(); 

        bool allIngredientsAvailable = true;

        foreach (var ingredient in currentRecipe.requiredIngredients)
        {
            GameObject obj = Instantiate(ingredientPrefab, ingredientsContainer);
            
            obj.transform.localScale = Vector3.one;
            
            var draggable = obj.GetComponent<UIDraggableItem>();
            if (draggable != null) draggable.enabled = false;

            var button = obj.GetComponent<Button>();
            if (button != null) button.enabled = false;
            
            var ingUI = obj.GetComponent<CraftingIngredientUI>();
            if (ingUI != null)
            {
                int currentAmount = InventoryManager.Instance.GetTotalAmountByName(ingredient.resourceName);
                ingUI.Setup(ingredient.resourceName, currentAmount, ingredient.amount);
                
                spawnedIngredients.Add(ingUI); 

                if (currentAmount < ingredient.amount) allIngredientsAvailable = false;
            }
        }

        UpdateCreateButtonState(allIngredientsAvailable);

        RectTransform contentRect = ingredientsContainer.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            StartCoroutine(ResetScrollRoutine(contentRect));
        }
    }

    private System.Collections.IEnumerator ResetScrollRoutine(RectTransform contentRect)
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        UnityEngine.UI.ScrollRect scroll = contentRect.GetComponentInParent<UnityEngine.UI.ScrollRect>();
        if (scroll != null) 
        {
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    private void UpdateDynamicAmounts()
    {
        bool allIngredientsAvailable = true;

        foreach (var ingUI in spawnedIngredients)
        {
            if (ingUI == null) continue;

            int currentAmount = InventoryManager.Instance.GetTotalAmountByName(ingUI.ResourceName);
            ingUI.UpdateCurrentAmount(currentAmount);

            if (currentAmount < ingUI.RequiredAmount)
            {
                allIngredientsAvailable = false;
            }
        }

        UpdateCreateButtonState(allIngredientsAvailable);
    }

    // === ИЗМЕНЕННЫЙ МЕТОД: УМНАЯ ПРОВЕРКА КНОПКИ ===
    private void UpdateCreateButtonState(bool isAvailable)
    {
         if (createButton != null && createButton.gameObject.activeSelf)
         {
             // Спрашиваем Очередь: "Есть ли у тебя свободные места?"
             bool hasQueueSpace = true;
             if (WorldCraftingQueue.Instance != null)
             {
                 hasQueueSpace = WorldCraftingQueue.Instance.craftingQueue.Count < WorldCraftingQueue.Instance.maxQueueSize;
             }

             // === ДОБАВИЛИ isWorkbenchTierMet В ПРОВЕРКУ ===
             // Кнопка активна ТОЛЬКО если есть и ресурсы, и место в очереди, и верстак нужного уровня!
             bool finalInteractable = isAvailable && hasQueueSpace && isWorkbenchTierMet;

             createButton.interactable = finalInteractable;

             var btnText = createButton.GetComponentInChildren<TextMeshProUGUI>();
             if (btnText != null)
             {
                 btnText.alpha = finalInteractable ? 1f : 0.5f;
             }
         }
    }

    public void OnCreateButtonClicked()
    {
        if (currentRecipe == null) return;

        // === ЖЕСТКАЯ ПРОВЕРКА (АНТИ-СПИДРАН) ===
        // Если очередь полна - отрубаем функцию ДО того, как спишутся ресурсы!
        if (WorldCraftingQueue.Instance != null && WorldCraftingQueue.Instance.craftingQueue.Count >= WorldCraftingQueue.Instance.maxQueueSize)
        {
            Debug.LogWarning("Очередь крафта переполнена! Ресурсы сохранены.");
            return;
        }

        if (InventoryManager.Instance.HasResources(currentRecipe.requiredIngredients))
        {
            InventoryManager.Instance.ConsumeResources(currentRecipe.requiredIngredients);

            if (WorldCraftingQueue.Instance != null)
            {
                WorldCraftingQueue.Instance.AddToQueue(currentRecipe);
            }
            else
            {
                ItemData resultItem = Resources.Load<ItemData>("Items/" + currentRecipe.outputResourceName);
                InventoryManager.Instance.AddItem(resultItem, currentRecipe.outputAmount);
            }

            // Мгновенно обновляем интерфейс, чтобы кнопка сразу погасла, если это был последний слот очереди
            RefreshIngredientsList();

            if (clearSlotsButton != null)
            {
                clearSlotsButton.onClick.Invoke();
            }
        }
        else
        {
            Debug.Log("Не хватает ресурсов!");
        }
    }

    public void CloseWindow()
    {
        if (detailsWindow != null) detailsWindow.SetActive(false);
        currentRecipe = null;
        spawnedIngredients.Clear(); 
    }
}