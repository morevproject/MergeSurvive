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
    // --------------------------------

    public void SaveGame()
    {
        // 1. Сохраняем ХП
        if (PlayerStats.Instance != null)
        {
            currentSave.currentHealth = PlayerStats.Instance.currentHealth;
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
                // Сохраняем уникальное имя ресурса, который получается из рецепта
                // (Это надежнее, чем имя файла)
                if (!string.IsNullOrEmpty(recipe.outputResourceName))
                {
                    currentSave.unlockedRecipeIDs.Add(recipe.outputResourceName);
                }
            }
        }
        
        // Запись на диск
        string json = JsonUtility.ToJson(currentSave, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Game Saved!");
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
        // 1. ХП
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.currentHealth = currentSave.currentHealth;
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
            // Передаем список строк в менеджер крафта, пусть сам ищет и открывает
            CraftingManager.Instance.RestoreUnlockedRecipes(currentSave.unlockedRecipeIDs);
        }
    }

    public void RespawnWorldKeepInventory()
    {
        SaveGame();
        currentSave.destroyedObjectIDs.Clear();
        
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

    // Проверка: уничтожен ли объект с таким ID?
    public bool IsObjectDestroyed(string id)
    {
        return currentSave.destroyedObjectIDs.Contains(id);
    }

    // Действие: запомнить, что объект уничтожен
    public void MarkAsDestroyed(string id)
    {
        if (!currentSave.destroyedObjectIDs.Contains(id))
        {
            currentSave.destroyedObjectIDs.Add(id);
            // Можно сразу сохранять, если боишься вылетов, 
            // но обычно хватает автосейва при выходе.
        }
    }
}