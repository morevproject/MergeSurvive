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

    private Recipe currentRecipe;
    
    // --- ДЛЯ ДИНАМИЧЕСКОГО ОБНОВЛЕНИЯ ---
    private List<CraftingIngredientUI> spawnedIngredients = new List<CraftingIngredientUI>();
    private float updateTimer = 0f;

    private void Awake()
    {
        Instance = this;
        if (detailsWindow != null) detailsWindow.SetActive(false);

        // Авто-подписка на кнопки
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
        // Проверяем инвентарь 2 раза в секунду, пока открыто окно
        if (detailsWindow != null && detailsWindow.activeInHierarchy && currentRecipe != null)
        {
            updateTimer += Time.deltaTime;
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

        // 1. Заполняем инфо
        ItemData resultItem = Resources.Load<ItemData>("Items/" + recipe.outputResourceName);
        if (resultItem == null) resultItem = Resources.Load<ItemData>(recipe.outputResourceName);

        if (resultItem != null)
        {
            if (resultIcon != null) resultIcon.sprite = resultItem.Icon;
            if (resultNameText != null) resultNameText.text = resultItem.GetName();
            if (resultDescriptionText != null) resultDescriptionText.text = resultItem.Description;
        }
        else
        {
            if (resultNameText != null) resultNameText.text = recipe.recipeName;
        }

        // 2. ВАЖНАЯ ПРОВЕРКА: Можно ли вообще это скрафтить?
        bool isCraftable = !recipe.isBaseResource && recipe.requiredIngredients.Count > 0;

        if (createButton != null)
        {
            // Прячем кнопку совсем, если это нельзя скрафтить
            createButton.gameObject.SetActive(isCraftable);
        }

        // 3. Генерируем список
        RefreshIngredientsList();
    }

    private void RefreshIngredientsList()
    {
        if (ingredientsContainer == null || ingredientPrefab == null || currentRecipe == null) return;

        // Чистим старый список
        foreach (Transform child in ingredientsContainer) 
        {
            // МГНОВЕННО отрываем объект от родителя, чтобы он не мешал пересчету размеров!
            child.SetParent(null); 
            Destroy(child.gameObject);
        }
        spawnedIngredients.Clear(); // Очищаем список ссылок

        bool allIngredientsAvailable = true;

        // Создаем строчки
        foreach (var ingredient in currentRecipe.requiredIngredients)
        {
            GameObject obj = Instantiate(ingredientPrefab, ingredientsContainer);
            
            // СБРОС МАСШТАБА (важно для UI)
            obj.transform.localScale = Vector3.one;
            
            // --- ФИКС БАГА: ОТКЛЮЧАЕМ ИНТЕРАКТИВНОСТЬ СЛОТА ---
            var draggable = obj.GetComponent<UIDraggableItem>();
            if (draggable != null) draggable.enabled = false;

            var button = obj.GetComponent<Button>();
            if (button != null) button.enabled = false;
            // ----------------------------------------------------
            
            var ingUI = obj.GetComponent<CraftingIngredientUI>();
            if (ingUI != null)
            {
                int currentAmount = InventoryManager.Instance.GetTotalAmountByName(ingredient.resourceName);
                ingUI.Setup(ingredient.resourceName, currentAmount, ingredient.amount);
                
                spawnedIngredients.Add(ingUI); // Добавляем в список для Update

                if (currentAmount < ingredient.amount) allIngredientsAvailable = false;
            }
        }

        // 4. Обновляем состояние кнопки
        UpdateCreateButtonState(allIngredientsAvailable);

        // === ВЫЗЫВАЕМ УЛЬТИМАТИВНЫЙ ФИКС ===
        RectTransform contentRect = ingredientsContainer.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            StartCoroutine(ResetScrollRoutine(contentRect));
        }
    }

    // === ДОБАВЛЯЕМ НОВЫЙ МЕТОД-КОРУТИНУ ===
    private System.Collections.IEnumerator ResetScrollRoutine(RectTransform contentRect)
    {
        // 1. Ждем, пока Unity прожует все Layout Groups, маски и нарисует кадр
        yield return new WaitForEndOfFrame();

        // 2. Принудительно обновляем все холсты
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        // 3. И вот теперь, когда Unity успокоилась, жестко прибиваем скролл к верху!
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

            // Запрашиваем свежее количество из инвентаря
            int currentAmount = InventoryManager.Instance.GetTotalAmountByName(ingUI.ResourceName);
            
            // Обновляем текст и цвет
            ingUI.UpdateCurrentAmount(currentAmount);

            if (currentAmount < ingUI.RequiredAmount)
            {
                allIngredientsAvailable = false;
            }
        }

        // Обновляем саму кнопку
        UpdateCreateButtonState(allIngredientsAvailable);
    }

    private void UpdateCreateButtonState(bool isAvailable)
    {
        if (createButton != null && createButton.gameObject.activeSelf)
        {
            createButton.interactable = isAvailable;
            
            // Меняем прозрачность текста кнопки для красоты
            var btnText = createButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.alpha = isAvailable ? 1f : 0.5f;
            }
        }
    }

    public void OnCreateButtonClicked()
    {
        if (currentRecipe == null) return;

        // Финальная проверка перед списанием
        if (InventoryManager.Instance.HasResources(currentRecipe.requiredIngredients))
        {
            // 1. Забираем ресурсы
            InventoryManager.Instance.ConsumeResources(currentRecipe.requiredIngredients);

            // 2. Добавляем в очередь
            if (WorldCraftingQueue.Instance != null)
            {
                WorldCraftingQueue.Instance.AddToQueue(currentRecipe);
            }
            else
            {
                // Fallback
                ItemData resultItem = Resources.Load<ItemData>("Items/" + currentRecipe.outputResourceName);
                InventoryManager.Instance.AddItem(resultItem, currentRecipe.outputAmount);
            }

            // 3. НЕ ЗАКРЫВАЕМ ОКНО, А ОБНОВЛЯЕМ ЦИФРЫ
            RefreshIngredientsList();
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
        spawnedIngredients.Clear(); // Чистим список при закрытии
    }
}