using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    public static event Action OnMergeFailed;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        discoveredRecipes.Clear();
        
        // --- ПЕРЕНЕСЕНО ИЗ START В AWAKE ---
        // Это гарантирует, что база рецептов готова ДО того, как SaveManager начнет загрузку
        var loaded = Resources.LoadAll<Recipe>("Recipes");
        
        if (loaded.Length > 0)
        {
            allRecipes.Clear(); // Чистим, чтобы не дублировать при перезагрузке сцены
            foreach (var newRecipe in loaded)
            {
                if (!allRecipes.Contains(newRecipe))
                {
                    allRecipes.Add(newRecipe);
                }
            }
            // Debug.Log($"[CraftingManager] База загружена. Рецептов: {allRecipes.Count}");
        }
        else
        {
            Debug.LogError("[CraftingManager] В папке Resources/Recipes пусто!");
        }
    }

    void Start()
    {
        // Стартовые рецепты открываем, только если список пуст (новая игра)
        // Если это загрузка, SaveManager сам всё проставит
        if (discoveredRecipes.Count == 0)
        {
            foreach (var recipe in startingRecipes)
            {
                DiscoverRecipe(recipe);
            }
        }
    }

    // =========================================================
    // ЛОГИКА СОХРАНЕНИЙ (НОВОЕ)
    // =========================================================

    // Метод для SaveManager: Восстановить рецепты по списку имен
    public void RestoreUnlockedRecipes(List<string> savedIDs)
    {
        discoveredRecipes.Clear(); // Чистим перед загрузкой
        
        foreach (var id in savedIDs)
        {
            // Ищем рецепт в базе allRecipes, у которого outputResourceName совпадает с id
            Recipe found = allRecipes.FirstOrDefault(r => r.outputResourceName == id);
            
            if (found != null && !discoveredRecipes.Contains(found))
            {
                discoveredRecipes.Add(found);
                // Тут можно вызвать событие обновления UI, если нужно
            }
        }
        Debug.Log($"[CraftingManager] Восстановлено изученных рецептов: {discoveredRecipes.Count}");
    }

    // =========================================================
    // ЛОГИКА ОТКРЫТИЯ
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
    // ЛОГИКА КРАФТА
    // =========================================================

    public Recipe TryMergeItems(List<ItemData> ingredientsInSlots)
    {
        ingredientsInSlots.RemoveAll(x => x == null);
        if (ingredientsInSlots.Count == 0) return null;

        foreach (var recipe in allRecipes)
        {
            if (discoveredRecipes.Contains(recipe)) continue; 

            int requiredCount = 0;
            foreach (var ing in recipe.requiredIngredients) requiredCount += ing.amount;

            if (ingredientsInSlots.Count != requiredCount) continue;

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
        // Сюда мы доходим, если ни один рецепт не подошел
        OnMergeFailed?.Invoke();
        return null;
    }

    public Recipe GetRecipeByOutputName(string resourceName)
    {
        foreach (var recipe in allRecipes)
        {
            if (recipe.outputResourceName == resourceName) return recipe;
        }
        return null;
    }
}