using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq; // Нужно для умного поиска

public class SoulItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Image icon; 
    public Text soulNameText;
    public GameObject lockedOverlay;
    
    [Header("Data")]
    public ItemData itemData; // Сюда мы пытаемся загрузить данные
    private Recipe myRecipe;
    
    public void Setup(Recipe recipe)
    {
        myRecipe = recipe;
        
        // 1. Настраиваем UI
        if (icon == null) icon = GetComponent<Image>();
        if (icon != null && recipe.icon != null) icon.sprite = recipe.icon;
        
        if (soulNameText != null) soulNameText.text = recipe.recipeName;

        bool isDiscovered = CraftingManager.Instance.GetDiscoveredRecipes().Contains(recipe);
        if (lockedOverlay != null) lockedOverlay.SetActive(!isDiscovered);

        // 2. ЗАГРУЗКА ITEM DATA (Улучшенная версия)
        if (!string.IsNullOrEmpty(recipe.outputResourceName))
        {
            // Попытка А: Прямой поиск в папке Resources/Items
            itemData = Resources.Load<ItemData>("Items/" + recipe.outputResourceName);
            
            // Попытка Б: Поиск просто в корне Resources
            if (itemData == null)
                itemData = Resources.Load<ItemData>(recipe.outputResourceName);

            // Попытка В: УМНЫЙ ПОИСК (Сканируем всё)
            if (itemData == null)
            {
                Debug.LogWarning($"[SoulItemUI] Не нашел '{recipe.outputResourceName}' по прямым путям. Ищу везде...");
                var allItems = Resources.LoadAll<ItemData>(""); // Грузим вообще всё
                
                // Ищем предмет, у которого имя файла ИЛИ поле ItemName совпадает с рецептом
                itemData = allItems.FirstOrDefault(x => x.name == recipe.outputResourceName || x.ItemName == recipe.outputResourceName);
            }

            // ИТОГ
            if (itemData == null)
            {
                Debug.LogError($"[SoulItemUI] КРИТИЧЕСКАЯ ОШИБКА: Не могу найти ItemData для '{recipe.outputResourceName}'! \n" +
                               "1. Убедитесь, что файл предмета лежит в папке Resources (или в любой подпапке внутри Resources).\n" +
                               "2. Убедитесь, что имя файла совпадает с Output Resource Name в рецепте.");
            }
            else
            {
                // Debug.Log($"[SoulItemUI] Успешно загрузил данные для: {itemData.ItemName}");
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CraftingPanel.Instance != null)
        {
            CraftingPanel.Instance.SelectRecipe(myRecipe);
        }
    }
}