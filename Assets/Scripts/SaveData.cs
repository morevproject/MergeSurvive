using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    // --- ИГРОК ---
    public int currentHealth;
    
    // Инвентарь
    public List<InventorySaveData> inventory = new List<InventorySaveData>();
    
    // --- ПРОГРЕСС (Новое!) ---
    // Список имен предметов (outputResourceName), рецепты которых мы изучили
    public List<string> unlockedRecipeIDs = new List<string>();

    // --- МИР ---
    public List<string> destroyedObjectIDs = new List<string>();

    public SaveData()
    {
        currentHealth = 100;
    }
}

[System.Serializable]
public struct InventorySaveData
{
    public string itemID;
    public int amount;

    public InventorySaveData(string id, int amt)
    {
        itemID = id;
        amount = amt;
    }
}