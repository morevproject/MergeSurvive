using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private string savePath;
    public SaveData currentSave;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, "gamesave.json");
            LoadFileOnly();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyLoadData();
    }

    // --- АВТОСОХРАНЕНИЕ ПРИ ВЫХОДЕ ---
    private void OnApplicationQuit()
    {
        SaveGame();
        Debug.Log("[SaveManager] Игра закрывается. Прогресс сохранен.");
    }

    public void SaveGame()
    {
        // 1. Сохраняем Статы Игрока (ХП + Шестерни + Тир + ЛАМПОЧКИ)
        if (PlayerStats.Instance != null)
        {
            currentSave.currentHealth = PlayerStats.Instance.currentHealth;
            currentSave.gearsCount = PlayerStats.Instance.Gears;
            currentSave.workbenchTier = PlayerStats.Instance.WorkbenchTier;
            // Сохраняем валюту напрямую из статов
            currentSave.hintBulbsCount = PlayerStats.Instance.HintBulbs;
        }
        
        // 2. Сохраняем Инвентарь
        currentSave.inventory.Clear();
        if (InventoryManager.Instance != null) 
        {
            foreach (var slot in InventoryManager.Instance.inventorySlots)
            {
                if (slot.itemData != null)
                {
                    currentSave.inventory.Add(new InventorySaveData(slot.itemData.ItemName, slot.stackSize));
                }
            }
        }

        // 3. Сохраняем Изученные Рецепты (Души)
        currentSave.unlockedRecipeIDs.Clear();
        if (CraftingManager.Instance != null)
        {
            foreach (var recipe in CraftingManager.Instance.GetDiscoveredRecipes())
            {
                if (!string.IsNullOrEmpty(recipe.outputResourceName))
                {
                    currentSave.unlockedRecipeIDs.Add(recipe.outputResourceName);
                }
            }
        }
        
        // Запись на диск
        string json = JsonUtility.ToJson(currentSave, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Game Saved! Gears: {currentSave.gearsCount}, Bulbs: {currentSave.hintBulbsCount}");
    }

    private void LoadFileOnly()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            currentSave = JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            currentSave = new SaveData();
        }
    }
    
    public void LoadGame()
    {
        LoadFileOnly();
        ApplyLoadData();
    }

    public void ApplyLoadData()
    {
        // 1. Загружаем Статы (ХП + Шестерни + Тир + Лампочки)
        if (PlayerStats.Instance != null)
        {
            // Передаем лампочки 4-м параметром
            PlayerStats.Instance.LoadProgress(
                currentSave.currentHealth, 
                currentSave.gearsCount, 
                currentSave.workbenchTier,
                currentSave.hintBulbsCount 
            );
        }

        // 2. Инвентарь
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ClearInventory();
            foreach (var savedItem in currentSave.inventory)
            {
                ItemData item = Resources.Load<ItemData>($"Items/{savedItem.itemID}");
                if (item != null)
                {
                    InventoryManager.Instance.AddItem(item, savedItem.amount);
                }
            }
        }

        // 3. Рецепты (Души)
        if (CraftingManager.Instance != null)
        {
            CraftingManager.Instance.RestoreUnlockedRecipes(currentSave.unlockedRecipeIDs);
        }
    }

    public void RespawnWorldKeepInventory()
    {
        SaveGame();
        
        currentSave.destroyedObjectIDs.Clear();
        if (currentSave.currentHealth <= 0) currentSave.currentHealth = 100;

        string json = JsonUtility.ToJson(currentSave, true);
        File.WriteAllText(savePath, json);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void DeleteSaveFile()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            currentSave = new SaveData();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public bool IsObjectDestroyed(string id)
    {
        return currentSave.destroyedObjectIDs.Contains(id);
    }

    public void MarkAsDestroyed(string id)
    {
        if (!currentSave.destroyedObjectIDs.Contains(id))
        {
            currentSave.destroyedObjectIDs.Add(id);
        }
    }
}