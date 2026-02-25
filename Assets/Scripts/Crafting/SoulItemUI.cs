using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

// Убрали интерфейсы Drag, оставили только Click
public class SoulItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Image icon; 
    public Text soulNameText;
    public GameObject lockedOverlay; 
    
    [Header("Data")]
    public ItemData itemData; 
    private Recipe myRecipe; 

    private bool isLocked = false;
    private int requiredTier = 1;

    public bool isTutorialLocked = false; // Невидимая блокировка для обучения

    // Ссылка на скрипт перетаскивания (который уже есть у тебя)
    private UIDraggableItem dragComponent;

    private void Awake()
    {
        // Пытаемся найти компонент перетаскивания на этом же объекте
        dragComponent = GetComponent<UIDraggableItem>();
    }
    
    public void Setup(Recipe recipe, bool locked)
    {
        myRecipe = recipe;
        requiredTier = recipe.requiredWorkbenchTier; 
        
        // 1. Настройка UI
        if (icon == null) icon = GetComponent<Image>();
        if (icon != null && recipe.icon != null) icon.sprite = recipe.icon;
        
        if (soulNameText != null) soulNameText.text = recipe.recipeName;

        // 2. Поиск ItemData (для слота)
        FindItemData(recipe);

        // 3. Установка замка
        SetLocked(locked);
    }

    private void FindItemData(Recipe recipe)
    {
        if (string.IsNullOrEmpty(recipe.outputResourceName)) return;

        // Попытка А
        itemData = Resources.Load<ItemData>("Items/" + recipe.outputResourceName);
        // Попытка Б
        if (itemData == null) itemData = Resources.Load<ItemData>(recipe.outputResourceName);
        // Попытка В (LINQ)
        if (itemData == null)
        {
            var allItems = Resources.LoadAll<ItemData>(""); 
            itemData = allItems.FirstOrDefault(x => x.name == recipe.outputResourceName || x.ItemName == recipe.outputResourceName);
        }
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        
        // Визуал замка
        if (lockedOverlay != null) lockedOverlay.SetActive(locked);
        if (icon != null) icon.color = locked ? Color.gray : Color.white;

        // Если заперто — отключаем компонент перетаскивания!
        if (dragComponent != null)
        {
            dragComponent.enabled = !locked;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isLocked)
        {
            Debug.Log($"<color=red>Рецепт заблокирован! Нужен верстак {requiredTier} уровня.</color>");
            return;
        }

        // --- НОВОЕ: Игнорируем клик, если туториал запрещает ---
        if (isTutorialLocked) return; 

        if (CraftingDetailsUI.Instance != null)
        {
            CraftingDetailsUI.Instance.ShowRecipe(myRecipe);
            
            if (TutorialManager.Instance != null && itemData != null)
            {
                TutorialManager.Instance.OnRecipeClicked(itemData.ItemName);
            }
        }
        else
        {
            Debug.LogError("CraftingDetailsUI не найден на сцене! Проверь GameManager.");
        }
    }

    public void SetTutorialState(bool isInteractive)
    {
        isTutorialLocked = !isInteractive;
        
        // Отключаем возможность перетаскивания, но ВИЗУАЛ (замок/цвет) не трогаем!
        if (dragComponent != null)
        {
            dragComponent.enabled = isInteractive && !isLocked;
        }
    }
}