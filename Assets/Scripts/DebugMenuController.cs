using UnityEngine;
using UnityEngine.UI;
using System; // ОБЯЗАТЕЛЬНО добавляем для работы с DateTime

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

    public void OnGiveAllItemsButton()
    {
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

    // --- НОВАЯ КНОПКА (МАШИНА ВРЕМЕНИ) ---
    public void OnSkipOneDayButton()
    {
        string lastLoginStr = PlayerPrefs.GetString("Player_LastLoginDate", "");
        
        if (DateTime.TryParse(lastLoginStr, out DateTime lastLoginDate))
        {
            // Берем записанную дату и отнимаем от нее 1 день
            PlayerPrefs.SetString("Player_LastLoginDate", lastLoginDate.AddDays(-1).Date.ToString());
        }
        else
        {
            // Если игрок еще ни разу не заходил (пусто), ставим "вчера", чтобы сегодня он забрал награду
            PlayerPrefs.SetString("Player_LastLoginDate", DateTime.Now.AddDays(-1).Date.ToString());
        }
        
        PlayerPrefs.Save();
        
        Debug.Log("<color=magenta>[DEBUG] Машина времени: дата последнего входа сдвинута на 1 день назад! (Перезапусти сцену, чтобы DailyStreakManager обновил данные)</color>");
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

    // --- НОВАЯ КНОПКА (ВЫДАТЬ АЛМАЗЫ) ---
    public void OnGiveGemsButton()
    {
        int amount = 100; // По умолчанию выдаем 100, если поле пустое
        if (!string.IsNullOrEmpty(amountInput.text))
        {
            int.TryParse(amountInput.text, out amount);
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGems(amount);
            Debug.Log($"<color=cyan>[DEBUG] Выдано алмазов: {amount}</color>");
        }
        else
        {
            Debug.LogError("[DEBUG] CurrencyManager не найден на сцене!");
        }
    }
}