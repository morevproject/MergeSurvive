using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    // --- ИГРОК ---
    public int currentHealth;
    
    // Инвентарь
    public List<InventorySaveData> inventory = new List<InventorySaveData>();

    public int gearsCount = 0;
    public int workbenchTier = 1;
    public int hintBulbsCount = 0;
    
    // --- ПРОГРЕСС ---
    public List<string> unlockedRecipeIDs = new List<string>();

    // --- МИР ---
    public List<string> destroyedObjectIDs = new List<string>();

    // --- НОВОЕ: ЭКИПИРОВКА ---
    public string equippedWeaponID;
    public string equippedArmorID;
    public string equippedAxeID;
    public string equippedPickaxeID;
    public string equippedShovelID;
    
    public string equippedAmmoID;
    public int equippedAmmoCount; // Для стрел важно помнить количество

    public SaveData()
    {
        currentHealth = 100;
        hintBulbsCount = 0;
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