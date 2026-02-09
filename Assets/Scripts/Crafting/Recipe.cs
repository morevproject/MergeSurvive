using UnityEngine;
using System.Collections.Generic;

// Если ты НЕ создавал отдельный файл GameEnums.cs, раскомментируй эти строки:
/*
public enum ItemTier
{
    Tier0_Basic = 0,
    Tier1_Improved = 1,
    Tier2_Advanced = 2,
    Tier3_Master = 3
}

public enum ItemCategory
{
    Resource,
    Tool,
    Weapon,
    Armor
}
*/

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Merge & Survive/Recipe")]
public class Recipe : ScriptableObject
{
    [Header("Recipe Identity")]
    public string recipeID;
    public string recipeName;
    [TextArea] public string description;
    public Sprite icon;

    // --- НОВЫЕ ПОЛЯ (TIER & CATEGORY) ---
    [Header("Settings & Progression")]
    public ItemTier itemTier;       // Уровень предмета (0, 1, 2...)
    public ItemCategory category;   // Категория (Ресурс, Инструмент...)
    public int requiredWorkbenchTier = 1; // 1 = доступно сразу
    // -------------------------------------

    [Header("Merge Pattern")]
    public List<RecipeIngredient> requiredIngredients;
    // Порядок ингредиентов не важен.

    [Header("Output")]
    public string outputResourceName; // ID результата (например, Stone_Axe)
    public int outputAmount = 1;
    public GameObject outputPrefab;   // Опционально: префаб для спавна в мире

    [Header("Discovery")]
    public bool isBaseResource = false; // Изначально открыт (камень, дерево)
    public bool isDiscovered = false;   // Открыт ли рецепт игроком

    [System.Serializable]
    public class RecipeIngredient
    {
        public string resourceName;
        public int amount = 1;
    }

    // Твой оригинальный, надежный метод проверки совпадений
    public bool Matches(Dictionary<string, int> ingredientsInSlots)
    {
        // 1. Проверяем, что все нужные ингредиенты присутствуют в нужном количестве
        foreach (var required in requiredIngredients)
        {
            if (!ingredientsInSlots.ContainsKey(required.resourceName) ||
                ingredientsInSlots[required.resourceName] < required.amount)
            {
                return false; // Какого-то ингредиента не хватает
            }
        }

        // 2. ВАЖНО: Проверяем, что нет ЛИШНИХ ингредиентов в слотах.
        // Сравниваем общее количество предметов в рецепте и в слотах.
        int totalRequiredItems = 0;
        foreach (var ing in requiredIngredients) totalRequiredItems += ing.amount;

        int totalItemsInSlots = 0;
        foreach (var slotItem in ingredientsInSlots) totalItemsInSlots += slotItem.Value;

        // Если количество предметов не совпадает - есть лишние или не все нужные
        if (totalItemsInSlots != totalRequiredItems)
        {
            return false;
        }

        // 3. Дополнительная проверка на "посторонние имена"
        foreach (var slotItem in ingredientsInSlots)
        {
            bool found = false;
            foreach (var required in requiredIngredients)
            {
                if (required.resourceName == slotItem.Key)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false; // Нашли ресурс в слотах, которого нет в рецепте!
        }

        return true; // Всё идеально совпало!
    }
}