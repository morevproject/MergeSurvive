using UnityEngine;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance;

    [Header("Настройки")]
    public int currentWorkbenchTier = 0;

    [Header("База данных")]
    public List<Recipe> allRecipes = new List<Recipe>();
    public List<Recipe> startingRecipes = new List<Recipe>();

    private List<Recipe> discoveredRecipes = new List<Recipe>();

    public delegate void RecipeAction(Recipe recipe);
    public static event RecipeAction OnRecipeDiscovered;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        discoveredRecipes.Clear();
    }

    void Start()
    {
        // 1. ВСЕГДА пробуем загрузить рецепты из папки Resources/Recipes
        var loaded = Resources.LoadAll<Recipe>("Recipes");
        
        if (loaded.Length > 0)
        {
            int addedCount = 0;
            foreach (var newRecipe in loaded)
            {
                // Добавляем только если такого еще нет в списке
                if (!allRecipes.Contains(newRecipe))
                {
                    allRecipes.Add(newRecipe);
                    addedCount++;
                }
            }
            Debug.Log($"[CraftingManager] Всего рецептов: {allRecipes.Count}. Из них новых загружено: {addedCount}");
        }
        else
        {
            Debug.LogError("[CraftingManager] В папке Resources/Recipes пусто или папка не найдена!");
        }

        // 2. Открытие стартовых рецептов
        foreach (var recipe in startingRecipes)
        {
            DiscoverRecipe(recipe);
        }
    }

    // =========================================================
    // ЛОГИКА ОТКРЫТИЯ (CheckUnlocks)
    // =========================================================

    public void CheckUnlocks(ItemData item)
    {
        if (item == null) return;

        foreach (var recipe in allRecipes)
        {
            if (discoveredRecipes.Contains(recipe)) continue;

            if (recipe.outputResourceName == item.ItemName)
            {
                if (recipe.requiredIngredients.Count == 0 || recipe.isBaseResource)
                {
                     DiscoverRecipe(recipe);
                }
            }
        }
    }

    public void DiscoverRecipe(Recipe recipe)
    {
        if (recipe == null) return;

        if (!discoveredRecipes.Contains(recipe))
        {
            discoveredRecipes.Add(recipe);
            OnRecipeDiscovered?.Invoke(recipe);
            Debug.Log($"[CraftingManager] Изучен рецепт: {recipe.recipeName}");
        }
    }

    public List<Recipe> GetDiscoveredRecipes()
    {
        return discoveredRecipes;
    }

    // =========================================================
    // ЛОГИКА КРАФТА (TryMergeItems) - ОБНОВЛЕНА
    // =========================================================

    public Recipe TryMergeItems(List<ItemData> ingredientsInSlots)
    {
        ingredientsInSlots.RemoveAll(x => x == null);
        
        if (ingredientsInSlots.Count == 0) return null;

        // Собираем имена предметов в слотах для лога
        string contents = "";
        foreach(var item in ingredientsInSlots) contents += item.ItemName + ", ";
        // Debug.Log($"[TryMerge] Проверяю слоты: {contents}");

        foreach (var recipe in allRecipes)
        {
            // 1. ФИКС ЖАДНОСТИ: Если рецепт уже открыт - пропускаем его!
            // Это позволит собирать сложные рецепты (Кирка), игнорируя простые (Топор/Цемент),
            // части которых уже лежат на столе.
            if (discoveredRecipes.Contains(recipe)) 
            {
                // Debug.Log($"[TryMerge] Пропускаем {recipe.recipeName}, так как он уже изучен.");
                continue; 
            }

            // 2. Проверка количества
            int requiredCount = 0;
            foreach (var ing in recipe.requiredIngredients) requiredCount += ing.amount;

            // Если количество предметов на столе не совпадает с рецептом - сразу нет
            if (ingredientsInSlots.Count != requiredCount) continue;

            // 3. Проверка состава
            List<string> tempRecipeIngredients = new List<string>();
            foreach (var ing in recipe.requiredIngredients)
            {
                for (int i = 0; i < ing.amount; i++) tempRecipeIngredients.Add(ing.resourceName);
            }

            bool match = true;
            foreach (var inputItem in ingredientsInSlots)
            {
                if (tempRecipeIngredients.Contains(inputItem.ItemName))
                {
                    tempRecipeIngredients.Remove(inputItem.ItemName);
                }
                else
                {
                    match = false; 
                    break;
                }
            }

            if (match && tempRecipeIngredients.Count == 0)
            {
                Debug.Log($"[TryMerge] УСПЕХ! Найден новый рецепт: {recipe.recipeName}");
                return recipe;
            }
        }

        // Если дошли сюда - значит, ничего нового не собрали
        return null;
    }

    public Recipe GetRecipeByOutputName(string resourceName)
    {
        foreach (var recipe in allRecipes)
        {
            if (recipe.outputResourceName == resourceName)
                return recipe;
        }
        return null;
    }
}