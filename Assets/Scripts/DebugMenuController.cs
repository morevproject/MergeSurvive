using UnityEngine;
using UnityEngine.UI;

public class DebugMenuController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject debugPanel;
    public InputField itemIdInput;
    public InputField amountInput;

    private bool isVisible = false;

    private void Start() 
    {
        debugPanel.SetActive(false);
        
        // Гарантируем, что курсор виден и свободен при старте
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote)) // Тильда (~)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isVisible = !isVisible;
        debugPanel.SetActive(isVisible);
    }

    // --- КНОПКИ ---

    public void OnGiveItemButton()
    {
        string id = itemIdInput.text;
        int amount = 1;
        int.TryParse(amountInput.text, out amount);

        ItemData item = Resources.Load<ItemData>($"Items/{id}");
        
        if (item != null)
        {
            InventoryManager.Instance.AddItem(item, amount);
            Debug.Log($"[DEBUG] Выдано: {item.ItemName} x{amount}");
        }
        else
        {
            Debug.LogError($"[DEBUG] Предмет '{id}' не найден в Resources/Items/! Проверь имя файла.");
        }
    }

    // --- НОВАЯ КНОПКА (ВЫДАТЬ ВСЁ) ---
    public void OnGiveAllItemsButton()
    {
        // Загружаем все ItemData из папки Resources (и подпапок)
        // Если твои предметы лежат строго в Resources/Items, можно написать "Items" вместо ""
        ItemData[] allItems = Resources.LoadAll<ItemData>(""); 
        
        int count = 0;
        foreach (var item in allItems)
        {
            if (item != null)
            {
                InventoryManager.Instance.AddItem(item, 1);
                count++;
            }
        }
        Debug.Log($"[DEBUG] Выдано {count} различных предметов по 1 шт.");
    }

    public void OnRespawnWorldButton()
    {
        Debug.Log("[DEBUG] Респаун мира...");
        SaveManager.Instance.RespawnWorldKeepInventory();
    }

    public void OnWipeSaveButton()
    {
        Debug.Log("[DEBUG] Полный вайп...");
        SaveManager.Instance.DeleteSaveFile();
    }
    
    public void OnSaveButton()
    {
        SaveManager.Instance.SaveGame();
    }
}